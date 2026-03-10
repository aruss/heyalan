namespace HeyAlan.SendGridIntegration;

using HeyAlan.Email;
using HeyAlan.Newsletter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class SendGridBuilderExtensions
{
    public const string SendGridClientName = "SendGridClient";

    public static HttpClient GetSendGridClient(this IHttpClientFactory clientFactory)
    {
        return clientFactory.CreateClient(SendGridClientName);
    }

    public static TBuilder AddNewsletterServices<TBuilder>(this TBuilder builder) 
        where TBuilder : IHostApplicationBuilder
    {
        SendGridOptions options = builder.Configuration.TryGetSendGridOptions();

        builder.Services.AddSingleton(options);

        // send grid 
        builder.Services
            .AddHttpClient(SendGridBuilderExtensions.SendGridClientName, client =>
            {
                client.BaseAddress = new Uri("https://api.sendgrid.com");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            });

        builder.Services.AddSingleton<ITransactionalEmailService, SendGridTransactionalEmailService>();
        builder.Services.AddSingleton<INewsletterUpsertService, SendGridNewsletterUpsertService>();

        builder.Services.AddSingleton<
            INewsletterConfirmationTokenService,
            NewsletterConfirmationTokenService>();

        return builder;
    }
}
