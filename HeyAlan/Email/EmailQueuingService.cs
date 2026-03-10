namespace HeyAlan.Email;

using Microsoft.Extensions.Logging;
using Wolverine;

public sealed class EmailQueuingService : IEmailQueuingService
{
    private readonly IMessageBus messageBus;
    private readonly ILogger<EmailQueuingService> logger;

    public EmailQueuingService(
        IMessageBus messageBus,
        ILogger<EmailQueuingService> logger)
    {
        this.messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task EnqueueAsync(EmailSendRequested message, CancellationToken cancellationToken = default)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        string normalizedRecipientEmail = NormalizeRequiredRecipientEmail(message.RecipientEmail);
        string normalizedTemplateKey = NormalizeRequiredTemplateKey(message.TemplateKey);
        Dictionary<string, string> normalizedTemplateData = NormalizeRequiredTemplateData(message.TemplateData);

        EmailSendRequested normalizedMessage = new(
            normalizedRecipientEmail,
            normalizedTemplateKey,
            normalizedTemplateData);

        this.logger.LogInformation(
            "Queued transactional email. TemplateKey={TemplateKey} To={MaskedEmail} TemplateFieldCount={TemplateFieldCount}",
            normalizedTemplateKey,
            MaskEmail(normalizedRecipientEmail),
            normalizedTemplateData.Count);

        return this.messageBus.SendAsync(normalizedMessage).AsTask();
    }

    private static string NormalizeRequiredRecipientEmail(string? recipientEmail)
    {
        if (String.IsNullOrWhiteSpace(recipientEmail))
        {
            throw new ArgumentException("Recipient email is required.", nameof(recipientEmail));
        }

        return recipientEmail.Trim();
    }

    private static string NormalizeRequiredTemplateKey(string? templateKey)
    {
        if (String.IsNullOrWhiteSpace(templateKey))
        {
            throw new ArgumentException("Template key is required.", nameof(templateKey));
        }

        string normalizedTemplateKey = templateKey.Trim();
        if (!EmailTemplateKey.IsSupported(normalizedTemplateKey))
        {
            throw new ArgumentException(
                $"Template key '{normalizedTemplateKey}' is not supported.",
                nameof(templateKey));
        }

        return normalizedTemplateKey;
    }

    private static Dictionary<string, string> NormalizeRequiredTemplateData(
        Dictionary<string, string>? templateData)
    {
        if (templateData is null)
        {
            throw new ArgumentNullException(nameof(templateData));
        }

        Dictionary<string, string> normalizedTemplateData = [];

        foreach (KeyValuePair<string, string> pair in templateData)
        {
            if (String.IsNullOrWhiteSpace(pair.Key))
            {
                throw new ArgumentException("Template data keys must be non-empty.", nameof(templateData));
            }

            if (pair.Value is null)
            {
                throw new ArgumentException(
                    $"Template data value for key '{pair.Key}' must be non-null.",
                    nameof(templateData));
            }

            normalizedTemplateData[pair.Key.Trim()] = pair.Value;
        }

        return normalizedTemplateData;
    }

    private static string MaskEmail(string email)
    {
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
