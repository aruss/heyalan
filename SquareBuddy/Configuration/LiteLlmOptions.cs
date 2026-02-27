namespace SquareBuddy.Configuration;

using Microsoft.Extensions.Configuration;

public static class LiteLlmConfigurationExtensions
{
    public static LiteLlmOptions TryGetLiteLlmOptions(this IConfiguration configuration)
    {
        var endpointRaw = configuration["LITELLM_ENDPOINT"]
            ?? throw ConfigurationErrors.Missing("LITELLM_ENDPOINT");

        if (!Uri.TryCreate(endpointRaw, UriKind.Absolute, out var endpoint))
        {
            throw ConfigurationErrors.Invalid("LITELLM_ENDPOINT");
        }

        var apiKey = configuration["LITELLM_API_KEY"]
            ?? throw ConfigurationErrors.Missing("LITELLM_API_KEY");

        return new LiteLlmOptions
        {
            Endpoint = endpoint,
            ApiKey = apiKey
        };
    }
}

public record LiteLlmOptions
{
    public Uri? Endpoint { get; init; }

    public string ApiKey { get; init; } = string.Empty;
}
