namespace HeyAlan.Messaging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class MessagingBuilderExtensions
{
    public static TBuilder AddMessagingServices<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddScoped<IConversationStore, ConversationStore>();
        return builder;
    }
}
