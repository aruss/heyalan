namespace HeyAlan.Data.Entities;

public class SubscriptionCatalogProduct : IEntityWithId, IEntityWithAudit
{
    public Guid Id { get; set; }

    public Guid SubscriptionId { get; set; }

    public Subscription Subscription { get; set; } = null!;

    public string SquareItemId { get; set; } = null!;

    public string SquareVariationId { get; set; } = null!;

    public string ItemName { get; set; } = null!;

    public string VariationName { get; set; } = null!;

    public string? Description { get; set; }

    public string? Sku { get; set; }

    public long? BasePriceAmount { get; set; }

    public string? BasePriceCurrency { get; set; }

    public bool IsSellable { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? SquareUpdatedAtUtc { get; set; }

    public long? SquareVersion { get; set; }

    public string SearchText { get; set; } = String.Empty;

    public ICollection<SubscriptionCatalogProductLocation> Locations { get; set; } = new List<SubscriptionCatalogProductLocation>();

    public ICollection<AgentCatalogProductAccess> AgentProductAccesses { get; set; } = new List<AgentCatalogProductAccess>();

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
