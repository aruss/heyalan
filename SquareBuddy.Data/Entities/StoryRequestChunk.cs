namespace SquareBuddy.Data.Entities;

/// <summary>
/// Durable sentence-level output persisted during story streaming.
/// </summary>
public class StoryRequestChunk : IEntityWithId, IEntityWithAudit
{
    public Guid Id { get; set; }

    public Guid StoryRequestId { get; set; }

    public StoryRequest StoryRequest { get; set; } = null!;

    public int Sequence { get; set; }

    public required string Text { get; set; }

    public required string AudioObjectKey { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
