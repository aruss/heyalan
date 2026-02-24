namespace SquareBuddy.WebApi.Infrastructure;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using System.Text;

public static class SwaggerBuilderExtensions
{
    public static TBuilder AddSwaggerServices<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SquareBuddy API",
                Version = "v1",
            });
        });

        /*builder.Services.Configure<RouteOptions>(options =>
        {
            options.SetParameterPolicy<RegexInlineRouteConstraint>("regex");
        });*/

        return builder;
    }
}

public static class SwaggerFileWriter
{
    public static async Task WriteSwaggerJson(WebApplication app)
    {

        using var scope = app.Services.CreateScope();

        var provider = scope.ServiceProvider.GetRequiredService<IOpenApiDocumentProvider>();
        var document = await provider.GetOpenApiDocumentAsync();

        var path = Path.Combine(app.Environment.ContentRootPath, "swagger.json");

        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        await using var writer = new StreamWriter(stream, Encoding.UTF8);

        document.SerializeAsV3(new OpenApiJsonWriter(writer));
    }
}
