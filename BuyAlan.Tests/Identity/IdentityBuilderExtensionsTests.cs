namespace BuyAlan.Tests;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using BuyAlan.Identity;
using BuyAlan.WebApi.Identity;
using System.Text.Json;

public class IdentityBuilderExtensionsTests
{
    [Theory]
    [InlineData("", "/auth/external-callback?remoteError=external_provider_error")]
    [InlineData("/tenant", "/tenant/auth/external-callback?remoteError=external_provider_error")]
    public void BuildExternalProviderFailureCallbackUrl_ReturnsExpectedUrl(
        string pathBase,
        string expected)
    {
        PathString requestPathBase = String.IsNullOrWhiteSpace(pathBase)
            ? PathString.Empty
            : new PathString(pathBase);

        string callbackUrl = IdentityBuilderExtensions.BuildExternalProviderFailureCallbackUrl(requestPathBase);

        Assert.Equal(expected, callbackUrl);
    }

    [Theory]
    [InlineData("https://buyalan.test", "/auth/providers/google/callback", "https://buyalan.test/api/auth/providers/google/callback")]
    [InlineData("https://buyalan.test/", "/auth/providers/google/callback", "https://buyalan.test/api/auth/providers/google/callback")]
    [InlineData("https://buyalan.test/tenant", "/auth/providers/google/callback", "https://buyalan.test/tenant/api/auth/providers/google/callback")]
    [InlineData("https://buyalan.test/tenant/", "/auth/providers/google/callback", "https://buyalan.test/tenant/api/auth/providers/google/callback")]
    public void BuildAbsoluteAuthCallbackUrl_ReturnsExpectedUrl(
        string publicBaseUrl,
        string callbackPath,
        string expected)
    {
        Uri baseUri = new(publicBaseUrl);

        string callbackUrl = IdentityBuilderExtensions.BuildAbsoluteAuthCallbackUrl(baseUri, callbackPath);

        Assert.Equal(expected, callbackUrl);
    }

    [Fact]
    public void BuildAbsoluteAuthCallbackUrl_WhenCallbackPathInvalid_ThrowsArgumentException()
    {
        Uri baseUri = new("https://buyalan.test");

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            IdentityBuilderExtensions.BuildAbsoluteAuthCallbackUrl(baseUri, "auth/providers/google/callback"));

        Assert.Contains("Callback path must start with '/'", exception.Message);
    }

    [Theory]
    [InlineData("https://buyalan.test", "/api/subscriptions/square/callback", "https://buyalan.test/api/subscriptions/square/callback")]
    [InlineData("https://buyalan.test/", "/api/subscriptions/square/callback", "https://buyalan.test/api/subscriptions/square/callback")]
    [InlineData("https://buyalan.test/tenant", "/api/subscriptions/square/callback", "https://buyalan.test/tenant/api/subscriptions/square/callback")]
    [InlineData("https://buyalan.test/tenant/", "/api/subscriptions/square/callback", "https://buyalan.test/tenant/api/subscriptions/square/callback")]
    public void BuildAbsolutePublicPathUrl_ReturnsExpectedUrl(
        string publicBaseUrl,
        string path,
        string expected)
    {
        Uri baseUri = new(publicBaseUrl);

        string callbackUrl = IdentityBuilderExtensions.BuildAbsolutePublicPathUrl(baseUri, path);

        Assert.Equal(expected, callbackUrl);
    }

    [Fact]
    public void BuildAbsolutePublicPathUrl_WhenPathInvalid_ThrowsArgumentException()
    {
        Uri baseUri = new("https://buyalan.test");

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            IdentityBuilderExtensions.BuildAbsolutePublicPathUrl(baseUri, "api/subscriptions/square/callback"));

        Assert.Contains("Path must start with '/'", exception.Message);
    }

    [Fact]
    public void ReplaceQueryParameter_ReplacesExistingValue()
    {
        string authorizationUrl = "https://connect.squareup.com/oauth2/authorize?client_id=abc&redirect_uri=http%3A%2F%2Flocalhost%3A5000%2Fauth%2Fproviders%2Fsquare%2Fcallback&state=xyz";
        string rewritten = IdentityBuilderExtensions.ReplaceQueryParameter(
            authorizationUrl,
            "redirect_uri",
            "https://buyalan.test/api/auth/providers/square/callback");

        Uri rewrittenUri = new(rewritten);
        Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query = QueryHelpers.ParseQuery(rewrittenUri.Query);

        Assert.Equal("abc", query["client_id"].ToString());
        Assert.Equal("xyz", query["state"].ToString());
        Assert.Equal("https://buyalan.test/api/auth/providers/square/callback", query["redirect_uri"].ToString());
    }

    [Theory]
    [InlineData(CookieSecurePolicy.SameAsRequest)]
    [InlineData(CookieSecurePolicy.Always)]
    public void ConfigureAuthCookie_SetsExpectedCookieShape(CookieSecurePolicy securePolicy)
    {
        CookieBuilder cookie = new();

        IdentityBuilderExtensions.ConfigureAuthCookie(cookie, securePolicy);

        Assert.True(cookie.HttpOnly);
        Assert.Equal("/", cookie.Path);
        Assert.Equal(SameSiteMode.Lax, cookie.SameSite);
        Assert.Equal(securePolicy, cookie.SecurePolicy);
    }

    [Theory]
    [InlineData("{\"verified_email\":true}", "verified_email", true, true)]
    [InlineData("{\"verified_email\":false}", "verified_email", true, false)]
    [InlineData("{\"verified_email\":\"true\"}", "verified_email", true, true)]
    [InlineData("{\"verified_email\":\"1\"}", "verified_email", true, true)]
    [InlineData("{\"verified_email\":\"yes\"}", "verified_email", true, true)]
    [InlineData("{\"verified_email\":\"false\"}", "verified_email", true, false)]
    [InlineData("{\"verified_email\":\"0\"}", "verified_email", true, false)]
    [InlineData("{\"verified_email\":\"no\"}", "verified_email", true, false)]
    [InlineData("{\"email_verified\":true}", "email_verified", true, true)]
    [InlineData("{\"email_verified\":\"unexpected\"}", "email_verified", false, false)]
    [InlineData("{}", "verified_email", false, false)]
    public void TryGetVerificationValue_ParsesExpectedValues(
        string json,
        string propertyName,
        bool expectedFound,
        bool expectedValue)
    {
        using JsonDocument jsonDocument = JsonDocument.Parse(json);
        JsonElement root = jsonDocument.RootElement;

        bool found = IdentityBuilderExtensions.TryGetVerificationValue(root, propertyName, out bool result);

        Assert.Equal(expectedFound, found);
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task AddIdentityServices_WhenSquareCredentialsMissing_DoesNotRegisterSquareScheme()
    {
        IServiceProvider services = BuildServices(new Dictionary<string, string?>
        {
            ["PUBLIC_BASE_URL"] = "https://buyalan.test"
        });

        IAuthenticationSchemeProvider schemeProvider = services.GetRequiredService<IAuthenticationSchemeProvider>();
        IEnumerable<AuthenticationScheme> allSchemes = await schemeProvider.GetAllSchemesAsync();

        Assert.DoesNotContain(allSchemes, scheme => String.Equals(scheme.Name, "square", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AddIdentityServices_WhenSquareCredentialsPresent_RegistersSquareSchemeWithExpectedCallbackAndScope()
    {
        IServiceProvider services = BuildServices(new Dictionary<string, string?>
        {
            ["PUBLIC_BASE_URL"] = "https://buyalan.test",
            ["SQUARE_CLIENT_ID"] = "sandbox-client-id",
            ["SQUARE_CLIENT_SECRET"] = "square-client-secret"
        });

        IAuthenticationSchemeProvider schemeProvider = services.GetRequiredService<IAuthenticationSchemeProvider>();
        IEnumerable<AuthenticationScheme> allSchemes = await schemeProvider.GetAllSchemesAsync();
        AuthenticationScheme squareScheme = Assert.Single(
            allSchemes,
            scheme => String.Equals(scheme.Name, "square", StringComparison.OrdinalIgnoreCase));

        Assert.Equal("Square", squareScheme.DisplayName);

        IOptionsMonitor<SquareOAuthOptions> optionsMonitor = services.GetRequiredService<IOptionsMonitor<SquareOAuthOptions>>();
        SquareOAuthOptions squareOptions = optionsMonitor.Get("square");

        Assert.Equal("/auth/providers/square/callback", squareOptions.CallbackPath.Value);
        Assert.Contains("MERCHANT_PROFILE_READ", squareOptions.Scope);
    }

    [Fact]
    public async Task AddIdentityServices_WhenSquareCredentialsPresent_ConfiguresBrokerRedirectUriForCodeExchange()
    {
        IServiceProvider services = BuildServices(new Dictionary<string, string?>
        {
            ["PUBLIC_BASE_URL"] = "https://buyalan.test",
            ["SQUARE_CLIENT_ID"] = "sandbox-client-id",
            ["SQUARE_CLIENT_SECRET"] = "square-client-secret"
        });

        IOptionsMonitor<SquareOAuthOptions> optionsMonitor = services.GetRequiredService<IOptionsMonitor<SquareOAuthOptions>>();
        SquareOAuthOptions squareOptions = optionsMonitor.Get("square");
        AuthenticationProperties properties = new();

        OAuthCodeExchangeContext exchangeContext = SquareOAuthHandler.CreateBrokerCodeExchangeContext(
            properties,
            "authorization-code",
            SquareOAuthHandler.GetRequiredBrokerRedirectUri(squareOptions));

        Assert.Equal("https://buyalan.test/api/subscriptions/square/callback", squareOptions.BrokerRedirectUri);
        Assert.Equal("https://buyalan.test/api/subscriptions/square/callback", exchangeContext.RedirectUri);
        Assert.Equal("authorization-code", exchangeContext.Code);
        Assert.Same(properties, exchangeContext.Properties);
    }

    [Fact]
    public void AddIdentityServices_WhenSquareRedirectConfigured_UsesBrokerCallbackRedirectUri()
    {
        IServiceProvider services = BuildServices(new Dictionary<string, string?>
        {
            ["PUBLIC_BASE_URL"] = "https://buyalan.test",
            ["SQUARE_CLIENT_ID"] = "sandbox-client-id",
            ["SQUARE_CLIENT_SECRET"] = "square-client-secret"
        });

        IOptionsMonitor<SquareOAuthOptions> optionsMonitor = services.GetRequiredService<IOptionsMonitor<SquareOAuthOptions>>();
        SquareOAuthOptions squareOptions = optionsMonitor.Get("square");

        Assert.Equal("https://buyalan.test/api/subscriptions/square/callback", squareOptions.BrokerRedirectUri);
        Assert.False(squareOptions.IncludeSessionFalse);
    }

    [Fact]
    public void AddIdentityServices_WhenSquareRedirectConfiguredForProduction_PreservesSessionFalse()
    {
        IServiceProvider services = BuildServices(new Dictionary<string, string?>
        {
            ["PUBLIC_BASE_URL"] = "https://buyalan.test",
            ["SQUARE_CLIENT_ID"] = "production-client-id",
            ["SQUARE_CLIENT_SECRET"] = "square-client-secret"
        });

        IOptionsMonitor<SquareOAuthOptions> optionsMonitor = services.GetRequiredService<IOptionsMonitor<SquareOAuthOptions>>();
        SquareOAuthOptions squareOptions = optionsMonitor.Get("square");

        Assert.Equal("https://buyalan.test/api/subscriptions/square/callback", squareOptions.BrokerRedirectUri);
        Assert.True(squareOptions.IncludeSessionFalse);
    }

    [Theory]
    [InlineData(null, "default")]
    [InlineData("", "default")]
    [InlineData("/admin", "admin")]
    [InlineData("/onboarding?step=1", "onboarding")]
    [InlineData("/invite/token-123", "invite")]
    [InlineData("/orders/123", "other-local")]
    public void DescribeReturnTarget_ReturnsSanitizedRouteCategory(
        string? returnUrl,
        string expected)
    {
        string category = IdentityEndpoints.DescribeReturnTarget(returnUrl ?? String.Empty);

        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData("square", "", "/auth/external-callback?remoteError=external_provider_error")]
    [InlineData("square", "/tenant", "/tenant/auth/external-callback?remoteError=external_provider_error")]
    [InlineData("google", "", "/auth/external-callback?remoteError=external_provider_error")]
    public async Task AddIdentityServices_WhenRemoteFailureRaised_RedirectsToExternalCallbackAndHandlesResponse(
        string schemeName,
        string pathBase,
        string expectedLocation)
    {
        IServiceProvider services = BuildServices(new Dictionary<string, string?>
        {
            ["PUBLIC_BASE_URL"] = "https://buyalan.test",
            ["AUTH_GOOGLE_CLIENT_ID"] = "google-client-id",
            ["AUTH_GOOGLE_CLIENT_SECRET"] = "google-client-secret",
            ["SQUARE_CLIENT_ID"] = "sandbox-client-id",
            ["SQUARE_CLIENT_SECRET"] = "square-client-secret"
        });

        DefaultHttpContext httpContext = new();
        httpContext.Request.PathBase = String.IsNullOrWhiteSpace(pathBase)
            ? PathString.Empty
            : new PathString(pathBase);

        IAuthenticationSchemeProvider schemeProvider = services.GetRequiredService<IAuthenticationSchemeProvider>();
        AuthenticationScheme scheme = await schemeProvider.GetSchemeAsync(schemeName)
            ?? throw new InvalidOperationException($"Authentication scheme '{schemeName}' was not registered.");

        if (String.Equals(schemeName, "google", StringComparison.OrdinalIgnoreCase))
        {
            IOptionsMonitor<GoogleOptions> optionsMonitor = services.GetRequiredService<IOptionsMonitor<GoogleOptions>>();
            GoogleOptions googleOptions = optionsMonitor.Get("google");
            RemoteFailureContext remoteFailureContext = new(httpContext, scheme, googleOptions, new Exception("expired callback"));

            await googleOptions.Events.OnRemoteFailure(remoteFailureContext);

            Assert.True(remoteFailureContext.Result?.Handled ?? false);
        }
        else
        {
            IOptionsMonitor<SquareOAuthOptions> optionsMonitor = services.GetRequiredService<IOptionsMonitor<SquareOAuthOptions>>();
            SquareOAuthOptions squareOptions = optionsMonitor.Get("square");
            RemoteFailureContext remoteFailureContext = new(httpContext, scheme, squareOptions, new Exception("expired callback"));

            await squareOptions.Events.OnRemoteFailure(remoteFailureContext);

            Assert.True(remoteFailureContext.Result?.Handled ?? false);
        }

        Assert.Equal(StatusCodes.Status302Found, httpContext.Response.StatusCode);
        Assert.Equal(expectedLocation, httpContext.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task AddIdentityServices_WhenRemoteFailureRaisedWithoutRequestServices_StillHandlesResponse()
    {
        IServiceProvider services = BuildServices(new Dictionary<string, string?>
        {
            ["PUBLIC_BASE_URL"] = "https://buyalan.test",
            ["AUTH_GOOGLE_CLIENT_ID"] = "google-client-id",
            ["AUTH_GOOGLE_CLIENT_SECRET"] = "google-client-secret"
        });

        DefaultHttpContext httpContext = new();
        IAuthenticationSchemeProvider schemeProvider = services.GetRequiredService<IAuthenticationSchemeProvider>();
        AuthenticationScheme scheme = await schemeProvider.GetSchemeAsync("google")
            ?? throw new InvalidOperationException("Authentication scheme 'google' was not registered.");

        IOptionsMonitor<GoogleOptions> optionsMonitor = services.GetRequiredService<IOptionsMonitor<GoogleOptions>>();
        GoogleOptions googleOptions = optionsMonitor.Get("google");
        RemoteFailureContext remoteFailureContext = new(httpContext, scheme, googleOptions, new Exception("expired callback"));

        await googleOptions.Events.OnRemoteFailure(remoteFailureContext);

        Assert.True(remoteFailureContext.Result?.Handled ?? false);
        Assert.Equal(StatusCodes.Status302Found, httpContext.Response.StatusCode);
        Assert.Equal("/auth/external-callback?remoteError=external_provider_error", httpContext.Response.Headers.Location.ToString());
    }

    private static IServiceProvider BuildServices(IDictionary<string, string?> configurationValues)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddInMemoryCollection(configurationValues);
        builder.AddIdentityServices();

        IHost host = builder.Build();
        return host.Services;
    }
}
