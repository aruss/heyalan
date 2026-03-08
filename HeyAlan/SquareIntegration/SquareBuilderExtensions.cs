namespace HeyAlan.SquareIntegration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HeyAlan.SquareIntegration;

public static class SquareBuilderExtensions
{
    public static TBuilder AddSquareServices<TBuilder>(this TBuilder builder) 
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHttpClient("SquareOAuthClient");
        builder.Services.AddScoped<IOAuthStateProtector, OAuthStateProtector>();
        builder.Services.AddScoped<ISquareService, SquareService>();

        return builder;
    }
}
