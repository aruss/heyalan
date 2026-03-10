namespace HeyAlan.WebApi.SquareIntegration;

public sealed record GetSubscriptionSquareCatalogSyncStateResult(
    Guid SubscriptionId,
    string Status,
    string? LastTriggerSource,
    DateTime? LastSyncedBeginTimeUtc,
    DateTime? NextScheduledSyncAtUtc,
    DateTime? LastSyncStartedAtUtc,
    DateTime? LastSyncCompletedAtUtc,
    bool SyncInProgress,
    bool PendingResync,
    string? LastErrorCode,
    string? LastErrorMessage,
    int CachedProductCount,
    int SellableProductCount,
    int DeletedProductCount);
