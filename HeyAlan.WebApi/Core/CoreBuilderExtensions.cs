namespace HeyAlan.WebApi.Core;

using HeyAlan.Agents;
using HeyAlan.Configuration;
using HeyAlan.Messaging;
using HeyAlan.Newsletter;
using HeyAlan.Onboarding;
using HeyAlan.SquareIntegration;
using HeyAlan.TelegramIntegration;

public static class CoreBuilderExtensions
{
    public static TBuilder AddCoreServices<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        AppOptions options = builder.Configuration.TryGetAppOptions();
        builder.Services.AddSingleton(options);

        // ... add here busines services, repositories, etc.
        builder.AddMessagingServices();
        builder.AddNewsletterServices();
        builder.Services.AddScoped<ISubscriptionAgentService, SubscriptionAgentService>();
        builder.Services.AddScoped<IAgentCatalogProductAccessService, AgentCatalogProductAccessService>();
        builder.Services.AddScoped<IAgentSalesZipCodeService, AgentSalesZipCodeService>();
        builder.Services.AddScoped<ISubscriptionOnboardingService, SubscriptionOnboardingService>();

        builder.AddTelegramServices();
        builder.AddSquareServices();

        return builder;
    }
}
