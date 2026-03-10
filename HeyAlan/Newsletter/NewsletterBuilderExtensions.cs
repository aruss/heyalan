namespace HeyAlan.Newsletter;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class SendGridBuilderExtensions
{
    public static TBuilder AddNewsletterServices<TBuilder>(this TBuilder builder) 
        where TBuilder : IHostApplicationBuilder
    {
        NewsletterOptions options = builder.Configuration.TryGetNewsletterOptions();

        builder.Services.AddSingleton(options);

        builder.Services.AddSingleton<
            INewsletterConfirmationTokenService,
            NewsletterConfirmationTokenService>();

        return builder;
    }
}
