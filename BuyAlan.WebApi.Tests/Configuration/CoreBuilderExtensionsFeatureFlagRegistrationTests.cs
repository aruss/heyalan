namespace BuyAlan.WebApi.Tests;

using BuyAlan.Configuration;
using BuyAlan.WebApi.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class CoreBuilderExtensionsFeatureFlagRegistrationTests
{
    [Fact]
    public void AddCoreServices_RegistersFeatureFlagServiceAsSingleton()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PUBLIC_BASE_URL"] = "https://buyalan.test",
            ["SENDGRID_API_KEY"] = "test-api-key",
            ["SENDGRID_EMAIL_FROM"] = "sender@buyalan.test",
            ["SENDGRID_NEWSLETTER_LIST_ID"] = "list-1",
            ["SENDGRID_TEMPLATE_GENERIC"] = "template-generic",
            ["SENDGRID_TEMPLATE_IDENTITY_CONFIRMATION_LINK"] = "template-confirm-link",
            ["SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_LINK"] = "template-reset-link",
            ["SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_CODE"] = "template-reset-code",
            ["SENDGRID_TEMPLATE_NEWSLETTER_CONFIRMATION"] = "template-newsletter",
            ["TELEGRAM_SECRET_TOKEN"] = "telegram-secret",
        });

        builder.AddCoreServices();

        using ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();

        IFeatureFlagService firstService = serviceProvider.GetRequiredService<IFeatureFlagService>();
        IFeatureFlagService secondService = serviceProvider.GetRequiredService<IFeatureFlagService>();

        Assert.Same(firstService, secondService);
        Assert.IsType<FeatureFlagService>(firstService);
    }
}
