using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

#region Configuration 

builder.Configuration
    .AddYamlFile("config.yaml", optional: false, reloadOnChange: false);

builder.Configuration.AddEnvFile("./.env");
builder.Configuration.AddEnvironmentVariables();

string publicBaseUrl = builder.Configuration["PUBLIC_BASE_URL"];
string ngrokDomain = builder.Configuration["NGROK_DOMAIN"];

if (String.IsNullOrEmpty(publicBaseUrl))
{
    publicBaseUrl = ngrokDomain;
}

if (String.IsNullOrEmpty(publicBaseUrl))
{
    publicBaseUrl = "http://localhost:5000";
}

string telegramSecretToken = builder.Configuration["TELEGRAM_SECRET_TOKEN"]
    ?? throw new InvalidOperationException("TELEGRAM_SECRET_TOKEN is missing in host configuration.");

#endregion

#region Infrastructure: Postgres

var postgres = builder.AddPostgres("postgresql")
    .WithContainerName("buyalan-postgres")
    .WithProjectName("buyalan")
    .WithOtlpExporter()
    // .WithDataVolume("buyalan-postgres")
    .WithPgAdmin(admin => admin
        .WithLifetime(ContainerLifetime.Persistent)
        .WithContainerName("buyalan-pgadmin")
        .WithProjectName("buyalan")
    )
    .WithLifetime(ContainerLifetime.Persistent);

var buyalanDb = postgres.AddDatabase("buyalan");

#endregion

#region Infrastructure: RabbitMQ


var rabbitUser = builder.AddParameter("RabbitUser", "admin");
var rabbitPass = builder.AddParameter("RabbitPass", "admin", secret: true);

var rabbitmq = builder.AddRabbitMQ("rabbitmq", userName: rabbitUser, password: rabbitPass)
    .WithContainerName("buyalan-rabbitmq")
    .WithProjectName("buyalan")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

#endregion

#region Application: Intializer

// Run database migrations and minio bucket setup before starting the main app
var initializer = builder.AddProject<Projects.BuyAlan_Initializer>("initializer")
    .WithReference(buyalanDb) // Injects the connection string
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("PUBLIC_BASE_URL", publicBaseUrl)
    .WithEnvironment("TELEGRAM_SECRET_TOKEN", telegramSecretToken)
    .WithEnvironment("RABBITMQ_MANAGEMENT_URL", rabbitmq.GetEndpoint("management"))
    .WithEnvironment("RABBITMQ_USER", rabbitUser)
    .WithEnvironment("RABBITMQ_PASS", rabbitPass)
    .WithEnvironment("RABBITMQ_VHOST", "buyalan") // The vhost name you want to use
    .WithReference(rabbitmq)
    .WithEnvironment("ConnectionStrings__rabbitmq", ReferenceExpression.Create($"{rabbitmq}/buyalan"))
    .WaitFor(postgres)
    .WaitFor(rabbitmq);

#endregion

#region Application: Web API

// Main Web API Project
var webapi = builder.AddProject<Projects.BuyAlan_WebApi>("webapi")
    .WithExternalHttpEndpoints()
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("SWAGGER_ENABLED", "true")
    .WithEnvironment("PUBLIC_BASE_URL", publicBaseUrl)
    .WithEnvironment("TELEGRAM_SECRET_TOKEN", telegramSecretToken)
    .WithEnvironment("AUTH_GOOGLE_CLIENT_ID", builder.Configuration["AUTH_GOOGLE_CLIENT_ID"])
    .WithEnvironment("AUTH_GOOGLE_CLIENT_SECRET", builder.Configuration["AUTH_GOOGLE_CLIENT_SECRET"])
    .WithEnvironment("AUTH_SQUARE_CLIENT_ID", builder.Configuration["AUTH_SQUARE_CLIENT_ID"])
    .WithEnvironment("AUTH_SQUARE_CLIENT_SECRET", builder.Configuration["AUTH_SQUARE_CLIENT_SECRET"])
    .WithEnvironment("SQUARE_CLIENT_ID", builder.Configuration["SQUARE_CLIENT_ID"])
    .WithEnvironment("SQUARE_CLIENT_SECRET", builder.Configuration["SQUARE_CLIENT_SECRET"])
    .WithEnvironment("SQUARE_WEBHOOK_SIGNATURE_KEY", builder.Configuration["SQUARE_WEBHOOK_SIGNATURE_KEY"]) 
    .WithEnvironment("SENDGRID_API_KEY", builder.Configuration["SENDGRID_API_KEY"])
    .WithEnvironment("SENDGRID_EMAIL_FROM", builder.Configuration["SENDGRID_EMAIL_FROM"])
    .WithEnvironment("SENDGRID_TEMPLATE_GENERIC", builder.Configuration["SENDGRID_TEMPLATE_GENERIC"])
    .WithEnvironment("SENDGRID_TEMPLATE_IDENTITY_CONFIRMATION_LINK", builder.Configuration["SENDGRID_TEMPLATE_IDENTITY_CONFIRMATION_LINK"])
    .WithEnvironment("SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_LINK", builder.Configuration["SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_LINK"])
    .WithEnvironment("SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_CODE", builder.Configuration["SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_CODE"])
    .WithEnvironment("SENDGRID_TEMPLATE_NEWSLETTER_CONFIRMATION", builder.Configuration["SENDGRID_TEMPLATE_NEWSLETTER_CONFIRMATION"])
    .WithEnvironment("SENDGRID_NEWSLETTER_LIST_ID", builder.Configuration["SENDGRID_NEWSLETTER_LIST_ID"])
    .WithEnvironment("NEWSLETTER_CONFIRM_TOKEN_TTL_MINUTES", builder.Configuration["NEWSLETTER_CONFIRM_TOKEN_TTL_MINUTES"])
    .WithEnvironment("Logging__LogLevel__Default", builder.Configuration["LOG_LEVEL"])
    .WithReference(rabbitmq)
    .WithEnvironment("ConnectionStrings__rabbitmq", ReferenceExpression.Create($"{rabbitmq}/buyalan"))
    .WithReference(buyalanDb)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WaitForCompletion(initializer);

#endregion

#region Application: Web App 

// if WebApp is part of the solution, use AddProject
/*var webapp = builder.AddJavaScriptApp("webapp", "../BuyAlan.WebApp", "dev")

    .WithYarn()
    .WithHttpEndpoint(env: "PORT", port: 3300)
    .WithExternalHttpEndpoints()
    .WithReference(webapi)
    .WaitFor(webapi)
    .WithOtlpExporter()
    .WithEnvironment("WEBAPI_ENDPOINT", webapi.GetEndpoint("http"))
    .WithEnvironment("NODE_OPTIONS", "--inspect=0.0.0.0:9229");
*/

var runFronend = true;
IResourceBuilder<ExecutableResource> webapp = null!;

if (runFronend) {
    // if WebApp run is via yarn/npm, use AddExecutable
    webapp = builder.AddExecutable("webapp", "yarn.cmd", "../BuyAlan.WebApp", "dev")
        // explicitly allow unsecure transport for local dev if needed, or use https
        .WithHttpEndpoint(env: "PORT", port: 3300, name: "http")
        .WithExternalHttpEndpoints()
        .WithReference(webapi)
        .WaitFor(webapi)
        .WithOtlpExporter()
        .WithEnvironment("APP_VERSION", "1.2.3")
        .WithEnvironment("FEATURE_FLAGS", builder.Configuration["FEATURE_FLAGS"])
        .WithEnvironment("LOG_LEVEL", builder.Configuration["LOG_LEVEL"])
        .WithEnvironment("WEBAPI_ENDPOINT", webapi.GetEndpoint("http"))
        .WithEnvironment("NODE_OPTIONS", "--inspect=0.0.0.0:9229");
}

#endregion

#region Infrastructure: ngrok

if (!String.IsNullOrEmpty(ngrokDomain))
{
    string ngrokAuthToken = builder.Configuration["NGROK_AUTHTOKEN"]
        ?? throw new InvalidOperationException("NGROK_AUTHTOKEN is missing in host configuration.");

    var ngrok = builder.AddContainer("ngrok", "ngrok/ngrok")
        .WithContainerName("buyalan-ngrok")
        .WithProjectName("buyalan")
        .WithEnvironment("NGROK_AUTHTOKEN", ngrokAuthToken)
        .WithHttpEndpoint(targetPort: 4040, port: 4040, name: "inspect")
        .WithArgs("http", $"--url={ngrokDomain}", "--log=stdout", "http://host.docker.internal:3300")
        .WithLifetime(ContainerLifetime.Persistent);

    if (webapp != null)
    {
        ngrok.WaitFor(webapp); 
    }
}

#endregion


builder.Build().Run();
