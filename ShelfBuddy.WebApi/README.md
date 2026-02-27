# ShelfBuddy.WebApi

Backend API for ShelfBuddy. Provides auth, webhooks, and core endpoints.

**Tech Stack**
- ASP.NET Core (.NET 10, Minimal APIs)
- EF Core with PostgreSQL (via ShelfBuddy.Data)
- ASP.NET Core Identity
- OpenTelemetry (OTLP exporter, ASP.NET Core/HTTP/Runtime instrumentation)
- OpenAI + Semantic Kernel (story generation/processing)
- MinIO client for object storage
- YAML configuration via `config.yaml`

**Project Structure**
- `Program.cs` app composition, middleware, and route groups
- `Core` story/board endpoints and domain services
- `Identity` auth endpoints and Identity setup
- `Database` database setup and migrations glue
- `ObjectStorage` MinIO integration
- `Extensions` service registration helpers

**HTTP Surface**
- Health checks endpoint mapped in `Program.cs`
- `POST /stories/_stream` audio streaming endpoint
- `/auth/*` Identity endpoints (register/login/logout/me)

**Architecture Decisions**
- TBD (tracked here for future ADR-style notes)
