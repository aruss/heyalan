namespace SquareBuddy.Data.Entities;

/// <summary>
/// Has a list of Users and SquareBuddys associated with it and membership tier info syched from the payment provider
/// </summary>
public class Subscription : IEntityWithId, IEntityWithAudit
{
    public Guid Id { get; set; }

    public ICollection<Agent> Agents { get; set; } = new List<Agent>();

    public ICollection<SubscriptionUser> SubscriptionUsers { get; set; } = new List<SubscriptionUser>();

    public string? StripeCustomerId { get; set; }

    public string? StripeSubscriptionId { get; set; }

    public int SubscriptionCreditBalance { get; set; }

    public int TopUpCreditBalance { get; set; }

    public string? StripePriceId  { get; set; }

    public DateTime? CurrentPeriodEnd { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
