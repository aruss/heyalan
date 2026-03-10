namespace HeyAlan.SquareIntegration;

using HeyAlan.Data.Entities;

public sealed record SubscriptionCatalogSyncInput(
    Guid SubscriptionId,
    CatalogSyncTriggerSource TriggerSource,
    bool ForceFullSync = false);

public abstract record SubscriptionCatalogSyncResult
{
    public sealed record Success(
        int ProductCount,
        int LocationCount,
        DateTime SyncStartedAtUtc,
        DateTime SyncCompletedAtUtc,
        bool WasFullSync) : SubscriptionCatalogSyncResult;

    public sealed record Failure(string ErrorCode) : SubscriptionCatalogSyncResult;
}

public interface ISubscriptionCatalogSyncService
{
    Task<SubscriptionCatalogSyncResult> SyncAsync(
        SubscriptionCatalogSyncInput input,
        CancellationToken cancellationToken = default);
}
