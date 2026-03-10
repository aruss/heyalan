namespace HeyAlan.Tests;

using HeyAlan.Data;
using HeyAlan.Data.Entities;
using HeyAlan.SquareIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

public class SubscriptionCatalogSyncTriggerServiceTests
{
    [Fact]
    public async Task RequestSyncAsync_WhenIdle_EnqueuesSingleReservedRun()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        await SeedSubscriptionAsync(dbContext, subscriptionId);

        RecordingMessagePublisher messagePublisher = new();
        SubscriptionCatalogSyncTriggerService service = CreateService(dbContext, messagePublisher);

        SubscriptionCatalogSyncRequestResult firstResult = await service.RequestSyncAsync(
            new SubscriptionCatalogSyncRequestInput(
                subscriptionId,
                CatalogSyncTriggerSource.Manual,
                true));

        SubscriptionCatalogSyncRequestResult secondResult = await service.RequestSyncAsync(
            new SubscriptionCatalogSyncRequestInput(
                subscriptionId,
                CatalogSyncTriggerSource.Manual,
                true));

        Assert.True(firstResult.Enqueued);
        Assert.False(secondResult.Enqueued);
        Assert.Single(messagePublisher.Messages);

        SubscriptionCatalogSyncState syncState = await dbContext.SubscriptionCatalogSyncStates
            .SingleAsync(item => item.SubscriptionId == subscriptionId);
        Assert.True(syncState.PendingResync);
    }

    [Fact]
    public async Task RequestSyncAsync_WhenWebhookAccepted_ResetsNextScheduledSyncTime()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        await SeedSubscriptionAsync(dbContext, subscriptionId);

        SubscriptionCatalogSyncState syncState = new()
        {
            SubscriptionId = subscriptionId,
            NextScheduledSyncAtUtc = DateTime.UtcNow.AddMinutes(1),
            SyncInProgress = false,
            PendingResync = false
        };

        dbContext.SubscriptionCatalogSyncStates.Add(syncState);
        await dbContext.SaveChangesAsync();

        RecordingMessagePublisher messagePublisher = new();
        SubscriptionCatalogSyncTriggerService service = CreateService(dbContext, messagePublisher);
        DateTime beforeRequestUtc = DateTime.UtcNow;

        await service.RequestSyncAsync(
            new SubscriptionCatalogSyncRequestInput(
                subscriptionId,
                CatalogSyncTriggerSource.Webhook));

        Assert.NotNull(syncState.NextScheduledSyncAtUtc);
        Assert.True(syncState.NextScheduledSyncAtUtc >= beforeRequestUtc.AddMinutes(14));
        Assert.True(syncState.NextScheduledSyncAtUtc <= DateTime.UtcNow.AddMinutes(16));
    }

    [Fact]
    public async Task EnqueueDuePeriodicSyncsAsync_WhenConnectionIsDue_OnlyEnqueuesConnectedSubscriptions()
    {
        MainDataContext dbContext = CreateContext();
        Guid dueSubscriptionId = Guid.NewGuid();
        Guid futureSubscriptionId = Guid.NewGuid();
        Guid disconnectedSubscriptionId = Guid.NewGuid();

        await SeedSubscriptionWithConnectionAsync(dbContext, dueSubscriptionId, null);
        await SeedSubscriptionWithConnectionAsync(dbContext, futureSubscriptionId, DateTime.UtcNow.AddMinutes(5));
        await SeedSubscriptionWithDisconnectedConnectionAsync(dbContext, disconnectedSubscriptionId);

        RecordingMessagePublisher messagePublisher = new();
        SubscriptionCatalogSyncTriggerService service = CreateService(dbContext, messagePublisher);

        int enqueuedCount = await service.EnqueueDuePeriodicSyncsAsync(DateTime.UtcNow);

        Assert.Equal(1, enqueuedCount);
        SquareCatalogSyncRequested message = Assert.Single(messagePublisher.Messages);
        Assert.Equal(dueSubscriptionId, message.SubscriptionId);
        Assert.Equal(CatalogSyncTriggerSource.Periodic, message.TriggerSource);
    }

    private static MainDataContext CreateContext()
    {
        DbContextOptions<MainDataContext> options = new DbContextOptionsBuilder<MainDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new MainDataContext(options);
    }

    private static SubscriptionCatalogSyncTriggerService CreateService(
        MainDataContext dbContext,
        RecordingMessagePublisher messagePublisher)
    {
        return new SubscriptionCatalogSyncTriggerService(
            dbContext,
            messagePublisher,
            NullLogger<SubscriptionCatalogSyncTriggerService>.Instance);
    }

    private static async Task SeedSubscriptionAsync(MainDataContext dbContext, Guid subscriptionId)
    {
        dbContext.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedSubscriptionWithConnectionAsync(
        MainDataContext dbContext,
        Guid subscriptionId,
        DateTime? nextScheduledSyncAtUtc)
    {
        dbContext.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId
        });

        dbContext.SubscriptionSquareConnections.Add(new SubscriptionSquareConnection
        {
            SubscriptionId = subscriptionId,
            ConnectedByUserId = Guid.NewGuid(),
            SquareMerchantId = $"merchant-{subscriptionId}",
            EncryptedAccessToken = "unused",
            EncryptedRefreshToken = "unused",
            AccessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(30),
            Scopes = "ITEMS_READ",
            DisconnectedAtUtc = null
        });

        dbContext.SubscriptionCatalogSyncStates.Add(new SubscriptionCatalogSyncState
        {
            SubscriptionId = subscriptionId,
            NextScheduledSyncAtUtc = nextScheduledSyncAtUtc,
            SyncInProgress = false,
            PendingResync = false
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedSubscriptionWithDisconnectedConnectionAsync(
        MainDataContext dbContext,
        Guid subscriptionId)
    {
        dbContext.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId
        });

        dbContext.SubscriptionSquareConnections.Add(new SubscriptionSquareConnection
        {
            SubscriptionId = subscriptionId,
            ConnectedByUserId = Guid.NewGuid(),
            SquareMerchantId = $"merchant-{subscriptionId}",
            EncryptedAccessToken = "unused",
            EncryptedRefreshToken = "unused",
            AccessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(30),
            Scopes = "ITEMS_READ",
            DisconnectedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }

    private sealed class RecordingMessagePublisher : ISubscriptionCatalogSyncMessagePublisher
    {
        public List<SquareCatalogSyncRequested> Messages { get; } = [];

        public Task PublishAsync(SquareCatalogSyncRequested message, CancellationToken cancellationToken = default)
        {
            this.Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
