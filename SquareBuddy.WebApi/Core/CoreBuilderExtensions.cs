namespace SquareBuddy.WebApi.Core;

using Minio;
using SquareBuddy.Configuration;
using SquareBuddy.Core.Conversations;

public static class CoreBuilderExtensions
{
    public static TBuilder AddCoreServices<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        AppOptions options = builder.Configuration.TryGetAppOptions(); 
        builder.Services.AddSingleton(options);

        // ... add here busines services, repositories, etc.
        builder.Services.AddScoped<IConversationStore, ConversationStore>();

        return builder;
    }
}
