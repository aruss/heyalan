namespace BuyAlan.Tests;

using BuyAlan.SendGridIntegration;
using Microsoft.Extensions.Configuration;

public class SendGridEmailOptionsTests
{
    [Fact]
    public void TryGetSendGridOptions_WhenAllValuesExist_ReturnsTrimmedOptions()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["SENDGRID_API_KEY"] = "  api-key-value  ",
            ["SENDGRID_EMAIL_FROM"] = "  notifications@example.com  ",
            ["SENDGRID_NEWSLETTER_LIST_ID"] = "  list-id-value  ",
            ["SENDGRID_TEMPLATE_GENERIC"] = "  d-generic  ",
            ["SENDGRID_TEMPLATE_IDENTITY_CONFIRMATION_LINK"] = "  d-confirm  ",
            ["SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_LINK"] = "  d-reset-link  ",
            ["SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_CODE"] = "  d-reset-code  ",
            ["SENDGRID_TEMPLATE_NEWSLETTER_CONFIRMATION"] = "  d-newsletter  "
        });

        SendGridOptions options = configuration.TryGetSendGridOptions();

        Assert.Equal("api-key-value", options.ApiKey);
        Assert.Equal("notifications@example.com", options.FromEmail);
        Assert.Equal("list-id-value", options.NewsletterListId);
        Assert.Equal("d-generic", options.GenericTemplateId);
        //Assert.Equal("d-confirm", options.IdentityConfirmationLinkTemplateId);
        //Assert.Equal("d-reset-link", options.IdentityPasswordResetLinkTemplateId);
        //Assert.Equal("d-reset-code", options.IdentityPasswordResetCodeTemplateId);
        //Assert.Equal("d-newsletter", options.NewsletterConfirmationTemplateId);
    }

    [Fact]
    public void TryGetSendGridOptions_WhenFromEmailMissing_Throws()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["SENDGRID_API_KEY"] = "api-key-value",
            ["SENDGRID_EMAIL_FROM"] = "",
            ["SENDGRID_NEWSLETTER_LIST_ID"] = "list-id-value",
            ["SENDGRID_TEMPLATE_GENERIC"] = "d-generic",
            ["SENDGRID_TEMPLATE_IDENTITY_CONFIRMATION_LINK"] = "d-confirm",
            ["SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_LINK"] = "d-reset-link",
            ["SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_CODE"] = "d-reset-code",
            ["SENDGRID_TEMPLATE_NEWSLETTER_CONFIRMATION"] = "d-newsletter"
        });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            configuration.TryGetSendGridOptions());

        Assert.Contains("SENDGRID_EMAIL_FROM", exception.Message);
    }

    [Fact]
    public void TryGetSendGridOptions_WhenTemplateIdBlank_Throws()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["SENDGRID_API_KEY"] = "api-key-value",
            ["SENDGRID_EMAIL_FROM"] = "notifications@example.com",
            ["SENDGRID_NEWSLETTER_LIST_ID"] = "list-id-value",
            ["SENDGRID_TEMPLATE_GENERIC"] = "d-generic",
            ["SENDGRID_TEMPLATE_IDENTITY_CONFIRMATION_LINK"] = "d-confirm",
            ["SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_LINK"] = "  ",
            ["SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_CODE"] = "d-reset-code",
            ["SENDGRID_TEMPLATE_NEWSLETTER_CONFIRMATION"] = "d-newsletter"
        });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            configuration.TryGetSendGridOptions());

        Assert.Contains("SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_LINK", exception.Message);
    }

    private static IConfiguration CreateConfiguration(IDictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
