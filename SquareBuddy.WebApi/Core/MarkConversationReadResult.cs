namespace SquareBuddy.WebApi.Core;

public record MarkConversationReadResult(
    Guid ConversationId,
    int MarkedCount,
    int ConversationUnreadCount);
