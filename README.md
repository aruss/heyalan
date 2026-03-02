# ShelfBuddy

ShelfBuddy is an AI teammate for merchants that handles customer conversations across SMS, WhatsApp, and Telegram.
It helps teams respond faster, guide buyers, support orders, and keep sales conversations moving without requiring constant manual effort.

## Product Overview

ShelfBuddy is built for operators, support teams, and store owners who need reliable customer communication across channels.
It combines channel messaging, merchant context, and workflow automation so teams can manage more conversations with less operational overhead.

### What ShelfBuddy Helps With
- Responding to customer messages across supported channels.
- Keeping conversation history and status available for follow-up.
- Supporting sales and service journeys with AI-assisted responses.
- Connecting merchant systems so conversation context can drive actions.

## Available Now

- Multi-channel messaging foundation for Telegram and SMS/Twilio paths, with outbound delivery pipeline support.
- Conversation persistence model for inbox-style history and read-state tracking foundations.
- Admin dashboard shell with authenticated user profile context.
- Identity and session-based authentication flows (local and external provider paths).
- Subscription-aware onboarding model with membership handling for first-time users.
- Square connection model for subscription-scoped merchant access and onboarding workflows.
- Feature-oriented backend organization improvements to make code easier to navigate and evolve.

## What's Next

- Deeper onboarding completion and invitation/team collaboration flows.
- Additional UI/UX polish in admin navigation and breadcrumb behavior.
- Expanded operational automation around channel and integration setup.
- Broader hardening of tests and end-to-end validation coverage.

## Technical Principles

ShelfBuddy follows a subscription-first operating model:

- Each subscription represents a tenant boundary for users, agents, and integrations.
- Messaging flows are event-driven, with channel ingestion, processing, and delivery stages.
- Conversation records are treated as durable operational history, not transient chat state.
- Integrations are isolated by provider domain (for example, Square and Telegram) and follow least-privilege access principles.
- Authentication and onboarding are aligned so users are provisioned into a valid subscription context before operational access.
- Security and privacy are prioritized by design: sensitive credentials stay server-side, and integrations are scoped per subscription.
- Backend code organization is feature-oriented, with shared infrastructure kept explicit and reusable.

## Documentation

For setup and operational HOWTOs, use the docs in `/docs`:

- [Getting Started](./docs/getting-started.md)
- [Setup Webhooks](./docs/setup-webhooks.md)
- [Square App Setup](./docs/square-app-setup.md)
- [Configuration Reference](./docs/configuration-reference.md)
- [Architecture Overview](./docs/architecture-overview.md)
