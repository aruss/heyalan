namespace ShelfBuddy.WebApi.Identity;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShelfBuddy.Data.Entities;
using System.Linq;
using System.Security.Claims;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var routeGroup = endpoints
            .MapGroup("/auth")
            .WithTags("Auth");

        routeGroup
            .MapPost("/login", LoginAsync)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AllowAnonymous();

        routeGroup
            .MapGet("/me", GetCurrentUserAsync)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        routeGroup
            .MapPost("/logout", LogoutAsync)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<Results<EmptyHttpResult, SignInHttpResult, ProblemHttpResult>> LoginAsync(
        [FromBody] LoginInput login,
        [FromQuery] bool? useCookies,
        [FromQuery] bool? useSessionCookies,
        SignInManager<ApplicationUser> signInManager)
    {
        var useCookieScheme = (useCookies == true) || (useSessionCookies == true);
        var isPersistent = (useCookies == true) && (useSessionCookies != true);

        signInManager.AuthenticationScheme = useCookieScheme ?
            IdentityConstants.ApplicationScheme :
            IdentityConstants.BearerScheme;

        var result = await signInManager.PasswordSignInAsync(
            login.Email,
            login.Password,
            isPersistent,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return TypedResults.Problem(
                result.ToString(),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        // If using Cookies, we return Empty (200 OK) as the cookie is set in headers.
        if (useCookieScheme)
        {
            return TypedResults.Empty;
        }

        return TypedResults.Empty;
    }

    private static async Task<Results<Ok<CurrentUserResult>, UnauthorizedHttpResult>> GetCurrentUserAsync(
        ClaimsPrincipal user,
        UserManager<ApplicationUser> userManager)
    {
        // 1. Get UserId from Claims directly (Fastest check)
        var userId = userManager.GetUserId(user);

        if (userId == null)
        {
            return TypedResults.Unauthorized();
        }

        // 2. Optimized Fetch: Use FindByIdAsync
        var applicationUser = await userManager.FindByIdAsync(userId);

        if (applicationUser == null)
        {
            return TypedResults.Unauthorized();
        }

        // 3. Get Roles
        var roles = await userManager.GetRolesAsync(applicationUser);

        return TypedResults.Ok(new CurrentUserResult(
            applicationUser.Id,
            applicationUser.Email ?? "",
            ResolveDisplayName(applicationUser),
            roles.ToArray()
        ));
    }

    private static async Task<Ok> LogoutAsync(SignInManager<ApplicationUser> signInManager)
    {
        // SignOutAsync clears the cookie. It has no effect on Bearer tokens (client-side deletion).
        await signInManager.SignOutAsync();
        return TypedResults.Ok();
    }

    private static string ResolveDisplayName(ApplicationUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            return user.DisplayName;
        }

        if (!string.IsNullOrWhiteSpace(user.UserName))
        {
            return user.UserName;
        }

        return user.Email ?? string.Empty;
    }
}
