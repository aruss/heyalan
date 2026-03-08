namespace HeyAlan.WebApi.SquareIntegration;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using HeyAlan.SquareIntegration;
using System.Security.Claims;

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
}
