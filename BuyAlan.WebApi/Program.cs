using BuyAlan.Identity;
using BuyAlan.TelegramIntegration;
using BuyAlan.WebApi.Agents;
using BuyAlan.WebApi.Core;
using BuyAlan.WebApi.Identity;
using BuyAlan.WebApi.Infrastructure;
using BuyAlan.WebApi.Newsletter;
using BuyAlan.WebApi.Onboarding;
using BuyAlan.WebApi.SquareIntegration;
using BuyAlan.WebApi.Subscriptions;
using BuyAlan.WebApi.TwilioIntegration;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Configuration
    .AddYamlFile("config.yaml", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables();

builder.AddDefaultServices();
builder.AddAiServices();
builder.AddDatabaseServices();
builder.AddIdentityServices();
builder.AddMinioServices();
builder.AddWolverineServices();
builder.AddCoreServices();

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

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;

    if (builder.Environment.IsDevelopment())
    {
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    }
});

builder.Services.AddValidation();

/*builder.Services.Configure<JsonOptions>(options =>
{
    // Enable if AOT compilation required
    // options.SerializerOptions.TypeInfoResolverChain.Clear();
    // options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default);
});*/

builder.Services.AddDistributedMemoryCache();

var app = builder.Build();
CookieSecurePolicy cookieSecurePolicy = app.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;

app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Logger.LogDebug(
        "Web API started. Environment={EnvironmentName} ApplicationName={ApplicationName}",
        app.Environment.EnvironmentName,
        app.Environment.ApplicationName);
});

app.UseForwardedHeaders();

app.Use((context, next) =>
{
    if (context.Request.Headers.TryGetValue("X-Forwarded-Prefix", out var prefix))
    {
        context.Request.PathBase = prefix.ToString();
    }
    return next(context);
});

app.UseStaticFiles();

app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Lax,
    Secure = cookieSecurePolicy,
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
        options.SwaggerEndpoint("../openapi/v1.json", "v1");
    });
}

app.MapInfoEndpoint("BuyAlan"); 
app.MapHealthChecks();
app.MapTwilioWebhookEndpoints();
app.MapTelegramWebhookEndpoints(); 
app.MapSquareCatalogWebhookEndpoints();
app.MapAuthEndpoints();
app.MapAgentEndpoints();
app.MapConversationEndpoints();
app.MapSquareConnectionEndpoints();
app.MapSubscriptionMemberEndpoints();
app.MapOnboardingEndpoints();
app.MapNewsletterEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // app.CheckIfAotSupported();
}

app.Run();
