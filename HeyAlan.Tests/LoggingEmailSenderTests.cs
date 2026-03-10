namespace HeyAlan.Tests;

using HeyAlan.Data.Entities;
using HeyAlan.Email;
using HeyAlan.Identity;
using Microsoft.Extensions.Logging.Abstractions;

public class LoggingEmailSenderTests
{
    [Fact]
    public async Task SendConfirmationLinkAsync_EnqueuesIdentityConfirmationTemplate()
    {
        RecordingEmailService emailService = new();
        LoggingEmailSender sender = new(emailService, NullLogger<LoggingEmailSender>.Instance);

        await sender.SendConfirmationLinkAsync(new ApplicationUser(), "person@example.com", "https://heyalan.test/confirm");

        Assert.NotNull(emailService.LastMessage);
        Assert.Equal("person@example.com", emailService.LastMessage!.RecipientEmail);
        Assert.Equal(EmailTemplateKey.IdentityConfirmationLink, emailService.LastMessage.TemplateKey);
        Assert.Equal("https://heyalan.test/confirm", emailService.LastMessage.TemplateData["confirmation_url"]);
    }

    [Fact]
    public async Task SendPasswordResetLinkAsync_EnqueuesIdentityResetLinkTemplate()
    {
        RecordingEmailService emailService = new();
        LoggingEmailSender sender = new(emailService, NullLogger<LoggingEmailSender>.Instance);

        await sender.SendPasswordResetLinkAsync(new ApplicationUser(), "person@example.com", "https://heyalan.test/reset");

        Assert.NotNull(emailService.LastMessage);
        Assert.Equal(EmailTemplateKey.IdentityPasswordResetLink, emailService.LastMessage!.TemplateKey);
        Assert.Equal("https://heyalan.test/reset", emailService.LastMessage.TemplateData["reset_url"]);
    }

    [Fact]
    public async Task SendPasswordResetCodeAsync_EnqueuesIdentityResetCodeTemplate()
    {
        RecordingEmailService emailService = new();
        LoggingEmailSender sender = new(emailService, NullLogger<LoggingEmailSender>.Instance);

        await sender.SendPasswordResetCodeAsync(new ApplicationUser(), "person@example.com", "123456");

        Assert.NotNull(emailService.LastMessage);
        Assert.Equal(EmailTemplateKey.IdentityPasswordResetCode, emailService.LastMessage!.TemplateKey);
        Assert.Equal("123456", emailService.LastMessage.TemplateData["reset_code"]);
    }

    private sealed class RecordingEmailService : IEmailService
    {
        public EmailSendRequested? LastMessage { get; private set; }

        public Task EnqueueAsync(EmailSendRequested message, CancellationToken cancellationToken = default)
        {
            this.LastMessage = message;
            return Task.CompletedTask;
        }
    }
}
