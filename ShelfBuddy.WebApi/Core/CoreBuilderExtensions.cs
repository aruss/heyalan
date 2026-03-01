namespace ShelfBuddy.WebApi.Core;

using Minio;
using ShelfBuddy.Configuration;
using ShelfBuddy.Core.Conversations;
using ShelfBuddy.Onboarding;
using ShelfBuddy.SquareIntegration;
using ShelfBuddy.TelegramIntegration;

public static class CoreBuilderExtensions
{
    public static TBuilder AddCoreServices<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        AppOptions options = builder.Configuration.TryGetAppOptions(); 
        builder.Services.AddSingleton(options);

        // ... add here busines services, repositories, etc.
        builder.Services.AddScoped<IConversationStore, ConversationStore>();
        builder.Services.AddScoped<ISubscriptionOnboardingService, SubscriptionOnboardingService>();

        builder.AddTelegramServices();
        builder.AddSquareServices();

        return builder;
    }
}
