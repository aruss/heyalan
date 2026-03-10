namespace HeyAlan.SquareIntegration;

using HeyAlan.Data.Entities;

public sealed record SubscriptionCatalogSyncRequestInput(
    Guid SubscriptionId,
    CatalogSyncTriggerSource TriggerSource,
    bool ForceFullSync = false);

public sealed record SubscriptionCatalogSyncRequestResult(bool Enqueued);

public interface ISubscriptionCatalogSyncTriggerService
{
    Task<SubscriptionCatalogSyncRequestResult> RequestSyncAsync(
        SubscriptionCatalogSyncRequestInput input,
        CancellationToken cancellationToken = default);

    Task<int> EnqueueDuePeriodicSyncsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
