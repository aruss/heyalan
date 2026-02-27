namespace SquareBuddy.WebApi.Core;

public class MarkConversationReadInput
{
    public Guid AgentId { get; init; }

    public Guid ConversationId { get; init; }
}
