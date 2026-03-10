namespace HeyAlan.Tests;

using HeyAlan.SendGridIntegration;
using Microsoft.Extensions.Configuration;

public class SendGridEmailOptionsTests
{
    [Fact]
    public void TryGetSendGridEmailOptions_WhenAllValuesExist_ReturnsTrimmedOptions()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["SENDGRID_API_KEY"] = "  api-key-value  ",
            ["SENDGRID_EMAIL_FROM"] = "  notifications@example.com  ",
            ["SENDGRID_TEMPLATE_IDENTITY_CONFIRMATION_LINK"] = "  d-confirm  ",
            ["SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_LINK"] = "  d-reset-link  ",
            ["SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_CODE"] = "  d-reset-code  ",
            ["SENDGRID_TEMPLATE_NEWSLETTER_CONFIRMATION"] = "  d-newsletter  "
        });

        SendGridEmailOptions options = configuration.TryGetSendGridEmailOptions();

        Assert.Equal("api-key-value", options.ApiKey);
        Assert.Equal("notifications@example.com", options.FromEmail);
        Assert.Equal("d-confirm", options.IdentityConfirmationLinkTemplateId);
        Assert.Equal("d-reset-link", options.IdentityPasswordResetLinkTemplateId);
        Assert.Equal("d-reset-code", options.IdentityPasswordResetCodeTemplateId);
        Assert.Equal("d-newsletter", options.NewsletterConfirmationTemplateId);
    }

    [Fact]
    public void TryGetSendGridEmailOptions_WhenFromEmailMissing_Throws()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["SENDGRID_API_KEY"] = "api-key-value",
            ["SENDGRID_TEMPLATE_IDENTITY_CONFIRMATION_LINK"] = "d-confirm",
            ["SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_LINK"] = "d-reset-link",
            ["SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_CODE"] = "d-reset-code",
            ["SENDGRID_TEMPLATE_NEWSLETTER_CONFIRMATION"] = "d-newsletter"
        });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            configuration.TryGetSendGridEmailOptions());

        Assert.Contains("SENDGRID_EMAIL_FROM", exception.Message);
    }

    [Fact]
    public void TryGetSendGridEmailOptions_WhenTemplateIdBlank_Throws()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["SENDGRID_API_KEY"] = "api-key-value",
            ["SENDGRID_EMAIL_FROM"] = "notifications@example.com",
            ["SENDGRID_TEMPLATE_IDENTITY_CONFIRMATION_LINK"] = "d-confirm",
            ["SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_LINK"] = "  ",
            ["SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_CODE"] = "d-reset-code",
            ["SENDGRID_TEMPLATE_NEWSLETTER_CONFIRMATION"] = "d-newsletter"
        });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            configuration.TryGetSendGridEmailOptions());

        Assert.Contains("SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_LINK", exception.Message);
    }

    private static IConfiguration CreateConfiguration(IDictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
