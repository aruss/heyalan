namespace BuyAlan.WebApi.SquareIntegration;

using BuyAlan.Data.Entities;

internal static class SubscriptionSquareCatalogProductMappings
{
    public static SubscriptionSquareCatalogProductItem ToItem(SubscriptionCatalogProduct product)
    {
        ArgumentNullException.ThrowIfNull(product);

        return new SubscriptionSquareCatalogProductItem(
            product.Id,
            product.SquareItemId,
            product.SquareVariationId,
            product.ItemName,
            product.VariationName,
            product.Description,
            product.Sku,
            product.BasePriceAmount,
            product.BasePriceCurrency,
            product.IsSellable,
            product.IsDeleted,
            product.SquareUpdatedAtUtc,
            product.Locations.Count);
    }
}
