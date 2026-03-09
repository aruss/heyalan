# HeyAlan

HeyAlan is an LLM-driven sales and service agent for merchants. It handles customer conversations across SMS, WhatsApp, and Telegram, answers questions about the company and its products, gathers the information required to place an order, creates payment links, tracks order progress, and supports human takeover when automation should stop.

## Main Goal

The main goal of HeyAlan is to operate the core customer commerce loop end to end:

1. A customer sends a message.
2. The agent understands the intent.
3. The agent answers questions about the business and the products it sells.
4. The agent gathers missing checkout information such as items, shipping address, and delivery preferences.
5. The agent looks up or creates the customer.
6. The agent creates the order.
7. The agent creates and shares a payment link.
8. The agent keeps the customer informed about order, payment, and delivery state.
9. The agent hands the conversation to a human when automation is not the right path.

Everything in this repository should move the product toward that loop. New features must make that loop possible, safer, faster, or easier to operate.

## Core Mechanics

- HeyAlan is multi-tenant. A subscription is the tenant boundary for users, agents, integrations, conversations, customers, and orders.
- A subscription can have multiple agents.
- An agent can sell all subscription products by default or a restricted assigned subset.
- An agent can have enabled skills. Skills are the approved execution surface for external side effects.
- Conversation state is durable. Message history alone is not enough; the system must maintain structured state for checkout, customer identity, fulfillment details, order linkage, payment linkage, and ownership.
- The LLM runtime decides whether to answer directly, ask follow-up questions, call a skill, update state, or request human handoff.
- External writes must go through controlled tool and provider boundaries. The LLM must not bypass those boundaries.
- Human handoff is a first-class behavior. A human must be able to take ownership, edit state, respond as the agent, and return control to automation.
- Square is the v1 commerce backend for catalog, customer, order, and payment operations.

## Current State

The repository already contains core foundations, but the full autonomous commerce loop is not complete yet.

Implemented or in progress:

- Multi-channel messaging foundation for Telegram and SMS/Twilio paths.
- Conversation persistence and inbox-style history.
- Subscription-aware identity, membership, and onboarding flows.
- Agent settings and channel configuration foundations.
- Square connection and token lifecycle management.
- Square catalog synchronization and agent product restriction direction.
- Skills and safe commerce operation planning for Square-backed actions.

Not complete yet:

- Durable conversation state as the main workflow object.
- Full LLM runtime orchestration of customer conversations.
- End-to-end checkout completion from chat state to order creation.
- Local order/payment status projections with proactive customer updates.
- Human handoff and manual state control as a finished operational workflow.

## Roadmap Direction

The roadmap is organized around the core loop, not around isolated integrations.

- Product availability and sellability foundation: synced catalog, agent product assignment, regional restrictions.
- Skill and credential foundation: agent-enabled skills, provider credentials, readiness checks.
- Safe commerce actions: catalog lookup, checkout validation, order creation, payment-link creation, order-status retrieval.
- Structured conversation state: durable checkout, customer, fulfillment, and ownership state.
- LLM orchestration: runtime decision-making across direct answers, follow-up questions, skill calls, and handoff.
- Checkout and payment completion: validate, create order, create or reuse payment link, persist local order/payment projections.
- Order tracking and customer updates: answer status questions and send meaningful proactive updates.
- Human takeover: handoff, manual state correction, operator replies, and return to agent ownership.

## Feature Planning Rules

Use this README as the product-level planning baseline.

- Every new feature must state which part of the core loop it belongs to.
- Every feature must preserve subscription isolation and least-privilege access.
- No feature may introduce direct external side effects outside approved service or skill boundaries.
- Conversation state ownership must stay explicit. Do not scatter checkout state across unrelated services.
- Runtime orchestration, checkout completion, status tracking, and human handoff must have clear ownership. Do not duplicate responsibility across milestones or services.
- New plans must distinguish clearly between current behavior and target behavior.
- New plans must be written in operational terms: inputs, outputs, boundaries, failure handling, and acceptance criteria.
- Features that only add infrastructure are valid only when they clearly unlock the core loop.

## System Boundaries

- `HeyAlan.WebApi` owns authenticated HTTP APIs, webhooks, endpoint contracts, and orchestration entry points.
- `HeyAlan.WebApp` owns admin and operator UI workflows.
- `HeyAlan` owns core domain logic, orchestration, integrations, and runtime behavior.
- `HeyAlan.Data` owns persistence models and EF Core data access foundations.
- `HeyAlan.AppHost` owns local development orchestration with Aspire.
- `HeyAlan.Initializer` owns initialization and migration workflow entry points.
- `HeyAlan.Tests` owns automated verification and regression coverage.

For v1, Square is the external system of record for merchant commerce operations. HeyAlan may keep local projections and workflow state, but provider-facing customer, order, payment, and catalog operations must remain consistent with Square.

## Repository Map

- `HeyAlan.WebApi`: API surface, auth, webhooks, endpoint mapping.
- `HeyAlan.WebApp`: admin application and operator-facing UI.
- `HeyAlan`: core business logic, integrations, orchestration, messaging flow.
- `HeyAlan.Data`: entities, DbContext, persistence configuration.
- `HeyAlan.AppHost`: Aspire host for local development.
- `HeyAlan.Initializer`: initialization and migration-related entry points.
- `HeyAlan.Tests`: backend and integration test coverage.

## Run Locally

From the repository root:

```powershell
dotnet watch run --project .\HeyAlan.AppHost\HeyAlan.AppHost.csproj
```

For setup and environment details, use the supporting docs below.

## Further Docs

- [Getting Started](./docs/getting-started.md)
- [Architecture Overview](./docs/architecture-overview.md)
- [Configuration Reference](./docs/configuration-reference.md)
- [Setup Webhooks](./docs/setup-webhooks.md)
- [Square App Setup](./docs/square-app-setup.md)
