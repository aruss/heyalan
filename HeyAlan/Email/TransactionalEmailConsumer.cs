namespace HeyAlan.Email;

using Microsoft.Extensions.Logging;

public sealed class TransactionalEmailConsumer
{
 
    private readonly ITransactionalEmailService transactionalEmailService;
    private readonly ILogger<TransactionalEmailConsumer> logger;

    public TransactionalEmailConsumer(
        ITransactionalEmailService transactionalEmailService,
        ILogger<TransactionalEmailConsumer> logger)
    {
        this.transactionalEmailService = transactionalEmailService ?? throw new ArgumentNullException(nameof(transactionalEmailService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(EmailSendRequested message, CancellationToken cancellationToken)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        this.logger.LogInformation(
            "Processing queued transactional email. TemplateKey={TemplateKey} To={MaskedEmail} TemplateFieldCount={TemplateFieldCount}",
            message.TemplateKey,
            MaskEmail(message.RecipientEmail),
            message.TemplateData.Count);

        await this.transactionalEmailService.SendTemplateAsync(
            message.RecipientEmail,
            message.TemplateKey,
            message.TemplateData,
            cancellationToken);
    }

    private static string MaskEmail(string email)
    {
        if (String.IsNullOrWhiteSpace(email))
        {
            return "<empty>";
        }

        int atIndex = email.IndexOf('@');
        if (atIndex <= 1)
        {
            return "***";
        }

        string prefix = email.Substring(0, 1);
        string domain = email.Substring(atIndex);
        return $"{prefix}***{domain}";
    }
}
