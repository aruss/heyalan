namespace SquareBuddy.WebApi.Core;

public class MarkConversationMessageReadInput
{
    public Guid AgentId { get; init; }

    public Guid ConversationId { get; init; }

    public Guid MessageId { get; init; }
}
