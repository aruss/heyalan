namespace HeyAlan.Email;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class EmailBuilderExtensions
{
    public static TBuilder AddEmailServices<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddScoped<IEmailQueuingService, EmailQueuingService>();
        
        return builder;
    }
}
