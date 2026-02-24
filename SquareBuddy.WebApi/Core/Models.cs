namespace SquareBuddy.WebApi.Core;

using System.Text.Json.Serialization;

// IMPORTANT: Property initializers are required for EF Core deserialization.

public record Figurine
{
    public string Id { get; init; } = string.Empty;
    public Point Location { get; init; } = null!;
}

public record Point
{
    public double X { get; init; }
    public double Y { get; init; }
}

public record StreamStoryInput
{
    public Guid BoardId { get; init; }

    public Figurine[] Figurines { get; init; } = [];
}

public record Story
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}

public record SceneGraph
{
    public List<SceneCluster> Clusters { get; init; } = [];
    public List<SceneRelation> Relations { get; init; } = [];
}

public record SceneCluster
{
    public int Id { get; init; }
    public string Position { get; init; } = string.Empty;
    public string Contents { get; init; } = string.Empty;
    public Point Centroid { get; init; } = null!;
}

public record SceneRelation
{
    public int FromClusterId { get; init; }
    public int ToClusterId { get; init; }
    public double DistanceUnits { get; init; }
    public string SemanticDistance { get; init; } = string.Empty;
}

