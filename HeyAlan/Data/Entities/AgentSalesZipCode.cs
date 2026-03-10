namespace HeyAlan.Data.Entities;

public class AgentSalesZipCode : IEntityWithId, IEntityWithAudit
{
    public Guid Id { get; set; }

    public Guid AgentId { get; set; }

    public Guid SubscriptionId { get; set; }

    public Agent Agent { get; set; } = null!;

    public string ZipCodeNormalized { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
