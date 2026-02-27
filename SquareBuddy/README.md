# SquareBuddy

Shared library for SquareBuddy services. Provides common models, configuration helpers, constants, and service defaults.

**Tech Stack**
- .NET 10 class library
- ASP.NET Core shared framework reference
- OpenTelemetry configuration helpers
- Service discovery + HTTP resilience defaults

**Project Structure**
- `Models.cs` shared request/response models (story inputs, scene graph)
- `Constants.cs` shared constants (table prefix, defaults, limits)
- `Configuration` typed options for MinIO and LiteLLM
- `Extensions` configuration helpers and service defaults (`AddDefaultServices`, health checks, OTEL)

**Architecture Decisions**
- TBD (tracked here for future ADR-style notes)
