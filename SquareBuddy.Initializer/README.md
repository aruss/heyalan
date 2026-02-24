# SquareBuddy.Initializer

Initializer service that applies EF Core migrations, seeds the admin user, and ensures the MinIO bucket exists.

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
- Reads connection string `squarebuddydb` and `ADMIN_EMAIL` / `ADMIN_PASSWORD` from configuration.
- Uses retry policy for database connectivity during startup.


**Create new migration**

	dotnet ef migrations add Init --context MainDataContext -o .\Migrations 

**Generate compiled model**

run it from solution root
	
	dotnet ef dbcontext optimize --project SquareBuddy.Data --startup-project SquareBuddy.Initializer --output-dir CompiledModels --namespace SquareBuddy.Data.CompiledModels


**Architecture Decisions**
- TBD (tracked here for future ADR-style notes)
