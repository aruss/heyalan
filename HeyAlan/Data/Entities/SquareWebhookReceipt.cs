namespace HeyAlan.Data.Entities;

public class SquareWebhookReceipt : IEntityWithId, IEntityWithAudit
{
    public Guid Id { get; set; }

    public Guid SubscriptionId { get; set; }

    public Subscription Subscription { get; set; } = null!;

    public string EventId { get; set; } = null!;

    public string EventType { get; set; } = null!;

    public string MerchantId { get; set; } = null!;

    public DateTime ReceivedAtUtc { get; set; }

    public bool IsProcessed { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
