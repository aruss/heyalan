namespace HeyAlan.SquareIntegration;

using HeyAlan.Data.Entities;
using HeyAlan.Extensions;

public sealed record SubscriptionCatalogProductLocationResult(
    string LocationId,
    long? PriceOverrideAmount,
    string? PriceOverrideCurrency,
    bool IsAvailableForSale,
    bool IsSoldOut);

public sealed record SubscriptionCatalogProductResult(
    Guid SubscriptionCatalogProductId,
    string SquareItemId,
    string SquareVariationId,
    string ItemName,
    string VariationName,
    string? Description,
    string? Sku,
    long? BasePriceAmount,
    string? BasePriceCurrency,
    bool IsSellable,
    bool IsDeleted,
    DateTime? SquareUpdatedAtUtc,
    long? SquareVersion,
    IReadOnlyList<SubscriptionCatalogProductLocationResult> Locations);

public sealed record SubscriptionCatalogFreshnessResult(
    DateTime? LastSyncedBeginTimeUtc,
    DateTime? NextScheduledSyncAtUtc,
    DateTime? LastSyncStartedAtUtc,
    DateTime? LastSyncCompletedAtUtc,
    CatalogSyncTriggerSource? LastTriggerSource,
    bool SyncInProgress,
    bool PendingResync,
    string? LastErrorCode,
    string? LastErrorMessage);

public sealed record GetSubscriptionCatalogProductsInput(
    Guid SubscriptionId,
    Guid AgentId,
    string? Query,
    int Skip = Constants.SkipDefault,
    int Take = Constants.TakeDefault);

public sealed record GetSubscriptionCatalogProductsResult(
    PagedList<SubscriptionCatalogProductResult> Products,
    SubscriptionCatalogFreshnessResult Freshness);

public interface ISubscriptionCatalogReadService
{
    Task<GetSubscriptionCatalogProductsResult> GetProductsAsync(
        GetSubscriptionCatalogProductsInput input,
        CancellationToken cancellationToken = default);

    Task<SubscriptionCatalogProductResult?> GetProductByCatalogProductIdAsync(
        Guid subscriptionId,
        Guid agentId,
        Guid subscriptionCatalogProductId,
        CancellationToken cancellationToken = default);

    Task<SubscriptionCatalogProductResult?> GetProductBySquareVariationIdAsync(
        Guid subscriptionId,
        Guid agentId,
        string squareVariationId,
        CancellationToken cancellationToken = default);

    Task<SubscriptionCatalogFreshnessResult> GetFreshnessAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);
}
