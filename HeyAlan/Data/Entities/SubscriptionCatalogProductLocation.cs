namespace HeyAlan.Data.Entities;

public class SubscriptionCatalogProductLocation : IEntityWithId, IEntityWithAudit
{
    public Guid Id { get; set; }

    public Guid SubscriptionId { get; set; }

    public Subscription Subscription { get; set; } = null!;

    public Guid SubscriptionCatalogProductId { get; set; }

    public SubscriptionCatalogProduct SubscriptionCatalogProduct { get; set; } = null!;

    public string SquareVariationId { get; set; } = null!;

    public string LocationId { get; set; } = null!;

    public long? PriceOverrideAmount { get; set; }

    public string? PriceOverrideCurrency { get; set; }

    public bool IsAvailableForSale { get; set; }

    public bool IsSoldOut { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
