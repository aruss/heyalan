namespace SquareBuddy.WebApi.Infrastructure;

using MassTransit;
using SquareBuddy.Consumers;

public static class MassTransitBuilderExtensions
{
    public static TBuilder AddMassTransitServices<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // TODO: add services here ...
        builder.Services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();
            x.AddConsumer<IncomingMessageConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitConnectionString = builder.Configuration.GetConnectionString("rabbitmq");
                cfg.Host(rabbitConnectionString);

                // Disable automatic creation since the Initializer handles it
                cfg.DeployPublishTopology = false;

                cfg.ConfigureEndpoints(context);
            });
        });

        return builder;
    }
}