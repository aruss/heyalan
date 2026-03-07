namespace HeyAlan.TelegramIntegration;

using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using HeyAlan.Configuration;
using System.Net;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
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
        if (String.IsNullOrWhiteSpace(botToken))
        {
            throw new ArgumentException("Telegram bot token is required.", nameof(botToken));
        }  

        string normalizedBotToken = botToken.Trim();

        Uri webhookUri = new(
            this.appOptions.PublicBaseUrl,
            $"/api/webhooks/telegram/{normalizedBotToken}");

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

    public async Task TryRegisterWebhookAsync(string botToken, CancellationToken ct = default)
    {
        AsyncRetryPolicy retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    this.logger.LogWarning(
                        exception,
                        "Webhook registration failed. Retrying ({RetryCount}/3) in {Delay}s. BotId: {BotId}.",
                        retryCount,
                        timeSpan.TotalSeconds,
                        ExtractBotId(botToken));
                });

        await retryPolicy.ExecuteAsync(async token =>
        {
            await RegisterWebhookAsync(botToken, token);
        }, ct);
    }

    public async Task<TelegramTokenRegistrationResult> RegisterWebhookIfTokenChangedAsync(
        string? previousBotToken,
        string? nextBotToken,
        CancellationToken ct = default)
    {
        string? normalizedPreviousBotToken = NormalizeOptionalToken(previousBotToken);
        string? normalizedNextBotToken = NormalizeOptionalToken(nextBotToken);

        if (String.IsNullOrWhiteSpace(normalizedNextBotToken) ||
            String.Equals(normalizedNextBotToken, normalizedPreviousBotToken, StringComparison.Ordinal))
        {
            return new TelegramTokenRegistrationResult(
                WasAttempted: false,
                ErrorCode: null);
        }

        try
        {
            await this.TryRegisterWebhookAsync(normalizedNextBotToken, ct);

            return new TelegramTokenRegistrationResult(
                WasAttempted: true,
                ErrorCode: null);
        }
        catch (ApiRequestException exception)
        {
            return new TelegramTokenRegistrationResult(
                WasAttempted: true,
                ErrorCode: ResolveWebhookRegistrationErrorCode(exception));
        }
        catch (Exception)
        {
            return new TelegramTokenRegistrationResult(
                WasAttempted: true,
                ErrorCode: "telegram_webhook_registration_failed");
        }
    }

    public async Task SendMessageAsync(string botToken, long chatId, string text, CancellationToken ct = default)
    {
        if (String.IsNullOrWhiteSpace(botToken))
        {
            throw new ArgumentException("Telegram bot token is required.", nameof(botToken));
        }

        if (String.IsNullOrWhiteSpace(text))
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

    private static string? NormalizeOptionalToken(string? value)
    {
        if (String.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string ResolveWebhookRegistrationErrorCode(ApiRequestException exception)
    {
        if (exception.ErrorCode == (int)HttpStatusCode.Unauthorized)
        {
            return "telegram_bot_token_invalid";
        }

        return "telegram_webhook_registration_failed";
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
