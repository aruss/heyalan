namespace ShelfBuddy.SquareIntegration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShelfBuddy.SquareIntegration;

public static class SquareBuilderExtensions
{
    public static TBuilder AddSquareServices<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHttpClient("SquareOAuthClient");
        builder.Services.AddHttpClient("SquareApiClient");
        builder.Services.AddScoped<ISquareTokenService, SquareTokenService>();
        builder.Services.AddScoped<IOAuthStateProtector, OAuthStateProtector>();
        builder.Services.AddScoped<ISquareOAuthClient, SquareOAuthClient>();
        builder.Services.AddScoped<ISquareMerchantClient, SquareMerchantClient>();
        builder.Services.AddScoped<ISubscriptionSquareConnectionService, SubscriptionSquareConnectionService>();

        return builder;
    }
}
