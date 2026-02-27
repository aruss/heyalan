namespace ShelfBuddy.TelegramIntegration;

using Microsoft.Extensions.Configuration;
using ShelfBuddy.Configuration;

public static class TelegramConfigurationExtensions
{
    public static TelegramOptions TryGetTelegramOptions(this IConfiguration configuration)
    {
        var secretToken = configuration["TELEGRAM_SECRET_TOKEN"]
            ?? throw ConfigurationErrors.Missing("TELEGRAM_SECRET_TOKEN");

        return new TelegramOptions
        {
            SecretToken = secretToken
        };
    }
}

public record TelegramOptions
{
    public string SecretToken { get; init; } = string.Empty;
}
