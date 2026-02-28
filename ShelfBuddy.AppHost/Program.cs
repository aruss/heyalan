using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

#region Configuration 

builder.Configuration
    .AddYamlFile("config.yaml", optional: false, reloadOnChange: false);

builder.Configuration.AddEnvFile("./.env");
builder.Configuration.AddEnvironmentVariables();

string? ngrokDomain = builder.Configuration["NGROK_DOMAIN"];
string publicBaseUrl = ngrokDomain ?? "http://localhost:5000";

string telegramSecretToken = builder.Configuration["TELEGRAM_SECRET_TOKEN"] 
    ?? throw new InvalidOperationException("TELEGRAM_SECRET_TOKEN is missing in host configuration.");


#endregion

#region Infrastructure: Postgres

var postgres = builder.AddPostgres("postgresql")
    .WithContainerName("shelfbuddy-postgres")
    .WithProjectName("shelfbuddy")
    .WithOtlpExporter()
    // .WithDataVolume("shelfbuddy-postgres")
    .WithPgAdmin(admin => admin
        .WithLifetime(ContainerLifetime.Persistent)
        .WithContainerName("shelfbuddy-pgadmin")
        .WithProjectName("shelfbuddy")
    )
    .WithLifetime(ContainerLifetime.Persistent);

var shelfbuddyDb = postgres.AddDatabase("shelfbuddy");

#endregion

#region Infrastructure: RabbitMQ


var rabbitUser = builder.AddParameter("RabbitUser", "admin");
var rabbitPass = builder.AddParameter("RabbitPass", "admin", secret: true);

var rabbitmq = builder.AddRabbitMQ("rabbitmq", userName: rabbitUser, password: rabbitPass)
    .WithContainerName("shelfbuddy-rabbitmq")
    .WithProjectName("shelfbuddy")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

#endregion

#region Application: Intializer

var adminEmail = builder.Configuration["ADMIN_EMAIL"]
        ?? throw new InvalidOperationException("ADMIN_EMAIL is missing in host configuration.");

var adminPassword = builder.Configuration["ADMIN_PASSWORD"]
        ?? throw new InvalidOperationException("ADMIN_PASSWORD is missing in host configuration.");

// Run database migrations and minio bucket setup before starting the main app
var initializer = builder.AddProject<Projects.ShelfBuddy_Initializer>("initializer")
    .WithReference(shelfbuddyDb) // Injects the connection string
    .WithEnvironment("ADMIN_EMAIL", adminEmail)
    .WithEnvironment("ADMIN_PASSWORD", adminPassword)
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("PUBLIC_BASE_URL", publicBaseUrl)
    .WithEnvironment("TELEGRAM_SECRET_TOKEN", telegramSecretToken)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq); 

#endregion 

#region Application: Web API

// Main Web API Project
var webapi = builder.AddProject<Projects.ShelfBuddy_WebApi>("webapi")
    .WithExternalHttpEndpoints()
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("SWAGGER_ENABLED", "true")
    .WithEnvironment("PUBLIC_BASE_URL", publicBaseUrl)
    .WithEnvironment("TELEGRAM_SECRET_TOKEN", telegramSecretToken)
    .WithEnvironment("GOOGLE_CLIENT_ID", builder.Configuration["GOOGLE_CLIENT_ID"])
    .WithEnvironment("GOOGLE_CLIENT_SECRET", builder.Configuration["GOOGLE_CLIENT_SECRET"])
    .WithEnvironment("SQUARE_CLIENT_ID", builder.Configuration["SQUARE_CLIENT_ID"])
    .WithEnvironment("SQUARE_CLIENT_SECRET", builder.Configuration["SQUARE_CLIENT_SECRET"])
    .WithReference(rabbitmq)
    .WithReference(shelfbuddyDb)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WaitForCompletion(initializer);

#endregion

#region Infrastructure: ngrok

if (!String.IsNullOrEmpty(ngrokDomain))
{
    string ngrokAuthToken = builder.Configuration["NGROK_AUTHTOKEN"]
        ?? throw new InvalidOperationException("NGROK_AUTHTOKEN is missing in host configuration.");

    builder.AddContainer("ngrok", "ngrok/ngrok")
        .WithContainerName("shelfbuddy-ngrok")
        .WithProjectName("shelfbuddy")
        .WithEnvironment("NGROK_AUTHTOKEN", ngrokAuthToken)
        .WithHttpEndpoint(targetPort: 4040, port: 4040, name: "inspect")
        .WithArgs("http", $"--url={ngrokDomain}", "--log=stdout", "http://host.docker.internal:5000")
        .WaitFor(webapi);
}

#endregion

#region Application: Web App 

// if WebApp is part of the solution, use AddProject
/*var webapp = builder.AddJavaScriptApp("webapp", "../ShelfBuddy.WebApp", "dev")

    .WithYarn()
    .WithHttpEndpoint(env: "PORT", port: 5010)
    .WithExternalHttpEndpoints()
    .WithReference(webapi)
    .WaitFor(webapi)
    .WithOtlpExporter()
    .WithEnvironment("WEBAPI_ENDPOINT", webapi.GetEndpoint("http"))
    .WithEnvironment("NODE_OPTIONS", "--inspect=0.0.0.0:9229");
*/

// if WebApp run is via yarn/npm, use AddExecutable
/*var webapp = builder.AddExecutable("webapp", "yarn.cmd",  "../ShelfBuddy.WebApp", "dev")
    // explicitly allow unsecure transport for local dev if needed, or use https
    .WithHttpEndpoint(env: "PORT", port: 5010, name: "http")
    .WithExternalHttpEndpoints()
    .WithReference(webapi)
    .WaitFor(webapi)
    .WithOtlpExporter()
    .WithEnvironment("APP_VERSION", "1.2.3")
    .WithEnvironment("WEBAPI_ENDPOINT", webapi.GetEndpoint("http"))
    .WithEnvironment("NODE_OPTIONS", "--inspect=0.0.0.0:9229");*/

#endregion

builder.Build().Run();
