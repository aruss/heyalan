namespace HeyAlan.WebApi.Onboarding;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HeyAlan.Data;
using HeyAlan.Data.Entities;
using HeyAlan.Onboarding;
using System.Security.Claims;
using ServiceCreateAgentResult = HeyAlan.Onboarding.CreateSubscriptionOnboardingAgentResult;
using ServiceGetStateResult = HeyAlan.Onboarding.GetSubscriptionOnboardingStateResult;

public static class OnboardingEndpoints
{
    public static IEndpointRouteBuilder MapOnboardingEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        RouteGroupBuilder onboardingGroup = routeBuilder
            .MapGroup("/onboarding")
            .WithTags("Onboarding")
            .RequireAuthorization();

        onboardingGroup
            .MapGet(
                "/subscriptions/active",
                GetActiveSubscriptionAsync)
            .Produces<GetActiveSubscriptionResult>(StatusCodes.Status200OK)
            .Produces<OnboardingErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<OnboardingErrorResult>(StatusCodes.Status404NotFound);

        onboardingGroup
            .MapGet(
                "/subscriptions/{subscriptionId:guid}/state",
                GetSubscriptionOnboardingStateAsync)
            .Produces<GetSubscriptionOnboardingStateResult>(StatusCodes.Status200OK)
            .Produces<OnboardingErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<OnboardingErrorResult>(StatusCodes.Status403Forbidden)
            .Produces<OnboardingErrorResult>(StatusCodes.Status404NotFound);

        onboardingGroup
            .MapPost(
                "/subscriptions/{subscriptionId:guid}/agents",
                CreateSubscriptionOnboardingAgentAsync)
            .Produces<CreateSubscriptionOnboardingAgentResult>(StatusCodes.Status200OK)
            .Produces<OnboardingErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<OnboardingErrorResult>(StatusCodes.Status403Forbidden)
            .Produces<OnboardingErrorResult>(StatusCodes.Status404NotFound);

        onboardingGroup
            .MapPatch(
                "/agents/{agentId:guid}/profile",
                PatchOnboardingAgentProfileAsync)
            .Produces<GetSubscriptionOnboardingStateResult>(StatusCodes.Status200OK)
            .Produces<OnboardingErrorResult>(StatusCodes.Status400BadRequest)
            .Produces<OnboardingErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<OnboardingErrorResult>(StatusCodes.Status403Forbidden)
            .Produces<OnboardingErrorResult>(StatusCodes.Status404NotFound);

        onboardingGroup
            .MapPatch(
                "/agents/{agentId:guid}/channels",
                PatchOnboardingAgentChannelsAsync)
            .Produces<GetSubscriptionOnboardingStateResult>(StatusCodes.Status200OK)
            .Produces<OnboardingErrorResult>(StatusCodes.Status400BadRequest)
            .Produces<OnboardingErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<OnboardingErrorResult>(StatusCodes.Status403Forbidden)
            .Produces<OnboardingErrorResult>(StatusCodes.Status404NotFound);

        onboardingGroup
            .MapPost(
                "/subscriptions/{subscriptionId:guid}/members/invitations",
                CompleteOnboardingInvitationsAsync)
            .Produces<GetSubscriptionOnboardingStateResult>(StatusCodes.Status200OK)
            .Produces<OnboardingErrorResult>(StatusCodes.Status400BadRequest)
            .Produces<OnboardingErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<OnboardingErrorResult>(StatusCodes.Status403Forbidden)
            .Produces<OnboardingErrorResult>(StatusCodes.Status404NotFound);

        onboardingGroup
            .MapPost(
                "/subscriptions/{subscriptionId:guid}/finalize",
                FinalizeOnboardingAsync)
            .Produces<GetSubscriptionOnboardingStateResult>(StatusCodes.Status200OK)
            .Produces<OnboardingErrorResult>(StatusCodes.Status400BadRequest)
            .Produces<OnboardingErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<OnboardingErrorResult>(StatusCodes.Status403Forbidden)
            .Produces<OnboardingErrorResult>(StatusCodes.Status404NotFound);

        return routeBuilder;
    }

    private static async Task<IResult> GetActiveSubscriptionAsync(
        ClaimsPrincipal user,
        MainDataContext dbContext,
        CancellationToken cancellationToken)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return UnauthorizedError("unauthenticated");
        }

        Guid? subscriptionId = await dbContext.SubscriptionUsers
            .Where(membership => membership.UserId == userId.Value)
            .OrderBy(membership => membership.Role)
            .ThenBy(membership => membership.CreatedAt)
            .Select(membership => (Guid?)membership.SubscriptionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!subscriptionId.HasValue)
        {
            OnboardingErrorResult payload = new(
                "subscription_membership_not_found",
                "No subscription membership exists for the current user.");
            return TypedResults.Json(payload, statusCode: StatusCodes.Status404NotFound);
        }

        return TypedResults.Ok(new GetActiveSubscriptionResult(subscriptionId.Value));
    }

    private static async Task<IResult> GetSubscriptionOnboardingStateAsync(
        [FromRoute] Guid subscriptionId,
        [FromQuery] bool resumeMode,
        ClaimsPrincipal user,
        ISubscriptionOnboardingService onboardingService,
        CancellationToken cancellationToken)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return UnauthorizedError("unauthenticated");
        }

        ServiceGetStateResult result = await onboardingService.GetStateAsync(
            subscriptionId,
            userId.Value,
            resumeMode,
            cancellationToken);

        if (result is ServiceGetStateResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        ServiceGetStateResult.Success success = (ServiceGetStateResult.Success)result;
        return TypedResults.Ok(ToEndpointResult(success.State));
    }

    private static async Task<IResult> CreateSubscriptionOnboardingAgentAsync(
        [FromRoute] Guid subscriptionId,
        ClaimsPrincipal user,
        ISubscriptionOnboardingService onboardingService,
        CancellationToken cancellationToken)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return UnauthorizedError("unauthenticated");
        }

        ServiceCreateAgentResult result = await onboardingService.CreatePrimaryAgentAsync(
            subscriptionId,
            userId.Value,
            cancellationToken);

        if (result is ServiceCreateAgentResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        ServiceCreateAgentResult.Success success = (ServiceCreateAgentResult.Success)result;

        return TypedResults.Ok(new HeyAlan.WebApi.Onboarding.CreateSubscriptionOnboardingAgentResult(
            success.AgentId,
            ToEndpointResult(success.State)));
    }

    private static async Task<IResult> PatchOnboardingAgentProfileAsync(
        [FromRoute] Guid agentId,
        [FromBody] PatchOnboardingAgentProfileInput input,
        ClaimsPrincipal user,
        ISubscriptionOnboardingService onboardingService,
        CancellationToken cancellationToken)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return UnauthorizedError("unauthenticated");
        }

        UpdateSubscriptionOnboardingStepResult result = await onboardingService.UpdateProfileAsync(
            new UpdateSubscriptionOnboardingProfileInput(agentId, userId.Value, input.Name, input.Personality),
            cancellationToken);

        if (result is UpdateSubscriptionOnboardingStepResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        UpdateSubscriptionOnboardingStepResult.Success success = (UpdateSubscriptionOnboardingStepResult.Success)result;
        return TypedResults.Ok(ToEndpointResult(success.State));
    }

    private static async Task<IResult> PatchOnboardingAgentChannelsAsync(
        [FromRoute] Guid agentId,
        [FromBody] PatchOnboardingAgentChannelsInput input,
        ClaimsPrincipal user,
        ISubscriptionOnboardingService onboardingService,
        CancellationToken cancellationToken)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return UnauthorizedError("unauthenticated");
        }

        UpdateSubscriptionOnboardingStepResult result = await onboardingService.UpdateChannelsAsync(
            new UpdateSubscriptionOnboardingChannelsInput(
                agentId,
                userId.Value,
                input.TwilioPhoneNumber,
                input.TelegramBotToken,
                input.WhatsappNumber),
            cancellationToken);

        if (result is UpdateSubscriptionOnboardingStepResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        UpdateSubscriptionOnboardingStepResult.Success success = (UpdateSubscriptionOnboardingStepResult.Success)result;
        return TypedResults.Ok(ToEndpointResult(success.State));
    }

    private static async Task<IResult> CompleteOnboardingInvitationsAsync(
        [FromRoute] Guid subscriptionId,
        ClaimsPrincipal user,
        ISubscriptionOnboardingService onboardingService,
        CancellationToken cancellationToken)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return UnauthorizedError("unauthenticated");
        }

        UpdateSubscriptionOnboardingStepResult result = await onboardingService.CompleteInvitationsAsync(
            subscriptionId,
            userId.Value,
            cancellationToken);

        if (result is UpdateSubscriptionOnboardingStepResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        UpdateSubscriptionOnboardingStepResult.Success success = (UpdateSubscriptionOnboardingStepResult.Success)result;
        return TypedResults.Ok(ToEndpointResult(success.State));
    }

    private static async Task<IResult> FinalizeOnboardingAsync(
        [FromRoute] Guid subscriptionId,
        ClaimsPrincipal user,
        ISubscriptionOnboardingService onboardingService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        CancellationToken cancellationToken)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return UnauthorizedError("unauthenticated");
        }

        UpdateSubscriptionOnboardingStepResult result = await onboardingService.FinalizeAsync(
            subscriptionId,
            userId.Value,
            cancellationToken);

        if (result is UpdateSubscriptionOnboardingStepResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        ApplicationUser? applicationUser = await userManager.FindByIdAsync(userId.Value.ToString());
        if (applicationUser is not null)
        {
            Claim onboardingClaim = new("onboarded", "true");
            await signInManager.SignInWithClaimsAsync(applicationUser, false, [onboardingClaim]);
        }

        UpdateSubscriptionOnboardingStepResult.Success success = (UpdateSubscriptionOnboardingStepResult.Success)result;
        return TypedResults.Ok(ToEndpointResult(success.State));
    }

    private static GetSubscriptionOnboardingStateResult ToEndpointResult(OnboardingStateResult state)
    {
        return new GetSubscriptionOnboardingStateResult(
            state.Status,
            state.CurrentStep,
            state.Steps,
            state.PrimaryAgentId,
            state.CanFinalize,
            state.ProfilePrefill,
            state.ChannelsPrefill);
    }

    private static IResult UnauthorizedError(string errorCode)
    {
        OnboardingErrorResult payload = new(errorCode, "Authentication is required.");
        return TypedResults.Json(payload, statusCode: StatusCodes.Status401Unauthorized);
    }

    private static IResult MapError(string errorCode)
    {
        OnboardingErrorResult payload = new(errorCode, ResolveErrorMessage(errorCode));

        int statusCode = errorCode switch
        {
            "subscription_member_required" => StatusCodes.Status403Forbidden,
            "agent_not_found" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };

        return TypedResults.Json(payload, statusCode: statusCode);
    }

    private static string ResolveErrorMessage(string errorCode)
    {
        return errorCode switch
        {
            "subscription_member_required" => "You must be a member of the subscription.",
            "agent_not_found" => "The requested agent was not found.",
            "agent_name_required" => "Agent name is required.",
            "agent_personality_required" => "Agent personality is required.",
            "channels_at_least_one_required" => "At least one channel must be configured.",
            "telegram_bot_token_already_in_use" => "This Telegram bot token is already connected to another agent. Use a different token.",
            "telegram_webhook_registration_failed" => "Telegram webhook registration failed. Verify the bot token, webhook URL reachability, and BotFather webhook settings, then try again.",
            "telegram_bot_token_invalid" => "Telegram rejected the bot token. Verify the token from BotFather and try again.",
            "onboarding_invitations_blocked" => "Complete square connection, profile, and channels before invitations.",
            "onboarding_finalize_incomplete" => "All required onboarding steps must be completed before finalize.",
            _ => "Onboarding request failed."
        };
    }
}
