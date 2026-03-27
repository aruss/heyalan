namespace BuyAlan.Identity;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using BuyAlan.Configuration;
using BuyAlan.Data;
using BuyAlan.Data.Entities;
using BuyAlan.SquareIntegration;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

public static class IdentityBuilderExtensions
{
    private const string ExternalApiPathPrefix = "/api";
    private const string GoogleProviderCallbackPath = "/auth/providers/google/callback";
    private const string SquareProviderCallbackPath = "/auth/providers/square/callback";

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
            string googleCallbackUrl = BuildAbsoluteAuthCallbackUrl(
                appOptions.PublicBaseUrl,
                GoogleProviderCallbackPath);

            authBuilder.AddGoogle("google", "Google", options =>
            {
                options.ClientId = appOptions.AuthGoogleClientId;
                options.ClientSecret = appOptions.AuthGoogleClientSecret;
                options.CallbackPath = GoogleProviderCallbackPath;
                options.UserInformationEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.Events.OnRedirectToAuthorizationEndpoint = context =>
                {
                    string authorizationUrl = ReplaceQueryParameter(
                        context.RedirectUri,
                        "redirect_uri",
                        googleCallbackUrl);

                    context.Response.Redirect(authorizationUrl);
                    return Task.CompletedTask;
                };
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
                    string callbackUrl = BuildExternalProviderFailureCallbackUrl(context.Request.PathBase);
                    context.Response.Redirect(callbackUrl);
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
            });
        }

        if (!String.IsNullOrWhiteSpace(appOptions.SquareClientId) &&
            !String.IsNullOrWhiteSpace(appOptions.SquareClientSecret))
        {
            string squareCallbackUrl = BuildAbsolutePublicPathUrl(
                appOptions.PublicBaseUrl,
                SquareIntegrationRules.ConnectCallbackPath);

            authBuilder.AddOAuth<SquareOAuthOptions, SquareOAuthHandler>("square", "Square", options =>
            {
                bool isSandbox = appOptions.SquareClientId.StartsWith("sandbox-", StringComparison.OrdinalIgnoreCase);

                string squareBaseUrl = isSandbox
                    ? "https://connect.squareupsandbox.com"
                    : "https://connect.squareup.com";

                options.ClientId = appOptions.SquareClientId;
                options.ClientSecret = appOptions.SquareClientSecret;
                options.CallbackPath = SquareProviderCallbackPath;
                options.BrokerRedirectUri = squareCallbackUrl;
                options.IncludeSessionFalse = !isSandbox;
                options.SignInScheme = IdentityConstants.ExternalScheme;

                options.AuthorizationEndpoint = $"{squareBaseUrl}/oauth2/authorize";
                options.TokenEndpoint = $"{squareBaseUrl}/oauth2/token";
                options.UserInformationEndpoint = $"{squareBaseUrl}/v2/merchants/me";

                options.Scope.Add("MERCHANT_PROFILE_READ");

                // Map Square JSON object to standard ASP.NET Core Identity Claims
                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                options.ClaimActions.MapJsonKey(ClaimTypes.Email, "owner_email");
                options.ClaimActions.MapJsonKey("name", "business_name");

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
                        !String.IsNullOrWhiteSpace(ownerEmailElement.GetString()))
                    {
                        context.Identity?.AddClaim(new Claim("email_verified", "true"));
                    }
                    else if (merchant.TryGetProperty("main_email", out JsonElement mainEmailElement) &&
                             mainEmailElement.ValueKind == JsonValueKind.String &&
                             !String.IsNullOrWhiteSpace(mainEmailElement.GetString()))
                    {
                        // Fallback for payloads that still emit main_email.
                        context.Identity?.AddClaim(new Claim(ClaimTypes.Email, mainEmailElement.GetString()!));
                        context.Identity?.AddClaim(new Claim("email_verified", "true"));
                    }
                };

                options.Events.OnRemoteFailure = context =>
                {
                    string callbackUrl = BuildExternalProviderFailureCallbackUrl(context.Request.PathBase);
                    context.Response.Redirect(callbackUrl);
                    context.HandleResponse();
                    return Task.CompletedTask;
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

    internal static string BuildExternalProviderFailureCallbackUrl(PathString pathBase)
    {
        string callbackPath = BuildAuthPath(pathBase, "/auth/external-callback");
        return QueryHelpers.AddQueryString(
            callbackPath,
            "remoteError",
            "external_provider_error");
    }

    internal static string BuildAbsoluteAuthCallbackUrl(Uri publicBaseUrl, string callbackPath)
    {
        if (!callbackPath.StartsWith('/'))
        {
            throw new ArgumentException("Callback path must start with '/'.", nameof(callbackPath));
        }

        string normalizedBasePath = publicBaseUrl.AbsolutePath.TrimEnd('/');

        PathString basePath = String.Equals(normalizedBasePath, "/", StringComparison.Ordinal) ||
            String.IsNullOrWhiteSpace(normalizedBasePath)
                ? PathString.Empty
                : new PathString(normalizedBasePath);

        PathString fullPath = basePath
            .Add(ExternalApiPathPrefix)
            .Add(callbackPath);

        UriBuilder uriBuilder = new(publicBaseUrl)
        {
            Path = fullPath.Value,
            Query = String.Empty,
            Fragment = String.Empty
        };

        return uriBuilder.Uri.ToString();
    }

    internal static string BuildAbsolutePublicPathUrl(Uri publicBaseUrl, string path)
    {
        if (!path.StartsWith('/'))
        {
            throw new ArgumentException("Path must start with '/'.", nameof(path));
        }

        string normalizedBasePath = publicBaseUrl.AbsolutePath.TrimEnd('/');

        PathString basePath = String.Equals(normalizedBasePath, "/", StringComparison.Ordinal) ||
            String.IsNullOrWhiteSpace(normalizedBasePath)
                ? PathString.Empty
                : new PathString(normalizedBasePath);

        PathString fullPath = basePath.Add(path);

        UriBuilder uriBuilder = new(publicBaseUrl)
        {
            Path = fullPath.Value,
            Query = String.Empty,
            Fragment = String.Empty
        };

        return uriBuilder.Uri.ToString();
    }

    internal static string ReplaceQueryParameter(string url, string key, string value)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? parsedUri))
        {
            throw new ArgumentException("URL must be an absolute URI.", nameof(url));
        }

        Dictionary<string, StringValues> parsedQuery = QueryHelpers.ParseQuery(parsedUri.Query);
        QueryBuilder queryBuilder = new();

        foreach (KeyValuePair<string, StringValues> queryItem in parsedQuery)
        {
            if (String.Equals(queryItem.Key, key, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (string? queryValue in queryItem.Value)
            {
                queryBuilder.Add(queryItem.Key, queryValue ?? String.Empty);
            }
        }

        queryBuilder.Add(key, value);

        string queryString = queryBuilder.ToQueryString().Value ?? String.Empty;
        UriBuilder uriBuilder = new(parsedUri)
        {
            Query = queryString.TrimStart('?')
        };

        return uriBuilder.Uri.ToString();
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
        if (String.IsNullOrWhiteSpace(propertyValue))
        {
            return false;
        }

        string normalizedValue = propertyValue.Trim();
        if (String.Equals(normalizedValue, "true", StringComparison.OrdinalIgnoreCase) ||
            String.Equals(normalizedValue, "1", StringComparison.OrdinalIgnoreCase) ||
            String.Equals(normalizedValue, "yes", StringComparison.OrdinalIgnoreCase))
        {
            result = true;
            return true;
        }

        if (String.Equals(normalizedValue, "false", StringComparison.OrdinalIgnoreCase) ||
            String.Equals(normalizedValue, "0", StringComparison.OrdinalIgnoreCase) ||
            String.Equals(normalizedValue, "no", StringComparison.OrdinalIgnoreCase))
        {
            result = false;
            return true;
        }

        return false;
    }
}
