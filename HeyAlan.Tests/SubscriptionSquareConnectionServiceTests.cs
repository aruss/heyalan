namespace HeyAlan.Tests;

using System.Net;
using System.Text;
using HeyAlan.Configuration;
using HeyAlan.Data;
using HeyAlan.Data.Entities;
using HeyAlan.Onboarding;
using HeyAlan.SquareIntegration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

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
        SquareService service = CreateService(dbContext, stateProtector, new RoutingHandler(_ => JsonResponse("{}")));

        StartSquareConnectResult result = await service.StartConnectAsync(
            new StartSquareConnectInput(subscriptionId, userId, "/onboarding/start"));

        StartSquareConnectResult.Success success = Assert.IsType<StartSquareConnectResult.Success>(result);
        Assert.Contains("ITEMS_READ", success.AuthorizeUrl, StringComparison.Ordinal);
        Assert.Contains("CUSTOMERS_READ", success.AuthorizeUrl, StringComparison.Ordinal);
        Assert.Contains("PAYMENTS_WRITE", success.AuthorizeUrl, StringComparison.Ordinal);
        Assert.Contains("state=", success.AuthorizeUrl, StringComparison.Ordinal);
        Assert.Contains("redirect_uri=", success.AuthorizeUrl, StringComparison.Ordinal);
        Assert.NotNull(stateProtector.LastProtectedPayload);
        Assert.Equal(subscriptionId, stateProtector.LastProtectedPayload!.SubscriptionId);
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
            PayloadToUnprotect = new SquareConnectStatePayload(subscriptionId, userId, "/onboarding", DateTime.UtcNow)
        };

        SquareService service = CreateService(dbContext, stateProtector, new RoutingHandler(_ => JsonResponse("{}")));
        CompleteSquareConnectResult result = await service.CompleteConnectAsync(
            new CompleteSquareConnectInput("valid-state", null, "access_denied"));

        CompleteSquareConnectResult.Failure failure = Assert.IsType<CompleteSquareConnectResult.Failure>(result);
        Assert.Equal("square_oauth_access_denied", failure.ErrorCode);
        Assert.Contains("squareConnectError=square_oauth_access_denied", failure.RedirectUrl, StringComparison.Ordinal);
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
            PayloadToUnprotect = new SquareConnectStatePayload(subscriptionId, userId, "/onboarding", DateTime.UtcNow)
        };

        RoutingHandler handler = new(_ =>
        {
            string payload = """
            {
              "access_token":"access-token",
              "refresh_token":"refresh-token",
              "expires_at":"2099-01-01T00:00:00Z",
              "merchant_id":"merchant-1",
              "scope":"ITEMS_READ CUSTOMERS_READ CUSTOMERS_WRITE ORDERS_READ ORDERS_WRITE PAYMENTS_WRITE"
            }
            """;

            return JsonResponse(payload);
        });

        StubSubscriptionOnboardingService onboardingService = new();
        RecordingSubscriptionCatalogSyncTriggerService triggerService = new();
        SquareService service = CreateService(
            dbContext,
            stateProtector,
            handler,
            onboardingService: onboardingService,
            triggerService: triggerService);

        CompleteSquareConnectResult result = await service.CompleteConnectAsync(
            new CompleteSquareConnectInput("valid-state", "auth-code", null));

        CompleteSquareConnectResult.Success success = Assert.IsType<CompleteSquareConnectResult.Success>(result);
        Assert.Contains("squareConnect=success", success.RedirectUrl, StringComparison.Ordinal);
        Assert.Equal(subscriptionId, onboardingService.LastRecomputeSubscriptionId);
        Assert.NotNull(triggerService.LastRequest);
        Assert.Equal(subscriptionId, triggerService.LastRequest!.SubscriptionId);
        Assert.Equal(CatalogSyncTriggerSource.Connect, triggerService.LastRequest.TriggerSource);
        Assert.True(triggerService.LastRequest.ForceFullSync);

        SubscriptionSquareConnection persisted = await dbContext.SubscriptionSquareConnections
            .SingleAsync(item => item.SubscriptionId == subscriptionId);
        Assert.Equal("merchant-1", persisted.SquareMerchantId);
    }

    [Fact]
    public async Task CompleteConnectAsync_WhenStateInvalid_ReturnsStateError()
    {
        MainDataContext dbContext = CreateContext();
        SquareService service = CreateService(dbContext, new RecordingOAuthStateProtector(), new RoutingHandler(_ => JsonResponse("{}")));

        CompleteSquareConnectResult result = await service.CompleteConnectAsync(
            new CompleteSquareConnectInput("invalid-state", "code", null));

        CompleteSquareConnectResult.Failure failure = Assert.IsType<CompleteSquareConnectResult.Failure>(result);
        Assert.Equal("square_oauth_state_invalid", failure.ErrorCode);
    }

    [Fact]
    public async Task DisconnectAsync_WhenRevokeSucceeds_RemovesConnection()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedOwnerMembershipAsync(dbContext, subscriptionId, userId);

        FakeDataProtectionProvider dataProtectionProvider = new();
        dbContext.SubscriptionSquareConnections.Add(new SubscriptionSquareConnection
        {
            SubscriptionId = subscriptionId,
            ConnectedByUserId = userId,
            SquareMerchantId = "merchant-1",
            EncryptedAccessToken = dataProtectionProvider.Protect("access-token"),
            EncryptedRefreshToken = dataProtectionProvider.Protect("refresh-token"),
            AccessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(20),
            Scopes = "ITEMS_READ"
        });
        await dbContext.SaveChangesAsync();

        RoutingHandler revokeHandler = new(_ => JsonResponse("{\"success\":true}"));
        SquareService service = CreateService(
            dbContext,
            new RecordingOAuthStateProtector(),
            revokeHandler,
            dataProtectionProvider: dataProtectionProvider);

        DisconnectSquareConnectionResult result = await service.DisconnectAsync(
            new DisconnectSquareConnectionInput(subscriptionId, userId));

        Assert.IsType<DisconnectSquareConnectionResult.Success>(result);
        SubscriptionSquareConnection? persisted = await dbContext.SubscriptionSquareConnections
            .SingleOrDefaultAsync(item => item.SubscriptionId == subscriptionId);
        Assert.Null(persisted);
    }

    private static MainDataContext CreateContext()
    {
        DbContextOptions<MainDataContext> options = new DbContextOptionsBuilder<MainDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new MainDataContext(options);
    }

    private static SquareService CreateService(
        MainDataContext dbContext,
        IOAuthStateProtector stateProtector,
        HttpMessageHandler handler,
        AppOptions? appOptions = null,
        IDataProtectionProvider? dataProtectionProvider = null,
        ISubscriptionOnboardingService? onboardingService = null,
        ISubscriptionCatalogSyncTriggerService? triggerService = null)
    {
        AppOptions resolvedAppOptions = appOptions ?? new AppOptions
        {
            PublicBaseUrl = new Uri("https://heyalan.test"),
            SquareClientId = "sandbox-client-id",
            SquareClientSecret = "square-client-secret"
        };

        return new SquareService(
            dbContext,
            new FakeHttpClientFactory(handler),
            resolvedAppOptions,
            dataProtectionProvider ?? new FakeDataProtectionProvider(),
            stateProtector,
            onboardingService ?? new StubSubscriptionOnboardingService(),
            triggerService ?? new RecordingSubscriptionCatalogSyncTriggerService(),
            NullLogger<SquareService>.Instance);
    }

    private static HttpResponseMessage JsonResponse(string payload)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
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

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient client;

        public FakeHttpClientFactory(HttpMessageHandler handler)
        {
            this.client = new HttpClient(handler);
        }

        public HttpClient CreateClient(string name)
        {
            return this.client;
        }
    }

    private sealed class FakeDataProtectionProvider : IDataProtectionProvider, IDataProtector
    {
        private const string Prefix = "enc::";

        public IDataProtector CreateProtector(string purpose)
        {
            return this;
        }

        public string Protect(string plaintext)
        {
            return $"{Prefix}{plaintext}";
        }

        public string Unprotect(string protectedData)
        {
            if (!protectedData.StartsWith(Prefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Invalid protected payload.");
            }

            return protectedData.Substring(Prefix.Length);
        }

        public byte[] Protect(byte[] plaintext)
        {
            string raw = Convert.ToBase64String(plaintext);
            string wrapped = this.Protect(raw);
            return Encoding.UTF8.GetBytes(wrapped);
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            string wrapped = Encoding.UTF8.GetString(protectedData);
            string raw = this.Unprotect(wrapped);
            return Convert.FromBase64String(raw);
        }
    }

    private sealed class StubSubscriptionOnboardingService : ISubscriptionOnboardingService
    {
        public Guid? LastRecomputeSubscriptionId { get; private set; }

        public Task<GetSubscriptionOnboardingStateResult> GetStateAsync(
            Guid subscriptionId,
            Guid userId,
            bool resumeMode = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<CreateSubscriptionOnboardingAgentResult> CreatePrimaryAgentAsync(
            Guid subscriptionId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateSubscriptionOnboardingStepResult> UpdateProfileAsync(
            UpdateSubscriptionOnboardingProfileInput input,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateSubscriptionOnboardingStepResult> UpdateChannelsAsync(
            UpdateSubscriptionOnboardingChannelsInput input,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateSubscriptionOnboardingStepResult> CompleteInvitationsAsync(
            Guid subscriptionId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateSubscriptionOnboardingStepResult> FinalizeAsync(
            Guid subscriptionId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateSubscriptionOnboardingStepResult> SkipStepAsync(
            Guid subscriptionId,
            Guid userId,
            string step,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<OnboardingStateResult> RecomputeStateAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default)
        {
            this.LastRecomputeSubscriptionId = subscriptionId;

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

    private sealed class RecordingSubscriptionCatalogSyncTriggerService : ISubscriptionCatalogSyncTriggerService
    {
        public SubscriptionCatalogSyncRequestInput? LastRequest { get; private set; }

        public Task<SubscriptionCatalogSyncRequestResult> RequestSyncAsync(
            SubscriptionCatalogSyncRequestInput input,
            CancellationToken cancellationToken = default)
        {
            this.LastRequest = input;
            return Task.FromResult(new SubscriptionCatalogSyncRequestResult(true));
        }

        public Task<int> EnqueueDuePeriodicSyncsAsync(
            DateTime utcNow,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class RoutingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responseFactory;

        public RoutingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            this.responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = this.responseFactory(request);
            return Task.FromResult(response);
        }
    }
}
