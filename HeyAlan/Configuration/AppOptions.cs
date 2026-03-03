namespace HeyAlan.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

public static class AppOptionsConfigurationExtensions
{
    public static AppOptions TryGetAppOptions(this IConfiguration configuration)
    {
        string endpointRaw = configuration["PUBLIC_BASE_URL"]
            ?? throw ConfigurationErrors.Missing("PUBLIC_BASE_URL");

        if (!Uri.TryCreate(endpointRaw, UriKind.Absolute, out var endpoint))
        {
            throw ConfigurationErrors.Invalid("PUBLIC_BASE_URL");
        }


        AppOptions options = new()
        {
            PublicBaseUrl = endpoint,
            AuthGoogleClientId = NormalizeOptional(configuration["AUTH_GOOGLE_CLIENT_ID"]),
            AuthGoogleClientSecret = NormalizeOptional(configuration["AUTH_GOOGLE_CLIENT_SECRET"]),
            AuthSquareClientId = NormalizeOptional(configuration["AUTH_SQUARE_CLIENT_ID"]),
            AuthSquareClientSecret = NormalizeOptional(configuration["AUTH_SQUARE_CLIENT_SECRET"]),
            SquareClientId = NormalizeOptional(configuration["SQUARE_CLIENT_ID"]),
            SquareClientSecret = NormalizeOptional(configuration["SQUARE_CLIENT_SECRET"])
        };

        ValidatePair(
            options.AuthGoogleClientId,
            options.AuthGoogleClientSecret,
            "AUTH_GOOGLE_CLIENT_ID and AUTH_GOOGLE_CLIENT_SECRET must both be set or both be missing");

        ValidatePair(
            options.AuthSquareClientId,
            options.AuthSquareClientSecret,
            "AUTH_SQUARE_CLIENT_ID and AUTH_SQUARE_CLIENT_SECRET must both be set or both be missing");

        ValidatePair(
            options.SquareClientId,
            options.SquareClientSecret,
            "SQUARE_CLIENT_ID and SQUARE_CLIENT_SECRET must both be set or both be missing");

        return options;
    }

    private static void ValidatePair(string? firstValue, string? secondValue, string errorMessage)
    {
        bool hasFirstValue = !string.IsNullOrWhiteSpace(firstValue);
        bool hasSecondValue = !string.IsNullOrWhiteSpace(secondValue);

        if (hasFirstValue != hasSecondValue)
        {
            throw ConfigurationErrors.Invalid(errorMessage);
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}

public record AppOptions
{
    public Uri PublicBaseUrl { get; init; }

    public string? AuthGoogleClientId { get; init; }

    public string? AuthGoogleClientSecret { get; init; }

    public string? AuthSquareClientId { get; init; }

    public string? AuthSquareClientSecret { get; init; }

    public string? SquareClientId { get; init; }

    public string? SquareClientSecret { get; init; }
}
