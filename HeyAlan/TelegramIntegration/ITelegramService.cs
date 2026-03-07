namespace HeyAlan.TelegramIntegration;

public interface ITelegramService
{
    Task RegisterWebhookAsync(string botToken, CancellationToken ct = default);

    Task TryRegisterWebhookAsync(string botToken, CancellationToken ct = default);

    Task<TelegramTokenRegistrationResult> RegisterWebhookIfTokenChangedAsync(
        string? previousBotToken,
        string? nextBotToken,
        CancellationToken ct = default);

    Task SendMessageAsync(string botToken, long chatId, string text, CancellationToken ct = default);
}
