namespace SquareBuddy.WebApi.Core;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SquareBuddy;
using SquareBuddy.Collections;
using SquareBuddy.Data;
using SquareBuddy.Data.Entities;
using System.Security.Claims;

public static class ConversationEndpoints
{
    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
         RouteGroupBuilder routeGroup = routeBuilder
             .MapGroup("/agents/{agentId:guid}/conversations")
             // .RequireAuthorization()
             .WithTags("Conversations");

         routeGroup.MapGet(string.Empty, GetAgentConversationsAsync);
         routeGroup.MapGet("{conversationId:guid}/messages", GetConversationMessagesAsync);
         routeGroup.MapPatch("{conversationId:guid}/messages/{messageId:guid}/read", MarkConversationMessageReadAsync);
         routeGroup.MapPatch("{conversationId:guid}/read", MarkConversationReadAsync);

        return routeBuilder;
    }

    private static async Task<Results<Ok<GetAgentConversationsResult>, NotFound>> GetAgentConversationsAsync(
        [AsParameters] GetAgentConversationsInput input,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        CancellationToken ct)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return TypedResults.NotFound();
        }

        bool hasAccess = await HasAgentAccessAsync(dbContext, input.AgentId, userId.Value, ct);
        if (!hasAccess)
        {
            return TypedResults.NotFound();
        }

        int skip = NormalizeSkip(input.Skip);
        int take = ClampTake(input.Take, 30, 100);

        CursorList<ConversationListItem> cursor = await dbContext.Conversations
            .Where(conversation => conversation.AgentId == input.AgentId)
            .OrderByDescending(conversation => conversation.LastMessageAt)
            .ThenByDescending(conversation => conversation.Id)
            .Select(conversation => new ConversationListItem(
                conversation.Id,
                conversation.ParticipantExternalId,
                conversation.Channel,
                conversation.LastMessagePreview,
                conversation.LastMessageAt,
                conversation.LastMessageRole,
                conversation.UnreadCount,
                conversation.UnreadCount > 0))
            .ToCursorListAsync(skip, take, ct);

        GetAgentConversationsResult result = new(cursor.Items.ToList(), skip, take);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<GetConversationMessagesResult>, NotFound>> GetConversationMessagesAsync(
        [AsParameters] GetConversationMessagesInput input,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        CancellationToken ct)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return TypedResults.NotFound();
        }

        bool hasAccess = await HasAgentAccessAsync(dbContext, input.AgentId, userId.Value, ct);
        if (!hasAccess)
        {
            return TypedResults.NotFound();
        }

        bool conversationExists = await dbContext.Conversations
            .AnyAsync(
                conversation =>
                    conversation.Id == input.ConversationId &&
                    conversation.AgentId == input.AgentId,
                ct);

        if (!conversationExists)
        {
            return TypedResults.NotFound();
        }

        int skip = NormalizeSkip(input.Skip);
        int take = ClampTake(input.Take, 50, 100);

        CursorList<ConversationMessageItem> cursor = await dbContext.ConversationMessages
            .Where(message =>
                message.AgentId == input.AgentId &&
                message.ConversationId == input.ConversationId)
            .OrderByDescending(message => message.OccurredAt)
            .ThenByDescending(message => message.Id)
            .Select(message => new ConversationMessageItem(
                message.Id,
                message.Role,
                message.Content,
                message.From,
                message.To,
                message.OccurredAt,
                message.IsRead,
                message.ReadAt))
            .ToCursorListAsync(skip, take, ct);

        GetConversationMessagesResult result = new(cursor.Items.ToList(), skip, take);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<MarkConversationMessageReadResult>, NotFound>> MarkConversationMessageReadAsync(
        [AsParameters] MarkConversationMessageReadInput input,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        CancellationToken ct)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return TypedResults.NotFound();
        }

        bool hasAccess = await HasAgentAccessAsync(dbContext, input.AgentId, userId.Value, ct);
        if (!hasAccess)
        {
            return TypedResults.NotFound();
        }

        Conversation? conversation = await dbContext.Conversations
            .SingleOrDefaultAsync(
                item => item.Id == input.ConversationId && item.AgentId == input.AgentId,
                ct);

        if (conversation is null)
        {
            return TypedResults.NotFound();
        }

        ConversationMessage? message = await dbContext.ConversationMessages
            .SingleOrDefaultAsync(
                item =>
                    item.Id == input.MessageId &&
                    item.ConversationId == input.ConversationId &&
                    item.AgentId == input.AgentId,
                ct);

        if (message is null)
        {
            return TypedResults.NotFound();
        }

        if (message.Role == MessageRole.Customer && !message.IsRead)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            message.IsRead = true;
            message.ReadAt = now;
            conversation.UnreadCount = Math.Max(0, conversation.UnreadCount - 1);
            await dbContext.SaveChangesAsync(ct);
        }

        MarkConversationMessageReadResult result = new(
            message.Id,
            message.IsRead,
            message.ReadAt,
            conversation.UnreadCount);

        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<MarkConversationReadResult>, NotFound>> MarkConversationReadAsync(
        [AsParameters] MarkConversationReadInput input,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        CancellationToken ct)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return TypedResults.NotFound();
        }

        bool hasAccess = await HasAgentAccessAsync(dbContext, input.AgentId, userId.Value, ct);
        if (!hasAccess)
        {
            return TypedResults.NotFound();
        }

        Conversation? conversation = await dbContext.Conversations
            .SingleOrDefaultAsync(
                item => item.Id == input.ConversationId && item.AgentId == input.AgentId,
                ct);

        if (conversation is null)
        {
            return TypedResults.NotFound();
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<ConversationMessage> unreadMessages = await dbContext.ConversationMessages
            .Where(message =>
                message.AgentId == input.AgentId &&
                message.ConversationId == input.ConversationId &&
                message.Role == MessageRole.Customer &&
                !message.IsRead)
            .ToListAsync(ct);

        foreach (ConversationMessage message in unreadMessages)
        {
            message.IsRead = true;
            message.ReadAt = now;
        }

        conversation.UnreadCount = 0;
        await dbContext.SaveChangesAsync(ct);

        MarkConversationReadResult result = new(
            conversation.Id,
            unreadMessages.Count,
            conversation.UnreadCount);

        return TypedResults.Ok(result);
    }

    private static async Task<bool> HasAgentAccessAsync(
        MainDataContext dbContext,
        Guid agentId,
        Guid userId,
        CancellationToken ct)
    {
        bool hasAccess = await (
            from agent in dbContext.Agents
            join subscriptionUser in dbContext.SubscriptionUsers
                on agent.SubscriptionId equals subscriptionUser.SubscriptionId
            where agent.Id == agentId && subscriptionUser.UserId == userId
            select agent.Id
        ).AnyAsync(ct);

        return hasAccess;
    }

    private static int NormalizeSkip(int skip)
    {
        return Math.Max(0, skip);
    }

    private static int ClampTake(int take, int defaultTake, int maxTake)
    {
        if (take <= 0)
        {
            return defaultTake;
        }

        return Math.Min(take, maxTake);
    }
}
