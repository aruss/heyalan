namespace BuyAlan.Tests.SquareIntegration;

using BuyAlan.Data.Entities;
using BuyAlan.WebApi.SquareIntegration;

public class SubscriptionSquareCatalogProductMappingsTests
{
    [Fact]
    public void ToItem_WhenProductHasDescription_MapsDescriptionToContract()
    {
        SubscriptionCatalogProduct product = CreateProduct("Fresh roast");

        SubscriptionSquareCatalogProductItem result = SubscriptionSquareCatalogProductMappings.ToItem(product);

        Assert.Equal("Fresh roast", result.Description);
    }

    [Fact]
    public void ToItem_WhenProductDescriptionIsNull_MapsNullDescriptionToContract()
    {
        SubscriptionCatalogProduct product = CreateProduct(null);

        SubscriptionSquareCatalogProductItem result = SubscriptionSquareCatalogProductMappings.ToItem(product);

        Assert.Null(result.Description);
    }

    private static SubscriptionCatalogProduct CreateProduct(string? description)
    {
        return new SubscriptionCatalogProduct
        {
            Id = Guid.NewGuid(),
            SubscriptionId = Guid.NewGuid(),
            SquareItemId = "item-1",
            SquareVariationId = "var-1",
            ItemName = "Coffee",
            VariationName = "Large",
            Description = description,
            Sku = "COF-L",
            BasePriceAmount = 1299,
            BasePriceCurrency = "USD",
            IsSellable = true,
            IsDeleted = false,
            Locations =
            [
                new SubscriptionCatalogProductLocation
                {
                    SubscriptionId = Guid.NewGuid(),
                    LocationId = "loc-1"
                }
            ]
        };
    }
}
