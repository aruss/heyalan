namespace HeyAlan.Identity;

using HeyAlan.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using HeyAlan.Data.Entities;

// TODO: rename to SendGridIdentityEmailSender and move to SendGridIntegration namespace 
public sealed class LoggingEmailSender : IEmailSender<ApplicationUser>
{
    private readonly IEmailQueuingService emailService;
    private readonly ILogger<LoggingEmailSender> logger;

    public LoggingEmailSender(
        IEmailQueuingService emailService,
        ILogger<LoggingEmailSender> logger)
    {
        this.emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        this.LogEmail(EmailTemplateKey.IdentityConfirmationLink, email);

        EmailSendRequested message = new(
            email,
            EmailTemplateKey.IdentityConfirmationLink,
            new Dictionary<string, string>
            {
                ["confirmation_url"] = confirmationLink
            });

        await this.emailService.EnqueueAsync(message);
    }

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        this.LogEmail(EmailTemplateKey.IdentityPasswordResetLink, email);

        EmailSendRequested message = new(
            email,
            EmailTemplateKey.IdentityPasswordResetLink,
            new Dictionary<string, string>
            {
                ["reset_url"] = resetLink
            });

        await this.emailService.EnqueueAsync(message);
    }

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        this.LogEmail(EmailTemplateKey.IdentityPasswordResetCode, email);

        EmailSendRequested message = new(
            email,
            EmailTemplateKey.IdentityPasswordResetCode,
            new Dictionary<string, string>
            {
                ["reset_code"] = resetCode
            });

        await this.emailService.EnqueueAsync(message);
    }

    private void LogEmail(string templateKey, string email)
    {
        string maskedEmail = MaskEmail(email);

        this.logger.LogInformation(
            "Queued identity email. TemplateKey={TemplateKey} To={MaskedEmail}",
            templateKey,
            maskedEmail);
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
