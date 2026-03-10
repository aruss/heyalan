namespace HeyAlan.Data.Entities;

public class AgentCatalogProductAccess : IEntityWithId, IEntityWithAudit
{
    public Guid Id { get; set; }

    public Guid AgentId { get; set; }

    public Guid SubscriptionId { get; set; }

    public Agent Agent { get; set; } = null!;

    public Guid SubscriptionCatalogProductId { get; set; }

    public SubscriptionCatalogProduct SubscriptionCatalogProduct { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
