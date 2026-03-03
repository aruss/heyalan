namespace HeyAlan.SquareIntegration;

using System.Collections.Concurrent;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HeyAlan.Configuration;
using HeyAlan.Data;
using HeyAlan.Data.Entities;
using Square;
using Square.OAuth;
using System.Text.Json;

public sealed class SquareTokenService : ISquareTokenService
{
    private const string DataProtectionPurpose = "SquareIntegration.TokenStore.v1";
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(5);
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> RefreshLocks = new();

    private readonly MainDataContext dbContext;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly AppOptions appOptions;
    private readonly IDataProtector dataProtector;
    private readonly ILogger<SquareTokenService> logger;

    public SquareTokenService(
        MainDataContext dbContext,
        IHttpClientFactory httpClientFactory,
        AppOptions appOptions,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<SquareTokenService> logger)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        this.appOptions = appOptions ?? throw new ArgumentNullException(nameof(appOptions));
        this.dataProtector = (dataProtectionProvider ?? throw new ArgumentNullException(nameof(dataProtectionProvider)))
            .CreateProtector(DataProtectionPurpose);
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StoreConnectionAsync(SquareTokenStoreInput input, CancellationToken cancellationToken = default)
    {
        if (input.SubscriptionId == Guid.Empty)
        {
            throw new ArgumentException("SubscriptionId is required.", nameof(input));
        }

        if (input.ConnectedByUserId == Guid.Empty)
        {
            throw new ArgumentException("ConnectedByUserId is required.", nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input.MerchantId))
        {
            throw new ArgumentException("MerchantId is required.", nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input.AccessToken))
        {
            throw new ArgumentException("AccessToken is required.", nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input.RefreshToken))
        {
            throw new ArgumentException("RefreshToken is required.", nameof(input));
        }

        if (input.AccessTokenExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new ArgumentException("AccessTokenExpiresAtUtc must be in the future.", nameof(input));
        }

        SubscriptionSquareConnection? existingConnection = await this.dbContext.SubscriptionSquareConnections
            .SingleOrDefaultAsync(connection => connection.SubscriptionId == input.SubscriptionId, cancellationToken);

        string normalizedScopes = NormalizeScopes(input.Scopes);
        string encryptedAccessToken = this.dataProtector.Protect(input.AccessToken);
        string encryptedRefreshToken = this.dataProtector.Protect(input.RefreshToken);

        if (existingConnection is null)
        {
            SubscriptionSquareConnection createdConnection = new()
            {
                SubscriptionId = input.SubscriptionId,
                ConnectedByUserId = input.ConnectedByUserId,
                SquareMerchantId = input.MerchantId.Trim(),
                EncryptedAccessToken = encryptedAccessToken,
                EncryptedRefreshToken = encryptedRefreshToken,
                AccessTokenExpiresAtUtc = input.AccessTokenExpiresAtUtc,
                Scopes = normalizedScopes,
                DisconnectedAtUtc = null
            };

            this.dbContext.SubscriptionSquareConnections.Add(createdConnection);
        }
        else
        {
            existingConnection.ConnectedByUserId = input.ConnectedByUserId;
            existingConnection.SquareMerchantId = input.MerchantId.Trim();
            existingConnection.EncryptedAccessToken = encryptedAccessToken;
            existingConnection.EncryptedRefreshToken = encryptedRefreshToken;
            existingConnection.AccessTokenExpiresAtUtc = input.AccessTokenExpiresAtUtc;
            existingConnection.Scopes = normalizedScopes;
            existingConnection.DisconnectedAtUtc = null;
        }

        await this.dbContext.SaveChangesAsync(cancellationToken);
        this.logger.LogInformation(
            "Stored Square token connection for subscription {SubscriptionId} and merchant {MerchantId}.",
            input.SubscriptionId,
            input.MerchantId);
    }

    public async Task<SquareTokenResolution> GetValidAccessTokenAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        SubscriptionSquareConnection? connection = await this.dbContext.SubscriptionSquareConnections
            .SingleOrDefaultAsync(item => item.SubscriptionId == subscriptionId, cancellationToken);

        if (connection is null)
        {
            return new SquareTokenResolution.ConnectionMissing();
        }

        SquareTokenResolution? immediateResolution = this.TryResolveWithoutRefresh(connection);
        if (immediateResolution is not null)
        {
            return immediateResolution;
        }

        SemaphoreSlim refreshLock = RefreshLocks.GetOrAdd(subscriptionId, static _ => new SemaphoreSlim(1, 1));
        await refreshLock.WaitAsync(cancellationToken);
        try
        {
            SubscriptionSquareConnection? lockedConnection = await this.dbContext.SubscriptionSquareConnections
                .SingleOrDefaultAsync(item => item.SubscriptionId == subscriptionId, cancellationToken);

            if (lockedConnection is null)
            {
                return new SquareTokenResolution.ConnectionMissing();
            }

            SquareTokenResolution? lockedImmediateResolution = this.TryResolveWithoutRefresh(lockedConnection);
            if (lockedImmediateResolution is not null)
            {
                return lockedImmediateResolution;
            }

            string refreshToken;
            try
            {
                refreshToken = this.dataProtector.Unprotect(lockedConnection.EncryptedRefreshToken);
            }
            catch (Exception exception)
            {
                this.logger.LogWarning(
                    exception,
                    "Unable to decrypt Square refresh token for subscription {SubscriptionId}. Reconnect required.",
                    subscriptionId);
                return new SquareTokenResolution.ReconnectRequired("token_decrypt_failed");
            }

            RefreshTokenApiOutcome refreshOutcome = await this.RefreshTokenAsync(refreshToken, cancellationToken);
            if (refreshOutcome.ResultType == RefreshTokenApiResultType.ReconnectRequired)
            {
                this.logger.LogWarning(
                    "Square token refresh requires reconnect for subscription {SubscriptionId} with code {ReasonCode}.",
                    subscriptionId,
                    refreshOutcome.ReasonCode);
                return new SquareTokenResolution.ReconnectRequired(refreshOutcome.ReasonCode);
            }

            if (refreshOutcome.ResultType != RefreshTokenApiResultType.Success || refreshOutcome.Payload is null)
            {
                this.logger.LogWarning(
                    "Square token refresh failed for subscription {SubscriptionId} with code {ReasonCode}.",
                    subscriptionId,
                    refreshOutcome.ReasonCode);
                return new SquareTokenResolution.RefreshFailed(refreshOutcome.ReasonCode);
            }

            RefreshTokenApiPayload payload = refreshOutcome.Payload;
            if (string.IsNullOrWhiteSpace(payload.AccessToken))
            {
                return new SquareTokenResolution.RefreshFailed("refresh_response_missing_access_token");
            }

            if (payload.AccessTokenExpiresAtUtc <= DateTime.UtcNow)
            {
                return new SquareTokenResolution.RefreshFailed("refresh_response_expired_token");
            }

            string rotatedRefreshToken = !string.IsNullOrWhiteSpace(payload.RefreshToken)
                ? payload.RefreshToken
                : refreshToken;

            lockedConnection.EncryptedAccessToken = this.dataProtector.Protect(payload.AccessToken);
            lockedConnection.EncryptedRefreshToken = this.dataProtector.Protect(rotatedRefreshToken);
            lockedConnection.AccessTokenExpiresAtUtc = payload.AccessTokenExpiresAtUtc;
            if (payload.Scopes.Count > 0)
            {
                lockedConnection.Scopes = NormalizeScopes(payload.Scopes);
            }
            lockedConnection.DisconnectedAtUtc = null;

            if (!string.IsNullOrWhiteSpace(payload.MerchantId))
            {
                lockedConnection.SquareMerchantId = payload.MerchantId.Trim();
            }

            await this.dbContext.SaveChangesAsync(cancellationToken);
            this.logger.LogInformation(
                "Square token refreshed for subscription {SubscriptionId}.",
                subscriptionId);

            return new SquareTokenResolution.Success(payload.AccessToken, payload.AccessTokenExpiresAtUtc);
        }
        finally
        {
            refreshLock.Release();
        }
    }

    private SquareTokenResolution? TryResolveWithoutRefresh(SubscriptionSquareConnection connection)
    {
        if (connection.AccessTokenExpiresAtUtc <= DateTime.UtcNow + RefreshSkew)
        {
            return null;
        }

        try
        {
            string decryptedAccessToken = this.dataProtector.Unprotect(connection.EncryptedAccessToken);
            if (string.IsNullOrWhiteSpace(decryptedAccessToken))
            {
                return new SquareTokenResolution.ReconnectRequired("token_decrypt_failed");
            }

            return new SquareTokenResolution.Success(decryptedAccessToken, connection.AccessTokenExpiresAtUtc);
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(
                exception,
                "Unable to decrypt Square access token for subscription {SubscriptionId}. Reconnect required.",
                connection.SubscriptionId);
            return new SquareTokenResolution.ReconnectRequired("token_decrypt_failed");
        }
    }

    private async Task<RefreshTokenApiOutcome> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(this.appOptions.SquareClientId) || string.IsNullOrWhiteSpace(this.appOptions.SquareClientSecret))
        {
            return new RefreshTokenApiOutcome(RefreshTokenApiResultType.ReconnectRequired, "square_not_configured");
        }

        string baseUrl = this.appOptions.SquareClientId.StartsWith("sandbox-", StringComparison.OrdinalIgnoreCase)
            ? "https://connect.squareupsandbox.com"
            : "https://connect.squareup.com";

        ClientOptions clientOptions = new()
        {
            BaseUrl = baseUrl,
            HttpClient = this.httpClientFactory.CreateClient("SquareOAuthClient")
        };

        SquareClient client = new(this.appOptions.SquareClientSecret!, clientOptions: clientOptions);
        ObtainTokenRequest request = new()
        {
            ClientId = this.appOptions.SquareClientId,
            ClientSecret = this.appOptions.SquareClientSecret,
            GrantType = "refresh_token",
            RefreshToken = refreshToken
        };

        try
        {
            ObtainTokenResponse response = await client.OAuth.ObtainTokenAsync(request, null, cancellationToken);
            RefreshTokenApiPayload? payload = ToRefreshPayload(response);
            if (payload is null)
            {
                return new RefreshTokenApiOutcome(RefreshTokenApiResultType.Failed, "refresh_response_invalid");
            }

            return new RefreshTokenApiOutcome(RefreshTokenApiResultType.Success, "ok", payload);
        }
        catch (SquareApiException exception)
        {
            bool reconnectRequired = exception.StatusCode is 400 or 401 ||
                                     HasErrorCode(exception.Errors, "invalid_grant") ||
                                     HasErrorCode(exception.Errors, "ACCESS_TOKEN_REVOKED");

            if (reconnectRequired)
            {
                return new RefreshTokenApiOutcome(RefreshTokenApiResultType.ReconnectRequired, "refresh_invalid_or_revoked");
            }

            return new RefreshTokenApiOutcome(RefreshTokenApiResultType.Failed, "refresh_request_failed");
        }
    }

    private static string NormalizeScopes(IEnumerable<string>? scopes)
    {
        if (scopes is null)
        {
            return string.Empty;
        }

        string[] normalizedScopes = scopes
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(scope => scope, StringComparer.Ordinal)
            .ToArray();

        return string.Join(' ', normalizedScopes);
    }

    private static RefreshTokenApiPayload? ToRefreshPayload(ObtainTokenResponse response)
    {
        if (string.IsNullOrWhiteSpace(response.AccessToken))
        {
            return null;
        }

        DateTime accessTokenExpiresAtUtc = ResolveAccessTokenExpiry(response.ExpiresAt);
        IReadOnlyCollection<string> scopes = ResolveScopes(response);

        return new RefreshTokenApiPayload(
            response.AccessToken,
            response.RefreshToken,
            response.MerchantId,
            accessTokenExpiresAtUtc,
            scopes);
    }

    private static DateTime ResolveAccessTokenExpiry(object? expiresAtRaw)
    {
        if (expiresAtRaw is null)
        {
            return DateTime.UtcNow.AddMinutes(-1);
        }

        if (expiresAtRaw is DateTime expiresAtDateTime)
        {
            return expiresAtDateTime.Kind == DateTimeKind.Utc
                ? expiresAtDateTime
                : expiresAtDateTime.ToUniversalTime();
        }

        if (expiresAtRaw is DateTimeOffset expiresAtOffset)
        {
            return expiresAtOffset.UtcDateTime;
        }

        if (DateTimeOffset.TryParse(expiresAtRaw.ToString(), out DateTimeOffset parsedOffset))
        {
            return parsedOffset.UtcDateTime;
        }

        return DateTime.UtcNow.AddMinutes(-1);
    }

    private static string[] ResolveScopes(ObtainTokenResponse response)
    {
        if (response.AdditionalProperties is null || response.AdditionalProperties.Count == 0)
        {
            return [];
        }

        if (response.AdditionalProperties.TryGetValue("scopes", out JsonElement scopesElement) &&
            scopesElement.ValueKind == JsonValueKind.Array)
        {
            return scopesElement
                .EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!.Trim())
                .ToArray();
        }

        if (response.AdditionalProperties.TryGetValue("scope", out JsonElement scopeElement) &&
            scopeElement.ValueKind == JsonValueKind.String)
        {
            string? scopeRaw = scopeElement.GetString();
            if (!string.IsNullOrWhiteSpace(scopeRaw))
            {
                return scopeRaw
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
        }

        return [];
    }

    private static bool HasErrorCode(IEnumerable<Error>? errors, string expectedCode)
    {
        if (errors is null)
        {
            return false;
        }

        foreach (Error error in errors)
        {
            if (string.Equals(error.Code.ToString(), expectedCode, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private enum RefreshTokenApiResultType
    {
        Success,
        ReconnectRequired,
        Failed
    }

    private sealed record RefreshTokenApiPayload(
        string? AccessToken,
        string? RefreshToken,
        string? MerchantId,
        DateTime AccessTokenExpiresAtUtc,
        IReadOnlyCollection<string> Scopes);

    private sealed record RefreshTokenApiOutcome(
        RefreshTokenApiResultType ResultType,
        string ReasonCode,
        RefreshTokenApiPayload? Payload = null);
}
