namespace SquareBuddy.TelegramIntegration;

public interface ITelegramService
{
    Task RegisterWebhookAsync(string botToken, CancellationToken ct = default);
}
