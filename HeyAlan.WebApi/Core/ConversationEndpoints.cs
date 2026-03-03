namespace HeyAlan.WebApi.Core;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HeyAlan;
using HeyAlan.Collections;
using HeyAlan.Data;
using HeyAlan.Data.Entities;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

public static class ConversationEndpoints
{
    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
         RouteGroupBuilder routeGroup = routeBuilder
             .MapGroup("/agents/{agentId:guid}/conversations")
             .RequireAuthorization("OnboardedOnly")
             .WithTags("Conversations");

         routeGroup
             .MapGet(string.Empty, GetAgentConversationsAsync)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError);

         routeGroup
             .MapGet("{conversationId:guid}/messages", GetConversationMessagesAsync)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError);

         routeGroup
             .MapPatch("{conversationId:guid}/messages/{messageId:guid}/read", MarkConversationMessageReadAsync)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError);

         routeGroup
             .MapPatch("{conversationId:guid}/read", MarkConversationReadAsync)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError);

        return routeBuilder;
    }

    private static async Task<Results<Ok<GetAgentConversationsResult>, NotFound, ProblemHttpResult>> GetAgentConversationsAsync(
        [FromRoute] Guid agentId,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        CancellationToken ct,
        [FromQuery]
        [Range(Constants.SkipMin, Constants.SkipMax)]
        int skip = Constants.SkipDefault,
        [FromQuery]
        [Range(Constants.TakeMin, Constants.TakeMax)]
        int take = Constants.TakeDefault)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return TypedResults.NotFound();
        }

        bool hasAccess = await HasAgentAccessAsync(dbContext, agentId, userId.Value, ct);
        if (!hasAccess)
        {
            return TypedResults.NotFound();
        }

        CursorList<ConversationListItem> cursor = await dbContext.Conversations
            .Where(conversation => conversation.AgentId == agentId)
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

    private static async Task<Results<Ok<GetConversationMessagesResult>, NotFound, ProblemHttpResult>> GetConversationMessagesAsync(
        [FromRoute] Guid agentId,
        [FromRoute] Guid conversationId,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        CancellationToken ct,
        [FromQuery]
        [Range(Constants.SkipMin, Constants.SkipMax)]
        int skip = Constants.SkipDefault,
        [FromQuery]
        [Range(Constants.TakeMin, Constants.TakeMax)]
        int take = Constants.TakeDefault)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return TypedResults.NotFound();
        }

        bool hasAccess = await HasAgentAccessAsync(dbContext, agentId, userId.Value, ct);
        if (!hasAccess)
        {
            return TypedResults.NotFound();
        }

        bool conversationExists = await dbContext.Conversations
            .AnyAsync(
                conversation =>
                    conversation.Id == conversationId &&
                    conversation.AgentId == agentId,
                ct);

        if (!conversationExists)
        {
            return TypedResults.NotFound();
        }

        CursorList<ConversationMessageItem> cursor = await dbContext.ConversationMessages
            .Where(message =>
                message.AgentId == agentId &&
                message.ConversationId == conversationId)
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

    private static async Task<Results<Ok<MarkConversationMessageReadResult>, NotFound, ProblemHttpResult>> MarkConversationMessageReadAsync(
        [FromRoute] Guid agentId,
        [FromRoute] Guid conversationId,
        [FromRoute] Guid messageId,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        CancellationToken ct)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return TypedResults.NotFound();
        }

        bool hasAccess = await HasAgentAccessAsync(dbContext, agentId, userId.Value, ct);
        if (!hasAccess)
        {
            return TypedResults.NotFound();
        }

        Conversation? conversation = await dbContext.Conversations
            .SingleOrDefaultAsync(
                item => item.Id == conversationId && item.AgentId == agentId,
                ct);

        if (conversation is null)
        {
            return TypedResults.NotFound();
        }

        ConversationMessage? message = await dbContext.ConversationMessages
            .SingleOrDefaultAsync(
                item =>
                    item.Id == messageId &&
                    item.ConversationId == conversationId &&
                    item.AgentId == agentId,
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

    private static async Task<Results<Ok<MarkConversationReadResult>, NotFound, ProblemHttpResult>> MarkConversationReadAsync(
        [FromRoute] Guid agentId,
        [FromRoute] Guid conversationId,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        CancellationToken ct)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return TypedResults.NotFound();
        }

        bool hasAccess = await HasAgentAccessAsync(dbContext, agentId, userId.Value, ct);
        if (!hasAccess)
        {
            return TypedResults.NotFound();
        }

        Conversation? conversation = await dbContext.Conversations
            .SingleOrDefaultAsync(
                item => item.Id == conversationId && item.AgentId == agentId,
                ct);

        if (conversation is null)
        {
            return TypedResults.NotFound();
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<ConversationMessage> unreadMessages = await dbContext.ConversationMessages
            .Where(message =>
                message.AgentId == agentId &&
                message.ConversationId == conversationId &&
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

}
