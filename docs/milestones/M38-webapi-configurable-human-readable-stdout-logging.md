# Milestone M38: WebApi Configurable Human-Readable Stdout Logging

## Summary
Add optional human-readable stdout logging to `BuyAlan.WebApi` while preserving the existing OpenTelemetry logging pipeline and OTLP export path.

This milestone is intentionally narrow. It changes the WebApi hosting/logging setup so operators can see readable application logs in container stdout when desired, without forcing that overhead in every environment.

- Scope is `BuyAlan.WebApi` runtime logging behavior.
- The target behavior is dual-provider support: built-in console logger for readable stdout plus OpenTelemetry logger for OTLP export.
- Human-readable stdout must be configurable because writing to stdout has non-zero runtime cost and can create backpressure at higher log volume.
- OTLP export must remain independent from stdout logging.
- The milestone must be implementable in a new context window without requiring additional repository discovery.

## Goals
- [x] Restore readable stdout logs for `BuyAlan.WebApi`.
- [x] Keep OTLP/OpenTelemetry logging available in parallel.
- [x] Make readable stdout logging explicitly configurable.
- [x] Default to readable stdout enabled in development and disabled outside development unless explicitly turned on.
- [x] Avoid HTTP/API, schema, migration, or OpenAPI changes.

## Non-Goals
- [ ] No WebApp logging changes.
- [ ] No database or schema changes.
- [ ] No OpenAPI or generated client changes.
- [ ] No replacement of the existing OpenTelemetry stack.
- [ ] No custom logging framework or third-party logger introduction.
- [ ] No telemetry infrastructure or collector configuration changes.

## User Decisions (Locked)
- [x] The desired target is not stdout-only; OTLP export should remain available.
- [x] Readable stdout should be configurable rather than always on.
- [x] Performance overhead from stdout is considered real enough to justify an explicit toggle.
- [x] The output format should be human-readable console logging, not raw OpenTelemetry console-exporter output.
- [x] The milestone must include complete handoff findings so implementation can start in a fresh context.

## Findings from Repository Analysis
- [x] `BuyAlan.WebApi/Program.cs` uses `builder.AddDefaultServices();` at startup.
- [x] `BuyAlan/Extensions/TBuilderExtensions.cs` owns the shared logging setup used by WebApi.
- [x] `ConfigureOpenTelemetry` currently calls `builder.Logging.ClearProviders();`.
- [x] After clearing providers, the shared setup only registers `builder.Logging.AddOpenTelemetry(...)`.
- [x] The shared logging setup currently sets:
  - [x] `IncludeFormattedMessage = true`
  - [x] `IncludeScopes = true`
- [x] No built-in console logger provider is currently re-added after `ClearProviders()`.
- [x] Because no console provider is registered, readable ASP.NET Core logs are not emitted to stdout by default.
- [x] OTLP export is currently gated by `OTEL_EXPORTER_OTLP_ENDPOINT` in `AddOpenTelemetryExporters`.
- [x] If `OTEL_EXPORTER_OTLP_ENDPOINT` is absent, the OTLP exporter is not enabled.
- [x] The current startup path already emits at least one application log that is useful for validation:
  - [x] `BuyAlan.WebApi/Program.cs` registers an `ApplicationStarted` callback that logs `Web API started...` at `Debug`.
- [x] `AddDefaultServices()` is only used by `BuyAlan.WebApi` in the current repository state.
- [x] Local/container telemetry wiring already exists in the repository:
  - [x] `docker-compose.local.yaml` sets OTEL env vars for `buyalan-webapi`
  - [x] `docker-compose.telemetry.yaml` defines collector, Loki, and Tempo services
- [x] The repository configuration model is YAML plus environment variables, with environment variables overriding file values.

## Architecture Decisions (Locked)
- [x] Keep `builder.Logging.ClearProviders()` so the logging surface remains explicit and controlled.
- [x] Re-add logging providers explicitly rather than relying on framework defaults.
- [x] Use the built-in ASP.NET Core console logger for readable stdout.
- [x] Prefer `AddSimpleConsole(...)` over raw `AddConsole()` for predictable readable formatting.
- [x] Keep OpenTelemetry logging enabled as a separate provider.
- [x] Gate readable stdout with a new explicit configuration key.
- [x] Resolve the stdout toggle with environment-aware defaults:
  - [x] Development default: enabled
  - [x] Non-development default: disabled
- [x] OTLP export remains controlled only by OTEL configuration and must not be disabled implicitly when stdout is disabled.
- [x] Console logging failure or OTLP exporter unavailability must not block app startup.
- [x] No OpenTelemetry console exporter should be introduced for this milestone because its output is not the target human-readable operator format.

## Public Interfaces / Config
- [x] No HTTP API changes.
- [x] No DTO/schema changes.
- [x] Add one new runtime configuration key:
  - [x] `LOGGING_STDOUT`
- [x] `LOGGING_STDOUT` must support standard .NET configuration parsing for booleans through existing configuration access.
- [ ] Effective behavior contract:
  - [ ] Console enabled + OTLP configured: readable stdout and OTLP export
  - [ ] Console enabled + OTLP not configured: readable stdout only
  - [ ] Console disabled + OTLP configured: OTLP export only
  - [ ] Console disabled + OTLP not configured: no readable application log sink from this shared setup; document this as an intentional but low-observability mode
- [ ] Existing OTEL configuration remains authoritative for export:
  - [ ] `OTEL_EXPORTER_OTLP_ENDPOINT`
  - [ ] `OTEL_EXPORTER_OTLP_PROTOCOL`
  - [ ] `OTEL_SERVICE_NAME`

## Implementation Plan by Gate

## Gate A: Configurable Console Provider
- [x] Update `BuyAlan/Extensions/TBuilderExtensions.cs` to compute whether readable console logging is enabled.
- [x] Read `LOGGING_STDOUT` from configuration.
- [x] If the key is absent, derive the default from environment:
  - [x] `true` for development
  - [x] `false` otherwise
- [x] Keep `builder.Logging.ClearProviders()`.
- [x] Register `AddSimpleConsole(...)` only when the toggle resolves to enabled.
- [x] Configure the console formatter for operator readability:
  - [x] single-line output enabled
  - [x] timestamps enabled
  - [x] scopes enabled where supported by the logger/provider setup
  - [x] color behavior chosen conservatively so container logs remain readable even without ANSI support
- [x] Keep the configuration code explicit and local to the shared builder extension.

### Gate A Acceptance Criteria
- [ ] WebApi can emit readable stdout logs when console logging is enabled.
- [ ] WebApi does not emit readable stdout logs when console logging is disabled.
- [ ] Development defaults to readable stdout without extra configuration.
- [ ] Non-development does not enable readable stdout unless explicitly configured.

## Gate B: Preserve OpenTelemetry Logging
- [x] Keep `builder.Logging.AddOpenTelemetry(...)` registered regardless of console toggle.
- [x] Preserve the existing OpenTelemetry logger options:
  - [x] `IncludeFormattedMessage = true`
  - [x] `IncludeScopes = true`
- [x] Keep OTLP exporter registration gated by `OTEL_EXPORTER_OTLP_ENDPOINT`.
- [x] Ensure stdout and OTLP can operate at the same time without choosing one over the other.
- [ ] Do not introduce duplicate log providers that would cause repeated console lines for the same event.

### Gate B Acceptance Criteria
- [ ] When OTLP is configured, logs still flow to the OpenTelemetry pipeline after the milestone.
- [ ] Enabling readable stdout does not disable OTLP export.
- [ ] A single application log event produces at most one human-readable console line.

## Gate C: Configuration and Documentation Alignment
- [x] Document the new `LOGGING_STDOUT` key in the relevant configuration docs if that documentation is maintained for runtime keys.
- [ ] Keep the repository’s existing precedence model intact:
  - [x] YAML defaults
  - [x] environment variable overrides
- [ ] If there is a service-local config file that should carry an explicit default comment or entry, update it only if doing so does not create ambiguity with environment-aware defaults.
- [ ] If no documentation update is made, implementation notes must still be sufficient in code comments or milestone completion notes for future operators.

### Gate C Acceptance Criteria
- [ ] Operators can discover how to enable or disable readable stdout for WebApi.
- [ ] The new flag does not conflict with existing OTEL keys or their meaning.

## Gate D: Validation and Regression Checks
- [ ] Run WebApi with console logging enabled and verify readable stdout output on startup.
- [ ] Verify the existing startup debug log is visible when the effective log level allows `Debug`.
- [ ] Exercise at least one request path and confirm logs remain readable in stdout.
- [ ] Run with OTLP configured and confirm stdout still works.
- [ ] Run with OTLP unavailable or unset and confirm stdout still works when enabled.
- [ ] Run with console logging disabled and confirm readable stdout lines disappear while OTLP behavior remains intact when configured.
- [ ] Check that enabling both providers does not create duplicate readable console lines.

### Gate D Acceptance Criteria
- [ ] Readable stdout behavior matches the configuration matrix.
- [ ] Existing OTEL export behavior is preserved.
- [ ] The logging setup degrades safely when OTLP is unavailable.

## Test Cases and Scenarios (Authoritative)
1. **Console enabled, OTLP configured**
- [ ] `LOGGING_STDOUT=true` with `OTEL_EXPORTER_OTLP_ENDPOINT` set emits readable stdout logs.
- [ ] The same run continues to export through OTLP.
- [ ] No duplicate human-readable console lines are produced.

2. **Console enabled, OTLP absent**
- [ ] `LOGGING_STDOUT=true` with no OTLP endpoint emits readable stdout logs.
- [ ] Application startup succeeds without an OTLP collector.

3. **Console disabled, OTLP configured**
- [ ] `LOGGING_STDOUT=false` suppresses readable stdout logs.
- [ ] OTLP export still functions when configured.

4. **Default behavior**
- [ ] In development, omitting `LOGGING_STDOUT` results in readable stdout logging enabled.
- [ ] Outside development, omitting `LOGGING_STDOUT` results in readable stdout logging disabled.

5. **Validation event coverage**
- [ ] The `ApplicationStarted` debug log from `BuyAlan.WebApi/Program.cs` is visible when debug-level logging is enabled and console logging is on.
- [ ] At least one request-generated framework or application log is visible in stdout when console logging is on.

6. **Failure handling**
- [ ] An unavailable OTLP collector does not prevent readable stdout logging.
- [ ] Disabling console logging does not crash startup or remove OTLP export when OTLP is configured.

## Risks and Mitigations
- [x] Risk: readable stdout increases CPU and I/O overhead at higher log volume.
- [x] Mitigation: make stdout logging configurable and default it off outside development.
- [x] Risk: enabling both providers could make operators think logs are duplicated if the same event appears in different sinks.
- [x] Mitigation: ensure only one console provider is active and validate only one readable stdout line per event.
- [x] Risk: configuration ambiguity around default behavior.
- [x] Mitigation: lock the default matrix in code and document it in milestone completion.
- [x] Risk: container/runtime environments may not render ANSI color reliably.
- [x] Mitigation: use conservative console formatting and avoid relying on color for readability.

## Implementation Notes for Fresh Context Handoff
- [x] Start in `BuyAlan/Extensions/TBuilderExtensions.cs`; that is the only shared logging composition point currently affecting WebApi.
- [x] Confirm current use sites before editing, but repository analysis at milestone creation time found `AddDefaultServices()` only in `BuyAlan.WebApi/Program.cs`.
- [x] Do not add third-party logger packages for this milestone.
- [x] Do not use the OpenTelemetry console exporter as a substitute for readable stdout.
- [x] Prefer the smallest possible change that restores readable stdout while preserving OTEL behavior.
- [ ] If implementation reveals an existing repo-tracked config file for WebApi where `LOGGING_STDOUT` should be declared, preserve existing keys and comments when adding it.
