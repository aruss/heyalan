namespace HeyAlan.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using HeyAlan.Data.Entities;

public sealed class LoggingEmailSender : IEmailSender<ApplicationUser>
{
    private readonly ILogger<LoggingEmailSender> logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        this.LogEmail("ConfirmationLink", email, confirmationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        this.LogEmail("PasswordResetLink", email, resetLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        this.LogEmail("PasswordResetCode", email, resetCode);
        return Task.CompletedTask;
    }

    private void LogEmail(string emailType, string email, string payload)
    {
        string maskedEmail = MaskEmail(email);
        int payloadLength = payload?.Length ?? 0;

        // Avoid logging PII; only log non-sensitive metadata for dev visibility.
        this.logger.LogInformation(
            "Dev email sender: Type={EmailType} To={MaskedEmail} PayloadLength={PayloadLength}",
            emailType,
            maskedEmail,
            payloadLength);
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
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
