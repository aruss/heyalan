namespace HeyAlan.WebApi.Core;

public record MarkConversationMessageReadResult(
    Guid MessageId,
    bool IsRead,
    DateTimeOffset? ReadAt,
    int ConversationUnreadCount);
