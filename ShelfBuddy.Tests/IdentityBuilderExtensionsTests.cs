namespace ShelfBuddy.Tests;

using Microsoft.AspNetCore.Http;
using ShelfBuddy.WebApi.Identity;
using System.Text.Json;

public class IdentityBuilderExtensionsTests
{
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
}
