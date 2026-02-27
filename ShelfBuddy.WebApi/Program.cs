using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Mvc;
using ShelfBuddy;
using ShelfBuddy.TelegramIntegration;
using ShelfBuddy.WebApi.Core;
using ShelfBuddy.WebApi.Identity;
using ShelfBuddy.WebApi.Infrastructure;
using ShelfBuddy.WebApi.TwilioIntegration;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Configuration
    .AddYamlFile("config.yaml", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables();

builder.AddDefaultServices();
builder.AddAiServices();
builder.AddDatabaseServices();
builder.AddIdentityServices();
builder.AddMinioServices();
builder.AddMassTransitServices(); 
builder.AddCoreServices();
builder.AddTelegram();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalProblemExceptionHandler>();
builder.Services.Configure<RouteHandlerOptions>(options =>
{
    options.ThrowOnBadRequest = true;
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi(options =>
    {
        options.AddOperationTransformer<CamelCaseQueryParameterOperationTransformer>();
        options.AddSchemaTransformer<PaginationIntegerSchemaTransformer>();
    });
}

builder.Services.AddValidation();

/*builder.Services.Configure<JsonOptions>(options =>
{
    // Enable if AOT compilation required
    // options.SerializerOptions.TypeInfoResolverChain.Clear();
    // options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default);
});*/

builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

app.UseStaticFiles();

app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Lax,
    Secure = CookieSecurePolicy.Always,
    HttpOnly = HttpOnlyPolicy.Always
});

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception exception)
    {
        if (context.Response.HasStarted)
        {
            throw;
        }

        IProblemDetailsService problemDetailsService = context.RequestServices.GetRequiredService<IProblemDetailsService>();
        int statusCode = exception is BadHttpRequestException badHttpRequestException
            ? badHttpRequestException.StatusCode
            : StatusCodes.Status500InternalServerError;

        ProblemDetails problemDetails = new()
        {
            Status = statusCode,
            Title = statusCode == StatusCodes.Status500InternalServerError ? "Internal Server Error" : "Bad Request",
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : exception.Message,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        context.Response.Clear();
        context.Response.StatusCode = statusCode;

        bool written = await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = problemDetails
        });

        if (!written)
        {
            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
});

app.UseExceptionHandler();
app.UseStatusCodePages(async statusCodeContext =>
{
    await TypedResults
        .Problem(statusCode: statusCodeContext.HttpContext.Response.StatusCode)
        .ExecuteAsync(statusCodeContext.HttpContext);
});

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.MapInfoEndpoint("ShelfBuddy"); 
app.MapHealthChecks();
app.MapTwilioWebhookEndpoints();
app.MapTelegramWebhookEndpoints(); 
app.MapAuthEndpoints();
app.MapConversationEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // app.CheckIfAotSupported();
}

app.Run();
