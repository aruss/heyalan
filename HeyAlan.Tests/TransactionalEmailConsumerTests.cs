namespace HeyAlan.Tests;

using HeyAlan.Email;
using Microsoft.Extensions.Logging.Abstractions;

public class TransactionalEmailConsumerTests
{
    [Fact]
    public async Task Consume_WhenMessageValid_ResolvesTemplateAndSends()
    {
        RecordingEmailTemplateCatalog templateCatalog = new();
        RecordingTransactionalEmailClient emailClient = new();
        TransactionalEmailConsumer consumer = new(
            templateCatalog,
            emailClient,
            NullLogger<TransactionalEmailConsumer>.Instance);

        EmailSendRequested message = new(
            "person@example.com",
            EmailTemplateKey.NewsletterConfirmation,
            new Dictionary<string, string>
            {
                ["confirmation_url"] = "https://heyalan.test/newsletter/confirm?token=abc"
            });

        await consumer.Consume(message, CancellationToken.None);

        Assert.Equal(EmailTemplateKey.NewsletterConfirmation, templateCatalog.LastTemplateKey);
        Assert.Equal("person@example.com", emailClient.LastRecipientEmail);
        Assert.Equal("d-newsletter", emailClient.LastTemplateId);
        Assert.Equal("https://heyalan.test/newsletter/confirm?token=abc", emailClient.LastTemplateData!["confirmation_url"]);
    }

    [Fact]
    public async Task Consume_WhenTransportFails_Throws()
    {
        ThrowingTransactionalEmailClient emailClient = new();
        TransactionalEmailConsumer consumer = new(
            new RecordingEmailTemplateCatalog(),
            emailClient,
            NullLogger<TransactionalEmailConsumer>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.Consume(
            new EmailSendRequested(
                "person@example.com",
                EmailTemplateKey.NewsletterConfirmation,
                new Dictionary<string, string>
                {
                    ["confirmation_url"] = "https://heyalan.test/newsletter/confirm?token=abc"
                }),
            CancellationToken.None));
    }

    private sealed class RecordingEmailTemplateCatalog : IEmailTemplateCatalog
    {
        public string? LastTemplateKey { get; private set; }

        public string ResolveTemplateId(string templateKey)
        {
            this.LastTemplateKey = templateKey;
            return "d-newsletter";
        }
    }

    private sealed class RecordingTransactionalEmailClient : ITransactionalEmailClient
    {
        public string? LastRecipientEmail { get; private set; }

        public string? LastTemplateId { get; private set; }

        public IReadOnlyDictionary<string, string>? LastTemplateData { get; private set; }

        public Task SendTemplateAsync(
            string recipientEmail,
            string templateId,
            IReadOnlyDictionary<string, string> templateData,
            CancellationToken cancellationToken = default)
        {
            this.LastRecipientEmail = recipientEmail;
            this.LastTemplateId = templateId;
            this.LastTemplateData = templateData;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingTransactionalEmailClient : ITransactionalEmailClient
    {
        public Task SendTemplateAsync(
            string recipientEmail,
            string templateId,
            IReadOnlyDictionary<string, string> templateData,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("send failed");
        }
    }
}
