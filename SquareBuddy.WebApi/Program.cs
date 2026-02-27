using Microsoft.AspNetCore.CookiePolicy;
using SquareBuddy;
using SquareBuddy.TelegramIntegration;
using SquareBuddy.WebApi.Core;
using SquareBuddy.WebApi.Identity;
using SquareBuddy.WebApi.Infrastructure;
using SquareBuddy.WebApi.TwilioIntegration;

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

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
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

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.MapInfoEndpoint("SquareBuddy"); 
app.MapHealthChecks();
app.MapTwilioWebhookEndpoints();
app.MapTelegramWebhookEndpoints(); 
app.MapAuthEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // app.CheckIfAotSupported();
}

app.Run();
