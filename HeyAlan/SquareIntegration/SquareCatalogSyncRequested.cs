namespace HeyAlan.SquareIntegration;

using HeyAlan.Data.Entities;

public sealed record SquareCatalogSyncRequested(
    Guid SubscriptionId,
    CatalogSyncTriggerSource TriggerSource,
    bool ForceFullSync = false);
