namespace BuyAlan.Tests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(" value ", "value")]
    public void TrimToNull_ReturnsExpectedValue(string? input, string? expected)
    {
        string? normalized = input.TrimToNull();

        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(" value ", "value")]
    public void TrimOrEmpty_ReturnsExpectedValue(string? input, string expected)
    {
        string normalized = input.TrimOrEmpty();

        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(" Hello World ", "hello world")]
    public void NormalizeSearchQuery_ReturnsExpectedValue(string? input, string? expected)
    {
        string? normalized = input.NormalizeSearchQuery();

        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData(null, false, "")]
    [InlineData("", false, "")]
    [InlineData(" person@example.com ", true, "person@example.com")]
    [InlineData("invalid", false, "")]
    [InlineData("person@example.com extra", false, "")]
    public void TryNormalizeEmail_ReturnsExpectedResult(string? input, bool expectedSuccess, string expectedEmail)
    {
        bool success = input.TryNormalizeEmail(out string normalizedEmail);

        Assert.Equal(expectedSuccess, success);
        Assert.Equal(expectedEmail, normalizedEmail);
    }

    [Theory]
    [InlineData(null, "<empty>")]
    [InlineData("", "<empty>")]
    [InlineData("a@example.com", "***")]
    [InlineData("person@example.com", "p***@example.com")]
    public void RedactEmail_ReturnsExpectedValue(string? input, string expected)
    {
        string redacted = input.RedactEmail();

        Assert.Equal(expected, redacted);
    }

    [Theory]
    [InlineData(null, "/admin")]
    [InlineData("", "/admin")]
    [InlineData("/admin/inbox", "/admin/inbox")]
    [InlineData("//evil.test/path", "/admin")]
    [InlineData("http://evil.test", "/admin")]
    [InlineData("admin/inbox", "/admin")]
    public void NormalizeLocalUrlOrDefault_ReturnsExpectedValue(string? input, string expected)
    {
        string normalized = input.NormalizeLocalUrlOrDefault("/admin");

        Assert.Equal(expected, normalized);
    }

    

}
