namespace HeyAlan.Data.Entities;

public enum CatalogSyncTriggerSource
{
    Periodic = 0,
    Webhook = 1,
    Manual = 2,
    Connect = 3
}

public class SubscriptionCatalogSyncState : IEntityWithAudit
{
    public Guid SubscriptionId { get; set; }

    public Subscription Subscription { get; set; } = null!;

    public DateTime? LastSyncedBeginTimeUtc { get; set; }

    public DateTime? NextScheduledSyncAtUtc { get; set; }

    public DateTime? LastSyncStartedAtUtc { get; set; }

    public DateTime? LastSyncCompletedAtUtc { get; set; }

    public CatalogSyncTriggerSource? LastTriggerSource { get; set; }

    public bool SyncInProgress { get; set; }

    public bool PendingResync { get; set; }

    public string? LastErrorCode { get; set; }

    public string? LastErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
