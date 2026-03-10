namespace HeyAlan.Tests;

using System.Net;
using System.Text;
using HeyAlan.Configuration;
using HeyAlan.Data;
using HeyAlan.Data.Entities;
using HeyAlan.SquareIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

public class SubscriptionCatalogServicesTests
{
    [Fact]
    public async Task GetProductsAsync_WhenAgentHasNoAssignments_ReturnsAllActiveSellableProducts()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid agentId = Guid.NewGuid();
        await SeedAgentAsync(dbContext, subscriptionId, agentId);

        SubscriptionCatalogProduct includedProduct = new()
        {
            SubscriptionId = subscriptionId,
            SquareItemId = "item-1",
            SquareVariationId = "var-1",
            ItemName = "Coffee",
            VariationName = "Large",
            IsSellable = true,
            IsDeleted = false,
            SearchText = "coffee large"
        };

        await dbContext.SubscriptionCatalogProducts.AddRangeAsync(
            includedProduct,
            new SubscriptionCatalogProduct
            {
                SubscriptionId = subscriptionId,
                SquareItemId = "item-2",
                SquareVariationId = "var-2",
                ItemName = "Coffee",
                VariationName = "Hidden",
                IsSellable = false,
                IsDeleted = false,
                SearchText = "coffee hidden"
            },
            new SubscriptionCatalogProduct
            {
                SubscriptionId = subscriptionId,
                SquareItemId = "item-3",
                SquareVariationId = "var-3",
                ItemName = "Archived",
                VariationName = "Large",
                IsSellable = true,
                IsDeleted = true,
                SearchText = "archived large"
            });

        await dbContext.SaveChangesAsync();

        SubscriptionCatalogReadService service = new(dbContext);
        GetSubscriptionCatalogProductsResult result = await service.GetProductsAsync(
            new GetSubscriptionCatalogProductsInput(subscriptionId, agentId, null));

        SubscriptionCatalogProductResult product = Assert.Single(result.Products.Items);
        Assert.Equal(includedProduct.SquareVariationId, product.SquareVariationId);
    }

    [Fact]
    public async Task GetProductsAsync_WhenAgentHasAssignments_ReturnsAssignedSubsetWithoutZipFiltering()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid agentId = Guid.NewGuid();
        await SeedAgentAsync(dbContext, subscriptionId, agentId);

        SubscriptionCatalogProduct allowedProduct = new()
        {
            SubscriptionId = subscriptionId,
            SquareItemId = "item-1",
            SquareVariationId = "var-1",
            ItemName = "Tea",
            VariationName = "Mint",
            IsSellable = true,
            IsDeleted = false,
            SearchText = "tea mint"
        };

        SubscriptionCatalogProduct blockedProduct = new()
        {
            SubscriptionId = subscriptionId,
            SquareItemId = "item-2",
            SquareVariationId = "var-2",
            ItemName = "Tea",
            VariationName = "Black",
            IsSellable = true,
            IsDeleted = false,
            SearchText = "tea black"
        };

        await dbContext.SubscriptionCatalogProducts.AddRangeAsync(allowedProduct, blockedProduct);
        await dbContext.SaveChangesAsync();

        dbContext.AgentCatalogProductAccesses.Add(
            new AgentCatalogProductAccess
            {
                SubscriptionId = subscriptionId,
                AgentId = agentId,
                SubscriptionCatalogProductId = allowedProduct.Id
            });

        dbContext.AgentSalesZipCodes.Add(
            new AgentSalesZipCode
            {
                SubscriptionId = subscriptionId,
                AgentId = agentId,
                ZipCodeNormalized = "12345"
            });

        await dbContext.SaveChangesAsync();

        SubscriptionCatalogReadService service = new(dbContext);
        GetSubscriptionCatalogProductsResult result = await service.GetProductsAsync(
            new GetSubscriptionCatalogProductsInput(subscriptionId, agentId, null));

        SubscriptionCatalogProductResult product = Assert.Single(result.Products.Items);
        Assert.Equal(allowedProduct.SquareVariationId, product.SquareVariationId);
    }

    [Fact]
    public async Task SyncAsync_WhenFullSyncResponseReceived_UpsertsProductsAndLocations()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid connectedByUserId = Guid.NewGuid();
        SeedSubscriptionAndConnection(dbContext, subscriptionId, connectedByUserId);

        RoutingHandler handler = new(_ =>
        {
            string payload = """
            {
              "objects": [
                {
                  "type": "ITEM",
                  "id": "item-1",
                  "updated_at": "2026-03-09T10:00:00Z",
                  "version": 1001,
                  "is_deleted": false,
                  "item_data": {
                    "name": "Coffee",
                    "description": "Fresh roast"
                  }
                },
                {
                  "type": "ITEM_VARIATION",
                  "id": "var-1",
                  "updated_at": "2026-03-09T10:00:00Z",
                  "version": 1002,
                  "is_deleted": false,
                  "present_at_all_locations": true,
                  "item_variation_data": {
                    "item_id": "item-1",
                    "name": "Large",
                    "sku": "COF-L",
                    "sellable": true,
                    "price_money": {
                      "amount": 1299,
                      "currency": "USD"
                    },
                    "location_overrides": [
                      {
                        "location_id": "loc-1",
                        "price_money": {
                          "amount": 1399,
                          "currency": "USD"
                        },
                        "sold_out": false
                      }
                    ]
                  }
                }
              ]
            }
            """;

            return JsonResponse(payload);
        });

        SubscriptionCatalogSyncService service = CreateSyncService(dbContext, handler);
        SubscriptionCatalogSyncResult result = await service.SyncAsync(
            new SubscriptionCatalogSyncInput(subscriptionId, CatalogSyncTriggerSource.Manual, true));

        Assert.True(
            result is SubscriptionCatalogSyncResult.Success,
            result is SubscriptionCatalogSyncResult.Failure failure
                ? $"Sync failed with error code '{failure.ErrorCode}'."
                : $"Sync returned unexpected type '{result.GetType().Name}'.");

        SubscriptionCatalogSyncResult.Success success = (SubscriptionCatalogSyncResult.Success)result;
        Assert.True(success.WasFullSync);

        SubscriptionCatalogProduct product = await dbContext.SubscriptionCatalogProducts
            .SingleAsync(item => item.SubscriptionId == subscriptionId);
        Assert.Equal("Coffee", product.ItemName);
        Assert.Equal("Large", product.VariationName);
        Assert.Equal(1299, product.BasePriceAmount);
        Assert.Equal("USD", product.BasePriceCurrency);
        Assert.False(product.IsDeleted);

        SubscriptionCatalogProductLocation location = await dbContext.SubscriptionCatalogProductLocations
            .SingleAsync(item => item.SubscriptionId == subscriptionId);
        Assert.Equal(product.Id, location.SubscriptionCatalogProductId);
        Assert.Equal("loc-1", location.LocationId);
        Assert.Equal(1399, location.PriceOverrideAmount);
        Assert.True(location.IsAvailableForSale);
        Assert.False(location.IsSoldOut);

        SubscriptionCatalogSyncState syncState = await dbContext.SubscriptionCatalogSyncStates
            .SingleAsync(item => item.SubscriptionId == subscriptionId);
        Assert.Equal(CatalogSyncTriggerSource.Manual, syncState.LastTriggerSource);
        Assert.Null(syncState.LastErrorCode);
        Assert.False(syncState.SyncInProgress);
    }

    private static MainDataContext CreateContext()
    {
        DbContextOptions<MainDataContext> options = new DbContextOptionsBuilder<MainDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new MainDataContext(options);
    }

    private static SubscriptionCatalogSyncService CreateSyncService(
        MainDataContext dbContext,
        HttpMessageHandler handler)
    {
        AppOptions appOptions = new()
        {
            PublicBaseUrl = new Uri("https://heyalan.test"),
            SquareClientId = "sandbox-client-id",
            SquareClientSecret = "square-client-secret"
        };

        FakeHttpClientFactory httpClientFactory = new(handler);
        return new SubscriptionCatalogSyncService(
            dbContext,
            new StubSquareService(),
            httpClientFactory,
            appOptions,
            NullLogger<SubscriptionCatalogSyncService>.Instance);
    }

    private static async Task SeedAgentAsync(MainDataContext dbContext, Guid subscriptionId, Guid agentId)
    {
        dbContext.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId
        });

        dbContext.Agents.Add(new Agent
        {
            Id = agentId,
            SubscriptionId = subscriptionId,
            Name = "Agent"
        });

        await dbContext.SaveChangesAsync();
    }

    private static void SeedSubscriptionAndConnection(
        MainDataContext dbContext,
        Guid subscriptionId,
        Guid connectedByUserId)
    {
        ApplicationUser user = new()
        {
            Id = connectedByUserId,
            DisplayName = "Owner",
            UserName = "owner@example.com",
            Email = "owner@example.com"
        };

        dbContext.Users.Add(user);
        dbContext.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId
        });
        dbContext.SubscriptionSquareConnections.Add(new SubscriptionSquareConnection
        {
            SubscriptionId = subscriptionId,
            ConnectedByUserId = connectedByUserId,
            SquareMerchantId = "merchant-1",
            EncryptedAccessToken = "unused",
            EncryptedRefreshToken = "unused",
            AccessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(20),
            Scopes = "ITEMS_READ"
        });
        dbContext.SubscriptionCatalogSyncStates.Add(new SubscriptionCatalogSyncState
        {
            SubscriptionId = subscriptionId,
            SyncInProgress = false,
            PendingResync = false
        });

        dbContext.SaveChanges();
    }

    private static HttpResponseMessage JsonResponse(string payload)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
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

    private sealed class StubSquareService : ISquareService
    {
        public Task<StartSquareConnectResult> StartConnectAsync(
            StartSquareConnectInput input,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<CompleteSquareConnectResult> CompleteConnectAsync(
            CompleteSquareConnectInput input,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DisconnectSquareConnectionResult> DisconnectAsync(
            DisconnectSquareConnectionInput input,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task StoreConnectionAsync(
            SquareTokenStoreInput input,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SquareTokenResolution> GetValidAccessTokenAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<SquareTokenResolution>(
                new SquareTokenResolution.Success("access-token", DateTime.UtcNow.AddMinutes(30)));
        }

        public Task<SquareTokenExchangeResult> ExchangeAuthorizationCodeAsync(
            string authorizationCode,
            string redirectUri,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SquareRevokeResult> RevokeAccessTokenAsync(
            string accessToken,
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
