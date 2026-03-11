namespace HeyAlan.WebApi.Core;

using HeyAlan.Agents;
using HeyAlan.Configuration;
using HeyAlan.Email;
using HeyAlan.Messaging;
using HeyAlan.Newsletter;
using HeyAlan.Onboarding;
using HeyAlan.SendGridIntegration;
using HeyAlan.SquareIntegration;
using HeyAlan.TelegramIntegration;

public static class CoreBuilderExtensions
{
    public static TBuilder AddCoreServices<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        AppOptions options = builder.Configuration.TryGetAppOptions();
        builder.Services.AddSingleton(options);

        builder.Services.AddSingleton(TimeProvider.System);

        // ... add here busines services, repositories, etc.
        builder.AddSendGridServices(); 
        builder.AddEmailServices();
        builder.AddNewsletterServices();
        builder.AddMessagingServices();

        // TODO: move to agents module
        builder.Services.AddScoped<ISubscriptionAgentService, SubscriptionAgentService>();
        builder.Services.AddScoped<IAgentCatalogProductAccessService, AgentCatalogProductAccessService>();
        builder.Services.AddScoped<IAgentSalesZipCodeService, AgentSalesZipCodeService>();
        builder.Services.AddScoped<ISubscriptionOnboardingService, SubscriptionOnboardingService>();

        builder.AddTelegramServices();
        builder.AddSquareServices();

        return builder;
    }
}
