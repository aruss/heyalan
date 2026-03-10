namespace HeyAlan.Tests;

using HeyAlan.Email;
using HeyAlan.SendGridIntegration;

public class SendGridEmailTemplateCatalogTests
{
    [Fact]
    public void ResolveTemplateId_WhenTemplateKeyIsKnown_ReturnsConfiguredTemplateId()
    {
        SendGridEmailOptions options = new()
        {
            ApiKey = "sendgrid-api-key",
            FromEmail = "notifications@heyalan.app",
            IdentityConfirmationLinkTemplateId = "d-confirm",
            IdentityPasswordResetLinkTemplateId = "d-reset-link",
            IdentityPasswordResetCodeTemplateId = "d-reset-code",
            NewsletterConfirmationTemplateId = "d-newsletter"
        };

        SendGridEmailTemplateCatalog catalog = new(options);

        string templateId = catalog.ResolveTemplateId(EmailTemplateKey.IdentityPasswordResetCode);

        Assert.Equal("d-reset-code", templateId);
    }

    [Fact]
    public void ResolveTemplateId_WhenTemplateKeyIsUnknown_Throws()
    {
        SendGridEmailOptions options = new()
        {
            ApiKey = "sendgrid-api-key",
            FromEmail = "notifications@heyalan.app",
            IdentityConfirmationLinkTemplateId = "d-confirm",
            IdentityPasswordResetLinkTemplateId = "d-reset-link",
            IdentityPasswordResetCodeTemplateId = "d-reset-code",
            NewsletterConfirmationTemplateId = "d-newsletter"
        };

        SendGridEmailTemplateCatalog catalog = new(options);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            catalog.ResolveTemplateId("missing-template"));

        Assert.Contains("missing-template", exception.Message);
    }
}
