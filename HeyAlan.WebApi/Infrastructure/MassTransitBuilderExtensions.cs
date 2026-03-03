namespace HeyAlan.WebApi.Infrastructure;

using MassTransit;
using HeyAlan.Messaging;

public static class MassTransitBuilderExtensions
{
    public static TBuilder AddMassTransitServices<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // TODO: add services here ...
        builder.Services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();
            x.AddConsumer<IncomingMessageConsumer>();
            x.AddConsumer<OutgoingTelegramMessageConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                string? rabbitConnectionString = builder.Configuration.GetConnectionString("rabbitmq");
                cfg.Host(rabbitConnectionString);

                // Disable automatic creation since the Initializer handles it
                cfg.DeployPublishTopology = false;

                cfg.ConfigureEndpoints(context);
            });
        });

        return builder;
    }
}
