namespace HeyAlan.Data.Entities;

public enum SubscriptionOnboardingStatus
{
    Draft = 0,
    InProgress = 1,
    Completed = 2
}

public class SubscriptionOnboardingState : IEntityWithAudit
{
    public Guid SubscriptionId { get; set; }

    public Subscription Subscription { get; set; } = null!;

    public SubscriptionOnboardingStatus Status { get; set; } = SubscriptionOnboardingStatus.Draft;

    public string CurrentStep { get; set; } = "square_connect";

    public Guid? PrimaryAgentId { get; set; }

    public Agent? PrimaryAgent { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
