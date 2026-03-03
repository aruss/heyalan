namespace HeyAlan.Identity;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HeyAlan.Configuration;
using HeyAlan.Data;
using HeyAlan.Data.Entities;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

public static class IdentityBuilderExtensions
{
    public static TBuilder AddIdentityServices<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        AppOptions appOptions = builder.Configuration.TryGetAppOptions();
        CookieSecurePolicy authCookieSecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        builder.Services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<MainDataContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        builder.Services.AddTransient<IEmailSender<ApplicationUser>, LoggingEmailSender>();

        var authBuilder = builder.Services
            .AddAuthentication(IdentityConstants.ApplicationScheme);            

        authBuilder.AddIdentityCookies(); 

        if (!String.IsNullOrWhiteSpace(appOptions.AuthGoogleClientId) &&
            !String.IsNullOrWhiteSpace(appOptions.AuthGoogleClientSecret))
        {
            authBuilder.AddGoogle("google", "Google", options =>
            {
                options.ClientId = appOptions.AuthGoogleClientId;
                options.ClientSecret = appOptions.AuthGoogleClientSecret;
                options.CallbackPath = "/auth/providers/google/callback";
                options.UserInformationEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.Events.OnCreatingTicket = context =>
                {
                    if (TryGetVerificationValue(context.User, "verified_email", out bool isVerifiedEmail))
                    {
                        context.Identity?.AddClaim(new Claim("verified_email", isVerifiedEmail ? "true" : "false"));
                    }

                    if (TryGetVerificationValue(context.User, "email_verified", out bool isEmailVerified))
                    {
                        context.Identity?.AddClaim(new Claim("email_verified", isEmailVerified ? "true" : "false"));
                    }

                    if (builder.Environment.IsDevelopment())
                    {
                        // TEMP-DIAG-REMOVE: Temporary diagnostics for Google email verification investigation.
                        string rawGoogleProfile = context.User.GetRawText();
                        string path = context.HttpContext.Request.Path;
                        string traceId = context.HttpContext.TraceIdentifier;
                        ILogger logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("GoogleAuthDiagnostics");

                        logger.LogInformation(
                            "Google raw profile received. TraceId: {TraceId}; Path: {Path}; Profile: {Profile}",
                            traceId,
                            path,
                            rawGoogleProfile);
                    }

                    return Task.CompletedTask;
                };
                options.Events.OnRemoteFailure = context =>
                {
                    string callbackPath = BuildAuthPath(
                        context.Request.PathBase,
                        "/auth/external-callback");

                    string callbackUrl = QueryHelpers.AddQueryString(
                        callbackPath,
                        "remoteError",
                        "external_provider_error");

                    context.Response.Redirect(callbackUrl);
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
            });
        }

        if (!string.IsNullOrWhiteSpace(appOptions.AuthSquareClientId) &&
            !string.IsNullOrWhiteSpace(appOptions.AuthSquareClientSecret))
        {
            authBuilder.AddOAuth("square", "Square", options =>
            {
                bool isSandbox = appOptions.AuthSquareClientId.StartsWith("sandbox-", StringComparison.OrdinalIgnoreCase);

                string squareBaseUrl = isSandbox
                    ? "https://connect.squareupsandbox.com"
                    : "https://connect.squareup.com";

                options.ClientId = appOptions.AuthSquareClientId;
                options.ClientSecret = appOptions.AuthSquareClientSecret;
                options.CallbackPath = "/auth/providers/square/callback";
                options.SignInScheme = IdentityConstants.ExternalScheme;

                options.AuthorizationEndpoint = $"{squareBaseUrl}/oauth2/authorize";
                options.TokenEndpoint = $"{squareBaseUrl}/oauth2/token";
                options.UserInformationEndpoint = $"{squareBaseUrl}/v2/merchants/me";

                options.Scope.Add("MERCHANT_PROFILE_READ");

                // Map Square JSON object to standard ASP.NET Core Identity Claims
                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                options.ClaimActions.MapJsonKey(ClaimTypes.Email, "owner_email");
                options.ClaimActions.MapJsonKey("name", "business_name");

                options.Events.OnRedirectToAuthorizationEndpoint = context =>
                {
                    // Enforce explicit login for Production per Square OAuth guidelines
                    string authorizationUrl = isSandbox
                        ? context.RedirectUri
                        : Microsoft.AspNetCore.WebUtilities.QueryHelpers
                            .AddQueryString(context.RedirectUri, "session", "false");

                    context.Response.Redirect(authorizationUrl);
                    return Task.CompletedTask;
                };

                options.Events.OnCreatingTicket = async context =>
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                    // Square requires an explicit API version header
                    request.Headers.Add("Square-Version", "2024-01-18");

                    using var response = await context.Backchannel
                        .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);

                    response.EnsureSuccessStatusCode();

                    if (builder.Environment.IsDevelopment())
                    {
                        var squareProfile = await response.Content.ReadAsStringAsync();
                    }

                    using var document = await JsonDocument
                        .ParseAsync(await response.Content.ReadAsStreamAsync(context.HttpContext.RequestAborted));

                    JsonElement merchant = document.RootElement.GetProperty("merchant");

                    context.RunClaimActions(merchant);

                    // Synthesize the email_verified claim to satisfy existing IsExternalEmailVerified validation in IdentityEndpoints
                    if (merchant.TryGetProperty("owner_email", out JsonElement ownerEmailElement) &&
                        ownerEmailElement.ValueKind == JsonValueKind.String &&
                        !string.IsNullOrWhiteSpace(ownerEmailElement.GetString()))
                    {
                        context.Identity?.AddClaim(new Claim("email_verified", "true"));
                    }
                    else if (merchant.TryGetProperty("main_email", out JsonElement mainEmailElement) &&
                             mainEmailElement.ValueKind == JsonValueKind.String &&
                             !string.IsNullOrWhiteSpace(mainEmailElement.GetString()))
                    {
                        // Fallback for payloads that still emit main_email.
                        context.Identity?.AddClaim(new Claim(ClaimTypes.Email, mainEmailElement.GetString()!));
                        context.Identity?.AddClaim(new Claim("email_verified", "true"));
                    }
                };
            });
        }

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("OnboardedOnly", p => p.RequireClaim("onboarded", "true"));
        });

        builder.Services.ConfigureApplicationCookie(options =>
        {
            ConfigureAuthCookie(options.Cookie, authCookieSecurePolicy);
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.ExternalScheme, options =>
        {
            ConfigureAuthCookie(options.Cookie, authCookieSecurePolicy);
        });

        // for development phase use simple passwords
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 1;
                options.Password.RequiredUniqueChars = 0;
            });
        }

        return builder;
    }

    private static string BuildAuthPath(PathString pathBase, string authPath)
    {
        if (!authPath.StartsWith('/'))
        {
            throw new ArgumentException("Auth path must start with '/'.", nameof(authPath));
        }

        return pathBase.HasValue
            ? $"{pathBase}{authPath}"
            : authPath;
    }

    internal static void ConfigureAuthCookie(CookieBuilder cookie, CookieSecurePolicy securePolicy)
    {
        cookie.HttpOnly = true;
        cookie.Path = "/";
        cookie.SameSite = SameSiteMode.Lax;
        cookie.SecurePolicy = securePolicy;
    }

    internal static bool TryGetVerificationValue(JsonElement source, string propertyName, out bool result)
    {
        result = false;

        if (!source.TryGetProperty(propertyName, out JsonElement property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.True)
        {
            result = true;
            return true;
        }

        if (property.ValueKind == JsonValueKind.False)
        {
            result = false;
            return true;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        string? propertyValue = property.GetString();
        if (string.IsNullOrWhiteSpace(propertyValue))
        {
            return false;
        }

        string normalizedValue = propertyValue.Trim();
        if (string.Equals(normalizedValue, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedValue, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedValue, "yes", StringComparison.OrdinalIgnoreCase))
        {
            result = true;
            return true;
        }

        if (string.Equals(normalizedValue, "false", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedValue, "0", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedValue, "no", StringComparison.OrdinalIgnoreCase))
        {
            result = false;
            return true;
        }

        return false;
    }
}
