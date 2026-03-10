namespace HeyAlan.Tests;

using System.Net;
using System.Text;
using HeyAlan.Configuration;
using HeyAlan.Data;
using HeyAlan.Onboarding;
using HeyAlan.SquareIntegration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

public class SquareOAuthClientTests
{
    [Fact]
    public async Task ExchangeAuthorizationCodeAsync_WhenExchangeScopesMissing_UsesTokenStatusScopes()
    {
        RoutingHandler handler = new(request =>
        {
            if (IsTokenStatusRequest(request))
            {
                string tokenStatusPayload = """
                {
                  "scopes":["ITEMS_READ","CUSTOMERS_READ","CUSTOMERS_WRITE","ORDERS_READ","ORDERS_WRITE","PAYMENTS_WRITE"],
                  "expires_at":"2099-01-01T00:00:00Z",
                  "client_id":"sandbox-client-id",
                  "merchant_id":"merchant-1"
                }
                """;

                return JsonResponse(tokenStatusPayload);
            }

            string exchangePayload = """
            {
              "access_token":"access-1",
              "refresh_token":"refresh-1",
              "expires_at":"2099-01-01T00:00:00Z",
              "merchant_id":"merchant-1"
            }
            """;

            return JsonResponse(exchangePayload);
        });

        SquareService service = CreateService(handler);
        SquareTokenExchangeResult result = await service.ExchangeAuthorizationCodeAsync(
            "auth-code",
            "https://heyalan.test/subscriptions/square/callback");

        SquareTokenExchangeResult.Success success = Assert.IsType<SquareTokenExchangeResult.Success>(result);
        Assert.Contains("ITEMS_READ", success.Payload.Scopes, StringComparer.Ordinal);
        Assert.Contains("PAYMENTS_WRITE", success.Payload.Scopes, StringComparer.Ordinal);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task ExchangeAuthorizationCodeAsync_WhenExchangeContainsScope_DoesNotCallTokenStatus()
    {
        RoutingHandler handler = new(_ =>
        {
            string exchangePayload = """
            {
              "access_token":"access-2",
              "refresh_token":"refresh-2",
              "expires_at":"2099-01-01T00:00:00Z",
              "merchant_id":"merchant-2",
              "scope":"ITEMS_READ CUSTOMERS_READ"
            }
            """;

            return JsonResponse(exchangePayload);
        });

        SquareService service = CreateService(handler);
        SquareTokenExchangeResult result = await service.ExchangeAuthorizationCodeAsync(
            "auth-code",
            "https://heyalan.test/subscriptions/square/callback");

        SquareTokenExchangeResult.Success success = Assert.IsType<SquareTokenExchangeResult.Success>(result);
        Assert.Contains("ITEMS_READ", success.Payload.Scopes, StringComparer.Ordinal);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task ExchangeAuthorizationCodeAsync_WhenTokenStatusFails_ReturnsSuccessWithEmptyScopes()
    {
        RoutingHandler handler = new(request =>
        {
            if (IsTokenStatusRequest(request))
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("{\"errors\":[{\"code\":\"UNAUTHORIZED\"}]}", Encoding.UTF8, "application/json")
                };
            }

            string exchangePayload = """
            {
              "access_token":"access-3",
              "refresh_token":"refresh-3",
              "expires_at":"2099-01-01T00:00:00Z",
              "merchant_id":"merchant-3"
            }
            """;

            return JsonResponse(exchangePayload);
        });

        SquareService service = CreateService(handler);
        SquareTokenExchangeResult result = await service.ExchangeAuthorizationCodeAsync(
            "auth-code",
            "https://heyalan.test/subscriptions/square/callback");

        SquareTokenExchangeResult.Success success = Assert.IsType<SquareTokenExchangeResult.Success>(result);
        Assert.Empty(success.Payload.Scopes);
        Assert.Equal(2, handler.CallCount);
    }

    private static SquareService CreateService(HttpMessageHandler handler)
    {
        DbContextOptions<MainDataContext> options = new DbContextOptionsBuilder<MainDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        MainDataContext dbContext = new(options);
        AppOptions appOptions = new()
        {
            PublicBaseUrl = new Uri("https://heyalan.test"),
            SquareClientId = "sandbox-client-id",
            SquareClientSecret = "square-client-secret"
        };

        return new SquareService(
            dbContext,
            new FakeHttpClientFactory(handler),
            appOptions,
            new PassThroughDataProtectionProvider(),
            new PassThroughStateProtector(),
            new StubSubscriptionOnboardingService(),
            new StubSubscriptionCatalogSyncTriggerService(),
            NullLogger<SquareService>.Instance);
    }

    private static HttpResponseMessage JsonResponse(string payload)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
    }

    private static bool IsTokenStatusRequest(HttpRequestMessage request)
    {
        string requestPath = request.RequestUri?.AbsolutePath ?? String.Empty;
        return requestPath.Contains("/oauth2/token/status", StringComparison.Ordinal);
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

    private sealed class PassThroughDataProtectionProvider : IDataProtectionProvider, IDataProtector
    {
        public IDataProtector CreateProtector(string purpose)
        {
            return this;
        }

        public string Protect(string plaintext)
        {
            return plaintext;
        }

        public string Unprotect(string protectedData)
        {
            return protectedData;
        }

        public byte[] Protect(byte[] plaintext)
        {
            return plaintext;
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            return protectedData;
        }
    }

    private sealed class PassThroughStateProtector : IOAuthStateProtector
    {
        public string Protect(SquareConnectStatePayload payload)
        {
            return "state";
        }

        public bool TryUnprotect(string protectedState, out SquareConnectStatePayload? payload)
        {
            payload = null;
            return false;
        }
    }

    private sealed class StubSubscriptionOnboardingService : ISubscriptionOnboardingService
    {
        public Task<GetSubscriptionOnboardingStateResult> GetStateAsync(Guid subscriptionId, Guid userId, bool resumeMode = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<CreateSubscriptionOnboardingAgentResult> CreatePrimaryAgentAsync(Guid subscriptionId, Guid userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateSubscriptionOnboardingStepResult> UpdateProfileAsync(UpdateSubscriptionOnboardingProfileInput input, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateSubscriptionOnboardingStepResult> UpdateChannelsAsync(UpdateSubscriptionOnboardingChannelsInput input, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateSubscriptionOnboardingStepResult> CompleteInvitationsAsync(Guid subscriptionId, Guid userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateSubscriptionOnboardingStepResult> FinalizeAsync(Guid subscriptionId, Guid userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateSubscriptionOnboardingStepResult> SkipStepAsync(Guid subscriptionId, Guid userId, string step, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<OnboardingStateResult> RecomputeStateAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
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

    private sealed class RoutingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responseFactory;

        public RoutingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            this.responseFactory = responseFactory;
        }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.CallCount++;
            HttpResponseMessage response = this.responseFactory(request);
            return Task.FromResult(response);
        }
    }

    private sealed class StubSubscriptionCatalogSyncTriggerService : ISubscriptionCatalogSyncTriggerService
    {
        public Task<SubscriptionCatalogSyncRequestResult> RequestSyncAsync(
            SubscriptionCatalogSyncRequestInput input,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SubscriptionCatalogSyncRequestResult(true));
        }

        public Task<int> EnqueueDuePeriodicSyncsAsync(
            DateTime utcNow,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
