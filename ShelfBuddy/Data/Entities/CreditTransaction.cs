namespace ShelfBuddy.Data.Entities;

public enum CreditTransactionSource
{
    Subscription = 1,
    TopUp = 2,
    Refund = 3
}

public class CreditTransaction : IEntityWithId, IEntityWithAudit
{
    public Guid Id { get; set; }

    public Guid SubscriptionId { get; set; }

    public Subscription Subscription { get; set; } = null!;

    public int Amount { get; set; }

    public CreditTransactionSource Source { get; set; }

    public string? StripeEventId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
