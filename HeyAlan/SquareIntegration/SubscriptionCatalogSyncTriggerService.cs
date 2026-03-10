namespace HeyAlan.SquareIntegration;

using HeyAlan.Data;
using HeyAlan.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public sealed class SubscriptionCatalogSyncTriggerService : ISubscriptionCatalogSyncTriggerService
{
    private static readonly TimeSpan PeriodicCadence = TimeSpan.FromMinutes(15);

    private readonly MainDataContext dbContext;
    private readonly ISubscriptionCatalogSyncMessagePublisher messagePublisher;
    private readonly ILogger<SubscriptionCatalogSyncTriggerService> logger;

    public SubscriptionCatalogSyncTriggerService(
        MainDataContext dbContext,
        ISubscriptionCatalogSyncMessagePublisher messagePublisher,
        ILogger<SubscriptionCatalogSyncTriggerService> logger)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SubscriptionCatalogSyncRequestResult> RequestSyncAsync(
        SubscriptionCatalogSyncRequestInput input,
        CancellationToken cancellationToken = default)
    {
        SubscriptionCatalogSyncState syncState = await this.GetOrCreateSyncStateAsync(input.SubscriptionId, cancellationToken);
        DateTime utcNow = DateTime.UtcNow;

        ApplyScheduleMutation(syncState, input.TriggerSource, utcNow);

        bool shouldEnqueue = !syncState.SyncInProgress && !syncState.PendingResync;
        if (syncState.SyncInProgress)
        {
            syncState.PendingResync = true;
        }
        else if (shouldEnqueue)
        {
            syncState.PendingResync = true;
        }

        await this.dbContext.SaveChangesAsync(cancellationToken);

        if (!shouldEnqueue)
        {
            return new SubscriptionCatalogSyncRequestResult(false);
        }

        SquareCatalogSyncRequested message = new(
            input.SubscriptionId,
            input.TriggerSource,
            input.ForceFullSync);

        try
        {
            await this.messagePublisher.PublishAsync(message, cancellationToken);
            this.logger.LogInformation(
                "Enqueued Square catalog sync for subscription {SubscriptionId} with trigger {TriggerSource}.",
                input.SubscriptionId,
                input.TriggerSource);
            return new SubscriptionCatalogSyncRequestResult(true);
        }
        catch
        {
            syncState.PendingResync = false;
            await this.dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task<int> EnqueueDuePeriodicSyncsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        List<Guid> dueSubscriptionIds = await (
            from connection in this.dbContext.SubscriptionSquareConnections
            join syncState in this.dbContext.SubscriptionCatalogSyncStates
                on connection.SubscriptionId equals syncState.SubscriptionId into syncStateGroup
            from syncState in syncStateGroup.DefaultIfEmpty()
            where connection.DisconnectedAtUtc == null &&
                  (syncState == null ||
                   syncState.NextScheduledSyncAtUtc == null ||
                   syncState.NextScheduledSyncAtUtc <= utcNow)
            orderby connection.SubscriptionId
            select connection.SubscriptionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        int enqueuedCount = 0;
        foreach (Guid subscriptionId in dueSubscriptionIds)
        {
            SubscriptionCatalogSyncRequestResult result = await this.RequestSyncAsync(
                new SubscriptionCatalogSyncRequestInput(
                    subscriptionId,
                    CatalogSyncTriggerSource.Periodic),
                cancellationToken);

            if (result.Enqueued)
            {
                enqueuedCount++;
            }
        }

        return enqueuedCount;
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

    private static void ApplyScheduleMutation(
        SubscriptionCatalogSyncState syncState,
        CatalogSyncTriggerSource triggerSource,
        DateTime utcNow)
    {
        if (triggerSource == CatalogSyncTriggerSource.Webhook)
        {
            syncState.NextScheduledSyncAtUtc = utcNow.Add(PeriodicCadence);
            return;
        }

        if (triggerSource == CatalogSyncTriggerSource.Connect &&
            syncState.NextScheduledSyncAtUtc is null)
        {
            syncState.NextScheduledSyncAtUtc = utcNow.Add(PeriodicCadence);
        }
    }
}
