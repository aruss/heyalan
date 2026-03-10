namespace HeyAlan.Email;

public interface IEmailQueuingService
{
    Task EnqueueAsync(EmailSendRequested message, CancellationToken cancellationToken = default);
}
