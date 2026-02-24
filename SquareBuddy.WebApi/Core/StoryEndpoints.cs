namespace SquareBuddy.WebApi.Core;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using SquareBuddy.Configuration;
using SquareBuddy.Data;
using SquareBuddy.Data.Entities;
using SquareBuddy.Shared.Collections;
using System.Linq;
using System.Security.Claims;

// https://www.youtube.com/watch?v=sW_AcN-nD0Y

public record StoryListItem(
    Guid Id,
    string Title,
    StoryRequestStatus Status,
    string CreatedWith,
    int Duration,
    DateTime CreatedAt);

public record CreateStoryResult
{
    public Guid RequestId { get; init; }
    public StoryRequestStatus Status { get; init; }
}

public record GetStoryResult(
    Guid Id,
    string Title,
    StoryRequestStatus Status,
    string CreatedWith,
    int Duration,
    DateTime CreatedAt);

public record GetStoryMetricsResult(
    int TotalPlaytimeSeconds,
    int WeekPlaytimeSeconds,
    int StoriesCreatedCount,
    DateTime? LastCreatedStoryAt);

public static class StoryEndpoints
{
    public static IEndpointRouteBuilder MapStoryEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        var routeGroup = routeBuilder
            .MapGroup("/stories")
            .WithTags("Story")
            .RequireAuthorization();

        // Query Endpoints
        routeGroup.MapGet("", GetStoriesAsync)
             .WithName("GetStories")
             .Produces<CursorList<StoryListItem>>();

        routeGroup.MapGet("/metrics", GetStoryMetricsAsync)
             .WithName("GetStoryMetrics")
             .Produces<GetStoryMetricsResult>();

        routeGroup.MapGet("/{storyId}", GetStoryAsync)
             .WithName("GetStory")
             .Produces<GetStoryResult>()
             .Produces(StatusCodes.Status404NotFound);

        // Streaming Endpoints
        routeGroup.MapGet("/{storyId}/_stream", GetStoryAudioStreamAsync)
             .WithName("GetStoryStream")
             .Produces(StatusCodes.Status200OK); // Returns audio/mpeg

        routeGroup.MapPost("/_stream", GenerateStoryStreamAsync)
             .WithName("GenerateStoryStream")
             .Produces(StatusCodes.Status200OK);

        return routeBuilder;
    }

    private static async Task<Results<Ok<CursorList<StoryListItem>>, UnauthorizedHttpResult>> GetStoriesAsync(
        ClaimsPrincipal user,
        MainDataContext dbContext,
        CancellationToken ct)
    {
        Guid? userId = user.GetUserId();

        if (!userId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        // Optimized Query: Filter first, then project
        var query = dbContext.StoryRequests.AsNoTracking()
            .Where(sr => sr.Board.Subscription.SubscriptionUsers.Any(su => su.UserId == userId) && sr.Duration > 0)
            .OrderByDescending(sr => sr.CreatedAt);

        var result = await query
            .SelectAsStoryListItem()
            .ToCursorListAsync(0, 50, ct);

        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<GetStoryResult>, NotFound, UnauthorizedHttpResult>> GetStoryAsync(
        Guid storyId,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        CancellationToken ct)
    {
        Guid? userId = user.GetUserId();

        if (!userId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        var story = await dbContext.StoryRequests.AsNoTracking()
            .Where(sr => sr.Id == storyId && sr.Board.Subscription.SubscriptionUsers.Any(su => su.UserId == userId))
            .SelectAsGetStoryResult()
            .FirstOrDefaultAsync(ct);

        return story is null ? TypedResults.NotFound() : TypedResults.Ok(story);
    }

    private static async Task<Results<Ok<GetStoryMetricsResult>, UnauthorizedHttpResult>> GetStoryMetricsAsync(
        ClaimsPrincipal user,
        MainDataContext dbContext,
        CancellationToken ct)
    {
        Guid? userId = user.GetUserId();

        if (!userId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        DateTime nowUtc = DateTime.UtcNow;
        DateTime weekCutoff = nowUtc.AddDays(-7);

        IQueryable<StoryRequest> scopedStories = dbContext.StoryRequests
            .AsNoTracking()
            .Where(sr => sr.Board.Subscription.SubscriptionUsers.Any(su => su.UserId == userId.Value));

        IQueryable<StoryRequest> playableStories = scopedStories
            .Where(sr =>
                sr.Status == StoryRequestStatus.Completed ||
                (sr.Status == StoryRequestStatus.Canceled && sr.Duration > 0));

        int totalPlaytimeSeconds = await playableStories.SumAsync(sr => (int?)sr.Duration, ct) ?? 0;
        int weekPlaytimeSeconds = await playableStories
            .Where(sr => sr.CreatedAt >= weekCutoff)
            .SumAsync(sr => (int?)sr.Duration, ct) ?? 0;
        int storiesCreatedCount = await playableStories.CountAsync(ct);
        DateTime? lastCreatedStoryAt = await scopedStories.MaxAsync(sr => (DateTime?)sr.CreatedAt, ct);

        GetStoryMetricsResult result = new(
            Math.Max(0, totalPlaytimeSeconds),
            Math.Max(0, weekPlaytimeSeconds),
            storiesCreatedCount,
            lastCreatedStoryAt);

        return TypedResults.Ok(result);
    }


    private static async Task GetStoryAudioStreamAsync(
        Guid storyId,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        HttpContext context,
        IMinioClient minioClient,
        MinioOptions minioOptions,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        if (!userId.HasValue)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // 1. Verify Access
        // Use Select(1) for efficient existence check (avoids fetching columns)
        bool exists = await dbContext.StoryRequests.AsNoTracking()
            .Where(sr => sr.Id == storyId && sr.Board.Subscription.SubscriptionUsers.Any(su => su.UserId == userId))
            .Select(x => 1)
            .AnyAsync(ct);

        if (!exists)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        // 2. Fetch Chunk Keys (Lightweight)
        var chunkKeys = await dbContext.StoryRequestChunks.AsNoTracking()
            .Where(c => c.StoryRequestId == storyId)
            .OrderBy(c => c.Sequence)
            .Select(c => c.AudioObjectKey)
            .ToListAsync(ct);

        // 3. Prepare Response
        context.Response.ContentType = "audio/mpeg";
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        // Do not disable buffering unless you are sure the client handles chunked transfer well with MP3.
        // context.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering(); 

        // 4. Stream Chunks Sequentially
        foreach (var key in chunkKeys)
        {
            if (ct.IsCancellationRequested) break;

            // Define the callback to copy directly to the response stream.
            // Note: If MinIO SDK forces Sync callback, we must pump carefully. 
            // Most modern MinIO SDKs support Async callbacks or return a stream.
            // Assuming standard MinIO SDK callback pattern:
            var args = new GetObjectArgs()
                .WithBucket(minioOptions.Bucket)
                .WithObject(key)
                .WithCallbackStream((stream) =>
                {
                    // WARNING: If this callback is executed synchronously by MinIO,
                    // we CANNOT use stream.CopyToAsync(context.Response.Body) directly 
                    // if it doesn't wait.
                    // Ideally, we copy simply:
                    stream.CopyTo(context.Response.Body);
                });

            try
            {
                await minioClient.GetObjectAsync(args, ct);
                // Flush after every chunk to ensure smooth playback
                await context.Response.Body.FlushAsync(ct);
            }
            catch (Exception)
            {
                // Log error, but we cannot change status code now.
                // Stop streaming to prevent corrupt audio.
                break;
            }
        }
    }

    private static async Task GenerateStoryStreamAsync(
        [FromBody] StreamStoryInput input,
        StoryStreamingService streamingService,
        ISceneGraphGenerator sceneGraphGenerator,
        MainDataContext dbContext,
        HttpContext context,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        // Headers must be set before writing to body
        context.Response.ContentType = "audio/mpeg";
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        try
        {
            logger.LogDebug("StreamStory: Init BoardId: {BoardId}", input.BoardId);

            var boardConfig = await  dbContext.BoardConfigs
                .AsNoTracking()
                .Where(c => c.BoardId == input.BoardId)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            var sceneGraph = sceneGraphGenerator.GenerateSceneGraph(input.Figurines, 6, 6);

            // Pass the Response Body directly to the service to write audio chunks
            await streamingService.ProcessStoryRequestAsync(
                input,
                sceneGraph,
                boardConfig,
                context.Response.Body,
                ct);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("StreamStory: Client disconnected.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "StreamStory: Failure");
            // Cannot return error code here as headers are likely sent.
        }
    }

    public static IQueryable<StoryListItem> SelectAsStoryListItem(this IQueryable<StoryRequest> query)
    {
        return query
            .Select(x => new StoryListItem(
                 x.Id,
                 x.Title,
                 x.Status,
                 x.CreatedWith,
                 x.Duration,
                 x.CreatedAt
            ));
    }

    public static IQueryable<GetStoryResult> SelectAsGetStoryResult(this IQueryable<StoryRequest> query)
    {
        return query
            .Select(x => new GetStoryResult(
                 x.Id,
                 x.Title,
                 x.Status,
                 x.CreatedWith,
                 x.Duration,
                 x.CreatedAt
            ));
    }
}
