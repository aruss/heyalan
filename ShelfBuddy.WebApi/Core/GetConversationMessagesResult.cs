namespace ShelfBuddy.WebApi.Core;

using ShelfBuddy;
using ShelfBuddy.Collections;

public record ConversationMessageItem(
    Guid MessageId,
    MessageRole Role,
    string Content,
    string From,
    string To,
    DateTimeOffset OccurredAt,
    bool IsRead,
    DateTimeOffset? ReadAt);

public record GetConversationMessagesResult : CursorList<ConversationMessageItem>
{
    public GetConversationMessagesResult(IReadOnlyCollection<ConversationMessageItem> items, int skip, int take)
        : base(items, skip, take)
    {
    }

    public GetConversationMessagesResult()
    {
    }
}
