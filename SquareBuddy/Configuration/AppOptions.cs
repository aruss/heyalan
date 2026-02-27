namespace SquareBuddy.Configuration;

using Microsoft.Extensions.Configuration;

public static class AppOptionsConfigurationExtensions
{
    public static AppOptions TryGetAppOptions(this IConfiguration configuration)
    {
        var endpointRaw = configuration["PUBLIC_BASE_URL"]
            ?? throw ConfigurationErrors.Missing("PUBLIC_BASE_URL");

        if (!Uri.TryCreate(endpointRaw, UriKind.Absolute, out var endpoint))
            throw ConfigurationErrors.Invalid("PUBLIC_BASE_URL");

        return new AppOptions
        {
            PublicBaseUrl = endpoint,
        };
    }
}

public record AppOptions
{
    public Uri PublicBaseUrl { get; init; }
}
