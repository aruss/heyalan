# ShelfBuddy.Data

Data access library for ShelfBuddy. Defines the EF Core `MainDataContext`, entity mappings, and shared persistence conventions.

**Tech Stack**
- .NET 10 class library
- EF Core (Relational) + Npgsql (PostgreSQL)
- ASP.NET Core Identity EF stores
- Data Protection key storage via EF Core

**Project Structure**
- `MainDataContext.cs` DbContext, model configuration, and audit/id hooks
- `Entities` domain entities (subscriptions, story boards/requests, credits, users/roles)
- `CompiledModels` EF Core compiled model (excluded from build inputs)
- `IEntityWithId.cs` and `IEntityWithAudit.cs` shared persistence contracts

**Persistence Conventions**
- Table/constraint/index names are prefixed and snake-cased via `ApplyPostgresNamingConvention`.
- `SaveChanges` assigns GUIDs and audit timestamps for `IEntityWithId` / `IEntityWithAudit`.

**Architecture Decisions**
- TBD (tracked here for future ADR-style notes)
