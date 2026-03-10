namespace HeyAlan.Newsletter;

using HeyAlan.Configuration;
using Microsoft.Extensions.Configuration;

public sealed record NewsletterOptions
{
    public int ConfirmTokenTtlMinutes { get; init; } = 24 * 60;
}

public static class NewsletterOptionsConfigurationExtensions
{
    public static NewsletterOptions TryGetNewsletterOptions(this IConfiguration configuration)
    {
        string? tokenTtlMinutesRaw = configuration.GetTrimmedValue("NEWSLETTER_CONFIRM_TOKEN_TTL_MINUTES");
        int tokenTtlMinutes = 24 * 60;
        if (!String.IsNullOrWhiteSpace(tokenTtlMinutesRaw))
        {
            string normalizedTokenTtlMinutesRaw = tokenTtlMinutesRaw.Trim();
            if (!int.TryParse(normalizedTokenTtlMinutesRaw, out tokenTtlMinutes) || tokenTtlMinutes <= 0)
            {
                throw ConfigurationErrors.Invalid("NEWSLETTER_CONFIRM_TOKEN_TTL_MINUTES");
            }
        }

        return new NewsletterOptions
        {
            ConfirmTokenTtlMinutes = tokenTtlMinutes
        };
    }
}
