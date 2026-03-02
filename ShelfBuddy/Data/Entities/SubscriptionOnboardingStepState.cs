namespace ShelfBuddy.Data.Entities;

public class SubscriptionOnboardingStepState : IEntityWithAudit
{
    public Guid SubscriptionId { get; set; }

    public Subscription Subscription { get; set; } = null!;

    public string Step { get; set; } = string.Empty;

    public string Status { get; set; } = "not_started";

    public DateTime? CompletedAt { get; set; }

    public DateTime? SkippedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
