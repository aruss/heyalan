# Milestone M35: WebApp OTLP Server Logging

## Summary
Add structured server-side application logging to `BuyAlan.WebApp` using a shared logger module and OTLP export to the existing local telemetry collector.

- Scope is server-side only.
- Browser/client `console.*` logs remain local and are out of scope for this milestone.
- Existing OTLP-related environment variables are assumed to already be present in the container environment.
- Server logs must continue to appear in stdout and must also be exported to OTLP for collection in Loki.
- Unhandled server exceptions and unhandled promise rejections must be caught and logged with error severity before the process exits when required.

## Scope
- [x] `BuyAlan.WebApp` only.
- [x] Server-side logging only.
- [x] Shared logger foundation for Next.js server/runtime code.
- [x] OTLP log export through the existing collector.
- [x] Structured logging migration for current server-side warning/error call sites.
- [x] Explicit process-level capture for unhandled promise rejections and uncaught exceptions.
- [ ] Validation that logs reach stdout and Loki without regressing traces.

## Non-Goals (Out of Scope)
- [ ] No browser/client log forwarding.
- [ ] No WebApi, database, schema, or OpenAPI changes.
- [ ] No telemetry environment-variable work in container definitions.
- [ ] No metrics backend work.
- [ ] No attempt to keep the process alive after `uncaughtException`.
- [ ] No monkey-patching of global `console.*` as the primary logging approach.

## User Decisions (Locked)
- [x] OTLP-related environment variables are already handled outside this milestone.
- [x] Use a shared logger module for server-side code instead of relying on `console.*`.
- [x] Server logs must still be visible in stdout.
- [x] OTLP logging scope is server-side only.
- [x] Unhandled exceptions must be caught and logged as errors/fatal errors.
- [x] No milestone scratchpad is required for this milestone.

## Findings from Repository Analysis
- [x] `BuyAlan.WebApp/src/instrumentation.ts` loads `src/instrumentation.otel.ts` only for `NEXT_RUNTIME === 'nodejs'`.
- [x] `BuyAlan.WebApp/src/instrumentation.otel.ts` currently configures traces and metrics only; no log exporter/logger is wired.
- [x] `BuyAlan.WebApp/src/lib/logger.ts` currently contains only a commented-out logging stub.
- [x] Current server-side log call sites are limited and currently use `console.warn`, including:
  - [x] `src/lib/feature-flags/feature-flag-env.ts`
  - [x] `src/app/health/route.ts`
- [x] `src/lib/use-debounced-effect.ts` contains `console.error` calls in client-side React code and must remain out of scope.
- [x] There are currently no existing process-level hooks for:
  - [x] `unhandledRejection`
  - [x] `uncaughtException`
- [x] There are currently no `error.tsx` or `global-error.tsx` files under `BuyAlan.WebApp/src`.

## Architecture Decisions (Locked)
- [x] Use `pino` as the shared server-side logger.
- [x] Use `pino-opentelemetry-transport` for OTLP log export.
- [x] Keep stdout output alongside OTLP export.
- [x] Keep trace/metric bootstrap in `src/instrumentation.otel.ts`; do not replace it with a new unified logging SDK approach in this milestone.
- [x] Add a dedicated server-only bootstrap module for process-level exception hooks.
- [x] Register exception hooks once per Node process using a sentinel guard.
- [x] `unhandledRejection` must be logged as an error event.
- [x] `uncaughtException` must be logged as a fatal/error event and followed by non-zero process exit after best-effort flush.
- [x] Logging must remain structured and secret-safe.

## Public Interfaces / Contracts
- [x] Add a server-only shared logger contract in `src/lib/logger.ts`:
  - [x] `logger`
  - [x] `createLogger(bindings)` or equivalent child-logger helper
  - [x] `serializeError(error: unknown)` helper for normalized structured error fields
- [x] Add a server-only exception bootstrap contract in a new module such as `src/lib/server-error-hooks.ts`:
  - [x] `registerServerErrorHooks()`
- [x] Standardize server log fields for searchability and diagnostics:
  - [x] `eventName`
  - [x] `errorName`
  - [x] `errorMessage`
  - [x] `stack`
  - [x] contextual structured fields such as `module`, `route`, `reason`, `upstreamStatus`

## Implementation Plan by Gate

## Gate A: Logger Foundation and Dependencies
- [x] Add runtime dependencies to `BuyAlan.WebApp`:
  - [x] `pino`
  - [x] `pino-opentelemetry-transport`
- [x] Replace the commented logger stub in `src/lib/logger.ts` with a real server-only implementation.
- [x] Configure the logger with:
  - [x] stdout output using `pino/file` to destination `1`
  - [x] OTLP export using `pino-opentelemetry-transport`
  - [x] `LOG_LEVEL` support with default `info`
- [x] Set OTEL resource attributes in code from existing environment values:
  - [x] `service.name` from `OTEL_SERVICE_NAME` with fallback `buyalan-webapp`
  - [x] `service.version` from `APP_VERSION` when present
  - [x] `deployment.environment` from `NODE_ENV`
- [x] Expose child-logger creation for contextual bindings.
- [x] Ensure the logger module is server-only and cannot be imported into client bundles.

### Gate A Acceptance Criteria
- [x] A shared server-side logger is available to all webapp server modules.
- [ ] Logs are emitted to stdout and OTLP simultaneously.
- [x] Logger initialization does not depend on browser APIs.
- [x] The logger module is isolated from client-side code.

## Gate B: Server Call-Site Migration
- [x] Replace current server-side `console.warn` and similar diagnostics with the shared logger.
- [x] Update `src/lib/feature-flags/feature-flag-env.ts` to use structured warning logs.
- [x] Update `src/app/health/route.ts` warning path to use the shared logger when the probe branch is active.
- [x] Ensure migrated logs use structured fields rather than interpolated-only free text where useful.
- [x] Leave client-only `console.error` usage in `src/lib/use-debounced-effect.ts` unchanged.

### Gate B Acceptance Criteria
- [x] Current server-side warning/error call sites no longer depend on raw `console.*`.
- [x] Feature-flag parsing warnings are structured and searchable.
- [x] Health-route warnings are structured and searchable.
- [x] No client-side code is accidentally migrated to the server-only logger.

## Gate C: Unhandled Exception and Rejection Capture
- [x] Add a server-only bootstrap module for process-level error hooks.
- [x] Register `process.on("unhandledRejection", ...)`.
- [x] Register `process.on("uncaughtException", ...)`.
- [x] Normalize unknown thrown values into a consistent structured error payload.
- [x] Log unhandled promise rejections as `error`.
- [x] Log uncaught exceptions as `fatal` or highest-severity error.
- [x] Perform best-effort transport flush before terminating on `uncaughtException`.
- [x] Exit the process with non-zero status after `uncaughtException`.
- [x] Guard hook registration so handlers are installed only once per process.
- [x] Import and execute the hook registration from the Node runtime startup path, using `src/instrumentation.otel.ts`.

### Gate C Acceptance Criteria
- [x] Unhandled promise rejections are captured and logged without silent loss.
- [x] Uncaught exceptions are captured and logged before process termination.
- [x] Duplicate process-hook registration does not occur across module reloads.
- [x] The exception-hook bootstrap executes only in the Node server runtime.

## Gate D: Validation and Regression Coverage
- [ ] Verify logs still appear in stdout.
- [ ] Verify server logs arrive in Loki through the existing collector.
- [ ] Verify traces still arrive in Tempo.
- [ ] Verify the logger does not introduce runtime crashes when the collector is unavailable.
- [ ] Verify browser-only logs do not appear in Loki.
- [x] Verify server-only modules are not bundled into client code.

### Gate D Acceptance Criteria
- [ ] Server logs are visible both locally in container output and in Loki.
- [ ] Existing trace export behavior is preserved.
- [ ] Logging degrades safely when OTLP export is unavailable.
- [x] Client logging behavior remains unchanged and out of scope.

## Test Cases and Scenarios (Authoritative)
1. **Logger initialization**
- [ ] The shared logger loads successfully in the Next.js server runtime.
- [x] Importing the logger from a client component is prevented by the server-only boundary.

2. **Structured server warnings**
- [ ] Invalid `FEATURE_FLAGS` segments emit structured warning logs.
- [ ] Unknown feature-flag keys emit structured warning logs.
- [ ] Duplicate feature-flag keys emit structured warning logs.
- [ ] The server-side health warning path emits a structured warning log when triggered.

3. **Unhandled error capture**
- [ ] A rejected promise without a local handler emits an `error` log entry.
- [ ] An uncaught exception emits a fatal/error log entry containing message and stack.
- [ ] After logging an uncaught exception, the process exits with a non-zero code.
- [ ] Unknown thrown values are serialized into a stable structured shape.

4. **Telemetry integration**
- [ ] Server-side logs appear in stdout.
- [ ] Server-side logs appear in Loki through the OTLP collector.
- [ ] Existing traces continue to appear in Tempo.
- [ ] Browser-only log sites such as `use-debounced-effect.ts` do not appear in Loki.

5. **Failure handling**
- [ ] If the OTLP collector is temporarily unavailable, stdout logging still works.
- [ ] OTLP export failure does not prevent application startup when stdout logging is still available.

## Risks and Mitigations
- [ ] Risk: server-only logging code leaks into client bundles.
- [ ] Mitigation: enforce `server-only` boundaries and keep logger imports limited to server modules.
- [ ] Risk: process-level hooks are registered multiple times during dev/hot reload.
- [ ] Mitigation: use a process-global sentinel to ensure one-time registration.
- [ ] Risk: error logs expose secrets, tokens, or excessive PII.
- [ ] Mitigation: keep structured logs sanitized and avoid raw secret/token payloads.
- [ ] Risk: attempting to continue after `uncaughtException` leaves the process in a corrupted state.
- [ ] Mitigation: log, flush best-effort, and terminate non-zero.

## Notes
- [ ] Do not edit generated files manually.
- [ ] Do not add browser/client log forwarding in this milestone.
- [ ] Do not add metrics backend work in this milestone.
- [ ] `next.config.js` evaluation-time failures are outside the runtime logger scope and do not need to be routed through the shared logger.
