namespace HeyAlan.Email;

public interface ITransactionalEmailService
{
    Task SendTemplateAsync(
        string recipientEmail,
        string templateId,
        IReadOnlyDictionary<string, string> templateData,
        CancellationToken cancellationToken = default);
}
