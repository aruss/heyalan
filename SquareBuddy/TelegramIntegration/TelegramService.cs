namespace SquareBuddy.TelegramIntegration;

using Microsoft.Extensions.Logging;
using SquareBuddy.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

public sealed class TelegramService : ITelegramService
{
    private static readonly UpdateType[] messageOnlyUpdates = [UpdateType.Message];

    private readonly TelegramClientFactory telegramClientFactory;
    private readonly TelegramOptions telegramOptions;
    private readonly AppOptions appOptions;
    private readonly ILogger<TelegramService> logger;

    public TelegramService(
        TelegramClientFactory telegramClientFactory,
        TelegramOptions telegramOptions,
        AppOptions appOptions,
        ILogger<TelegramService> logger)
    {
        this.telegramClientFactory = telegramClientFactory 
            ?? throw new ArgumentNullException(nameof(telegramClientFactory));

        this.telegramOptions = telegramOptions 
            ?? throw new ArgumentNullException(nameof(telegramOptions));

        this.appOptions = appOptions 
            ?? throw new ArgumentNullException(nameof(appOptions));

        this.logger = logger 
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RegisterWebhookAsync(string botToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(botToken))
        {
            throw new ArgumentException("Telegram bot token is required.", nameof(botToken));
        }  

        string normalizedBotToken = botToken.Trim();

        Uri webhookUri = new(
            this.appOptions.PublicBaseUrl,
            $"webhooks/telegram/{normalizedBotToken}");

        this.logger.LogInformation(
            "Registering Telegram webhook URL for bot id {BotId}.", 
            ExtractBotId(normalizedBotToken));

        await this.telegramClientFactory
            .GetClient(normalizedBotToken)
            .SetWebhook(
                url: webhookUri.ToString(),
                allowedUpdates: messageOnlyUpdates,
                secretToken: this.telegramOptions.SecretToken,
                cancellationToken: ct);
    }

    private static string ExtractBotId(string botToken)
    {
        int separatorIndex = botToken.IndexOf(':');

        if (separatorIndex <= 0)
        {
            return "unknown";
        }

        return botToken[..separatorIndex];
    }
}
