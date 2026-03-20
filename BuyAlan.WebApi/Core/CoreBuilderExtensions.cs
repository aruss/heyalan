namespace BuyAlan.WebApi.Core;

using BuyAlan.Agents;
using BuyAlan.Configuration;
using BuyAlan.Email;
using BuyAlan.Messaging;
using BuyAlan.Newsletter;
using BuyAlan.Onboarding;
using BuyAlan.SendGridIntegration;
using BuyAlan.Subscriptions;
using BuyAlan.SquareIntegration;
using BuyAlan.TelegramIntegration;

public static class CoreBuilderExtensions
{
    public static TBuilder AddCoreServices<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        AppOptions options = builder.Configuration.TryGetAppOptions();
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<IFeatureFlagService, FeatureFlagService>();

        builder.Services.AddSingleton(TimeProvider.System);

        // ... add here busines services, repositories, etc.
        builder.AddSendGridServices(); 
        builder.AddEmailServices();
        builder.AddNewsletterServices();
        builder.AddMessagingServices();
        builder.AddSubscriptionInvitationServices();

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
