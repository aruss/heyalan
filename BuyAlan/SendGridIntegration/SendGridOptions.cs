namespace BuyAlan.SendGridIntegration;

using Microsoft.Extensions.Configuration;

public record SendGridOptions
{
    public string ApiKey { get; init; } = String.Empty;

    public string FromEmail { get; init; } = String.Empty;

    public string NewsletterListId { get; init; } = String.Empty;

    public string GenericTemplateId { get; init; } = String.Empty;

    // public string IdentityConfirmationLinkTemplateId { get; init; } = String.Empty;

    // public string IdentityPasswordResetLinkTemplateId { get; init; } = String.Empty;

    // public string IdentityPasswordResetCodeTemplateId { get; init; } = String.Empty;

    // public string NewsletterConfirmationTemplateId { get; init; } = String.Empty;
}

public static class SendGridOptionsConfigurationExtensions
{
    public static SendGridOptions TryGetSendGridOptions(this IConfiguration configuration)
    {
        string apiKey = configuration.GetTrimmedValue("SENDGRID_API_KEY");
        string fromEmail = configuration.GetTrimmedValue("SENDGRID_EMAIL_FROM");
        string newsletterListId = configuration.GetTrimmedValue("SENDGRID_NEWSLETTER_LIST_ID");
        string genericTemplateId = configuration.GetTrimmedValue("SENDGRID_TEMPLATE_GENERIC");
        //string identityConfirmationLinkTemplateId = configuration.GetTrimmedValue("SENDGRID_TEMPLATE_IDENTITY_CONFIRMATION_LINK");
        //string identityPasswordResetLinkTemplateId = configuration.GetTrimmedValue("SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_LINK");
        //string identityPasswordResetCodeTemplateId = configuration.GetTrimmedValue("SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_CODE");
        //string newsletterConfirmationTemplateId = configuration.GetTrimmedValue("SENDGRID_TEMPLATE_NEWSLETTER_CONFIRMATION");

        return new SendGridOptions
        {
            ApiKey = apiKey,
            FromEmail = fromEmail,
            NewsletterListId = newsletterListId,
            GenericTemplateId = genericTemplateId,
            //IdentityConfirmationLinkTemplateId = identityConfirmationLinkTemplateId,
            //IdentityPasswordResetLinkTemplateId = identityPasswordResetLinkTemplateId,
            //IdentityPasswordResetCodeTemplateId = identityPasswordResetCodeTemplateId,
            //NewsletterConfirmationTemplateId = newsletterConfirmationTemplateId,
        };
    }
}
