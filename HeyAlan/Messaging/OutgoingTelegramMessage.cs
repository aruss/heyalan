namespace HeyAlan.Messaging;

public record OutgoingTelegramMessage
{
    public Guid SubscriptionId { get; init; }

    public Guid AgentId { get; init; }

    public string Content { get; init; } = string.Empty;

    public string To { get; init; } = string.Empty;
}
