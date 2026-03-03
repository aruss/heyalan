# HeyAlan

Shared library for HeyAlan services. Provides common models, configuration helpers, constants, and service defaults.

**Tech Stack**
- .NET 10 class library
- ASP.NET Core shared framework reference
- OpenTelemetry configuration helpers
- Service discovery + HTTP resilience defaults

**Project Structure**
- `Messaging` incoming/outgoing message contracts, consumers, conversation store, and messaging DI wiring
- `Onboarding` onboarding contracts and service logic
- `Identity` identity/auth helpers and extensions
- `SquareIntegration` Square OAuth/token/integration services
- `TelegramIntegration` Telegram client/service/options
- `Data` EF Core `MainDataContext`, entity contracts, and entities
- `Common` shared constants, enums, and query/pagination utilities
- `Configuration` typed options and configuration validation helpers
- `Extensions` cross-cutting hosting/configuration/string extensions

**Architecture Decisions**
- TBD (tracked here for future ADR-style notes)
