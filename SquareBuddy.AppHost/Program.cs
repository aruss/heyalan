using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

#region Configuration 

builder.Configuration
    .AddYamlFile("config.yaml", optional: false, reloadOnChange: false);

builder.Configuration.AddEnvFile("./.env");
builder.Configuration.AddEnvironmentVariables();

#endregion

#region Infrastructure: Postgres

var postgres = builder.AddPostgres("postgresql")
    .WithContainerName("squarebuddy-postgres")
    .WithProjectName("squarebuddy")
    .WithOtlpExporter()
    // .WithDataVolume("squarebuddy-postgres")
    .WithPgAdmin(admin => admin
        .WithLifetime(ContainerLifetime.Persistent)
        .WithContainerName("squarebuddy-pgadmin")
        .WithProjectName("squarebuddy")
    )
    .WithLifetime(ContainerLifetime.Persistent);

var litellmDb = postgres.AddDatabase("litellmdb");

var squarebuddyDb = postgres.AddDatabase("squarebuddydb");

#endregion

#region Infrastructure: MinIO

// Default credentials for local development
var minioUser = "minioadmin";
var minioPass = "minioadmin";

// Use generic AddContainer since the specific hosting package is unavailable
var minio = builder.AddContainer("minio", "quay.io/minio/minio")
    .WithContainerName("squarebuddy-minio")
    .WithProjectName("squarebuddy")
    .WithHttpEndpoint(targetPort: 9000, port: 9000, name: "api")
    .WithHttpEndpoint(targetPort: 9001, port: 9001, name: "console")
    .WithEnvironment("MINIO_ROOT_USER", minioUser)
    .WithEnvironment("MINIO_ROOT_PASSWORD", minioPass)
    .WithEnvironment("OTEL_SERVICE_NAME", "minio")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://host.docker.internal:4317")
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithLifetime(ContainerLifetime.Persistent);

#endregion

#region Infrastructure: LiteLLM

var pgEndpoint = postgres.GetEndpoint("tcp");
var litellmConnectionString = ReferenceExpression.Create(
    $"postgresql://postgres:{postgres.Resource.PasswordParameter}@{pgEndpoint.Property(EndpointProperty.Host)}:{pgEndpoint.Property(EndpointProperty.Port)}/{litellmDb.Resource.Name}?schema=litellm");

var openAiKey = builder.Configuration["OPENAI_API_KEY"]
    ?? throw new InvalidOperationException(
        "OPENAI_API_KEY is missing in host configuration.");

var litellmMasterKey = builder.Configuration["LITELLM_MASTER_KEY"]
    ?? throw new InvalidOperationException(
        "LITELLM_MASTER_KEY is missing in host configuration.");

var litellm = builder.AddContainer("litellm", "ghcr.io/berriai/litellm", "litellm_stable_release_branch-v1.77.5-stable")
    .WithContainerName("squarebuddy-litellm")
    .WithProjectName("squarebuddy")
    .WithBindMount(Path.GetFullPath("./LiteLLM/config.yaml"), "/app/config.yaml")
    .WithHttpEndpoint(targetPort: 4000, port: 4000, name: "api")
    .WithEnvironment("DATABASE_URL", litellmConnectionString)
    .WithEnvironment("OPENAI_API_KEY", openAiKey)
    .WithEnvironment("LITELLM_MASTER_KEY", litellmMasterKey)
    .WithEnvironment("OTEL_SERVICE_NAME", "litellm")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://host.docker.internal:4317")
    .WithEnvironment("LITELLM_CALLBACKS", "otel")
    .WithReference(litellmDb)
    .WithArgs("--config", "/app/config.yaml")
    .WithLifetime(ContainerLifetime.Persistent);

#endregion

#region Application: Intializer

var adminEmail = builder.Configuration["ADMIN_EMAIL"]
        ?? throw new InvalidOperationException("ADMIN_EMAIL is missing in host configuration.");

var adminPassword = builder.Configuration["ADMIN_PASSWORD"]
        ?? throw new InvalidOperationException("ADMIN_PASSWORD is missing in host configuration."); 

// Run database migrations and minio bucket setup before starting the main app
var initializer = builder.AddProject<Projects.SquareBuddy_Initializer>("initializer")
    .WithReference(squarebuddyDb) // Injects the connection string
    .WithEnvironment("MINIO_ENDPOINT", minio.GetEndpoint("api"))
    .WithEnvironment("MINIO_ACCESS_KEY", minioUser)
    .WithEnvironment("MINIO_SECRET_KEY", minioPass)
    .WithEnvironment("ADMIN_EMAIL", adminEmail)
    .WithEnvironment("ADMIN_PASSWORD", adminPassword)
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WaitFor(postgres)
    .WaitFor(minio);

#endregion 

#region Application: Web API

// Background Vite watcher for the frontend
/* var vite = builder.AddExecutable("vite", "npm.cmd", "../SquareBuddy.WebApi", "run", "watch")
    .WithOtlpExporter();*/

// Main Web API Project
var webapi = builder.AddProject<Projects.SquareBuddy_WebApi>("webapi")
    .WithExternalHttpEndpoints()
    .WithEnvironment("LITELLM_ENDPOINT", litellm.GetEndpoint("api"))
    .WithEnvironment("LITELLM_API_KEY", litellmMasterKey)
    .WithEnvironment("MINIO_ENDPOINT", minio.GetEndpoint("api"))
    .WithEnvironment("MINIO_ACCESS_KEY", minioUser)
    .WithEnvironment("MINIO_SECRET_KEY", minioPass)
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("SWAGGER_ENABLED", "true")
    .WithReference(squarebuddyDb)
    .WaitFor(postgres)
   //  .WaitFor(vite)
    .WaitFor(litellm)
    .WaitFor(minio)
    .WaitForCompletion(initializer);

#endregion

#region Application: Web App 

// if WebApp is part of the solution, use AddProject
/*var webapp = builder.AddJavaScriptApp("webapp", "../SquareBuddy.WebApp", "dev")

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
var webapp = builder.AddExecutable("webapp", "yarn.cmd",  "../SquareBuddy.WebApp", "dev")
    // explicitly allow unsecure transport for local dev if needed, or use https
    .WithHttpEndpoint(env: "PORT", port: 5010, name: "http")
    .WithExternalHttpEndpoints()
    .WithReference(webapi)
    .WaitFor(webapi)
    .WithOtlpExporter()
    .WithEnvironment("APP_VERSION", "1.2.3")
    .WithEnvironment("WEBAPI_ENDPOINT", webapi.GetEndpoint("http"))
    .WithEnvironment("NODE_OPTIONS", "--inspect=0.0.0.0:9229");

#endregion

builder.Build().Run();
