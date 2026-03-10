namespace HeyAlan.Tests;

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using HeyAlan.Configuration;
using HeyAlan.Data;
using HeyAlan.Data.Entities;
using HeyAlan.SquareIntegration;
using HeyAlan.WebApi.SquareIntegration;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class SquareCatalogWebhookEndpointsTests
{
    [Fact]
    public async Task IngestSquareCatalogWebhookAsync_WhenSignatureIsInvalid_ReturnsUnauthorized()
    {
        MainDataContext dbContext = CreateContext();
        RecordingCatalogSyncTriggerService triggerService = new();
        DefaultHttpContext context = CreateHttpContext(
            body: "{\"type\":\"catalog.version.updated\",\"event_id\":\"evt-1\",\"merchant_id\":\"merchant-1\"}",
            signatureHeader: "invalid-signature");

        AppOptions appOptions = CreateAppOptions();

        IResult result = await InvokeEndpointAsync(
            context,
            dbContext,
            appOptions,
            triggerService);

        HttpContext responseContext = CreateHttpContext();
        responseContext.Response.Body = new MemoryStream();
        await result.ExecuteAsync(responseContext);

        Assert.Equal(StatusCodes.Status401Unauthorized, responseContext.Response.StatusCode);
        Assert.Empty(triggerService.Requests);
    }

    [Fact]
    public async Task IngestSquareCatalogWebhookAsync_WhenCatalogEventIsValid_PersistsReceiptAndRequestsWebhookSync()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        await SeedSubscriptionConnectionAsync(dbContext, subscriptionId, "merchant-1");

        string body = """
            {
              "merchant_id": "merchant-1",
              "type": "catalog.version.updated",
              "event_id": "evt-1",
              "created_at": "2026-03-09T11:00:00Z"
            }
            """;

        AppOptions appOptions = CreateAppOptions();
        DefaultHttpContext context = CreateHttpContext(body, CreateSignature(body, appOptions));
        RecordingCatalogSyncTriggerService triggerService = new();

        IResult result = await InvokeEndpointAsync(
            context,
            dbContext,
            appOptions,
            triggerService);

        HttpContext responseContext = CreateHttpContext();
        responseContext.Response.Body = new MemoryStream();
        await result.ExecuteAsync(responseContext);

        Assert.Equal(StatusCodes.Status200OK, responseContext.Response.StatusCode);

        SquareWebhookReceipt receipt = await dbContext.SquareWebhookReceipts.SingleAsync();
        Assert.Equal(subscriptionId, receipt.SubscriptionId);
        Assert.Equal("evt-1", receipt.EventId);
        Assert.Equal("catalog.version.updated", receipt.EventType);
        Assert.Equal("merchant-1", receipt.MerchantId);
        Assert.True(receipt.IsProcessed);

        SubscriptionCatalogSyncRequestInput request = Assert.Single(triggerService.Requests);
        Assert.Equal(subscriptionId, request.SubscriptionId);
        Assert.Equal(CatalogSyncTriggerSource.Webhook, request.TriggerSource);
        Assert.False(request.ForceFullSync);
    }

    [Fact]
    public async Task IngestSquareCatalogWebhookAsync_WhenEventWasAlreadyReceived_ReturnsOkWithoutDuplicateProcessing()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        await SeedSubscriptionConnectionAsync(dbContext, subscriptionId, "merchant-1");

        dbContext.SquareWebhookReceipts.Add(new SquareWebhookReceipt
        {
            SubscriptionId = subscriptionId,
            EventId = "evt-1",
            EventType = "catalog.version.updated",
            MerchantId = "merchant-1",
            ReceivedAtUtc = DateTime.UtcNow,
            IsProcessed = true
        });

        await dbContext.SaveChangesAsync();

        string body = """
            {
              "merchant_id": "merchant-1",
              "type": "catalog.version.updated",
              "event_id": "evt-1",
              "created_at": "2026-03-09T11:00:00Z"
            }
            """;

        AppOptions appOptions = CreateAppOptions();
        DefaultHttpContext context = CreateHttpContext(body, CreateSignature(body, appOptions));
        RecordingCatalogSyncTriggerService triggerService = new();

        IResult result = await InvokeEndpointAsync(
            context,
            dbContext,
            appOptions,
            triggerService);

        HttpContext responseContext = CreateHttpContext();
        responseContext.Response.Body = new MemoryStream();
        await result.ExecuteAsync(responseContext);

        Assert.Equal(StatusCodes.Status200OK, responseContext.Response.StatusCode);
        Assert.Empty(triggerService.Requests);
        Assert.Single(dbContext.SquareWebhookReceipts);
    }

    private static MainDataContext CreateContext()
    {
        DbContextOptions<MainDataContext> options = new DbContextOptionsBuilder<MainDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new MainDataContext(options);
    }

    private static DefaultHttpContext CreateHttpContext(string body = "", string? signatureHeader = null)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.ConfigureHttpJsonOptions(_ => { });

        DefaultHttpContext context = new();
        context.RequestServices = services.BuildServiceProvider();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentLength = context.Request.Body.Length;
        context.Request.Path = SquareIntegrationRules.CatalogWebhookPath;

        if (!String.IsNullOrWhiteSpace(signatureHeader))
        {
            context.Request.Headers["x-square-hmacsha256-signature"] = signatureHeader;
        }

        return context;
    }

    private static AppOptions CreateAppOptions()
    {
        return new AppOptions
        {
            PublicBaseUrl = new Uri("https://heyalan.test"),
            SquareWebhookSignatureKey = "webhook-signature-key"
        };
    }

    private static string CreateSignature(string body, AppOptions appOptions)
    {
        string notificationUrl = new Uri(appOptions.PublicBaseUrl, SquareIntegrationRules.CatalogWebhookPath).ToString();
        byte[] keyBytes = Encoding.UTF8.GetBytes(appOptions.SquareWebhookSignatureKey!);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(notificationUrl + body);

        using HMACSHA256 hmac = new(keyBytes);
        byte[] hashBytes = hmac.ComputeHash(payloadBytes);
        return Convert.ToBase64String(hashBytes);
    }

    private static async Task SeedSubscriptionConnectionAsync(
        MainDataContext dbContext,
        Guid subscriptionId,
        string merchantId)
    {
        Guid userId = Guid.NewGuid();

        dbContext.Users.Add(new ApplicationUser
        {
            Id = userId,
            DisplayName = "Webhook Test User",
            UserName = "webhook-test@example.com",
            Email = "webhook-test@example.com"
        });

        dbContext.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId
        });

        dbContext.SubscriptionSquareConnections.Add(new SubscriptionSquareConnection
        {
            SubscriptionId = subscriptionId,
            SquareMerchantId = merchantId,
            EncryptedAccessToken = "unused",
            EncryptedRefreshToken = "unused",
            AccessTokenExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            Scopes = "ITEMS_READ",
            ConnectedByUserId = userId
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task<IResult> InvokeEndpointAsync(
        HttpContext context,
        MainDataContext dbContext,
        AppOptions appOptions,
        RecordingCatalogSyncTriggerService triggerService)
    {
        MethodInfo method = typeof(SquareCatalogWebhookEndpoints).GetMethod(
            "IngestSquareCatalogWebhookAsync",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        Task task = (Task)method.Invoke(null, [context.Request, dbContext, appOptions, triggerService, context.RequestServices.GetRequiredService<ILoggerFactory>(), CancellationToken.None])!;
        await task;

        object result = task.GetType().GetProperty("Result")!.GetValue(task)!;
        return (IResult)result;
    }

    private sealed class RecordingCatalogSyncTriggerService : ISubscriptionCatalogSyncTriggerService
    {
        public List<SubscriptionCatalogSyncRequestInput> Requests { get; } = [];

        public Task<SubscriptionCatalogSyncRequestResult> RequestSyncAsync(
            SubscriptionCatalogSyncRequestInput input,
            CancellationToken cancellationToken = default)
        {
            this.Requests.Add(input);
            return Task.FromResult(new SubscriptionCatalogSyncRequestResult(true));
        }

        public Task<int> EnqueueDuePeriodicSyncsAsync(DateTime utcNow, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
