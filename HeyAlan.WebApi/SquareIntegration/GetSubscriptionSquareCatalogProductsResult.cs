namespace HeyAlan.WebApi.SquareIntegration;

using HeyAlan.Extensions;

public sealed record SubscriptionSquareCatalogProductItem(
    Guid SubscriptionCatalogProductId,
    string SquareItemId,
    string SquareVariationId,
    string ItemName,
    string VariationName,
    string? Sku,
    long? BasePriceAmount,
    string? BasePriceCurrency,
    bool IsSellable,
    bool IsDeleted,
    DateTime? SquareUpdatedAtUtc,
    int LocationCount);

public sealed record GetSubscriptionSquareCatalogProductsResult : PagedList<SubscriptionSquareCatalogProductItem>
{
    public GetSubscriptionSquareCatalogProductsResult(
        IReadOnlyCollection<SubscriptionSquareCatalogProductItem> items,
        int total,
        int skip,
        int take)
        : base(items, total, skip, take)
    {
    }

    public GetSubscriptionSquareCatalogProductsResult()
    {
    }
}
