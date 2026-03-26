# Configuration Reference

BuyAlan uses explicit configuration access and validation to keep runtime behavior predictable.

## Configuration Approach

BuyAlan favors explicit `IConfiguration` extension methods over implicit options binding for infrastructure-critical settings.

This model provides:

- fail-fast startup validation
- clear error messages
- predictable behavior across local/dev/container environments
- low ambiguity around key names

## Core Runtime Keys

The exact active set varies by service, but common cross-service keys include:

- `PUBLIC_BASE_URL`
- `LOGGING_STDOUT`
- channel/integration keys (Telegram, Square, auth providers)
- connection strings injected by host orchestration

## Pair Validation Rules

Several auth/integration credentials are validated as required pairs (both present or both missing), such as:

- external auth provider credentials
- Square connection credentials

If only one key in a pair is provided, startup validation fails.

## Source Precedence

Configuration can come from:

- YAML defaults (service config files)
- environment variables

Environment variables override file values.

## Logging

`BuyAlan.WebApi` supports optional human-readable stdout logging through `LOGGING_STDOUT`.

- `LOGGING_STDOUT=true` enables the built-in simple console logger.
- `LOGGING_STDOUT=false` disables the readable stdout console logger.
- When unset in development, readable stdout logging defaults to enabled.
- When unset outside development, readable stdout logging defaults to disabled.

This toggle only controls readable stdout logging. OpenTelemetry/OTLP logging remains controlled by existing OTEL configuration such as `OTEL_EXPORTER_OTLP_ENDPOINT`.

## Operational Guidance

- Keep credentials in environment variables, not source-controlled config.
- Use a stable, externally reachable `PUBLIC_BASE_URL` for callbacks and webhooks.
- Validate provider-specific setup using the related setup docs before runtime testing.

## Related Docs

- [Getting Started](./getting-started.md)
- [Setup Webhooks](./setup-webhooks.md)
- [Square App Setup](./square-app-setup.md)
