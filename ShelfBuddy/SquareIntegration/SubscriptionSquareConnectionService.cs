namespace ShelfBuddy.SquareIntegration;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShelfBuddy.Configuration;
using ShelfBuddy.Data;
using ShelfBuddy.Data.Entities;
using ShelfBuddy.Onboarding;

public sealed class SubscriptionSquareConnectionService : ISubscriptionSquareConnectionService
{
    private static readonly string[] RequiredFullScopes =
    [
        "ITEMS_READ",
        "CUSTOMERS_READ",
        "CUSTOMERS_WRITE",
        "ORDERS_READ",
        "ORDERS_WRITE",
        "PAYMENTS_WRITE"
    ];

    private readonly MainDataContext dbContext;
    private readonly AppOptions appOptions;
    private readonly IOAuthStateProtector stateProtector;
    private readonly ISquareOAuthClient squareOAuthClient;
    private readonly ISquareTokenService squareTokenService;
    private readonly ISquareMerchantClient squareMerchantClient;
    private readonly ISubscriptionOnboardingService subscriptionOnboardingService;
    private readonly ILogger<SubscriptionSquareConnectionService> logger;

    public SubscriptionSquareConnectionService(
        MainDataContext dbContext,
        AppOptions appOptions,
        IOAuthStateProtector stateProtector,
        ISquareOAuthClient squareOAuthClient,
        ISquareTokenService squareTokenService,
        ISquareMerchantClient squareMerchantClient,
        ISubscriptionOnboardingService subscriptionOnboardingService,
        ILogger<SubscriptionSquareConnectionService> logger)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.appOptions = appOptions ?? throw new ArgumentNullException(nameof(appOptions));
        this.stateProtector = stateProtector ?? throw new ArgumentNullException(nameof(stateProtector));
        this.squareOAuthClient = squareOAuthClient ?? throw new ArgumentNullException(nameof(squareOAuthClient));
        this.squareTokenService = squareTokenService ?? throw new ArgumentNullException(nameof(squareTokenService));
        this.squareMerchantClient = squareMerchantClient ?? throw new ArgumentNullException(nameof(squareMerchantClient));
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

        if (string.IsNullOrWhiteSpace(this.appOptions.SquareClientId) || string.IsNullOrWhiteSpace(this.appOptions.SquareClientSecret))
        {
            return new StartSquareConnectResult.Failure("square_not_configured");
        }

        string safeReturnUrl = NormalizeReturnUrl(input.ReturnUrl, input.Intent);
        string callbackUrl = BuildAbsoluteCallbackUrl(this.appOptions.PublicBaseUrl, "/onboarding/square/connect/callback");
        SquareConnectStatePayload payload = new(
            input.SubscriptionId,
            input.UserId,
            safeReturnUrl,
            input.Intent,
            DateTime.UtcNow);

        string protectedState = this.stateProtector.Protect(payload);
        string oauthBaseUrl = this.appOptions.SquareClientId.StartsWith("sandbox-", StringComparison.OrdinalIgnoreCase)
            ? "https://connect.squareupsandbox.com/oauth2/authorize"
            : "https://connect.squareup.com/oauth2/authorize";

        Dictionary<string, string?> parameters = new()
        {
            ["client_id"] = this.appOptions.SquareClientId,
            ["scope"] = string.Join(' ', RequiredFullScopes),
            ["state"] = protectedState,
            ["redirect_uri"] = callbackUrl,
            ["response_type"] = "code"
        };

        if (!this.appOptions.SquareClientId.StartsWith("sandbox-", StringComparison.OrdinalIgnoreCase))
        {
            parameters["session"] = "false";
        }

        string authorizeUrl = QueryHelpers.AddQueryString(oauthBaseUrl, parameters);
        return new StartSquareConnectResult.Success(authorizeUrl);
    }

    public async Task<CompleteSquareConnectResult> CompleteConnectAsync(
        CompleteSquareConnectInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.State) || !this.stateProtector.TryUnprotect(input.State, out SquareConnectStatePayload? state))
        {
            string redirectUrl = AddQuery("/onboarding", "squareConnectError", "square_oauth_state_invalid");
            return new CompleteSquareConnectResult.Failure(redirectUrl, "square_oauth_state_invalid");
        }

        if (!await this.IsSubscriptionOwnerAsync(state.SubscriptionId, state.UserId, cancellationToken))
        {
            string redirectUrl = AddQuery(state.ReturnUrl, "squareConnectError", "subscription_owner_required");
            return new CompleteSquareConnectResult.Failure(redirectUrl, "subscription_owner_required");
        }

        if (string.IsNullOrWhiteSpace(input.AuthorizationCode))
        {
            string redirectUrl = AddQuery(state.ReturnUrl, "squareConnectError", "square_oauth_code_missing");
            return new CompleteSquareConnectResult.Failure(redirectUrl, "square_oauth_code_missing");
        }

        string callbackUrl = BuildAbsoluteCallbackUrl(this.appOptions.PublicBaseUrl, "/onboarding/square/connect/callback");
        SquareTokenExchangeResult tokenExchange = await this.squareOAuthClient.ExchangeAuthorizationCodeAsync(
            input.AuthorizationCode.Trim(),
            callbackUrl,
            cancellationToken);

        if (tokenExchange is SquareTokenExchangeResult.Failure tokenFailure)
        {
            string redirectUrl = AddQuery(state.ReturnUrl, "squareConnectError", tokenFailure.ErrorCode);
            return new CompleteSquareConnectResult.Failure(redirectUrl, tokenFailure.ErrorCode);
        }

        SquareTokenExchangeResult.Success tokenSuccess = (SquareTokenExchangeResult.Success)tokenExchange;
        if (!HasRequiredScopes(tokenSuccess.Payload.Scopes))
        {
            string redirectUrl = AddQuery(state.ReturnUrl, "squareConnectError", "square_required_scopes_missing");
            return new CompleteSquareConnectResult.Failure(redirectUrl, "square_required_scopes_missing");
        }

        try
        {
            await this.squareTokenService.StoreConnectionAsync(new SquareTokenStoreInput(
                state.SubscriptionId,
                state.UserId,
                tokenSuccess.Payload.MerchantId,
                tokenSuccess.Payload.AccessToken,
                tokenSuccess.Payload.RefreshToken,
                tokenSuccess.Payload.AccessTokenExpiresAtUtc,
                tokenSuccess.Payload.Scopes),
                cancellationToken);
            await this.subscriptionOnboardingService.RecomputeStateAsync(state.SubscriptionId, cancellationToken);

            string successRedirectUrl = AddQuery(state.ReturnUrl, "squareConnect", "success");
            return new CompleteSquareConnectResult.Success(successRedirectUrl);
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(
                exception,
                "Failed storing square token connection for subscription {SubscriptionId}.",
                state.SubscriptionId);
            string redirectUrl = AddQuery(state.ReturnUrl, "squareConnectError", "square_connection_persist_failed");
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

        SquareTokenResolution tokenResolution = await this.squareTokenService.GetValidAccessTokenAsync(
            input.SubscriptionId,
            cancellationToken);

        if (tokenResolution is SquareTokenResolution.RefreshFailed)
        {
            return new DisconnectSquareConnectionResult.Failure("square_revoke_failed");
        }

        if (tokenResolution is SquareTokenResolution.Success success)
        {
            SquareRevokeResult revokeResult = await this.squareOAuthClient.RevokeAccessTokenAsync(
                success.AccessToken,
                cancellationToken);

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

    public async Task<ProbeSquareConnectionResult> ProbeAsync(
        ProbeSquareConnectionInput input,
        CancellationToken cancellationToken = default)
    {
        if (!await this.IsSubscriptionOwnerAsync(input.SubscriptionId, input.UserId, cancellationToken))
        {
            return new ProbeSquareConnectionResult.Failure("subscription_owner_required");
        }

        SquareTokenResolution tokenResolution = await this.squareTokenService.GetValidAccessTokenAsync(
            input.SubscriptionId,
            cancellationToken);

        if (tokenResolution is SquareTokenResolution.ConnectionMissing)
        {
            return new ProbeSquareConnectionResult.Failure("connection_not_found");
        }

        if (tokenResolution is SquareTokenResolution.ReconnectRequired)
        {
            return new ProbeSquareConnectionResult.Failure("square_reconnect_required");
        }

        if (tokenResolution is SquareTokenResolution.RefreshFailed)
        {
            return new ProbeSquareConnectionResult.Failure("square_token_refresh_failed");
        }

        SquareTokenResolution.Success tokenSuccess = (SquareTokenResolution.Success)tokenResolution;
        SquareMerchantProfileResult merchantResult = await this.squareMerchantClient.GetMerchantProfileAsync(
            tokenSuccess.AccessToken,
            cancellationToken);

        if (merchantResult is SquareMerchantProfileResult.ReconnectRequired)
        {
            return new ProbeSquareConnectionResult.Failure("square_reconnect_required");
        }

        if (merchantResult is SquareMerchantProfileResult.Failure merchantFailure)
        {
            return new ProbeSquareConnectionResult.Failure(merchantFailure.ErrorCode);
        }

        SquareMerchantProfileResult.Success merchantSuccess = (SquareMerchantProfileResult.Success)merchantResult;
        SubscriptionSquareConnection? connection = await this.dbContext.SubscriptionSquareConnections
            .SingleOrDefaultAsync(item => item.SubscriptionId == input.SubscriptionId, cancellationToken);

        string[] scopes = connection is null
            ? []
            : connection.Scopes
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new ProbeSquareConnectionResult.Success(
            merchantSuccess.Profile.MerchantId,
            tokenSuccess.AccessTokenExpiresAtUtc,
            scopes);
    }

    private async Task<bool> IsSubscriptionOwnerAsync(Guid subscriptionId, Guid userId, CancellationToken cancellationToken)
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

    private static bool HasRequiredScopes(IEnumerable<string> scopes)
    {
        HashSet<string> grantedScopes = scopes
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope.Trim())
            .ToHashSet(StringComparer.Ordinal);

        return RequiredFullScopes.All(grantedScopes.Contains);
    }

    private static string NormalizeReturnUrl(string? rawReturnUrl, SquareConnectIntent intent)
    {
        string fallback = intent == SquareConnectIntent.Onboarding ? "/onboarding" : "/admin";
        if (string.IsNullOrWhiteSpace(rawReturnUrl))
        {
            return fallback;
        }

        string trimmed = rawReturnUrl.Trim();
        if (!trimmed.StartsWith('/'))
        {
            return fallback;
        }

        if (trimmed.StartsWith("//", StringComparison.Ordinal))
        {
            return fallback;
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out _))
        {
            return fallback;
        }

        return trimmed;
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
}
