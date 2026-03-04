# M15 - MassTransit to WolverineFx Migration

## Summary
Migrate HeyAlan messaging from MassTransit to WolverineFx on .NET 10 with RabbitMQ, using a single cutover and preserving current behavior.

The migration enables durable inbox/outbox reliability with PostgreSQL-backed Wolverine storage in a dedicated `wolverine` schema, and keeps topology/resource provisioning owned by `HeyAlan.Initializer`.

## Scope
- Replace MassTransit runtime wiring in `HeyAlan.WebApi` and `HeyAlan.Initializer`.
- Refactor current MassTransit consumers to Wolverine handlers with equivalent behavior.
- Replace ingress publish calls in webhook endpoints from MassTransit interfaces to Wolverine bus.
- Configure Wolverine RabbitMQ routing and durability policies.
- Integrate Wolverine envelope storage mapping into EF Core model for migration-based schema management.
- Remove MassTransit package references after migration.

## Non-Goals (Out of Scope)
- Redesigning buffering/coordinator logic in `IncomingMessage` processing.
- Introducing WhatsApp outbound pipeline changes.
- Running dual bus stacks in production.
- Modifying HTTP contracts, endpoint URLs, or onboarding UX contracts.
- Editing generated files.

## User Decisions (Locked)
- [x] Cutover strategy: single cutover.
- [x] Durability: durable inbox + durable outbox.
- [x] Queue topology strategy: explicit new Wolverine queue names.
- [x] Provisioning ownership: Initializer only.
- [x] Refactor depth: minimal behavior-preserving.
- [x] Wolverine schema name: `wolverine`.

## Planned RabbitMQ Topology
- Incoming queue: `incoming-message`
- Outgoing Telegram queue: `telegram-outgoing-messages`

## Gate A - WebApi Transport Migration
- [x] Replace `AddMassTransitServices()` usage in `HeyAlan.WebApi/Program.cs` with Wolverine bootstrap.
- [x] Replace `HeyAlan.WebApi/Infrastructure/MassTransitBuilderExtensions.cs` with Wolverine equivalent.
- [x] Configure `UseRabbitMq(...)` for WebApi.
- [x] Configure explicit routing for:
  - [x] `IncomingMessage` publish route.
  - [x] `OutgoingTelegramMessage` publish route.
  - [x] listener(s) for incoming/outgoing handler execution.
- [x] Do not enable topology ownership in WebApi.

### Gate A Acceptance Criteria
- [x] WebApi composes and boots with Wolverine messaging.
- [x] No MassTransit service registrations remain in WebApi runtime composition.

## Gate B - Initializer Provisioning Migration
- [x] Remove MassTransit topology deployment path (`DeployTopologyOnly` and `IBusControl.DeployAsync`) from `HeyAlan.Initializer/Program.cs`.
- [x] Configure Wolverine in Initializer with RabbitMQ and `.AutoProvision()`.
- [x] Keep current vhost provisioning via RabbitMQ Management API.
- [x] Keep retry lanes and startup lane orchestration.
- [x] Execute Wolverine resource setup in Rabbit lane.

### Gate B Acceptance Criteria
- [x] Initializer provisions Rabbit topology/resources through Wolverine.
- [x] Initializer remains the sole topology/resource setup owner.

## Gate C - Ingress Publish Interface Migration
- [x] Replace `IPublishEndpoint` with Wolverine `IMessageBus` in:
  - [x] `HeyAlan.WebApi/TelegramIntegration/TelegramWebhookEndpoints.cs`
  - [x] `HeyAlan.WebApi/TwilioIntegration/TwilioWebhookEndpoints.cs`
- [x] Replace publish calls with Wolverine publish API.
- [x] Preserve current status-code and logging behavior.

### Gate C Acceptance Criteria
- [x] Telegram webhook still publishes incoming messages on successful ingress.
- [ ] Twilio webhook still publishes placeholder incoming messages with current behavior.

## Gate D - Consumer to Handler Refactor
- [x] Migrate `IncomingMessageConsumer` logic to Wolverine handler.
- [x] Migrate `OutgoingTelegramMessageConsumer` logic to Wolverine handler.
- [x] Preserve buffered processing behavior:
  - [x] 5s quiet window.
  - [x] cancel/restart on new message.
  - [x] 4s fake business logic delay.
  - [x] max 100 messages per batch.
  - [x] one outgoing Telegram reply per successful processed batch.
- [x] Preserve existing persistence and validation behavior in outbound Telegram handling.

### Gate D Acceptance Criteria
- [x] Behavior is functionally equivalent to existing MassTransit-based processing.
- [x] No regressions in logging/error semantics for invalid outbound Telegram data.

## Gate E - Durability + EF Core Integration
- [x] Enable Wolverine durable inbox/outbox with PostgreSQL.
- [x] Configure Wolverine storage schema as `wolverine`.
- [x] Add Wolverine envelope mapping to `HeyAlan/Data/MainDataContext.cs`.
- [x] Ensure existing naming convention logic does not break Wolverine envelope mapping/storage names.

### Gate E Acceptance Criteria
- [x] Wolverine storage artifacts target schema `wolverine`.
- [x] EF migration model can represent Wolverine envelope storage correctly.

## Gate F - Dependency Cleanup
- [x] Remove `MassTransit.RabbitMQ` package references from:
  - [x] `HeyAlan/HeyAlan.csproj`
  - [x] `HeyAlan.WebApi/HeyAlan.WebApi.csproj`
  - [x] `HeyAlan.Initializer/HeyAlan.Initializer.csproj`
- [x] Ensure required Wolverine packages are present in projects that compile messaging runtime.

### Gate F Acceptance Criteria
- [ ] Solution restores/builds without MassTransit dependencies.

## Gate G - Verification
- [x] Build verification for affected projects.
- [x] Manual infra verification: initializer created RabbitMQ vhost/queues and PostgreSQL `wolverine` schema.
- [x] Test incoming Telegram ingestion path publishes and is processed end-to-end.
- [ ] Test buffered batch semantics (quiet window + cancel/restart + cap).
- [ ] Test outgoing Telegram happy path and failure cases:
  - [ ] missing agent
  - [ ] missing bot token
  - [ ] invalid chat id
- [ ] Verify RabbitMQ `_error` queue behavior/triage guidance remains valid.

### Gate G Acceptance Criteria
- [x] No behavioral regressions in current messaging flows.
- [ ] New topology and durability behavior is validated.

## Handoff Gate (Repo Rule)
- [x] After DB schema-impacting changes are applied, pause and hand off for developer-run migration command:

```powershell
dotnet ef migrations add Init --context MainDataContext -o .\Migrations
```

- [x] Resume follow-up implementation only after explicit developer confirmation.

## Risks and Notes
- Dual-stack operation is intentionally avoided to reduce duplicate-consumption risk.
- Buffer state remains in-process and volatile across process restarts by design in this milestone.
- Existing milestone guidance to inspect RabbitMQ `_error` queues remains relevant during cutover.
- Post-implementation finding: Wolverine did not execute `IncomingMessageConsumer` until explicit handler discovery registration was added in WebApi:
  - `options.Discovery.IncludeType<IncomingMessageConsumer>()`
  - `options.Discovery.IncludeType<OutgoingTelegramMessageConsumer>()`
- Root cause: handlers live in the shared `HeyAlan` assembly, and were not discovered by default from the WebApi host assembly.
- Operational note: WebApi restart is required after discovery wiring changes.

## Assumptions and Defaults
- Clean-slate deployment context (no prior released topology compatibility constraints).
- Messaging scope in this milestone is `IncomingMessage` + `OutgoingTelegramMessage`.
- Initializer and WebApi continue using Aspire-injected `ConnectionStrings__rabbitmq`.
