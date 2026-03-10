namespace HeyAlan.WebApi.Infrastructure;

using HeyAlan.Email;
using HeyAlan.Messaging;
using HeyAlan.Newsletter;
using HeyAlan.SquareIntegration;
using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.Postgresql;

public static class WolverineBuilderExtensions
{
    private const string WolverineSchema = "wolverine";

    public static WebApplicationBuilder AddWolverineServices(this WebApplicationBuilder builder)
    {
        builder.Host.UseWolverine(options =>
        {
            options.Discovery.IncludeType<TransactionalEmailConsumer>();
            options.Discovery.IncludeType<IncomingMessageConsumer>();
            options.Discovery.IncludeType<OutgoingTelegramMessageConsumer>();
            options.Discovery.IncludeType<SubscriptionCatalogSyncConsumer>();

            string? rabbitConnectionString = builder.Configuration.GetConnectionString("rabbitmq");
            if (String.IsNullOrWhiteSpace(rabbitConnectionString))
            {
                throw new InvalidOperationException("RabbitMQ connection string 'rabbitmq' is missing.");
            }

            RabbitMqStartupGuard.EnsureReachable(rabbitConnectionString, TimeSpan.FromSeconds(3));
            options.UseRabbitMq(rabbitConnectionString);

            options.ListenToRabbitQueue("email-send-requested");
            options.ListenToRabbitQueue("incoming-message");
            options.ListenToRabbitQueue("telegram-outgoing-messages");
            options.ListenToRabbitQueue("newsletter-subscription");

            options.PublishMessage<EmailSendRequested>().ToRabbitQueue("email-send-requested");
            options.PublishMessage<IncomingMessage>().ToRabbitQueue("incoming-message");
            options.PublishMessage<OutgoingTelegramMessage>().ToRabbitQueue("telegram-outgoing-messages");

            options.Policies.UseDurableInboxOnAllListeners();
            options.Policies.UseDurableOutboxOnAllSendingEndpoints();


            string? databaseConnectionString = builder.Configuration.GetConnectionString("heyalan");
            if (String.IsNullOrWhiteSpace(databaseConnectionString))
            {
                throw new InvalidOperationException("Database connection string 'heyalan' is missing.");
            }

            options.PersistMessagesWithPostgresql(databaseConnectionString, WolverineSchema);
        });

        return builder;
    }
}
