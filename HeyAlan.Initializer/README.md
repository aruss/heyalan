# HeyAlan.Initializer

Initializer service that applies EF Core migrations, seeds the admin user, registers Telegram webhook defaults, and deploys RabbitMQ topology.

**Tech Stack**
- .NET 10 console/hosted app
- EF Core + Npgsql (PostgreSQL)
- ASP.NET Core Identity (admin seed + data protection keys in DB)
- MinIO client
- Polly resilience pipeline
- YAML configuration via `config.yaml`

**Project Structure**
- `Program.cs` migration/seed/bucket orchestration
- `Migrations` EF Core migrations
- `DesignTimeDbContextFactory.cs` design-time EF tooling

**Operational Notes**
- Reads connection string `heyalan` and `ADMIN_EMAIL` / `ADMIN_PASSWORD` from configuration.
- Uses separate Polly resilience pipelines per startup lane and runs DB and RabbitMQ setup concurrently.
- M8 schema now includes `subscription_square_connections` and `subscription_onboarding_states`; initializer migration runs are expected to create/update these tables.


**Create new migration**

	dotnet ef migrations add Init --context MainDataContext -o .\Migrations 

**Generate compiled model**

run it from solution root
	
	dotnet ef dbcontext optimize --project HeyAlan.Data --startup-project HeyAlan.Initializer --output-dir CompiledModels --namespace HeyAlan.Data.CompiledModels
