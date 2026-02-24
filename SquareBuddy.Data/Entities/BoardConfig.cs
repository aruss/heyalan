namespace SquareBuddy.Data.Entities;

public enum AgeGroup
{
    OneToThree = 1,
    FourToSix = 4,
}

public class Board : IEntityWithId, IEntityWithAudit
{
    public Guid Id { get; set; }

    public string Name { get; set; } 

    public ICollection<StoryRequest> Stories { get; set; } = new List<StoryRequest>();

    public ICollection<BoardConfig> Configs { get; set; } = new List<BoardConfig>();

    public Guid SubscriptionId { get; set; }

    public Subscription Subscription { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// This entity represend the configuration for a SquareBuddy device, it is the actaul toy doig the requests
/// </summary>
public class BoardConfig : IEntityWithId, IEntityWithAudit 
{    
    public Guid Id { get; set; }

    public Guid BoardId { get; set; }

    public Board Board { get; set; } = null!;

    public ICollection<StoryRequest> Stories { get; set; } = new List<StoryRequest>();
    
    public AgeGroup AgeGroup { get; init; } = AgeGroup.OneToThree;

    public string Language { get; init; } = Constants.DefaultLanguage;

    public string? Voice { get; init; } = null;

    public string? ProducerUserPrompt { get; init; } = null;

    public string? EvaluatorUserPrompt { get; init; } = null;

    public string? ProducerUserPromptCompiled { get; init; } = null;

    public string? EvaluatorUserPromptCompiled { get; init; } = null;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
