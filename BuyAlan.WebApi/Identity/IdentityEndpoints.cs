namespace BuyAlan.WebApi.Identity;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BuyAlan.Data.Entities;
using BuyAlan.Data;
using System.Linq;
using System.Security.Claims;

public static class IdentityEndpoints
{
    private const string DefaultReturnUrl = "/admin";
    private const string OnboardingReturnUrl = "/onboarding";
    private const string InvitationRoutePrefix = "/invite";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder routeGroup = endpoints
            .MapGroup("/auth")
            .WithTags("Auth");

        routeGroup
            .MapGet("/providers", GetExternalLoginProvidersAsync)
            .Produces<GetExternalLoginProvidersResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AllowAnonymous();

        routeGroup
            .MapGet("/providers/{provider}/authorize", StartExternalLoginAsync)
            .Produces(StatusCodes.Status302Found)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AllowAnonymous();

        routeGroup
            .MapGet("/external-callback", CompleteExternalLoginAsync)
            .Produces(StatusCodes.Status302Found)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AllowAnonymous();

        // NOTE: email/password logins are not used yet
        /*routeGroup
            .MapPost("/login", LoginAsync)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AllowAnonymous();*/

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

    private static async Task<Ok<GetExternalLoginProvidersResult>> GetExternalLoginProvidersAsync(
        SignInManager<ApplicationUser> signInManager)
    {
        IEnumerable<AuthenticationScheme> schemes = await signInManager.GetExternalAuthenticationSchemesAsync();

        ExternalLoginProviderItem[] providers = schemes
            .Select(static scheme => new ExternalLoginProviderItem(
                scheme.Name,
                scheme.DisplayName ?? scheme.Name))
            .OrderBy(static provider => provider.DisplayName)
            .ToArray();

        GetExternalLoginProvidersResult result = new(providers);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<ChallengeHttpResult, NotFound>> StartExternalLoginAsync(
        [FromRoute] string provider,
        [FromQuery] string? returnUrl,
        HttpContext context,
        SignInManager<ApplicationUser> signInManager)
    {
        string? providerScheme = await ResolveProviderSchemeNameAsync(provider, signInManager);

        if (providerScheme is null)
        {
            return TypedResults.NotFound();
        }

        string safeReturnUrl = NormalizeReturnUrl(returnUrl);
        string callbackPath = BuildAuthPath(context.Request.PathBase, "/auth/external-callback");
        string callbackUrl = QueryHelpers.AddQueryString(callbackPath, "returnUrl", safeReturnUrl);

        AuthenticationProperties properties = 
            signInManager.ConfigureExternalAuthenticationProperties(providerScheme, callbackUrl);

        return TypedResults.Challenge(properties, [providerScheme]);
    }

    private static async Task<RedirectHttpResult> CompleteExternalLoginAsync(
        [FromQuery] string? returnUrl,
        [FromQuery] string? remoteError,
        HttpContext httpContext,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        MainDataContext dbContext)
    {
        string safeReturnUrl = NormalizeReturnUrl(returnUrl);
        bool isInvitationFlow = IsInvitationReturnUrl(safeReturnUrl);

        try
        {
            if (!String.IsNullOrWhiteSpace(remoteError))
            {
                return TypedResults.Redirect(BuildLoginRedirectUrl(safeReturnUrl, "external_provider_error"));
            }

            ExternalLoginInfo? externalLoginInfo = await signInManager.GetExternalLoginInfoAsync();

            if (externalLoginInfo is null)
            {
                return TypedResults.Redirect(BuildLoginRedirectUrl(safeReturnUrl, "external_login_info_missing"));
            }

            ApplicationUser? existingUser = await userManager.FindByLoginAsync(
                externalLoginInfo.LoginProvider,
                externalLoginInfo.ProviderKey);

            if (existingUser is not null)
            {
                if (!await signInManager.CanSignInAsync(existingUser))
                {
                    return TypedResults.Redirect(BuildLoginRedirectUrl(safeReturnUrl, "user_not_allowed"));
                }

                if (await userManager.IsLockedOutAsync(existingUser))
                {
                    return TypedResults.Redirect(BuildLoginRedirectUrl(safeReturnUrl, "user_locked_out"));
                }

                bool existingUserOnboarded = await IsActiveSubscriptionOnboardedAsync(
                    existingUser.Id,
                    dbContext,
                    httpContext.RequestAborted);
                await SignInWithOnboardingClaimAsync(signInManager, existingUser, existingUserOnboarded);
                string existingUserRedirect = ResolvePostLoginRedirectTarget(safeReturnUrl, existingUserOnboarded);
                return TypedResults.Redirect(existingUserRedirect);
            }

            string? emailClaim = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.Email);

            if (String.IsNullOrWhiteSpace(emailClaim))
            {
                return TypedResults.Redirect(BuildLoginRedirectUrl(safeReturnUrl, "email_claim_missing"));
            }

            IHostEnvironment hostEnvironment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
            if (hostEnvironment.IsDevelopment())
            {
                // TEMP-DIAG-REMOVE: Temporary diagnostics for Google email verification investigation.
                string traceId = httpContext.TraceIdentifier;
                string path = httpContext.Request.Path;

                IEnumerable<string> claimPairs = externalLoginInfo.Principal.Claims
                    .Select(claim => $"{claim.Type}={claim.Value}");

                string allClaims = String.Join("; ", claimPairs);

                ILogger logger = httpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("ExternalLoginDiagnostics");

                logger.LogInformation(
                    "External login claims before verification check. TraceId: {TraceId}; Path: {Path}; Provider: {Provider}; Claims: {Claims}",
                    traceId,
                    path,
                    externalLoginInfo.LoginProvider,
                    allClaims);
            }

            if (!IsExternalEmailVerified(externalLoginInfo.Principal))
            {
                return TypedResults.Redirect(BuildLoginRedirectUrl(safeReturnUrl, "external_email_not_verified"));
            }

            string normalizedEmail = emailClaim.Trim();

            ApplicationUser? applicationUser =
                await userManager.FindByEmailAsync(normalizedEmail);

            bool isOnboarded = false;
            bool isNewUser = false;

            await using IDbContextTransaction? provisioningTransaction = applicationUser is null
                && !isInvitationFlow
                ? await dbContext.Database.BeginTransactionAsync(httpContext.RequestAborted)
                : null;

            if (applicationUser is null)
            {
                applicationUser = new ApplicationUser
                {
                    Email = normalizedEmail,
                    UserName = normalizedEmail,
                    EmailConfirmed = true,
                    DisplayName = ResolveDisplayName(
                        externalLoginInfo.Principal, normalizedEmail)
                };

                IdentityResult createUserResult =
                    await userManager.CreateAsync(applicationUser);

                if (!createUserResult.Succeeded)
                {
                    return TypedResults.Redirect(
                        BuildLoginRedirectUrl(safeReturnUrl, "user_create_failed"));
                }

                isNewUser = true;
            }
            else
            {
                bool isLocalEmailConfirmed =
                    await userManager.IsEmailConfirmedAsync(applicationUser);

                if (!isLocalEmailConfirmed)
                {
                    return TypedResults.Redirect(
                        BuildLoginRedirectUrl(safeReturnUrl, "local_email_not_confirmed"));
                }

                isOnboarded = await IsActiveSubscriptionOnboardedAsync(
                    applicationUser.Id,
                    dbContext,
                    httpContext.RequestAborted);
            }

            IdentityResult addLoginResult = await userManager.AddLoginAsync(applicationUser, externalLoginInfo);

            if (!addLoginResult.Succeeded)
            {
                return TypedResults.Redirect(BuildLoginRedirectUrl(safeReturnUrl, "external_login_link_failed"));
            }

            if (isNewUser && !isInvitationFlow)
            {
                try
                {
                    await CreateInitialSubscriptionOwnerMembershipAsync(
                        dbContext,
                        applicationUser.Id,
                        httpContext.RequestAborted);

                    if (provisioningTransaction is not null)
                    {
                        await provisioningTransaction.CommitAsync(httpContext.RequestAborted);
                    }
                }
                catch (Exception)
                {
                    return TypedResults.Redirect(BuildLoginRedirectUrl(safeReturnUrl, "subscription_provision_failed"));
                }
            }

            isOnboarded = await RefreshCurrentUserSessionAsync(
                signInManager,
                applicationUser,
                dbContext,
                httpContext.RequestAborted);
            string callbackRedirect = ResolvePostLoginRedirectTarget(safeReturnUrl, isOnboarded);
            return TypedResults.Redirect(callbackRedirect);
        }
        finally
        {
            // External cookie is handshake-only state and must not persist after callback processing.
            await httpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }
    }

    private static async Task<Results<EmptyHttpResult, SignInHttpResult, ProblemHttpResult>> LoginAsync(
        [FromBody] LoginInput login,
        [FromQuery] bool? useCookies,
        [FromQuery] bool? useSessionCookies,
        SignInManager<ApplicationUser> signInManager)
    {
        bool useCookieScheme = (useCookies == true) || (useSessionCookies == true);
        bool isPersistent = (useCookies == true) && (useSessionCookies != true);

        signInManager.AuthenticationScheme = useCookieScheme
            ? IdentityConstants.ApplicationScheme
            : IdentityConstants.BearerScheme;

        Microsoft.AspNetCore.Identity.SignInResult result = await signInManager.PasswordSignInAsync(
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

        return TypedResults.Empty;
    }

    private static async Task<Results<Ok<CurrentUserResult>, UnauthorizedHttpResult>> GetCurrentUserAsync(
        ClaimsPrincipal user,
        UserManager<ApplicationUser> userManager,
        MainDataContext dbContext,
        CancellationToken cancellationToken)
    {
        string? userId = userManager.GetUserId(user);

        if (userId == null)
        {
            return TypedResults.Unauthorized();
        }

        ApplicationUser? applicationUser = await userManager.FindByIdAsync(userId);

        if (applicationUser == null)
        {
            return TypedResults.Unauthorized();
        }

        IList<string> roles = await userManager.GetRolesAsync(applicationUser);
        Guid? activeSubscriptionId = await GetActiveSubscriptionIdAsync(applicationUser.Id, dbContext, cancellationToken);
        bool isOnboarded = await IsActiveSubscriptionOnboardedAsync(applicationUser.Id, dbContext, cancellationToken);

        return TypedResults.Ok(new CurrentUserResult(
            applicationUser.Id,
            applicationUser.Email ?? "",
            ResolveDisplayName(applicationUser),
            roles.ToArray(),
            activeSubscriptionId,
            isOnboarded
        ));
    }

    private static async Task<Ok> LogoutAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return TypedResults.Ok();
    }

    private static async Task<string?> ResolveProviderSchemeNameAsync(
        string provider,
        SignInManager<ApplicationUser> signInManager)
    {
        IEnumerable<AuthenticationScheme> schemes = await signInManager.GetExternalAuthenticationSchemesAsync();
        AuthenticationScheme? matchingScheme = schemes
            .FirstOrDefault(scheme => String.Equals(
                scheme.Name,
                provider,
                StringComparison.OrdinalIgnoreCase));

        return matchingScheme?.Name;
    }

    internal static string NormalizeReturnUrl(string? returnUrl)
    {
        if (String.IsNullOrWhiteSpace(returnUrl))
        {
            return DefaultReturnUrl;
        }

        string trimmedReturnUrl = returnUrl.Trim();

        if (!trimmedReturnUrl.StartsWith('/'))
        {
            return DefaultReturnUrl;
        }

        if (trimmedReturnUrl.StartsWith("//", StringComparison.Ordinal))
        {
            return DefaultReturnUrl;
        }

        if (Uri.TryCreate(trimmedReturnUrl, UriKind.Absolute, out _))
        {
            return DefaultReturnUrl;
        }

        return trimmedReturnUrl;
    }

    internal static string RemoveAuthErrorFromReturnUrl(string returnUrl)
    {
        int fragmentIndex = returnUrl.IndexOf('#');
        string fragment = fragmentIndex >= 0
            ? returnUrl.Substring(fragmentIndex)
            : String.Empty;

        string urlWithoutFragment = fragmentIndex >= 0
            ? returnUrl.Substring(0, fragmentIndex)
            : returnUrl;

        int queryIndex = urlWithoutFragment.IndexOf('?');
        if (queryIndex < 0)
        {
            return returnUrl;
        }

        string path = urlWithoutFragment.Substring(0, queryIndex);
        string query = urlWithoutFragment.Substring(queryIndex + 1);
        Dictionary<string, Microsoft.Extensions.Primitives.StringValues> parsedQuery = QueryHelpers.ParseQuery(query);
        QueryBuilder queryBuilder = new();

        foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> queryItem in parsedQuery)
        {
            if (String.Equals(queryItem.Key, "authError", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (string? value in queryItem.Value)
            {
                queryBuilder.Add(queryItem.Key, value ?? String.Empty);
            }
        }

        string queryString = queryBuilder.ToQueryString().Value ?? String.Empty;
        return $"{path}{queryString}{fragment}";
    }

    internal static string BuildLoginRedirectUrl(string returnUrl, string error)
    {
        string cleanedReturnUrl = RemoveAuthErrorFromReturnUrl(returnUrl);
        Dictionary<string, string?> queryValues = new()
        {
            ["returnUrl"] = cleanedReturnUrl,
            ["authError"] = error
        };

        return QueryHelpers.AddQueryString("/login", queryValues);
    }

    private static string ResolveDisplayName(ClaimsPrincipal principal, string fallbackEmail)
    {
        string? displayName = principal.FindFirstValue("name");

        if (!String.IsNullOrWhiteSpace(displayName))
        {
            return displayName.Trim();
        }

        return fallbackEmail;
    }

    private static string ResolveDisplayName(ApplicationUser user)
    {
        if (!String.IsNullOrWhiteSpace(user.DisplayName))
        {
            return user.DisplayName;
        }

        if (!String.IsNullOrWhiteSpace(user.UserName))
        {
            return user.UserName;
        }

        return user.Email ?? String.Empty;
    }

    internal static bool IsExternalEmailVerified(ClaimsPrincipal principal)
    {
        string[] verificationClaimTypes =
        [
            "email_verified",
            "verified_email",
            "urn:google:email_verified"
        ];

        foreach (string claimType in verificationClaimTypes)
        {
            string? verificationValue = principal.FindFirstValue(claimType);
            if (String.IsNullOrWhiteSpace(verificationValue))
            {
                continue;
            }

            if (String.Equals(verificationValue, "true", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(verificationValue, "1", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(verificationValue, "yes", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static string ResolvePostLoginRedirectTarget(string safeReturnUrl, bool isOnboarded)
    {
        if (IsInvitationReturnUrl(safeReturnUrl))
        {
            return safeReturnUrl;
        }

        return isOnboarded ? safeReturnUrl : OnboardingReturnUrl;
    }

    internal static bool IsInvitationReturnUrl(string returnUrl)
    {
        if (String.IsNullOrWhiteSpace(returnUrl))
        {
            return false;
        }

        string normalizedReturnUrl = returnUrl.Trim();
        int queryIndex = normalizedReturnUrl.IndexOfAny(['?', '#']);
        string path = queryIndex >= 0
            ? normalizedReturnUrl.Substring(0, queryIndex)
            : normalizedReturnUrl;

        return String.Equals(path, InvitationRoutePrefix, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith($"{InvitationRoutePrefix}/", StringComparison.OrdinalIgnoreCase);
    }

    internal static string BuildAuthPath(PathString pathBase, string authPath)
    {
        if (!authPath.StartsWith('/'))
        {
            throw new ArgumentException("Auth path must start with '/'.", nameof(authPath));
        }

        return pathBase.HasValue
            ? $"{pathBase}{authPath}"
            : authPath;
    }

    internal static async Task<Guid?> GetActiveSubscriptionIdAsync(
        Guid userId,
        MainDataContext dbContext,
        CancellationToken cancellationToken = default)
    {
        Guid? persistedActiveSubscriptionId = await dbContext.Users
            .Where(user => user.Id == userId)
            .Select(user => user.ActiveSubscriptionId)
            .SingleOrDefaultAsync(cancellationToken);

        if (persistedActiveSubscriptionId.HasValue)
        {
            bool persistedMembershipExists = await dbContext.SubscriptionUsers
                .AnyAsync(
                    membership =>
                        membership.UserId == userId &&
                        membership.SubscriptionId == persistedActiveSubscriptionId.Value,
                    cancellationToken);

            if (persistedMembershipExists)
            {
                return persistedActiveSubscriptionId.Value;
            }
        }

        Guid? activeSubscriptionId = await dbContext.SubscriptionUsers
            .Where(membership => membership.UserId == userId)
            .OrderBy(membership => membership.Role)
            .ThenBy(membership => membership.CreatedAt)
            .Select(membership => (Guid?)membership.SubscriptionId)
            .FirstOrDefaultAsync(cancellationToken);

        return activeSubscriptionId;
    }

    internal static async Task<bool> IsActiveSubscriptionOnboardedAsync(
        Guid userId,
        MainDataContext dbContext,
        CancellationToken cancellationToken = default)
    {
        Guid? activeSubscriptionId = await GetActiveSubscriptionIdAsync(userId, dbContext, cancellationToken);
        if (!activeSubscriptionId.HasValue)
        {
            return false;
        }

        bool hasCompletedOnboarding = await dbContext.SubscriptionOnboardingStates
            .AnyAsync(
                state =>
                    state.SubscriptionId == activeSubscriptionId.Value &&
                    state.Status == SubscriptionOnboardingStatus.Completed,
                cancellationToken);

        return hasCompletedOnboarding;
    }

    // TODO: move to Subscription Service 
    internal static async Task CreateInitialSubscriptionOwnerMembershipAsync(
        MainDataContext dbContext,
        Guid userId,
        CancellationToken cancellationToken)
    {
        bool userHasMembership = await dbContext.SubscriptionUsers
            .AnyAsync(membership => membership.UserId == userId, cancellationToken);

        if (userHasMembership)
        {
            return;
        }

        Subscription subscription = new()
        {
            Id = Guid.NewGuid(),
            SubscriptionCreditBalance = 0,
            TopUpCreditBalance = 0
        };

        ApplicationUser applicationUser = await dbContext.Users
            .SingleAsync(user => user.Id == userId, cancellationToken);
        applicationUser.ActiveSubscriptionId = subscription.Id;

        SubscriptionUser ownerMembership = new()
        {
            Subscription = subscription,
            UserId = userId,
            Role = SubscriptionUserRole.Owner
        };

        dbContext.SubscriptionUsers.Add(ownerMembership);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    internal static async Task<bool> RefreshCurrentUserSessionAsync(
        SignInManager<ApplicationUser> signInManager,
        ApplicationUser user,
        MainDataContext dbContext,
        CancellationToken cancellationToken)
    {
        bool isOnboarded = await IsActiveSubscriptionOnboardedAsync(user.Id, dbContext, cancellationToken);
        await SignInWithOnboardingClaimAsync(signInManager, user, isOnboarded);
        return isOnboarded;
    }

    private static async Task SignInWithOnboardingClaimAsync(
        SignInManager<ApplicationUser> signInManager,
        ApplicationUser user,
        bool isOnboarded)
    {
        // Always stamp onboarding state in the auth cookie so policy checks are deterministic.
        Claim onboardingClaim = new("onboarded", isOnboarded ? "true" : "false");
        await signInManager.SignInWithClaimsAsync(user, false, [onboardingClaim]);
    }
}
