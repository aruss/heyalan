namespace HeyAlan.SquareIntegration;

using System.Collections.Concurrent;
using System.Text.Json;
using HeyAlan.Configuration;
using HeyAlan.Data;
using HeyAlan.Data.Entities;
using HeyAlan.Onboarding;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Square;
using Square.OAuth;

public sealed class SquareService : ISquareService
{
    private const string DataProtectionPurpose = "SquareIntegration.TokenStore.v1";
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(5);
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> RefreshLocks = new();

    private readonly MainDataContext dbContext;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly AppOptions appOptions;
    private readonly IDataProtector dataProtector;
    private readonly IOAuthStateProtector stateProtector;
    private readonly ISubscriptionOnboardingService subscriptionOnboardingService;
    private readonly ILogger<SquareService> logger;

    public SquareService(
        MainDataContext dbContext,
        IHttpClientFactory httpClientFactory,
        AppOptions appOptions,
        IDataProtectionProvider dataProtectionProvider,
        IOAuthStateProtector stateProtector,
        ISubscriptionOnboardingService subscriptionOnboardingService,
        ILogger<SquareService> logger)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        this.appOptions = appOptions ?? throw new ArgumentNullException(nameof(appOptions));
        this.dataProtector = (dataProtectionProvider ?? throw new ArgumentNullException(nameof(dataProtectionProvider)))
            .CreateProtector(DataProtectionPurpose);
        this.stateProtector = stateProtector ?? throw new ArgumentNullException(nameof(stateProtector));
        this.subscriptionOnboardingService = subscriptionOnboardingService ?? throw new ArgumentNullException(nameof(subscriptionOnboardingService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<StartSquareConnectResult> StartConnectAsync(
        StartSquareConnectInput input,
        CancellationToken cancellationToken = default)
    {
        if (!await this.IsSubscriptionOwnerAsync(input.SubscriptionId, input.UserId, cancellationToken))
        {
            return new StartSquareConnectResult.Failure("subscription_owner_required");
        }

        if (String.IsNullOrWhiteSpace(this.appOptions.SquareClientId) || String.IsNullOrWhiteSpace(this.appOptions.SquareClientSecret))
        {
            return new StartSquareConnectResult.Failure("square_not_configured");
        }

        if (!TryNormalizeReturnUrl(input.ReturnUrl, out string safeReturnUrl))
        {
            return new StartSquareConnectResult.Failure("return_url_required");
        }

        string callbackUrl = BuildAbsoluteCallbackUrl(this.appOptions.PublicBaseUrl, SquareIntegrationRules.ConnectCallbackPath);
        SquareConnectStatePayload payload = new(input.SubscriptionId, input.UserId, safeReturnUrl, DateTime.UtcNow);
        string protectedState = this.stateProtector.Protect(payload);
        string authorizeBaseUrl = SquareIntegrationRules.ResolveAuthorizeUrl(this.appOptions.SquareClientId);

        Dictionary<string, string?> parameters = new()
        {
            ["client_id"] = this.appOptions.SquareClientId,
            ["scope"] = String.Join(' ', SquareIntegrationRules.GetRequiredScopes()),
            ["state"] = protectedState,
            ["redirect_uri"] = callbackUrl,
            ["response_type"] = "code"
        };

        if (!SquareIntegrationRules.IsSandboxClientId(this.appOptions.SquareClientId))
        {
            parameters["session"] = "false";
        }

        string authorizeUrl = QueryHelpers.AddQueryString(authorizeBaseUrl, parameters);
        return new StartSquareConnectResult.Success(authorizeUrl);
    }

    public async Task<CompleteSquareConnectResult> CompleteConnectAsync(
        CompleteSquareConnectInput input,
        CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrWhiteSpace(input.State) || !this.stateProtector.TryUnprotect(input.State, out SquareConnectStatePayload? state))
        {
            string redirectUrl = AddQuery("/onboarding", "squareConnectError", "square_oauth_state_invalid");
            return new CompleteSquareConnectResult.Failure(redirectUrl, "square_oauth_state_invalid");
        }

        SquareConnectStatePayload statePayload = state!;

        if (!await this.IsSubscriptionOwnerAsync(statePayload.SubscriptionId, statePayload.UserId, cancellationToken))
        {
            string redirectUrl = AddQuery(statePayload.ReturnUrl, "squareConnectError", "subscription_owner_required");
            return new CompleteSquareConnectResult.Failure(redirectUrl, "subscription_owner_required");
        }

        if (String.IsNullOrWhiteSpace(input.AuthorizationCode))
        {
                string? oauthError = NormalizeOAuthError(input.OAuthError);
                if (!String.IsNullOrWhiteSpace(oauthError))
                {
                    string errorCode = ResolveOAuthErrorCode(oauthError);
                    string oauthErrorRedirectUrl = AddQuery(statePayload.ReturnUrl, "squareConnectError", errorCode);
                    return new CompleteSquareConnectResult.Failure(oauthErrorRedirectUrl, errorCode);
                }
            }

        if (String.IsNullOrWhiteSpace(input.AuthorizationCode))
        {
            string redirectUrl = AddQuery(statePayload.ReturnUrl, "squareConnectError", "square_oauth_code_missing");
            return new CompleteSquareConnectResult.Failure(redirectUrl, "square_oauth_code_missing");
        }

        string callbackUrl = BuildAbsoluteCallbackUrl(this.appOptions.PublicBaseUrl, SquareIntegrationRules.ConnectCallbackPath);
        SquareTokenExchangeResult tokenExchange = await this.ExchangeAuthorizationCodeAsync(
            input.AuthorizationCode.Trim(),
            callbackUrl,
            cancellationToken);

        if (tokenExchange is SquareTokenExchangeResult.Failure tokenFailure)
        {
            string redirectUrl = AddQuery(statePayload.ReturnUrl, "squareConnectError", tokenFailure.ErrorCode);
            return new CompleteSquareConnectResult.Failure(redirectUrl, tokenFailure.ErrorCode);
        }

        SquareTokenExchangeResult.Success tokenSuccess = (SquareTokenExchangeResult.Success)tokenExchange;
        if (!SquareIntegrationRules.HasRequiredScopes(tokenSuccess.Payload.Scopes))
        {
            string redirectUrl = AddQuery(statePayload.ReturnUrl, "squareConnectError", "square_required_scopes_missing");
            return new CompleteSquareConnectResult.Failure(redirectUrl, "square_required_scopes_missing");
        }

        try
        {
            await this.StoreConnectionAsync(new SquareTokenStoreInput(
                statePayload.SubscriptionId,
                statePayload.UserId,
                tokenSuccess.Payload.MerchantId,
                tokenSuccess.Payload.AccessToken,
                tokenSuccess.Payload.RefreshToken,
                tokenSuccess.Payload.AccessTokenExpiresAtUtc,
                tokenSuccess.Payload.Scopes), cancellationToken);

            await this.subscriptionOnboardingService.RecomputeStateAsync(statePayload.SubscriptionId, cancellationToken);
            string successRedirectUrl = AddQuery(statePayload.ReturnUrl, "squareConnect", "success");
            return new CompleteSquareConnectResult.Success(successRedirectUrl);
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(
                exception,
                "Failed storing square token connection for subscription {SubscriptionId}.",
                statePayload.SubscriptionId);
            string redirectUrl = AddQuery(statePayload.ReturnUrl, "squareConnectError", "square_connection_persist_failed");
            return new CompleteSquareConnectResult.Failure(redirectUrl, "square_connection_persist_failed");
        }
    }

    public async Task<DisconnectSquareConnectionResult> DisconnectAsync(
        DisconnectSquareConnectionInput input,
        CancellationToken cancellationToken = default)
    {
        if (!await this.IsSubscriptionOwnerAsync(input.SubscriptionId, input.UserId, cancellationToken))
        {
            return new DisconnectSquareConnectionResult.Failure("subscription_owner_required");
        }

        SubscriptionSquareConnection? connection = await this.dbContext.SubscriptionSquareConnections
            .SingleOrDefaultAsync(item => item.SubscriptionId == input.SubscriptionId, cancellationToken);
        if (connection is null)
        {
            return new DisconnectSquareConnectionResult.Failure("connection_not_found");
        }

        SquareTokenResolution tokenResolution = await this.GetValidAccessTokenAsync(input.SubscriptionId, cancellationToken);
        if (tokenResolution is SquareTokenResolution.RefreshFailed)
        {
            return new DisconnectSquareConnectionResult.Failure("square_revoke_failed");
        }

        if (tokenResolution is SquareTokenResolution.Success success)
        {
            SquareRevokeResult revokeResult = await this.RevokeAccessTokenAsync(success.AccessToken, cancellationToken);
            if (revokeResult is SquareRevokeResult.Failure revokeFailure)
            {
                return new DisconnectSquareConnectionResult.Failure(revokeFailure.ErrorCode);
            }
        }

        this.dbContext.SubscriptionSquareConnections.Remove(connection);
        await this.dbContext.SaveChangesAsync(cancellationToken);
        await this.subscriptionOnboardingService.RecomputeStateAsync(input.SubscriptionId, cancellationToken);
        return new DisconnectSquareConnectionResult.Success();
    }

    public async Task StoreConnectionAsync(
        SquareTokenStoreInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.SubscriptionId == Guid.Empty)
        {
            throw new ArgumentException("SubscriptionId is required.", nameof(input));
        }

        if (input.ConnectedByUserId == Guid.Empty)
        {
            throw new ArgumentException("ConnectedByUserId is required.", nameof(input));
        }

        if (String.IsNullOrWhiteSpace(input.MerchantId))
        {
            throw new ArgumentException("MerchantId is required.", nameof(input));
        }

        if (String.IsNullOrWhiteSpace(input.AccessToken))
        {
            throw new ArgumentException("AccessToken is required.", nameof(input));
        }

        if (String.IsNullOrWhiteSpace(input.RefreshToken))
        {
            throw new ArgumentException("RefreshToken is required.", nameof(input));
        }

        if (input.AccessTokenExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new ArgumentException("AccessTokenExpiresAtUtc must be in the future.", nameof(input));
        }

        SubscriptionSquareConnection? existingConnection = await this.dbContext.SubscriptionSquareConnections
            .SingleOrDefaultAsync(connection => connection.SubscriptionId == input.SubscriptionId, cancellationToken);

        string normalizedScopes = SquareIntegrationRules.NormalizeScopesForStorage(input.Scopes);
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

    public async Task<SquareTokenResolution> GetValidAccessTokenAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
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
            if (String.IsNullOrWhiteSpace(payload.AccessToken))
            {
                return new SquareTokenResolution.RefreshFailed("refresh_response_missing_access_token");
            }

            if (payload.AccessTokenExpiresAtUtc <= DateTime.UtcNow)
            {
                return new SquareTokenResolution.RefreshFailed("refresh_response_expired_token");
            }

            string rotatedRefreshToken = !String.IsNullOrWhiteSpace(payload.RefreshToken)
                ? payload.RefreshToken
                : refreshToken;

            lockedConnection.EncryptedAccessToken = this.dataProtector.Protect(payload.AccessToken);
            lockedConnection.EncryptedRefreshToken = this.dataProtector.Protect(rotatedRefreshToken);
            lockedConnection.AccessTokenExpiresAtUtc = payload.AccessTokenExpiresAtUtc;
            if (payload.Scopes.Count > 0)
            {
                lockedConnection.Scopes = SquareIntegrationRules.NormalizeScopesForStorage(payload.Scopes);
            }

            lockedConnection.DisconnectedAtUtc = null;

            if (!String.IsNullOrWhiteSpace(payload.MerchantId))
            {
                lockedConnection.SquareMerchantId = payload.MerchantId.Trim();
            }

            await this.dbContext.SaveChangesAsync(cancellationToken);
            this.logger.LogInformation("Square token refreshed for subscription {SubscriptionId}.", subscriptionId);
            return new SquareTokenResolution.Success(payload.AccessToken, payload.AccessTokenExpiresAtUtc);
        }
        finally
        {
            refreshLock.Release();
        }
    }

    public async Task<SquareTokenExchangeResult> ExchangeAuthorizationCodeAsync(
        string authorizationCode,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrWhiteSpace(authorizationCode))
        {
            return new SquareTokenExchangeResult.Failure("square_oauth_code_missing");
        }

        if (String.IsNullOrWhiteSpace(this.appOptions.SquareClientId) || String.IsNullOrWhiteSpace(this.appOptions.SquareClientSecret))
        {
            return new SquareTokenExchangeResult.Failure("square_not_configured");
        }

        SquareClient client = this.CreateAppClient();
        ObtainTokenRequest request = new()
        {
            ClientId = this.appOptions.SquareClientId,
            ClientSecret = this.appOptions.SquareClientSecret,
            Code = authorizationCode,
            RedirectUri = redirectUri,
            GrantType = "authorization_code"
        };

        try
        {
            ObtainTokenResponse response = await client.OAuth.ObtainTokenAsync(request, null, cancellationToken);
            string[] resolvedScopes = await this.ResolveScopesWithTokenStatusFallbackAsync(response, cancellationToken);
            SquareTokenExchangePayload? payload = ToTokenExchangePayload(response, resolvedScopes);
            if (payload is null)
            {
                return new SquareTokenExchangeResult.Failure("square_token_exchange_failed");
            }

            return new SquareTokenExchangeResult.Success(payload);
        }
        catch (SquareApiException)
        {
            return new SquareTokenExchangeResult.Failure("square_token_exchange_failed");
        }
    }

    public async Task<SquareRevokeResult> RevokeAccessTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrWhiteSpace(accessToken))
        {
            return new SquareRevokeResult.InvalidOrRevoked();
        }

        if (String.IsNullOrWhiteSpace(this.appOptions.SquareClientId) || String.IsNullOrWhiteSpace(this.appOptions.SquareClientSecret))
        {
            return new SquareRevokeResult.Failure("square_not_configured");
        }

        SquareClient client = this.CreateAppClient();
        RevokeTokenRequest request = new()
        {
            AccessToken = accessToken,
            ClientId = this.appOptions.SquareClientId
        };

        RequestOptions requestOptions = new()
        {
            AdditionalHeaders = new Dictionary<string, string?>
            {
                ["Authorization"] = $"Client {this.appOptions.SquareClientSecret}"
            }
        };

        try
        {
            RevokeTokenResponse response = await client.OAuth.RevokeTokenAsync(request, requestOptions, cancellationToken);

            if (response.Success == true)
            {
                return new SquareRevokeResult.Success();
            }

            if (HasErrorCode(response.Errors, "ACCESS_TOKEN_REVOKED") ||
                HasErrorCode(response.Errors, "UNAUTHORIZED") ||
                HasErrorCode(response.Errors, "invalid_grant"))
            {
                return new SquareRevokeResult.InvalidOrRevoked();
            }

            if (response.Errors is null || !response.Errors.Any())
            {
                return new SquareRevokeResult.InvalidOrRevoked();
            }

            return new SquareRevokeResult.Failure("square_revoke_failed");
        }
        catch (SquareApiException exception)
        {
            if (exception.StatusCode is 400 or 401 ||
                HasErrorCode(exception.Errors, "ACCESS_TOKEN_REVOKED") ||
                HasErrorCode(exception.Errors, "UNAUTHORIZED") ||
                HasErrorCode(exception.Errors, "invalid_grant"))
            {
                return new SquareRevokeResult.InvalidOrRevoked();
            }

            return new SquareRevokeResult.Failure("square_revoke_failed");
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
            if (String.IsNullOrWhiteSpace(decryptedAccessToken))
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

    private async Task<RefreshTokenApiOutcome> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        if (String.IsNullOrWhiteSpace(this.appOptions.SquareClientId) || String.IsNullOrWhiteSpace(this.appOptions.SquareClientSecret))
        {
            return new RefreshTokenApiOutcome(RefreshTokenApiResultType.ReconnectRequired, "square_not_configured");
        }

        SquareClient client = this.CreateAppClient();
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

    private SquareClient CreateAppClient()
    {
        string baseUrl = SquareIntegrationRules.ResolveOAuthBaseUrl(this.appOptions.SquareClientId!);
        ClientOptions options = new()
        {
            BaseUrl = baseUrl,
            HttpClient = this.httpClientFactory.CreateClient("SquareOAuthClient")
        };

        return new SquareClient(this.appOptions.SquareClientSecret!, clientOptions: options);
    }

    private SquareClient CreateAccessTokenClient(string accessToken)
    {
        string baseUrl = SquareIntegrationRules.ResolveOAuthBaseUrl(this.appOptions.SquareClientId!);
        ClientOptions options = new()
        {
            BaseUrl = baseUrl,
            HttpClient = this.httpClientFactory.CreateClient("SquareOAuthClient")
        };

        return new SquareClient(accessToken, clientOptions: options);
    }

    private async Task<string[]> ResolveScopesWithTokenStatusFallbackAsync(
        ObtainTokenResponse response,
        CancellationToken cancellationToken)
    {
        string[] scopesFromExchange = ResolveScopes(response);
        if (scopesFromExchange.Length > 0)
        {
            return scopesFromExchange;
        }

        if (String.IsNullOrWhiteSpace(response.AccessToken))
        {
            return [];
        }

        SquareClient accessTokenClient = this.CreateAccessTokenClient(response.AccessToken);
        try
        {
            RetrieveTokenStatusResponse tokenStatus = await accessTokenClient.OAuth.RetrieveTokenStatusAsync(null, cancellationToken);
            return SquareIntegrationRules.NormalizeScopes(tokenStatus.Scopes);
        }
        catch (SquareApiException)
        {
            return [];
        }
    }

    private static SquareTokenExchangePayload? ToTokenExchangePayload(
        ObtainTokenResponse response,
        string[] scopes)
    {
        if (String.IsNullOrWhiteSpace(response.AccessToken) ||
            String.IsNullOrWhiteSpace(response.RefreshToken) ||
            String.IsNullOrWhiteSpace(response.MerchantId))
        {
            return null;
        }

        DateTime expiresAtUtc = SquareIntegrationRules.ResolveAccessTokenExpiry(response.ExpiresAt);
        if (expiresAtUtc <= DateTime.UtcNow)
        {
            return null;
        }

        return new SquareTokenExchangePayload(
            response.AccessToken,
            response.RefreshToken,
            response.MerchantId,
            expiresAtUtc,
            scopes);
    }

    private static RefreshTokenApiPayload? ToRefreshPayload(ObtainTokenResponse response)
    {
        if (String.IsNullOrWhiteSpace(response.AccessToken))
        {
            return null;
        }

        DateTime accessTokenExpiresAtUtc = SquareIntegrationRules.ResolveAccessTokenExpiry(response.ExpiresAt);
        IReadOnlyCollection<string> scopes = ResolveScopes(response);

        return new RefreshTokenApiPayload(
            response.AccessToken,
            response.RefreshToken,
            response.MerchantId,
            accessTokenExpiresAtUtc,
            scopes);
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
            IEnumerable<string?> rawScopes = scopesElement
                .EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString());
            IEnumerable<string> filteredScopes = rawScopes
                .Where(item => !String.IsNullOrWhiteSpace(item))
                .Select(item => item!);
            return SquareIntegrationRules.NormalizeScopes(filteredScopes);
        }

        if (response.AdditionalProperties.TryGetValue("scope", out JsonElement scopeElement) &&
            scopeElement.ValueKind == JsonValueKind.String)
        {
            string? scopeRaw = scopeElement.GetString();
            if (!String.IsNullOrWhiteSpace(scopeRaw))
            {
                string[] splitScopes = scopeRaw.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return SquareIntegrationRules.NormalizeScopes(splitScopes);
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
            if (String.Equals(error.Code.ToString(), expectedCode, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> IsSubscriptionOwnerAsync(
        Guid subscriptionId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (subscriptionId == Guid.Empty || userId == Guid.Empty)
        {
            return false;
        }

        bool isOwner = await this.dbContext.SubscriptionUsers
            .AnyAsync(
                membership =>
                    membership.SubscriptionId == subscriptionId &&
                    membership.UserId == userId &&
                    membership.Role == SubscriptionUserRole.Owner,
                cancellationToken);

        return isOwner;
    }

    private static bool TryNormalizeReturnUrl(string rawReturnUrl, out string safeReturnUrl)
    {
        safeReturnUrl = String.Empty;
        if (String.IsNullOrWhiteSpace(rawReturnUrl))
        {
            return false;
        }

        string trimmed = rawReturnUrl.Trim();
        if (!trimmed.StartsWith('/'))
        {
            return false;
        }

        if (trimmed.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out _))
        {
            return false;
        }

        safeReturnUrl = trimmed;
        return true;
    }

    private static string BuildAbsoluteCallbackUrl(Uri publicBaseUrl, string callbackPath)
    {
        Uri callbackUri = new(publicBaseUrl, callbackPath);
        return callbackUri.ToString();
    }

    private static string AddQuery(string path, string key, string value)
    {
        return QueryHelpers.AddQueryString(path, key, value);
    }

    private static string? NormalizeOAuthError(string? rawOAuthError)
    {
        if (String.IsNullOrWhiteSpace(rawOAuthError))
        {
            return null;
        }

        return rawOAuthError.Trim();
    }

    private static string ResolveOAuthErrorCode(string oauthError)
    {
        if (String.Equals(oauthError, "access_denied", StringComparison.OrdinalIgnoreCase))
        {
            return "square_oauth_access_denied";
        }

        return "square_oauth_callback_error";
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
