namespace ShelfBuddy.Data.Entities;

using ShelfBuddy;

public class ConversationMessage : IEntityWithId, IEntityWithAudit
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public Conversation Conversation { get; set; } = null!;

    public Guid AgentId { get; set; }

    public Agent Agent { get; set; } = null!;

    public MessageRole Role { get; set; }

    public string Content { get; set; } = string.Empty;

    public string From { get; set; } = string.Empty;

    public string To { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }

    public bool IsRead { get; set; }

    public DateTimeOffset? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
