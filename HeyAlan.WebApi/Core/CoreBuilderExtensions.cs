namespace HeyAlan.WebApi.Core;

using HeyAlan.Configuration;
using HeyAlan.Messaging;
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
        builder.Services.AddScoped<ISubscriptionOnboardingService, SubscriptionOnboardingService>();

        builder.AddTelegramServices();
        builder.AddSquareServices();

        return builder;
    }
}
