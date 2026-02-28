namespace ShelfBuddy.Tests;

using Microsoft.AspNetCore.Http;
using ShelfBuddy.WebApi.Identity;
using System.Security.Claims;

public class IdentityEndpointsSecurityTests
{
    [Theory]
    [InlineData(null, "/admin")]
    [InlineData("", "/admin")]
    [InlineData(" ", "/admin")]
    [InlineData("/admin/inbox", "/admin/inbox")]
    [InlineData("http://evil.test", "/admin")]
    [InlineData("//evil.test/path", "/admin")]
    [InlineData("admin/inbox", "/admin")]
    public void NormalizeReturnUrl_ReturnsExpectedValue(string? input, string expected)
    {
        string normalized = IdentityEndpoints.NormalizeReturnUrl(input);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("email_verified", "true", true)]
    [InlineData("email_verified", "True", true)]
    [InlineData("verified_email", "1", true)]
    [InlineData("urn:google:email_verified", "yes", true)]
    [InlineData("email_verified", "false", false)]
    [InlineData("email_verified", "0", false)]
    [InlineData("unrelated", "true", false)]
    public void IsExternalEmailVerified_ParsesVerificationClaims(string claimType, string claimValue, bool expected)
    {
        ClaimsPrincipal principal = CreatePrincipal(new Claim(claimType, claimValue));

        bool isVerified = IdentityEndpoints.IsExternalEmailVerified(principal);

        Assert.Equal(expected, isVerified);
    }

    [Fact]
    public void ResolvePostLoginRedirectTarget_WhenNotOnboarded_ReturnsOnboardingRoute()
    {
        string redirectTarget = IdentityEndpoints.ResolvePostLoginRedirectTarget("/admin/inbox", isOnboarded: false);
        Assert.Equal("/onboarding", redirectTarget);
    }

    [Fact]
    public void ResolvePostLoginRedirectTarget_WhenOnboarded_ReturnsRequestedRoute()
    {
        string redirectTarget = IdentityEndpoints.ResolvePostLoginRedirectTarget("/admin/inbox", isOnboarded: true);
        Assert.Equal("/admin/inbox", redirectTarget);
    }

    [Theory]
    [InlineData("", "/auth/external-callback", "/auth/external-callback")]
    [InlineData("/api", "/auth/external-callback", "/api/auth/external-callback")]
    public void BuildAuthPath_ReturnsExpectedPath(string pathBase, string authPath, string expected)
    {
        string result = IdentityEndpoints.BuildAuthPath(new PathString(pathBase), authPath);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildAuthPath_WhenAuthPathIsInvalid_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            IdentityEndpoints.BuildAuthPath(new PathString("/api"), "auth/external-callback"));

        Assert.Contains("Auth path must start with '/'", exception.Message);
    }

    [Theory]
    [InlineData("/admin/inbox", "/admin/inbox")]
    [InlineData("/admin/inbox?foo=bar", "/admin/inbox?foo=bar")]
    [InlineData("/admin/inbox?authError=external_email_not_verified", "/admin/inbox")]
    [InlineData("/admin/inbox?foo=bar&authError=external_email_not_verified&bar=baz", "/admin/inbox?foo=bar&bar=baz")]
    [InlineData("/admin/inbox?authError=a&authError=b&x=1", "/admin/inbox?x=1")]
    public void RemoveAuthErrorFromReturnUrl_ReturnsExpectedValue(string input, string expected)
    {
        string cleanedReturnUrl = IdentityEndpoints.RemoveAuthErrorFromReturnUrl(input);

        Assert.Equal(expected, cleanedReturnUrl);
    }

    [Fact]
    public void BuildLoginRedirectUrl_UsesTopLevelAuthErrorAndSanitizedReturnUrl()
    {
        string redirectUrl = IdentityEndpoints.BuildLoginRedirectUrl(
            "/admin/inbox?authError=old_error&filter=unread",
            "external_email_not_verified");

        Assert.Equal(
            "/login?returnUrl=%2Fadmin%2Finbox%3Ffilter%3Dunread&authError=external_email_not_verified",
            redirectUrl);
    }

    [Fact]
    public void BuildLoginRedirectUrl_DoesNotDuplicateAuthError()
    {
        string redirectUrl = IdentityEndpoints.BuildLoginRedirectUrl(
            "/admin?authError=first&authError=second",
            "external_provider_error");

        Assert.Equal(
            "/login?returnUrl=%2Fadmin&authError=external_provider_error",
            redirectUrl);
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
    {
        ClaimsIdentity identity = new(claims, "test");
        return new ClaimsPrincipal(identity);
    }
}
