# Architecture Overview

This document describes how ShelfBuddy operates at a high level.

## Operating Model

ShelfBuddy is a multi-tenant platform. Each subscription acts as a tenant boundary for:

- users and team membership
- agents
- integration connections
- onboarding state and operational access

## Core Runtime Flows

### Authentication and Access

- Users authenticate through the Web API auth surface.
- Session-oriented access is used for dashboard workflows.
- Subscription membership determines what a user can access.

### Onboarding

- Onboarding state is maintained per subscription.
- Connection and setup steps are evaluated server-side.
- Completion state controls access to post-onboarding workflows.

### Messaging

- Inbound channel traffic enters through webhook endpoints.
- Messages are published into the internal processing pipeline.
- Consumers handle routing, delivery, and persistence responsibilities.
- Conversation history is stored for inbox and follow-up operations.

### Integrations

- Square integration supports merchant connection and token lifecycle.
- Telegram and Twilio support channel communication paths.
- Integration concerns are isolated by provider domain.

## Code Organization Principles

- Feature-first organization for discoverability.
- Shared cross-cutting code in explicit shared folders.
- Store-centric data access direction for EF-backed persistence concerns.
- Strong separation between transport contracts, orchestration, and persistence logic.

## Reliability and Security Principles

- Fail-fast configuration validation.
- Least-privilege external access scopes.
- Sensitive values remain server-side.
- Structured observability and diagnosability across services.

## Related Docs

- [Getting Started](./getting-started.md)
- [Configuration Reference](./configuration-reference.md)
- [Setup Webhooks](./setup-webhooks.md)
- [Square App Setup](./square-app-setup.md)
