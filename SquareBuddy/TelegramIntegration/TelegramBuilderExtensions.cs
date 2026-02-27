namespace SquareBuddy.TelegramIntegration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class TelegramBuilderExtensions
{
    public static TBuilder AddTelegram<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        TelegramOptions options = builder.Configuration.TryGetTelegramOptions();
        builder.Services.AddSingleton(options);

        builder.Services
            .AddHttpClient("TelegramBotClient")
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            });

        builder.Services.AddSingleton<TelegramClientFactory>();
        builder.Services.AddSingleton<ITelegramService, TelegramService>();

        return builder;
    }
}

