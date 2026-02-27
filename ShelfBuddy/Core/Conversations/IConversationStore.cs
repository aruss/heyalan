namespace ShelfBuddy.Core.Conversations;

using ShelfBuddy.Consumers;

public interface IConversationStore
{
    Task UpsertIncomingMessageAsync(IncomingMessage message, CancellationToken ct);

    Task AppendOutgoingTelegramMessageAsync(
        OutgoingTelegramMessage message,
        DateTimeOffset occurredAt,
        CancellationToken ct);
}
