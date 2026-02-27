namespace ShelfBuddy.TelegramIntegration;

using Microsoft.Extensions.Logging;
using ShelfBuddy.Configuration;
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

    public async Task SendMessageAsync(string botToken, long chatId, string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(botToken))
        {
            throw new ArgumentException("Telegram bot token is required.", nameof(botToken));
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Telegram message text is required.", nameof(text));
        }

        string normalizedBotToken = botToken.Trim();
        string trimmedText = text.Trim();

        this.logger.LogInformation(
            "Sending Telegram message for bot id {BotId} to chat {ChatId}.",
            ExtractBotId(normalizedBotToken),
            chatId);

        await this.telegramClientFactory
            .GetClient(normalizedBotToken)
            .SendMessage(
                chatId: chatId,
                text: trimmedText,
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
