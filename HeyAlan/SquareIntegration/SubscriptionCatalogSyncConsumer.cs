namespace HeyAlan.SquareIntegration;

using HeyAlan.Data;
using HeyAlan.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public sealed class SubscriptionCatalogSyncConsumer
{
    private static readonly TimeSpan PeriodicCadence = TimeSpan.FromMinutes(15);

    private readonly MainDataContext dbContext;
    private readonly ISubscriptionCatalogSyncService syncService;
    private readonly ISubscriptionCatalogSyncMessagePublisher messagePublisher;
    private readonly ILogger<SubscriptionCatalogSyncConsumer> logger;

    public SubscriptionCatalogSyncConsumer(
        MainDataContext dbContext,
        ISubscriptionCatalogSyncService syncService,
        ISubscriptionCatalogSyncMessagePublisher messagePublisher,
        ILogger<SubscriptionCatalogSyncConsumer> logger)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
        this.messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(SquareCatalogSyncRequested message, CancellationToken cancellationToken)
    {
        SubscriptionCatalogSyncState syncState = await this.GetOrCreateSyncStateAsync(message.SubscriptionId, cancellationToken);
        if (syncState.SyncInProgress)
        {
            syncState.PendingResync = true;
            await this.dbContext.SaveChangesAsync(cancellationToken);

            this.logger.LogInformation(
                "Catalog sync already in progress for subscription {SubscriptionId}. Marked pending resync for trigger {TriggerSource}.",
                message.SubscriptionId,
                message.TriggerSource);
            return;
        }

        if (syncState.PendingResync)
        {
            syncState.PendingResync = false;
        }

        if (message.TriggerSource == CatalogSyncTriggerSource.Periodic)
        {
            syncState.NextScheduledSyncAtUtc = DateTime.UtcNow.Add(PeriodicCadence);
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        SubscriptionCatalogSyncResult result = await this.syncService.SyncAsync(
            new SubscriptionCatalogSyncInput(
                message.SubscriptionId,
                message.TriggerSource,
                message.ForceFullSync),
            cancellationToken);

        if (syncState.PendingResync)
        {
            SquareCatalogSyncRequested followUpMessage = new(
                message.SubscriptionId,
                message.TriggerSource,
                false);

            try
            {
                await this.messagePublisher.PublishAsync(followUpMessage, cancellationToken);
                this.logger.LogInformation(
                    "Enqueued follow-up catalog sync for subscription {SubscriptionId} after pending resync.",
                    message.SubscriptionId);
            }
            catch
            {
                syncState.PendingResync = false;
                await this.dbContext.SaveChangesAsync(cancellationToken);
                throw;
            }
        }

        if (result is SubscriptionCatalogSyncResult.Failure failure)
        {
            this.logger.LogWarning(
                "Catalog sync message for subscription {SubscriptionId} finished with error code {ErrorCode}.",
                message.SubscriptionId,
                failure.ErrorCode);
        }
    }

    private async Task<SubscriptionCatalogSyncState> GetOrCreateSyncStateAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        SubscriptionCatalogSyncState? existingState = await this.dbContext.SubscriptionCatalogSyncStates
            .SingleOrDefaultAsync(item => item.SubscriptionId == subscriptionId, cancellationToken);

        if (existingState is not null)
        {
            return existingState;
        }

        SubscriptionCatalogSyncState createdState = new()
        {
            SubscriptionId = subscriptionId,
            SyncInProgress = false,
            PendingResync = false,
            NextScheduledSyncAtUtc = DateTime.UtcNow.Add(PeriodicCadence)
        };

        this.dbContext.SubscriptionCatalogSyncStates.Add(createdState);
        await this.dbContext.SaveChangesAsync(cancellationToken);
        return createdState;
    }
}
