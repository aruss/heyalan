namespace BuyAlan.WebApi.Subscriptions;

using System.Security.Claims;
using BuyAlan.Data;
using BuyAlan.Data.Entities;
using BuyAlan.Subscriptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public static class SubscriptionMemberEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionMemberEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        RouteGroupBuilder subscriptionsGroup = routeBuilder
            .MapGroup("/subscriptions")
            .WithTags("SubscriptionMembers")
            .RequireAuthorization();

        subscriptionsGroup
            .MapGet(
                "/{subscriptionId:guid}/members",
                GetSubscriptionMembersAsync)
            .Produces<GetSubscriptionMembersResult>(StatusCodes.Status200OK)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status403Forbidden);

        subscriptionsGroup
            .MapPost(
                "/{subscriptionId:guid}/members/invitations",
                PostSubscriptionInvitationAsync)
            .Produces<SubscriptionInvitationItem>(StatusCodes.Status200OK)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status400BadRequest)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status403Forbidden);

        subscriptionsGroup
            .MapPost(
                "/{subscriptionId:guid}/members/invitations/{invitationId:guid}/resend",
                PostSubscriptionInvitationResendAsync)
            .Produces<SubscriptionInvitationItem>(StatusCodes.Status200OK)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status400BadRequest)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status403Forbidden)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status404NotFound);

        subscriptionsGroup
            .MapGet(
                "/{subscriptionId:guid}/members/invitations/{invitationId:guid}/link",
                GetSubscriptionInvitationLinkAsync)
            .Produces<GetSubscriptionInvitationLinkResult>(StatusCodes.Status200OK)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status400BadRequest)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status403Forbidden)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status404NotFound);

        subscriptionsGroup
            .MapDelete(
                "/{subscriptionId:guid}/members/invitations/{invitationId:guid}",
                DeleteSubscriptionInvitationAsync)
            .Produces<DeleteSubscriptionInvitationResult>(StatusCodes.Status200OK)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status400BadRequest)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status403Forbidden)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status404NotFound);

        subscriptionsGroup
            .MapPut(
                "/{subscriptionId:guid}/members/{memberUserId:guid}/role",
                PutSubscriptionMemberRoleAsync)
            .Produces<SubscriptionMemberItem>(StatusCodes.Status200OK)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status400BadRequest)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status403Forbidden)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status404NotFound);

        subscriptionsGroup
            .MapDelete(
                "/{subscriptionId:guid}/members/{memberUserId:guid}",
                DeleteSubscriptionMemberAsync)
            .Produces<DeleteSubscriptionMemberResult>(StatusCodes.Status200OK)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status400BadRequest)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status403Forbidden)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status404NotFound);

        routeBuilder
            .MapGet(
                "/subscription-invitations/{token}",
                GetSubscriptionInvitationByTokenAsync)
            .WithTags("SubscriptionInvitations")
            .AllowAnonymous()
            .Produces<GetSubscriptionInvitationByTokenResult>(StatusCodes.Status200OK)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status404NotFound);

        routeBuilder
            .MapPost(
                "/subscription-invitations/{token}/accept",
                PostSubscriptionInvitationAcceptAsync)
            .WithTags("SubscriptionInvitations")
            .RequireAuthorization()
            .Produces<PostSubscriptionInvitationAcceptResult>(StatusCodes.Status200OK)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status400BadRequest)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status401Unauthorized)
            .Produces<SubscriptionMemberManagementErrorResult>(StatusCodes.Status404NotFound);

        return routeBuilder;
    }

    private static async Task<IResult> GetSubscriptionMembersAsync(
        [FromRoute] Guid subscriptionId,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        CancellationToken cancellationToken)
    {
        SubscriptionOwnerAuthorizationResult authorization = await AuthorizeSubscriptionOwnerAsync(
            subscriptionId,
            user,
            dbContext,
            cancellationToken);
        if (authorization.ErrorResult is not null)
        {
            return authorization.ErrorResult;
        }

        GetSubscriptionMembersResult result = await SubscriptionMemberReadModelBuilder.BuildMembersResultAsync(
            subscriptionId,
            authorization.UserId!.Value,
            dbContext,
            cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<IResult> PostSubscriptionInvitationAsync(
        [FromRoute] Guid subscriptionId,
        [FromBody] PostSubscriptionInvitationInput input,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        ISubscriptionInvitationService invitationService,
        CancellationToken cancellationToken)
    {
        SubscriptionOwnerAuthorizationResult authorization = await AuthorizeSubscriptionOwnerAsync(
            subscriptionId,
            user,
            dbContext,
            cancellationToken);
        if (authorization.ErrorResult is not null)
        {
            return authorization.ErrorResult;
        }

        CreateSubscriptionInvitationResult result = await invitationService.CreateAsync(
            new CreateSubscriptionInvitationInput(
                subscriptionId,
                authorization.UserId!.Value,
                input.Email,
                input.Role ?? SubscriptionUserRole.Member),
            cancellationToken);

        if (result is CreateSubscriptionInvitationResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        return await GetSubscriptionInvitationItemAsync(
            subscriptionId,
            ((CreateSubscriptionInvitationResult.Success)result).Invitation.InvitationId,
            authorization.UserId.Value,
            dbContext,
            cancellationToken);
    }

    private static async Task<IResult> PostSubscriptionInvitationResendAsync(
        [FromRoute] Guid subscriptionId,
        [FromRoute] Guid invitationId,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        ISubscriptionInvitationService invitationService,
        CancellationToken cancellationToken)
    {
        SubscriptionOwnerAuthorizationResult authorization = await AuthorizeSubscriptionOwnerAsync(
            subscriptionId,
            user,
            dbContext,
            cancellationToken);
        if (authorization.ErrorResult is not null)
        {
            return authorization.ErrorResult;
        }

        ResendSubscriptionInvitationResult result = await invitationService.ResendAsync(
            invitationId,
            authorization.UserId!.Value,
            cancellationToken);

        if (result is ResendSubscriptionInvitationResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        return await GetSubscriptionInvitationItemAsync(
            subscriptionId,
            ((ResendSubscriptionInvitationResult.Success)result).Invitation.InvitationId,
            authorization.UserId.Value,
            dbContext,
            cancellationToken);
    }

    private static async Task<IResult> GetSubscriptionInvitationLinkAsync(
        [FromRoute] Guid subscriptionId,
        [FromRoute] Guid invitationId,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        ISubscriptionInvitationService invitationService,
        CancellationToken cancellationToken)
    {
        SubscriptionOwnerAuthorizationResult authorization = await AuthorizeSubscriptionOwnerAsync(
            subscriptionId,
            user,
            dbContext,
            cancellationToken);
        if (authorization.ErrorResult is not null)
        {
            return authorization.ErrorResult;
        }

        CopySubscriptionInvitationLinkResult result = await invitationService.CopyLinkAsync(
            invitationId,
            authorization.UserId!.Value,
            cancellationToken);

        if (result is CopySubscriptionInvitationLinkResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        return TypedResults.Ok(new GetSubscriptionInvitationLinkResult(
            ((CopySubscriptionInvitationLinkResult.Success)result).InvitationUrl));
    }

    private static async Task<IResult> DeleteSubscriptionInvitationAsync(
        [FromRoute] Guid subscriptionId,
        [FromRoute] Guid invitationId,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        ISubscriptionInvitationService invitationService,
        CancellationToken cancellationToken)
    {
        SubscriptionOwnerAuthorizationResult authorization = await AuthorizeSubscriptionOwnerAsync(
            subscriptionId,
            user,
            dbContext,
            cancellationToken);
        if (authorization.ErrorResult is not null)
        {
            return authorization.ErrorResult;
        }

        RevokeSubscriptionInvitationResult result = await invitationService.RevokeAsync(
            invitationId,
            authorization.UserId!.Value,
            cancellationToken);

        if (result is RevokeSubscriptionInvitationResult.Failure failure)
        {
            return MapError(failure.ErrorCode);
        }

        return TypedResults.Ok(new DeleteSubscriptionInvitationResult(true));
    }

    private static async Task<IResult> PutSubscriptionMemberRoleAsync(
        [FromRoute] Guid subscriptionId,
        [FromRoute] Guid memberUserId,
        [FromBody] PutSubscriptionMemberRoleInput input,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        CancellationToken cancellationToken)
    {
        SubscriptionOwnerAuthorizationResult authorization = await AuthorizeSubscriptionOwnerAsync(
            subscriptionId,
            user,
            dbContext,
            cancellationToken);
        if (authorization.ErrorResult is not null)
        {
            return authorization.ErrorResult;
        }

        if (!input.Role.HasValue || !Enum.IsDefined(input.Role.Value))
        {
            return MapError("role_invalid");
        }

        SubscriptionUser? membership = await dbContext.SubscriptionUsers
            .Include(item => item.User)
            .SingleOrDefaultAsync(
                item => item.SubscriptionId == subscriptionId && item.UserId == memberUserId,
                cancellationToken);

        if (membership is null)
        {
            return MapError("subscription_member_not_found");
        }

        if (membership.Role == SubscriptionUserRole.Owner &&
            input.Role.Value != SubscriptionUserRole.Owner &&
            await WouldRemoveLastOwnerAsync(subscriptionId, dbContext, cancellationToken))
        {
            return MapError("subscription_last_owner_required");
        }

        membership.Role = input.Role.Value;
        await dbContext.SaveChangesAsync(cancellationToken);

        GetSubscriptionMembersResult result = await SubscriptionMemberReadModelBuilder.BuildMembersResultAsync(
            subscriptionId,
            authorization.UserId!.Value,
            dbContext,
            cancellationToken);

        SubscriptionMemberItem item = result.Members.Single(member => member.UserId == memberUserId);
        return TypedResults.Ok(item);
    }

    private static async Task<IResult> DeleteSubscriptionMemberAsync(
        [FromRoute] Guid subscriptionId,
        [FromRoute] Guid memberUserId,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        CancellationToken cancellationToken)
    {
        SubscriptionOwnerAuthorizationResult authorization = await AuthorizeSubscriptionOwnerAsync(
            subscriptionId,
            user,
            dbContext,
            cancellationToken);
        if (authorization.ErrorResult is not null)
        {
            return authorization.ErrorResult;
        }

        SubscriptionUser? membership = await dbContext.SubscriptionUsers
            .Include(item => item.User)
            .SingleOrDefaultAsync(
                item => item.SubscriptionId == subscriptionId && item.UserId == memberUserId,
                cancellationToken);

        if (membership is null)
        {
            return MapError("subscription_member_not_found");
        }

        if (membership.Role == SubscriptionUserRole.Owner &&
            await WouldRemoveLastOwnerAsync(subscriptionId, dbContext, cancellationToken))
        {
            return MapError("subscription_last_owner_required");
        }

        if (membership.User.ActiveSubscriptionId == subscriptionId)
        {
            Guid? replacementSubscriptionId = await dbContext.SubscriptionUsers
                .AsNoTracking()
                .Where(item => item.UserId == memberUserId && item.SubscriptionId != subscriptionId)
                .OrderBy(item => item.Role)
                .ThenBy(item => item.CreatedAt)
                .Select(item => (Guid?)item.SubscriptionId)
                .FirstOrDefaultAsync(cancellationToken);

            membership.User.ActiveSubscriptionId = replacementSubscriptionId;
        }

        dbContext.SubscriptionUsers.Remove(membership);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new DeleteSubscriptionMemberResult(true));
    }

    private static async Task<IResult> GetSubscriptionInvitationByTokenAsync(
        [FromRoute] string token,
        ISubscriptionInvitationService invitationService,
        CancellationToken cancellationToken)
    {
        BuyAlan.Subscriptions.GetSubscriptionInvitationByTokenResult result = await invitationService.GetByTokenAsync(
            token,
            cancellationToken);

        if (result is BuyAlan.Subscriptions.GetSubscriptionInvitationByTokenResult.Failure failure)
        {
            return MapLookupError(failure.ErrorCode);
        }

        return result switch
        {
            BuyAlan.Subscriptions.GetSubscriptionInvitationByTokenResult.Success success => TypedResults.Ok(
                new GetSubscriptionInvitationByTokenResult(
                    "pending",
                    SubscriptionMemberReadModelBuilder.ToLookupItem(success.Invitation))),
            BuyAlan.Subscriptions.GetSubscriptionInvitationByTokenResult.Accepted accepted => TypedResults.Ok(
                new GetSubscriptionInvitationByTokenResult(
                    "accepted",
                    SubscriptionMemberReadModelBuilder.ToLookupItem(accepted.Invitation))),
            BuyAlan.Subscriptions.GetSubscriptionInvitationByTokenResult.Revoked revoked => TypedResults.Ok(
                new GetSubscriptionInvitationByTokenResult(
                    "revoked",
                    SubscriptionMemberReadModelBuilder.ToLookupItem(revoked.Invitation))),
            BuyAlan.Subscriptions.GetSubscriptionInvitationByTokenResult.Expired expired => TypedResults.Ok(
                new GetSubscriptionInvitationByTokenResult(
                    "expired",
                    SubscriptionMemberReadModelBuilder.ToLookupItem(expired.Invitation))),
            _ => MapLookupError("invitation_invalid")
        };
    }

    private static async Task<IResult> PostSubscriptionInvitationAcceptAsync(
        [FromRoute] string token,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        ISubscriptionInvitationService invitationService,
        SignInManager<ApplicationUser> signInManager,
        CancellationToken cancellationToken)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return UnauthorizedError("unauthenticated");
        }

        AcceptSubscriptionInvitationResult result = await invitationService.AcceptAsync(
            new AcceptSubscriptionInvitationInput(userId.Value, token),
            cancellationToken);

        if (result is AcceptSubscriptionInvitationResult.Success or AcceptSubscriptionInvitationResult.AlreadyAccepted)
        {
            ApplicationUser? applicationUser = await dbContext.Users
                .SingleOrDefaultAsync(item => item.Id == userId.Value, cancellationToken);

            if (applicationUser is not null)
            {
                // TODO: never use endpoint functions as service functions...
                await Identity.IdentityEndpoints.RefreshCurrentUserSessionAsync(
                    signInManager,
                    applicationUser,
                    dbContext,
                    cancellationToken);
            }
        }

        return result switch
        {
            AcceptSubscriptionInvitationResult.Success success => TypedResults.Ok(
                new PostSubscriptionInvitationAcceptResult(
                    "accepted",
                    success.SubscriptionId,
                    success.MembershipCreated)),
            AcceptSubscriptionInvitationResult.AlreadyAccepted accepted => TypedResults.Ok(
                new PostSubscriptionInvitationAcceptResult(
                    "already_accepted",
                    accepted.SubscriptionId,
                    false)),
            AcceptSubscriptionInvitationResult.Failure failure => MapError(failure.ErrorCode),
            _ => MapError("invitation_invalid")
        };
    }

    private static async Task<IResult> GetSubscriptionInvitationItemAsync(
        Guid subscriptionId,
        Guid invitationId,
        Guid currentUserId,
        MainDataContext dbContext,
        CancellationToken cancellationToken)
    {
        GetSubscriptionMembersResult result = await SubscriptionMemberReadModelBuilder.BuildMembersResultAsync(
            subscriptionId,
            currentUserId,
            dbContext,
            cancellationToken);

        SubscriptionInvitationItem? invitation = result.Invitations
            .SingleOrDefault(item => item.InvitationId == invitationId);

        if (invitation is null)
        {
            return MapError("invitation_not_found");
        }

        return TypedResults.Ok(invitation);
    }

    private static async Task<SubscriptionOwnerAuthorizationResult> AuthorizeSubscriptionOwnerAsync(
        Guid subscriptionId,
        ClaimsPrincipal user,
        MainDataContext dbContext,
        CancellationToken cancellationToken)
    {
        Guid? userId = user.GetUserId();
        if (userId is null)
        {
            return new SubscriptionOwnerAuthorizationResult(null, UnauthorizedError("unauthenticated"));
        }

        SubscriptionUser? membership = await dbContext.SubscriptionUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.SubscriptionId == subscriptionId && item.UserId == userId.Value,
                cancellationToken);

        if (membership is null || membership.Role != SubscriptionUserRole.Owner)
        {
            return new SubscriptionOwnerAuthorizationResult(userId, MapError("subscription_owner_required"));
        }

        return new SubscriptionOwnerAuthorizationResult(userId, null);
    }

    private static async Task<bool> WouldRemoveLastOwnerAsync(
        Guid subscriptionId,
        MainDataContext dbContext,
        CancellationToken cancellationToken)
    {
        int ownerCount = await dbContext.SubscriptionUsers
            .CountAsync(
                item =>
                    item.SubscriptionId == subscriptionId &&
                    item.Role == SubscriptionUserRole.Owner,
                cancellationToken);

        return ownerCount <= 1;
    }

    private static IResult UnauthorizedError(string errorCode)
    {
        SubscriptionMemberManagementErrorResult payload = new(errorCode, "Authentication is required.");
        return TypedResults.Json(payload, statusCode: StatusCodes.Status401Unauthorized);
    }

    private static IResult MapLookupError(string errorCode)
    {
        SubscriptionMemberManagementErrorResult payload = new(errorCode, ResolveErrorMessage(errorCode));

        int statusCode = errorCode switch
        {
            "invitation_invalid" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };

        return TypedResults.Json(payload, statusCode: statusCode);
    }

    private static IResult MapError(string errorCode)
    {
        SubscriptionMemberManagementErrorResult payload = new(errorCode, ResolveErrorMessage(errorCode));

        int statusCode = errorCode switch
        {
            "subscription_owner_required" => StatusCodes.Status403Forbidden,
            "invitation_invalid" => StatusCodes.Status404NotFound,
            "invitation_not_found" => StatusCodes.Status404NotFound,
            "subscription_member_not_found" => StatusCodes.Status404NotFound,
            "user_not_found" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };

        return TypedResults.Json(payload, statusCode: statusCode);
    }

    private static string ResolveErrorMessage(string errorCode)
    {
        return errorCode switch
        {
            "subscription_owner_required" => "Subscription owner permissions are required.",
            "invitation_not_found" => "The requested invitation was not found.",
            "invitation_invalid" => "The invitation is invalid.",
            "subscription_member_not_found" => "The requested subscription member was not found.",
            "subscription_last_owner_required" => "A subscription must keep at least one owner.",
            "subscription_user_exists" => "That email already belongs to a current subscription member.",
            "subscription_member_required" => "You must be a member of the subscription.",
            "email_invalid" => "A valid email address is required.",
            "role_invalid" => "A supported role is required.",
            "invitation_revoked" => "The invitation has already been revoked.",
            "invitation_expired" => "The invitation has expired.",
            "invitation_already_accepted" => "The invitation has already been accepted.",
            "invitation_email_mismatch" => "The signed-in user's email does not match the invitation email.",
            "user_email_invalid" => "The signed-in account does not have a usable email address.",
            "user_not_found" => "The signed-in user could not be found.",
            _ => "Subscription member-management request failed."
        };
    }

    private sealed record SubscriptionOwnerAuthorizationResult(
        Guid? UserId,
        IResult? ErrorResult);
}
