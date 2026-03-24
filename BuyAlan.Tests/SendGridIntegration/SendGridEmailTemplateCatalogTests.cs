namespace BuyAlan.Tests;

using BuyAlan.Email;
using BuyAlan.SendGridIntegration;

public class SendGridEmailTemplateCatalogTests
{
    [Fact]
    public void ResolveTemplateId_WhenTemplateKeyIsKnown_ReturnsConfiguredTemplateId()
    {
        SendGridOptions options = new()
        {
            ApiKey = "sendgrid-api-key",
            FromEmail = "notifications@buyalan.app",
            NewsletterListId = "newsletter-list-id",
            GenericTemplateId = "d-generic",
            //IdentityConfirmationLinkTemplateId = "d-confirm",
            //IdentityPasswordResetLinkTemplateId = "d-reset-link",
            //IdentityPasswordResetCodeTemplateId = "d-reset-code",
            //NewsletterConfirmationTemplateId = "d-newsletter"
        };

        SendGridTransactionalEmailService service = new(new FakeHttpClientFactory(), options);

        string templateId = service.ResolveTemplateId(EmailTemplateKey.IdentityPasswordResetCode);

        Assert.Equal("d-generic", templateId);
    }

    [Fact]
    public void ResolveTemplateId_WhenTemplateKeyIsUnknown_Throws()
    {
        SendGridOptions options = new()
        {
            ApiKey = "sendgrid-api-key",
            FromEmail = "notifications@buyalan.app",
            NewsletterListId = "newsletter-list-id",
            GenericTemplateId = "d-generic",
            //IdentityConfirmationLinkTemplateId = "d-confirm",
            //IdentityPasswordResetLinkTemplateId = "d-reset-link",
            //IdentityPasswordResetCodeTemplateId = "d-reset-code",
            //NewsletterConfirmationTemplateId = "d-newsletter"
        };

        SendGridTransactionalEmailService service = new(new FakeHttpClientFactory(), options);

        string templateId = service.ResolveTemplateId("missing-template");

        Assert.Equal("d-generic", templateId);
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient
            {
                BaseAddress = new Uri("https://api.sendgrid.com")
            };
        }
    }
}
