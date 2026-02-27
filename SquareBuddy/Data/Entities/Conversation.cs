namespace SquareBuddy.Data.Entities;

using SquareBuddy;

public class Conversation : IEntityWithId, IEntityWithAudit
{
    public Guid Id { get; set; }

    public Guid AgentId { get; set; }

    public Agent Agent { get; set; } = null!;

    public string ParticipantExternalId { get; set; } = string.Empty;

    public MessageChannel Channel { get; set; }

    public string? LastMessagePreview { get; set; }

    public DateTimeOffset? LastMessageAt { get; set; }

    public MessageRole? LastMessageRole { get; set; }

    public int UnreadCount { get; set; }

    public ICollection<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
