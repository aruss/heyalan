namespace SquareBuddy.Data.Entities;

public enum SubscriptionUserRole
{
    Owner = 0,
    Member = 1,
}

/// <summary>
/// Weak entity that represents a user's membership in a subscription.
/// </summary>
public class SubscriptionUser : IEntityWithAudit
{
    public Guid SubscriptionId { get; set; }

    public Subscription Subscription { get; set; } = null!;

    public Guid UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public SubscriptionUserRole Role { get; set; } = SubscriptionUserRole.Member;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
