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
    .WithContainerName("heyalan-postgres")
    .WithProjectName("heyalan")
    .WithOtlpExporter()
    // .WithDataVolume("heyalan-postgres")
    .WithPgAdmin(admin => admin
        .WithLifetime(ContainerLifetime.Persistent)
        .WithContainerName("heyalan-pgadmin")
        .WithProjectName("heyalan")
    )
    .WithLifetime(ContainerLifetime.Persistent);

var heyalanDb = postgres.AddDatabase("heyalan");

#endregion

#region Infrastructure: RabbitMQ


var rabbitUser = builder.AddParameter("RabbitUser", "admin");
var rabbitPass = builder.AddParameter("RabbitPass", "admin", secret: true);

var rabbitmq = builder.AddRabbitMQ("rabbitmq", userName: rabbitUser, password: rabbitPass)
    .WithContainerName("heyalan-rabbitmq")
    .WithProjectName("heyalan")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

#endregion

#region Application: Intializer

// Run database migrations and minio bucket setup before starting the main app
var initializer = builder.AddProject<Projects.HeyAlan_Initializer>("initializer")
    .WithReference(heyalanDb) // Injects the connection string
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("PUBLIC_BASE_URL", publicBaseUrl)
    .WithEnvironment("TELEGRAM_SECRET_TOKEN", telegramSecretToken)
    .WithEnvironment("RABBITMQ_MANAGEMENTURL", rabbitmq.GetEndpoint("management"))
    .WithEnvironment("RABBITMQ_USER", rabbitUser)
    .WithEnvironment("RABBITMQ_PASS", rabbitPass)
    .WithEnvironment("RABBITMQ_VHOST", "heyalan") // The vhost name you want to use
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq); 

#endregion 

#region Application: Web API

// Main Web API Project
var webapi = builder.AddProject<Projects.HeyAlan_WebApi>("webapi")
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
    .WithReference(rabbitmq)
    .WithEnvironment("ConnectionStrings__rabbitmq", ReferenceExpression.Create($"{rabbitmq}/heyalan"))
    .WithReference(heyalanDb)
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
        .WithContainerName("heyalan-ngrok")
        .WithProjectName("heyalan")
        .WithEnvironment("NGROK_AUTHTOKEN", ngrokAuthToken)
        .WithHttpEndpoint(targetPort: 4040, port: 4040, name: "inspect")
        .WithArgs("http", $"--url={ngrokDomain}", "--log=stdout", "http://host.docker.internal:5000")
        .WaitFor(webapi);
}

#endregion

#region Application: Web App 

// if WebApp is part of the solution, use AddProject
/*var webapp = builder.AddJavaScriptApp("webapp", "../HeyAlan.WebApp", "dev")

    .WithYarn()
    .WithHttpEndpoint(env: "PORT", port: 5010)
    .WithExternalHttpEndpoints()
    .WithReference(webapi)
    .WaitFor(webapi)
    .WithOtlpExporter()
    .WithEnvironment("WEBAPI_ENDPOINT", webapi.GetEndpoint("http"))
    .WithEnvironment("NODE_OPTIONS", "--inspect=0.0.0.0:9229");
*/

if (true)
{
    // if WebApp run is via yarn/npm, use AddExecutable
    var webapp = builder.AddExecutable("webapp", "yarn.cmd", "../HeyAlan.WebApp", "dev")
        // explicitly allow unsecure transport for local dev if needed, or use https
        .WithHttpEndpoint(env: "PORT", port: 5010, name: "http")
        .WithExternalHttpEndpoints()
        .WithReference(webapi)
        .WaitFor(webapi)
        .WithOtlpExporter()
        .WithEnvironment("APP_VERSION", "1.2.3")
        .WithEnvironment("WEBAPI_ENDPOINT", webapi.GetEndpoint("http"))
        .WithEnvironment("NODE_OPTIONS", "--inspect=0.0.0.0:9229");
}

#endregion

builder.Build().Run();
