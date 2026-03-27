namespace BuyAlan.Tests;

using Microsoft.Extensions.Configuration;
using BuyAlan.Configuration;

public class AppOptionsTests
{
    [Fact]
    public void TryGetAppOptions_WhenOnlyPublicBaseUrlExists_ReturnsOptionsWithoutAuthProviderCredentials()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["PUBLIC_BASE_URL"] = "https://buyalan.test"
        });

        AppOptions appOptions = configuration.TryGetAppOptions();

        Assert.Equal(new Uri("https://buyalan.test"), appOptions.PublicBaseUrl);
        Assert.Null(appOptions.AuthGoogleClientId);
        Assert.Null(appOptions.AuthGoogleClientSecret);
        Assert.Null(appOptions.SquareClientId);
        Assert.Null(appOptions.SquareClientSecret);
    }

    [Fact]
    public void TryGetAppOptions_WhenBothAuthGoogleCredentialsExist_ReturnsTrimmedCredentials()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["PUBLIC_BASE_URL"] = "https://buyalan.test",
            ["AUTH_GOOGLE_CLIENT_ID"] = "  test-client-id  ",
            ["AUTH_GOOGLE_CLIENT_SECRET"] = "  test-client-secret  "
        });

        AppOptions appOptions = configuration.TryGetAppOptions();

        Assert.Equal("test-client-id", appOptions.AuthGoogleClientId);
        Assert.Equal("test-client-secret", appOptions.AuthGoogleClientSecret);
    }

    [Fact]
    public void TryGetAppOptions_WhenOnlyClientIdExists_ThrowsInvalidOperationException()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["PUBLIC_BASE_URL"] = "https://buyalan.test",
            ["AUTH_GOOGLE_CLIENT_ID"] = "test-client-id"
        });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            configuration.TryGetAppOptions());

        Assert.Contains("AUTH_GOOGLE_CLIENT_ID and AUTH_GOOGLE_CLIENT_SECRET", exception.Message);
    }

    [Fact]
    public void TryGetAppOptions_WhenOnlyClientSecretExists_ThrowsInvalidOperationException()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["PUBLIC_BASE_URL"] = "https://buyalan.test",
            ["AUTH_GOOGLE_CLIENT_SECRET"] = "test-client-secret"
        });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            configuration.TryGetAppOptions());

        Assert.Contains("AUTH_GOOGLE_CLIENT_ID and AUTH_GOOGLE_CLIENT_SECRET", exception.Message);
    }

    [Fact]
    public void TryGetAppOptions_WhenPublicBaseUrlIsMissing_ThrowsInvalidOperationException()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            configuration.TryGetAppOptions());

        Assert.Contains("PUBLIC_BASE_URL", exception.Message);
    }

    [Fact]
    public void TryGetAppOptions_WhenPublicBaseUrlIsInvalid_ThrowsInvalidOperationException()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["PUBLIC_BASE_URL"] = "not-a-valid-url"
        });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            configuration.TryGetAppOptions());

        Assert.Contains("PUBLIC_BASE_URL", exception.Message);
    }

    [Fact]
    public void TryGetAppOptions_WhenBothConnectionSquareCredentialsExist_ReturnsTrimmedSquareCredentials()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["PUBLIC_BASE_URL"] = "https://buyalan.test",
            ["SQUARE_CLIENT_ID"] = "  square-client-id  ",
            ["SQUARE_CLIENT_SECRET"] = "  square-client-secret  "
        });

        AppOptions appOptions = configuration.TryGetAppOptions();

        Assert.Equal("square-client-id", appOptions.SquareClientId);
        Assert.Equal("square-client-secret", appOptions.SquareClientSecret);
    }

    [Fact]
    public void TryGetAppOptions_WhenSquareWebhookSignatureKeyExists_ReturnsTrimmedValue()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["PUBLIC_BASE_URL"] = "https://buyalan.test",
            ["SQUARE_WEBHOOK_SIGNATURE_KEY"] = "  webhook-signature-key  "
        });

        AppOptions appOptions = configuration.TryGetAppOptions();

        Assert.Equal("webhook-signature-key", appOptions.SquareWebhookSignatureKey);
    }

    [Fact]
    public void TryGetAppOptions_WhenOnlySquareClientIdExists_ThrowsInvalidOperationException()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["PUBLIC_BASE_URL"] = "https://buyalan.test",
            ["SQUARE_CLIENT_ID"] = "square-client-id"
        });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            configuration.TryGetAppOptions());

        Assert.Contains("SQUARE_CLIENT_ID and SQUARE_CLIENT_SECRET", exception.Message);
    }

    [Fact]
    public void TryGetAppOptions_WhenOnlySquareClientSecretExists_ThrowsInvalidOperationException()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["PUBLIC_BASE_URL"] = "https://buyalan.test",
            ["SQUARE_CLIENT_SECRET"] = "square-client-secret"
        });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            configuration.TryGetAppOptions());

        Assert.Contains("SQUARE_CLIENT_ID and SQUARE_CLIENT_SECRET", exception.Message);
    }

    private static IConfiguration CreateConfiguration(IDictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
