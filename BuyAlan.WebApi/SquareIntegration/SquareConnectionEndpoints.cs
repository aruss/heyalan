namespace BuyAlan.WebApi.SquareIntegration;

using BuyAlan;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BuyAlan.SquareIntegration;
using System.Security.Claims;
using BuyAlan.Data;
using BuyAlan.Data.Entities;
using BuyAlan.Extensions;

public static class SquareConnectionEndpoints
{
    public static IEndpointRouteBuilder MapSquareConnectionEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        RouteGroupBuilder subscriptionsGroup = routeBuilder
            .MapGroup("/subscriptions")
            .WithTags("SubscriptionSquareConnections")
            .RequireAuthorization();

        subscriptionsGroup
            .MapPost(
                "/{subscriptionId:guid}/square/authorize",
                StartSquareConnectAuthorizeAsync)
            .Produces<StartSubscriptionSquareConnectAuthorizeResult>(StatusCodes.Status200OK)
            .Produces<SquareConnectionErrorResult>(StatusCodes.Status400BadRequest)
            .Produces<SquareConnectionErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<SquareConnectionErrorResult>(StatusCodes.Status403Forbidden);

        subscriptionsGroup
            .MapPost(
                "/{subscriptionId:guid}/square/catalog/sync",
                PostSubscriptionSquareCatalogSyncAsync)
            .Produces<PostSubscriptionSquareCatalogSyncResult>(StatusCodes.Status200OK)
            .Produces<SubscriptionCatalogSyncErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<SubscriptionCatalogSyncErrorResult>(StatusCodes.Status403Forbidden);

        subscriptionsGroup
            .MapGet(
                "/{subscriptionId:guid}/square/catalog/sync-state",
                GetSubscriptionSquareCatalogSyncStateAsync)
            .Produces<GetSubscriptionSquareCatalogSyncStateResult>(StatusCodes.Status200OK)
            .Produces<SubscriptionCatalogSyncErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<SubscriptionCatalogSyncErrorResult>(StatusCodes.Status403Forbidden);

        subscriptionsGroup
            .MapGet(
                "/{subscriptionId:guid}/square/catalog/products",
                GetSubscriptionSquareCatalogProductsAsync)
            .Produces<GetSubscriptionSquareCatalogProductsResult>(StatusCodes.Status200OK)
            .Produces<SubscriptionCatalogSyncErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<SubscriptionCatalogSyncErrorResult>(StatusCodes.Status403Forbidden);

        subscriptionsGroup
            .MapDelete(
                "/{subscriptionId:guid}/square/connection",
                DisconnectSquareConnectionAsync)
            .Produces<DeleteSubscriptionSquareConnectionResult>(StatusCodes.Status200OK)
            .Produces<SquareConnectionErrorResult>(StatusCodes.Status400BadRequest)
            .Produces<SquareConnectionErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<SquareConnectionErrorResult>(StatusCodes.Status403Forbidden)
            .Produces<SquareConnectionErrorResult>(StatusCodes.Status404NotFound);

        routeBuilder
            .MapGet("/subscriptions/square/callback", CompleteSquareConnectCallbackAsync)
            .WithTags("SubscriptionSquareConnections")
            .AllowAnonymous();

        return routeBuilder;
    }

    private static async Task<IResult> StartSquareConnectAuthorizeAsync(
        [FromRoute] Guid subscriptionId,
        [AsParameters] StartSubscriptionSquareConnectAuthorizeInput input,
        ClaimsPrincipal user,
        ISquareService service,
        CancellationToken cancellationToken)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return UnauthorizedError("unauthenticated");
        }

        StartSquareConnectResult result = await service.StartConnectAsync(
            new StartSquareConnectInput(subscriptionId, userId.Value, input.ReturnUrl ?? String.Empty),
            cancellationToken);

        if (result is StartSquareConnectResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        StartSquareConnectResult.Success success = (StartSquareConnectResult.Success)result;
        return TypedResults.Ok(new StartSubscriptionSquareConnectAuthorizeResult(success.AuthorizeUrl));
    }

    private static async Task<RedirectHttpResult> CompleteSquareConnectCallbackAsync(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery(Name = "error")] string? oauthError,
        ISquareService service,
        CancellationToken cancellationToken)
    {
        CompleteSquareConnectResult result = await service.CompleteConnectAsync(
            new CompleteSquareConnectInput(state, code, oauthError),
            cancellationToken);

        if (result is CompleteSquareConnectResult.Success success)
        {
            return TypedResults.Redirect(success.RedirectUrl);
        }

        CompleteSquareConnectResult.Failure failure = (CompleteSquareConnectResult.Failure)result;
        return TypedResults.Redirect(failure.RedirectUrl);
    }

    private static async Task<IResult> DisconnectSquareConnectionAsync(
        [FromRoute] Guid subscriptionId,
        ClaimsPrincipal user,
        ISquareService service,
        CancellationToken cancellationToken)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return UnauthorizedError("unauthenticated");
        }

        DisconnectSquareConnectionResult result = await service.DisconnectAsync(
            new DisconnectSquareConnectionInput(subscriptionId, userId.Value),
            cancellationToken);

        if (result is DisconnectSquareConnectionResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        return TypedResults.Ok(new DeleteSubscriptionSquareConnectionResult(true));
    }

    private static async Task<IResult> PostSubscriptionSquareCatalogSyncAsync(
        [FromRoute] Guid subscriptionId,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        ISubscriptionCatalogSyncTriggerService triggerService,
        CancellationToken cancellationToken)
    {
        IResult? authorizationError = await AuthorizeSubscriptionMemberAsync(
            subscriptionId,
            user,
            dbContext,
            cancellationToken);
        if (authorizationError is not null)
        {
            return authorizationError;
        }

        SubscriptionCatalogSyncRequestResult result = await triggerService.RequestSyncAsync(
            new SubscriptionCatalogSyncRequestInput(
                subscriptionId,
                BuyAlan.Data.Entities.CatalogSyncTriggerSource.Manual,
                true),
            cancellationToken);

        return TypedResults.Ok(new PostSubscriptionSquareCatalogSyncResult(result.Enqueued));
    }

    private static async Task<IResult> GetSubscriptionSquareCatalogSyncStateAsync(
        [FromRoute] Guid subscriptionId,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        ISubscriptionCatalogReadService readService,
        CancellationToken cancellationToken)
    {
        IResult? authorizationError = await AuthorizeSubscriptionMemberAsync(
            subscriptionId,
            user,
            dbContext,
            cancellationToken);
        if (authorizationError is not null)
        {
            return authorizationError;
        }

        SubscriptionCatalogFreshnessResult freshness = await readService.GetFreshnessAsync(
            subscriptionId,
            cancellationToken);

        CatalogProductCountsResult counts = await GetCatalogProductCountsAsync(
            subscriptionId,
            dbContext,
            cancellationToken);

        return TypedResults.Ok(
            new GetSubscriptionSquareCatalogSyncStateResult(
                subscriptionId,
                ResolveCatalogSyncStatus(freshness),
                freshness.LastTriggerSource?.ToString(),
                freshness.LastSyncedBeginTimeUtc,
                freshness.NextScheduledSyncAtUtc,
                freshness.LastSyncStartedAtUtc,
                freshness.LastSyncCompletedAtUtc,
                freshness.SyncInProgress,
                freshness.PendingResync,
                freshness.LastErrorCode,
                freshness.LastErrorMessage,
                counts.CachedProductCount,
                counts.SellableProductCount,
                counts.DeletedProductCount));
    }

    private static async Task<IResult> GetSubscriptionSquareCatalogProductsAsync(
        [FromRoute] Guid subscriptionId,
        [AsParameters] GetSubscriptionSquareCatalogProductsInput input,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        CancellationToken cancellationToken)
    {
        IResult? authorizationError = await AuthorizeSubscriptionMemberAsync(
            subscriptionId,
            user,
            dbContext,
            cancellationToken);
        if (authorizationError is not null)
        {
            return authorizationError;
        }

        string? normalizedQuery = input.Query.NormalizeSearchQuery();

        IQueryable<SubscriptionCatalogProduct> query = dbContext.SubscriptionCatalogProducts
            .AsNoTracking()
            .Include(item => item.Locations)
            .Where(item => item.SubscriptionId == subscriptionId);

        if (!String.IsNullOrWhiteSpace(normalizedQuery))
        {
            query = query.Where(item => item.SearchText.Contains(normalizedQuery));
        }

        int skip = Math.Clamp(input.Skip, Constants.SkipMin, Constants.SkipMax);
        int take = Math.Clamp(input.Take, Constants.TakeMin, Constants.TakeMax);

        PagedList<SubscriptionCatalogProduct> page = await query
            .OrderBy(item => item.ItemName)
            .ThenBy(item => item.VariationName)
            .ThenBy(item => item.Id)
            .ToPagedListAsync(skip, take, cancellationToken);

        List<SubscriptionSquareCatalogProductItem> items = page.Items
            .Select(
                item => SubscriptionSquareCatalogProductMappings.ToItem(item))
            .ToList();

        return TypedResults.Ok(
            new GetSubscriptionSquareCatalogProductsResult(
                items,
                page.Total,
                page.Skip,
                page.Take));
    }

    private static IResult UnauthorizedError(string errorCode)
    {
        SquareConnectionErrorResult payload = new(errorCode, "Authentication is required.");
        return TypedResults.Json(payload, statusCode: StatusCodes.Status401Unauthorized);
    }

    private static IResult MapError(string errorCode)
    {
        SquareConnectionErrorResult payload = new(errorCode, ResolveErrorMessage(errorCode));

        int statusCode = errorCode switch
        {
            "subscription_owner_required" => StatusCodes.Status403Forbidden,
            "connection_not_found" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };

        return TypedResults.Json(payload, statusCode: statusCode);
    }

    private static IResult UnauthorizedCatalogSyncError(string errorCode)
    {
        SubscriptionCatalogSyncErrorResult payload = new(errorCode, "Authentication is required.");
        return TypedResults.Json(payload, statusCode: StatusCodes.Status401Unauthorized);
    }

    private static IResult MapCatalogSyncError(string errorCode)
    {
        SubscriptionCatalogSyncErrorResult payload = new(errorCode, ResolveCatalogSyncErrorMessage(errorCode));

        int statusCode = errorCode switch
        {
            "subscription_member_required" => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };

        return TypedResults.Json(payload, statusCode: statusCode);
    }

    private static string ResolveErrorMessage(string errorCode)
    {
        return errorCode switch
        {
            "subscription_owner_required" => "Subscription owner permissions are required.",
            "square_not_configured" => "Square integration is not configured.",
            "return_url_required" => "A valid internal returnUrl is required.",
            "square_oauth_state_invalid" => "The Square connect state is invalid or expired.",
            "square_oauth_code_missing" => "The Square authorization code is missing.",
            "square_oauth_access_denied" => "Square authorization was denied by the user.",
            "square_oauth_callback_error" => "Square callback returned an error.",
            "square_token_exchange_failed" => "Square token exchange failed.",
            "square_required_scopes_missing" => "Square did not grant all required scopes.",
            "connection_not_found" => "No Square connection exists for this subscription.",
            "square_revoke_failed" => "Square token revoke failed.",
            "square_reconnect_required" => "Square requires reconnect.",
            "square_token_refresh_failed" => "Square token refresh failed.",
            "square_connection_persist_failed" => "Failed to persist the Square connection.",
            "square_probe_failed" => "Square probe request failed.",
            _ => "Square connection request failed."
        };
    }

    private static string ResolveCatalogSyncErrorMessage(string errorCode)
    {
        return errorCode switch
        {
            "subscription_member_required" => "You must be a member of the subscription.",
            _ => "Square catalog sync request failed."
        };
    }

    private static async Task<IResult?> AuthorizeSubscriptionMemberAsync(
        Guid subscriptionId,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        CancellationToken cancellationToken)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return UnauthorizedCatalogSyncError("unauthenticated");
        }

        bool isMember = await dbContext.SubscriptionUsers
            .AnyAsync(
                membership =>
                    membership.SubscriptionId == subscriptionId &&
                    membership.UserId == userId.Value,
                cancellationToken);

        if (!isMember)
        {
            return MapCatalogSyncError("subscription_member_required");
        }

        return null;
    }

    private static async Task<CatalogProductCountsResult> GetCatalogProductCountsAsync(
        Guid subscriptionId,
        MainDataContext dbContext,
        CancellationToken cancellationToken)
    {
        List<SubscriptionCatalogProduct> products = await dbContext.SubscriptionCatalogProducts
            .AsNoTracking()
            .Where(item => item.SubscriptionId == subscriptionId)
            .ToListAsync(cancellationToken);

        return new CatalogProductCountsResult(
            products.Count,
            products.Count(item => item.IsSellable && !item.IsDeleted),
            products.Count(item => item.IsDeleted));
    }

    private static string ResolveCatalogSyncStatus(SubscriptionCatalogFreshnessResult freshness)
    {
        if (freshness.SyncInProgress)
        {
            return "in_progress";
        }

        if (freshness.PendingResync)
        {
            return "pending_resync";
        }

        if (!String.IsNullOrWhiteSpace(freshness.LastErrorCode))
        {
            return "failed";
        }

        if (freshness.LastSyncCompletedAtUtc.HasValue)
        {
            return "idle";
        }

        return "not_started";
    }
    private sealed record CatalogProductCountsResult(
        int CachedProductCount,
        int SellableProductCount,
        int DeletedProductCount);
}


