namespace HeyAlan.Tests;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using HeyAlan.Configuration;
using HeyAlan.Data;
using HeyAlan.Data.Entities;
using HeyAlan.Onboarding;
using HeyAlan.SquareIntegration;

public class SubscriptionSquareConnectionServiceTests
{
    [Fact]
    public async Task StartConnectAsync_WhenOwner_ReturnsAuthorizeUrlWithRequiredScopes()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedOwnerMembershipAsync(dbContext, subscriptionId, userId);

        RecordingOAuthStateProtector stateProtector = new();
        SubscriptionSquareConnectionService service = CreateService(
            dbContext,
            stateProtector,
            new StubSquareOAuthClient(),
            new StubSquareTokenService(),
            new StubSquareMerchantClient());

        StartSquareConnectResult result = await service.StartConnectAsync(
            new StartSquareConnectInput(subscriptionId, userId, "/onboarding/start", SquareConnectIntent.Onboarding));

        StartSquareConnectResult.Success success = Assert.IsType<StartSquareConnectResult.Success>(result);
        Assert.Contains("ITEMS_READ", success.AuthorizeUrl, StringComparison.Ordinal);
        Assert.Contains("CUSTOMERS_READ", success.AuthorizeUrl, StringComparison.Ordinal);
        Assert.Contains("PAYMENTS_WRITE", success.AuthorizeUrl, StringComparison.Ordinal);
        Assert.Contains("state=", success.AuthorizeUrl, StringComparison.Ordinal);
        Assert.NotNull(stateProtector.LastProtectedPayload);
        Assert.Equal(subscriptionId, stateProtector.LastProtectedPayload!.SubscriptionId);
    }

    [Fact]
    public async Task StartConnectAsync_WhenUserIsNotOwner_ReturnsSubscriptionOwnerRequired()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        RecordingOAuthStateProtector stateProtector = new();
        SubscriptionSquareConnectionService service = CreateService(
            dbContext,
            stateProtector,
            new StubSquareOAuthClient(),
            new StubSquareTokenService(),
            new StubSquareMerchantClient());

        StartSquareConnectResult result = await service.StartConnectAsync(
            new StartSquareConnectInput(subscriptionId, userId, "/onboarding", SquareConnectIntent.Onboarding));

        StartSquareConnectResult.Failure failure = Assert.IsType<StartSquareConnectResult.Failure>(result);
        Assert.Equal("subscription_owner_required", failure.ErrorCode);
    }

    [Fact]
    public async Task CompleteConnectAsync_WhenSuccessful_StoresConnectionAndReturnsSuccessRedirect()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedOwnerMembershipAsync(dbContext, subscriptionId, userId);

        RecordingOAuthStateProtector stateProtector = new()
        {
            PayloadToUnprotect = new SquareConnectStatePayload(
                subscriptionId,
                userId,
                "/onboarding",
                SquareConnectIntent.Onboarding,
                DateTime.UtcNow)
        };

        StubSquareOAuthClient oauthClient = new()
        {
            ExchangeResult = new SquareTokenExchangeResult.Success(
                new SquareTokenExchangePayload(
                    "access-token",
                    "refresh-token",
                    "merchant-1",
                    DateTime.UtcNow.AddMinutes(30),
                    ["ITEMS_READ", "CUSTOMERS_READ", "CUSTOMERS_WRITE", "ORDERS_READ", "ORDERS_WRITE", "PAYMENTS_WRITE"]))
        };

        RecordingSquareTokenService tokenService = new();
        SubscriptionSquareConnectionService service = CreateService(
            dbContext,
            stateProtector,
            oauthClient,
            tokenService,
            new StubSquareMerchantClient());

        CompleteSquareConnectResult result = await service.CompleteConnectAsync(
            new CompleteSquareConnectInput("valid-state", "auth-code", null));

        CompleteSquareConnectResult.Success success = Assert.IsType<CompleteSquareConnectResult.Success>(result);
        Assert.Contains("squareConnect=success", success.RedirectUrl, StringComparison.Ordinal);
        Assert.NotNull(tokenService.LastStoredInput);
        Assert.Equal(subscriptionId, tokenService.LastStoredInput!.SubscriptionId);
        Assert.Equal(userId, tokenService.LastStoredInput.ConnectedByUserId);
    }

    [Fact]
    public async Task CompleteConnectAsync_WhenCallbackAccessDenied_ReturnsDeterministicFailureCode()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedOwnerMembershipAsync(dbContext, subscriptionId, userId);

        RecordingOAuthStateProtector stateProtector = new()
        {
            PayloadToUnprotect = new SquareConnectStatePayload(
                subscriptionId,
                userId,
                "/onboarding",
                SquareConnectIntent.Onboarding,
                DateTime.UtcNow)
        };

        SubscriptionSquareConnectionService service = CreateService(
            dbContext,
            stateProtector,
            new StubSquareOAuthClient(),
            new StubSquareTokenService(),
            new StubSquareMerchantClient());

        CompleteSquareConnectResult result = await service.CompleteConnectAsync(
            new CompleteSquareConnectInput("valid-state", null, "access_denied"));

        CompleteSquareConnectResult.Failure failure = Assert.IsType<CompleteSquareConnectResult.Failure>(result);
        Assert.Equal("square_oauth_access_denied", failure.ErrorCode);
        Assert.Contains("squareConnectError=square_oauth_access_denied", failure.RedirectUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisconnectAsync_WhenRevokeSucceeds_RemovesConnection()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedOwnerMembershipAsync(dbContext, subscriptionId, userId);

        dbContext.SubscriptionSquareConnections.Add(new SubscriptionSquareConnection
        {
            SubscriptionId = subscriptionId,
            ConnectedByUserId = userId,
            SquareMerchantId = "merchant-1",
            EncryptedAccessToken = "enc-access",
            EncryptedRefreshToken = "enc-refresh",
            AccessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(20),
            Scopes = "ITEMS_READ",
        });
        await dbContext.SaveChangesAsync();

        StubSquareOAuthClient oauthClient = new()
        {
            RevokeResult = new SquareRevokeResult.Success()
        };
        StubSquareTokenService tokenService = new()
        {
            TokenResolution = new SquareTokenResolution.Success("access-token", DateTime.UtcNow.AddMinutes(20))
        };

        SubscriptionSquareConnectionService service = CreateService(
            dbContext,
            new RecordingOAuthStateProtector(),
            oauthClient,
            tokenService,
            new StubSquareMerchantClient());

        DisconnectSquareConnectionResult result = await service.DisconnectAsync(
            new DisconnectSquareConnectionInput(subscriptionId, userId));

        Assert.IsType<DisconnectSquareConnectionResult.Success>(result);
        SubscriptionSquareConnection? persisted = await dbContext.SubscriptionSquareConnections
            .SingleOrDefaultAsync(item => item.SubscriptionId == subscriptionId);
        Assert.Null(persisted);
    }

    [Fact]
    public async Task ProbeAsync_WhenTokenReconnectRequired_ReturnsReconnectRequiredCode()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedOwnerMembershipAsync(dbContext, subscriptionId, userId);

        StubSquareTokenService tokenService = new()
        {
            TokenResolution = new SquareTokenResolution.ReconnectRequired("refresh_invalid_or_revoked")
        };

        SubscriptionSquareConnectionService service = CreateService(
            dbContext,
            new RecordingOAuthStateProtector(),
            new StubSquareOAuthClient(),
            tokenService,
            new StubSquareMerchantClient());

        ProbeSquareConnectionResult result = await service.ProbeAsync(
            new ProbeSquareConnectionInput(subscriptionId, userId));

        ProbeSquareConnectionResult.Failure failure = Assert.IsType<ProbeSquareConnectionResult.Failure>(result);
        Assert.Equal("square_reconnect_required", failure.ErrorCode);
    }

    [Fact]
    public async Task StartConnectAsync_WhenConnectionSquareCredentialsMissing_ReturnsSquareNotConfigured()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedOwnerMembershipAsync(dbContext, subscriptionId, userId);

        AppOptions appOptions = new()
        {
            PublicBaseUrl = new Uri("https://heyalan.test"),
            AuthSquareClientId = "sandbox-auth-client-id",
            AuthSquareClientSecret = "auth-square-client-secret",
            SquareClientId = null,
            SquareClientSecret = null
        };

        SubscriptionSquareConnectionService service = CreateService(
            dbContext,
            new RecordingOAuthStateProtector(),
            new StubSquareOAuthClient(),
            new StubSquareTokenService(),
            new StubSquareMerchantClient(),
            appOptions);

        StartSquareConnectResult result = await service.StartConnectAsync(
            new StartSquareConnectInput(subscriptionId, userId, "/onboarding", SquareConnectIntent.Onboarding));

        StartSquareConnectResult.Failure failure = Assert.IsType<StartSquareConnectResult.Failure>(result);
        Assert.Equal("square_not_configured", failure.ErrorCode);
    }

    private static SubscriptionSquareConnectionService CreateService(
        MainDataContext dbContext,
        IOAuthStateProtector stateProtector,
        ISquareOAuthClient squareOAuthClient,
        ISquareTokenService squareTokenService,
        ISquareMerchantClient squareMerchantClient,
        AppOptions? appOptions = null)
    {
        AppOptions resolvedAppOptions = appOptions ?? new AppOptions
        {
            PublicBaseUrl = new Uri("https://heyalan.test"),
            SquareClientId = "sandbox-client-id",
            SquareClientSecret = "square-client-secret"
        };

        return new SubscriptionSquareConnectionService(
            dbContext,
            resolvedAppOptions,
            stateProtector,
            squareOAuthClient,
            squareTokenService,
            squareMerchantClient,
            new StubSubscriptionOnboardingService(),
            NullLogger<SubscriptionSquareConnectionService>.Instance);
    }

    private static MainDataContext CreateContext()
    {
        DbContextOptions<MainDataContext> options = new DbContextOptionsBuilder<MainDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new MainDataContext(options);
    }

    private static async Task SeedOwnerMembershipAsync(MainDataContext dbContext, Guid subscriptionId, Guid userId)
    {
        Subscription subscription = new()
        {
            Id = subscriptionId
        };

        ApplicationUser user = new()
        {
            Id = userId,
            DisplayName = "Owner Test User",
            UserName = "owner@example.com",
            Email = "owner@example.com"
        };

        dbContext.Subscriptions.Add(subscription);
        dbContext.Users.Add(user);
        dbContext.SubscriptionUsers.Add(new SubscriptionUser
        {
            SubscriptionId = subscriptionId,
            UserId = userId,
            Role = SubscriptionUserRole.Owner
        });

        await dbContext.SaveChangesAsync();
    }

    private sealed class RecordingOAuthStateProtector : IOAuthStateProtector
    {
        public SquareConnectStatePayload? LastProtectedPayload { get; private set; }

        public SquareConnectStatePayload? PayloadToUnprotect { get; init; }

        public string Protect(SquareConnectStatePayload payload)
        {
            this.LastProtectedPayload = payload;
            return "state-token";
        }

        public bool TryUnprotect(string protectedState, out SquareConnectStatePayload? payload)
        {
            payload = this.PayloadToUnprotect;
            return payload is not null;
        }
    }

    private sealed class StubSquareOAuthClient : ISquareOAuthClient
    {
        public SquareTokenExchangeResult ExchangeResult { get; init; } =
            new SquareTokenExchangeResult.Failure("square_token_exchange_failed");

        public SquareRevokeResult RevokeResult { get; init; } =
            new SquareRevokeResult.Success();

        public Task<SquareTokenExchangeResult> ExchangeAuthorizationCodeAsync(
            string authorizationCode,
            string redirectUri,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.ExchangeResult);
        }

        public Task<SquareRevokeResult> RevokeAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.RevokeResult);
        }
    }

    private sealed class StubSquareMerchantClient : ISquareMerchantClient
    {
        public SquareMerchantProfileResult Result { get; init; } =
            new SquareMerchantProfileResult.Success(new SquareMerchantProfile("merchant-1"));

        public Task<SquareMerchantProfileResult> GetMerchantProfileAsync(string accessToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.Result);
        }
    }

    private sealed class StubSquareTokenService : ISquareTokenService
    {
        public SquareTokenResolution TokenResolution { get; init; } =
            new SquareTokenResolution.Success("access-token", DateTime.UtcNow.AddMinutes(20));

        public Task StoreConnectionAsync(SquareTokenStoreInput input, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<SquareTokenResolution> GetValidAccessTokenAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.TokenResolution);
        }
    }

    private sealed class RecordingSquareTokenService : ISquareTokenService
    {
        public SquareTokenStoreInput? LastStoredInput { get; private set; }

        public Task StoreConnectionAsync(SquareTokenStoreInput input, CancellationToken cancellationToken = default)
        {
            this.LastStoredInput = input;
            return Task.CompletedTask;
        }

        public Task<SquareTokenResolution> GetValidAccessTokenAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<SquareTokenResolution>(
                new SquareTokenResolution.Success("access-token", DateTime.UtcNow.AddMinutes(20)));
        }
    }

    private sealed class StubSubscriptionOnboardingService : ISubscriptionOnboardingService
    {
        public Task<GetSubscriptionOnboardingStateResult> GetStateAsync(
            Guid subscriptionId,
            Guid userId,
            bool resumeMode = false,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<GetSubscriptionOnboardingStateResult>(
                new GetSubscriptionOnboardingStateResult.Success(new OnboardingStateResult(
                    "Draft",
                    "square_connect",
                    [new OnboardingStepState("square_connect", "in_progress", true, [])],
                    null,
                    false,
                    new OnboardingProfilePrefill(null, null),
                    new OnboardingChannelsPrefill(null, null, false))));
        }

        public Task<CreateSubscriptionOnboardingAgentResult> CreatePrimaryAgentAsync(
            Guid subscriptionId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<CreateSubscriptionOnboardingAgentResult>(
                new CreateSubscriptionOnboardingAgentResult.Failure("not_implemented"));
        }

        public Task<UpdateSubscriptionOnboardingStepResult> UpdateProfileAsync(
            UpdateSubscriptionOnboardingProfileInput input,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<UpdateSubscriptionOnboardingStepResult>(
                new UpdateSubscriptionOnboardingStepResult.Failure("not_implemented"));
        }

        public Task<UpdateSubscriptionOnboardingStepResult> UpdateChannelsAsync(
            UpdateSubscriptionOnboardingChannelsInput input,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<UpdateSubscriptionOnboardingStepResult>(
                new UpdateSubscriptionOnboardingStepResult.Failure("not_implemented"));
        }

        public Task<UpdateSubscriptionOnboardingStepResult> CompleteInvitationsAsync(
            Guid subscriptionId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<UpdateSubscriptionOnboardingStepResult>(
                new UpdateSubscriptionOnboardingStepResult.Failure("not_implemented"));
        }

        public Task<UpdateSubscriptionOnboardingStepResult> FinalizeAsync(
            Guid subscriptionId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<UpdateSubscriptionOnboardingStepResult>(
                new UpdateSubscriptionOnboardingStepResult.Failure("not_implemented"));
        }

        public Task<UpdateSubscriptionOnboardingStepResult> SkipStepAsync(
            Guid subscriptionId,
            Guid userId,
            string step,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<UpdateSubscriptionOnboardingStepResult>(
                new UpdateSubscriptionOnboardingStepResult.Failure("not_implemented"));
        }

        public Task<OnboardingStateResult> RecomputeStateAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OnboardingStateResult(
                "Draft",
                "square_connect",
                [new OnboardingStepState("square_connect", "in_progress", true, [])],
                null,
                false,
                new OnboardingProfilePrefill(null, null),
                new OnboardingChannelsPrefill(null, null, false)));
        }
    }
}
