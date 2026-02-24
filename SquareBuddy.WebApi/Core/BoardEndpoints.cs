namespace SquareBuddy.WebApi.Core;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SquareBuddy.Data;
using SquareBuddy.Data.Entities;
using SquareBuddy.Shared.Collections;
using System.Security.Claims;

public record UpdateBoardInput(
    string Name,
    AgeGroup AgeGroup,
    string? Voice,
    string Language,
    string? ProducerUserPrompt,
    string? EvaluatorUserPrompt
);

public record BoardListItem(
    Guid Id,
    string Name,
    AgeGroup AgeGroup,
    string? Voice,
    string Language,
    string? ProducerUserPrompt,
    string? EvaluatorUserPrompt
);

public record UpdateBoardResult(
    Guid Id,
    string Name,
    AgeGroup AgeGroup,
    string? Voice,
    string Language,
    string? ProducerUserPrompt,
    string? EvaluatorUserPrompt);

// https://www.youtube.com/watch?v=sW_AcN-nD0Y
public static class BoardEndpoints
{
    public static IEndpointRouteBuilder MapBoardEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        RouteGroupBuilder routeGroup = routeBuilder
            .MapGroup("/boards")
            .WithTags("Board");

        routeGroup
            .MapGet("", GetBoards)
            .WithName("GetBoards")
            .Produces<CursorList<BoardListItem>>(StatusCodes.Status200OK)
            .RequireAuthorization();

        routeGroup
            .MapPost("{boardId}", UpdateBoard)
            .WithName("UpdateBoard")
            .RequireAuthorization();

        return routeBuilder;
    }

    private static async Task<Results<Ok<CursorList<BoardListItem>>, UnauthorizedHttpResult>> GetBoards(
            ClaimsPrincipal user,
            MainDataContext dbContext,
            CancellationToken ct)
    {
        Guid? userId = user.GetUserId();

        if (!userId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        // get all boards with the latest config 
        var result = await dbContext.Boards
            .AsNoTracking()
            .Where(b => b.Subscription.SubscriptionUsers.Any(su => su.UserId == userId.Value))
            .SelectAsBoardListItem()
            .ToCursorListAsync(0, 50, ct);

        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<UpdateBoardResult>, NotFound, UnauthorizedHttpResult>> UpdateBoard(
            [FromRoute] Guid boardId,
            [FromBody] UpdateBoardInput input,
            ClaimsPrincipal user,
            MainDataContext dbContext,
            CancellationToken ct)
    {
        var userId = user.GetUserId();

        if (!userId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var board = await dbContext.Boards
                .Include(b => b.Subscription.SubscriptionUsers)
                .FirstOrDefaultAsync(b => b.Id == boardId &&
                     b.Subscription.SubscriptionUsers.Any(su => su.UserId == userId), ct);

            if (board == null)
            {
                throw new KeyNotFoundException($"Board {boardId} not found or access denied.");
            }

            // 2. Fetch Latest Config (NoTracking is fine as we don't modify it, only read)
            var currentConfig = await dbContext.BoardConfigs
                .AsNoTracking()
                .Where(c => c.BoardId == boardId)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync(ct);

            // 3. Update Identity (Mutable)
            bool identityChanged = false;
            if (board.Name != input.Name)
            {
                board.Name = input.Name;
                identityChanged = true;
            }

            // 4. Update Configuration (Immutable / Append-Only)
            // Detect functional changes to avoid spamming the history table
            bool configChanged = currentConfig is null ||
                currentConfig.AgeGroup != input.AgeGroup ||
                currentConfig.Voice != input.Voice ||
                currentConfig.Language != input.Language ||
                currentConfig.ProducerUserPrompt != input.ProducerUserPrompt ||
                currentConfig.EvaluatorUserPrompt != input.EvaluatorUserPrompt;

            BoardConfig? newConfig = null;

            if (configChanged)
            {
                newConfig = new BoardConfig
                {
                    BoardId = boardId,
                    AgeGroup = input.AgeGroup,
                    Voice = input.Voice,
                    Language = input.Language,
                    ProducerUserPrompt = input.ProducerUserPrompt,
                    EvaluatorUserPrompt = input.EvaluatorUserPrompt
                    // CreatedAt/Id handled by DB context hooks
                };

                dbContext.BoardConfigs.Add(newConfig);
            }

            // 5. Commit Transaction
            if (identityChanged || configChanged)
            {
                await dbContext.SaveChangesAsync(ct);
            }

            // 6. Return Updated Model
            // If no config changed, use the current one. If no current one exists, use default.
            var configToReturn = newConfig ?? currentConfig;

            // Use mapping method 
            var result = new UpdateBoardResult(
                board.Id,
                board.Name,
                configToReturn?.AgeGroup ?? AgeGroup.OneToThree,
                configToReturn?.Voice,
                configToReturn?.Language ?? Constants.DefaultLanguage,
                configToReturn?.ProducerUserPrompt,
                configToReturn?.EvaluatorUserPrompt
            );

            return TypedResults.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    public static IQueryable<BoardListItem> SelectAsBoardListItem(this IQueryable<Board> query)
    {
        // 1. Fetch Board + Latest Config (Intermediate Projection)
        // 2. Flatten into DTO (Final Projection)
        return query
            .Select(b => new
            {
                Board = b,
                // Optimized: Subquery for latest config
                Config = b.Configs.OrderByDescending(c => c.CreatedAt).FirstOrDefault()
            })
            .Select(x => new BoardListItem(
                x.Board.Id,
                x.Board.Name,
                x.Config != null ? x.Config.AgeGroup : AgeGroup.OneToThree,
                x.Config != null ? x.Config.Voice : null,
                x.Config != null ? x.Config.Language : Constants.DefaultLanguage,
                x.Config != null ? x.Config.ProducerUserPrompt : null,
                x.Config != null ? x.Config.EvaluatorUserPrompt : null
            ));
    }
}
