namespace SquareBuddy.WebApi.Infrastructure;

using Minio;
using SquareBuddy.Configuration;

public static class MinioBuilderExtensions
{
    public static TBuilder AddMinioServices<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        MinioOptions options = builder.Configuration.TryGetMinioOptions(); 
        builder.Services.AddSingleton(options);

        builder.Services.AddScoped<IMinioClient>(sp =>
        {
            Uri endpoint = options.Endpoint!;
            
            return new MinioClient()
                .WithEndpoint(endpoint.Authority)
                .WithCredentials(options.AccessKey, options.SecretKey)
                .WithSSL(endpoint.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                .Build();
        });

        return builder;
    }
}