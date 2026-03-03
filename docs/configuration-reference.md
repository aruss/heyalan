# Configuration Reference

HeyAlan uses explicit configuration access and validation to keep runtime behavior predictable.

## Configuration Approach

HeyAlan favors explicit `IConfiguration` extension methods over implicit options binding for infrastructure-critical settings.

This model provides:

- fail-fast startup validation
- clear error messages
- predictable behavior across local/dev/container environments
- low ambiguity around key names

## Core Runtime Keys

The exact active set varies by service, but common cross-service keys include:

- `PUBLIC_BASE_URL`
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

## Operational Guidance

- Keep credentials in environment variables, not source-controlled config.
- Use a stable, externally reachable `PUBLIC_BASE_URL` for callbacks and webhooks.
- Validate provider-specific setup using the related setup docs before runtime testing.

## Related Docs

- [Getting Started](./getting-started.md)
- [Setup Webhooks](./setup-webhooks.md)
- [Square App Setup](./square-app-setup.md)
