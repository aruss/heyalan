namespace ShelfBuddy.TelegramIntegration;

public interface ITelegramService
{
    Task RegisterWebhookAsync(string botToken, CancellationToken ct = default);

    Task SendMessageAsync(string botToken, long chatId, string text, CancellationToken ct = default);
}
