namespace SquareBuddy.Data.Entities;

/// <summary>
/// This is a story request made by a SquareBuddy device for generating a story
/// </summary>
public class StoryRequest : IEntityWithId, IEntityWithAudit
{
    public Guid Id { get; set; }

    public Guid ConfigId { get; set; }

    public BoardConfig Config { get; set; } = null!;

    // Denormalized direct link to the board for easier querying
    public Guid BoardId { get; set; }

    public Board Board { get; set; } = null!;

    public StoryRequestStatus Status { get; set; }

    public required string Input { get; set; }

    public required string SceneGraph { get; set; }

    public required string Title { get; set; }

    public required string CreatedWith { get; set; }

    public int Duration { get; set; }

    public ICollection<StoryRequestChunk> Chunks { get; set; } = new List<StoryRequestChunk>();

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
