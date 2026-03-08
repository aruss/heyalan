namespace HeyAlan.Tests;

using System.Net;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using HeyAlan.Configuration;
using HeyAlan.Data;
using HeyAlan.Data.Entities;
using HeyAlan.Onboarding;
using HeyAlan.SquareIntegration;

public class SquareServiceTokenTests
{
    [Fact]
    public async Task StoreConnectionAsync_ThenGetValidAccessTokenAsync_ReturnsStoredTokenWithoutRefresh()
    {
        MainDataContext dbContext = CreateContext();
        RecordingHandler handler = new(static _ =>
        {
            throw new InvalidOperationException("Refresh endpoint should not be called for valid tokens.");
        });

        SquareService service = CreateService(dbContext, handler);
        Guid subscriptionId = Guid.NewGuid();

        await service.StoreConnectionAsync(new SquareTokenStoreInput(
            subscriptionId,
            Guid.NewGuid(),
            "merchant-1",
            "access-1",
            "refresh-1",
            DateTime.UtcNow.AddMinutes(30),
            ["MERCHANT_PROFILE_READ"]));

        SquareTokenResolution resolution = await service.GetValidAccessTokenAsync(subscriptionId);
        SquareTokenResolution.Success success = Assert.IsType<SquareTokenResolution.Success>(resolution);
        Assert.Equal("access-1", success.AccessToken);
        Assert.Equal(0, handler.CallCount);

        SubscriptionSquareConnection persistedConnection = await dbContext.SubscriptionSquareConnections
            .SingleAsync(connection => connection.SubscriptionId == subscriptionId);
        Assert.NotEqual("access-1", persistedConnection.EncryptedAccessToken);
        Assert.NotEqual("refresh-1", persistedConnection.EncryptedRefreshToken);
    }

    [Fact]
    public async Task GetValidAccessTokenAsync_WhenExpired_RefreshesAndRotatesRefreshToken()
    {
        MainDataContext dbContext = CreateContext();
        RecordingHandler handler = new(static _ =>
        {
            string payload = """
            {
              "access_token":"access-2",
              "refresh_token":"refresh-2",
              "expires_at":"2099-01-01T00:00:00Z",
              "merchant_id":"merchant-2",
              "scope":"ITEMS_READ CUSTOMERS_READ"
            }
            """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        });

        SquareService service = CreateService(dbContext, handler);
        Guid subscriptionId = Guid.NewGuid();

        await service.StoreConnectionAsync(new SquareTokenStoreInput(
            subscriptionId,
            Guid.NewGuid(),
            "merchant-old",
            "access-old",
            "refresh-old",
            DateTime.UtcNow.AddMinutes(30),
            ["MERCHANT_PROFILE_READ"]));

        SubscriptionSquareConnection expiredConnection = await dbContext.SubscriptionSquareConnections
            .SingleAsync(connection => connection.SubscriptionId == subscriptionId);
        expiredConnection.AccessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(-30);
        await dbContext.SaveChangesAsync();

        SubscriptionSquareConnection beforeRefresh = await dbContext.SubscriptionSquareConnections
            .SingleAsync(connection => connection.SubscriptionId == subscriptionId);
        string encryptedRefreshBefore = beforeRefresh.EncryptedRefreshToken;

        SquareTokenResolution resolution = await service.GetValidAccessTokenAsync(subscriptionId);
        SquareTokenResolution.Success success = Assert.IsType<SquareTokenResolution.Success>(resolution);

        Assert.Equal("access-2", success.AccessToken);
        Assert.Equal(1, handler.CallCount);

        SubscriptionSquareConnection afterRefresh = await dbContext.SubscriptionSquareConnections
            .SingleAsync(connection => connection.SubscriptionId == subscriptionId);

        Assert.NotEqual(encryptedRefreshBefore, afterRefresh.EncryptedRefreshToken);
        Assert.Equal("merchant-2", afterRefresh.SquareMerchantId);
        Assert.Contains("ITEMS_READ", afterRefresh.Scopes);
    }

    [Fact]
    public async Task GetValidAccessTokenAsync_WhenRefreshInvalidGrant_ReturnsReconnectRequired()
    {
        MainDataContext dbContext = CreateContext();
        RecordingHandler handler = new(static _ =>
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":\"invalid_grant\"}", Encoding.UTF8, "application/json")
            };
        });

        SquareService service = CreateService(dbContext, handler);
        Guid subscriptionId = Guid.NewGuid();

        await service.StoreConnectionAsync(new SquareTokenStoreInput(
            subscriptionId,
            Guid.NewGuid(),
            "merchant-1",
            "access-1",
            "refresh-1",
            DateTime.UtcNow.AddMinutes(30),
            ["MERCHANT_PROFILE_READ"]));

        SubscriptionSquareConnection expiredConnection = await dbContext.SubscriptionSquareConnections
            .SingleAsync(connection => connection.SubscriptionId == subscriptionId);
        expiredConnection.AccessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(-30);
        await dbContext.SaveChangesAsync();

        SquareTokenResolution resolution = await service.GetValidAccessTokenAsync(subscriptionId);
        SquareTokenResolution.ReconnectRequired reconnectRequired = Assert.IsType<SquareTokenResolution.ReconnectRequired>(resolution);
        Assert.Equal("refresh_invalid_or_revoked", reconnectRequired.ReasonCode);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GetValidAccessTokenAsync_WhenSubscriptionHasNoConnection_ReturnsConnectionMissing()
    {
        MainDataContext dbContext = CreateContext();
        RecordingHandler handler = new(static _ => new HttpResponseMessage(HttpStatusCode.OK));
        SquareService service = CreateService(dbContext, handler);

        SquareTokenResolution resolution = await service.GetValidAccessTokenAsync(Guid.NewGuid());

        Assert.IsType<SquareTokenResolution.ConnectionMissing>(resolution);
        Assert.Equal(0, handler.CallCount);
    }

    private static MainDataContext CreateContext()
    {
        DbContextOptions<MainDataContext> options = new DbContextOptionsBuilder<MainDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new MainDataContext(options);
    }

    private static SquareService CreateService(MainDataContext dbContext, HttpMessageHandler handler)
    {
        AppOptions appOptions = new()
        {
            PublicBaseUrl = new Uri("https://heyalan.test"),
            SquareClientId = "sandbox-client-id",
            SquareClientSecret = "square-client-secret"
        };

        FakeHttpClientFactory httpClientFactory = new(handler);
        FakeDataProtectionProvider dataProtectionProvider = new();

        return new SquareService(
            dbContext,
            httpClientFactory,
            appOptions,
            dataProtectionProvider,
            new PassThroughStateProtector(),
            new StubSubscriptionOnboardingService(),
            NullLogger<SquareService>.Instance);
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

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responseFactory;

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
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
}
