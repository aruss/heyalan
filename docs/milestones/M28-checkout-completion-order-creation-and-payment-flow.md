# Milestone M28: Checkout Completion, Order Creation, and Payment Flow

## Summary
Build the end-to-end commerce completion flow that turns collected conversation state into an order and payment request.

This milestone uses the structured state from M26, the orchestration layer from M27, and the safe Square operations from M25 to validate checkout completeness, create orders, generate payment links, and persist a local order/payment projection the agent can reuse in later turns.

This milestone owns order completion behavior. It does not own long-lived order status sync or proactive post-order notifications.

## Dependencies and Preconditions
- [ ] M25 Square operation skills are available for order creation and payment link generation.
- [ ] M26 structured conversation state is available.
- [ ] M27 runtime orchestration can collect missing checkout fields and invoke approved skills.

## User Decisions (Locked)
- [x] Square is the v1 authoritative commerce backend.
- [x] Checkout completion is driven from structured conversation state, not from ad hoc message parsing at order time.
- [x] Order creation and payment-link creation results must be projected locally for later reuse.
- [x] Payment links may be re-shared without duplicating order creation.
- [x] Order creation must be repeat-safe and honor M25 confirmation/idempotency rules.
- [x] This milestone does not own post-order status webhooks or proactive status-change notifications.

## Public API and Contract Changes
- [ ] Add local projection models:
  - [ ] `ConversationOrderProjection`
  - [ ] `ConversationPaymentProjection`
- [ ] Add internal `ICheckoutOrchestrationService`:
  - [ ] evaluate checkout completeness from conversation state,
  - [ ] request missing-field prompts,
  - [ ] coordinate order create + payment link create,
  - [ ] persist resulting local projections.
- [ ] Add authenticated read endpoints:
  - [ ] `GET /agents/{agentId}/conversations/{conversationId}/order`
  - [ ] `GET /agents/{agentId}/conversations/{conversationId}/payment`
- [ ] Keep state-changing checkout actions internal to the runtime/orchestration path in this milestone.

## Authoritative Checkout Rules
- [ ] Checkout completeness evaluation must consider:
  - [ ] at least one valid line item,
  - [ ] customer identity linkage or sufficient customer creation data,
  - [ ] normalized fulfillment address when shipping is required,
  - [ ] valid zip/agent sellability constraints,
  - [ ] delivery appointment/preferences when the selected fulfillment mode requires them,
  - [ ] any required confirmations before write operations.
- [ ] Order creation flow must:
  - [ ] validate state,
  - [ ] prepare order summary for confirmation,
  - [ ] create Square order,
  - [ ] create or attach payment link,
  - [ ] persist result ids and statuses locally.
- [ ] Retry rules:
  - [ ] repeated confirm with same idempotency key must not duplicate order side effects,
  - [ ] payment link resend path should reuse existing valid link when possible.

## Gate A - Local Order and Payment Projection Model
- [ ] Add persistence models for conversation-scoped order and payment projections.
- [ ] Persist:
  - [ ] local projection ids,
  - [ ] conversation id,
  - [ ] Square order id,
  - [ ] payment link id/url,
  - [ ] current known order/payment status,
  - [ ] created/updated timestamps,
  - [ ] summary values needed for read models.
- [ ] Add indexes and uniqueness rules so one active checkout projection exists per conversation unless future versioning is explicitly introduced.
- [ ] Register EF mappings in `MainDataContext`.
- [ ] Stop and hand off for migration generation/run from `HeyAlan.Initializer` per repo rule.

### Gate A Acceptance Criteria
- [ ] Order and payment results can be persisted and queried per conversation.
- [ ] Schema is compatible with later status-sync updates.

## Gate B - Checkout Completeness Evaluation
- [ ] Implement `ICheckoutOrchestrationService` completeness logic.
- [ ] Define deterministic missing-field output categories:
  - [ ] `missing_items`
  - [ ] `missing_customer_identity`
  - [ ] `missing_shipping_address`
  - [ ] `missing_delivery_preference`
  - [ ] `invalid_product_selection`
  - [ ] `zip_restriction_failed`
- [ ] Normalize item references from conversation state into M23 catalog entities.
- [ ] Fail safely when catalog cache or Square dependency is unavailable.

### Gate B Acceptance Criteria
- [ ] Checkout validation produces machine-consumable missing/invalid field results.
- [ ] Eligibility and completeness rules are deterministic.

## Gate C - Order Creation and Payment Link Coordination
- [ ] Wire orchestrator/runtime to `ICheckoutOrchestrationService`.
- [ ] Use M25 prepare/confirm flow for write operations.
- [ ] Create Square order only after confirmation requirements are met.
- [ ] Create payment link after successful order creation.
- [ ] Persist or update local order/payment projections and corresponding conversation state references.
- [ ] Support resend path:
  - [ ] if a valid payment link already exists for the active order, reuse and reshare it,
  - [ ] otherwise create a new link through approved write flow.

### Gate C Acceptance Criteria
- [ ] Successful checkout creates one order and one usable payment path without duplicate writes.
- [ ] Conversation state and local projections remain consistent after success and failure paths.

## Gate D - Read Models and Operator Visibility
- [ ] Add read-only order/payment endpoints for inbox/admin consumers.
- [ ] Return compact summary DTOs:
  - [ ] order number/id/status,
  - [ ] item summary,
  - [ ] total/currency,
  - [ ] payment link status/url,
  - [ ] last updated time.
- [ ] Apply subscription membership authorization and tenant-safe not-found behavior.

### Gate D Acceptance Criteria
- [ ] Operators can inspect the latest checkout result and payment state per conversation.
- [ ] Read models are suitable for later handoff and order-status workflows.

## Gate E - Testing and Regression Coverage
- [ ] Unit tests:
  - [ ] completeness validation rules,
  - [ ] zip/product eligibility checks,
  - [ ] projection persistence/update behavior,
  - [ ] payment-link reuse behavior.
- [ ] Integration tests:
  - [ ] complete checkout success path,
  - [ ] missing-field follow-up path,
  - [ ] order create failure path,
  - [ ] payment-link create failure path after order create,
  - [ ] retry/idempotency behavior.
- [ ] Regression tests:
  - [ ] M25 prepare/confirm semantics remain intact,
  - [ ] M26 state integrity remains intact,
  - [ ] M27 runtime can continue conversation after checkout failures.

### Gate E Acceptance Criteria
- [ ] Checkout completion and payment-link flows are covered across success, retry, and failure paths.
- [ ] Existing skill safety and state behavior are not regressed.

## Implementation Sequence
- [ ] 1) Gate A: projection schema and migration handoff.
- [ ] 2) Gate B: checkout completeness and eligibility rules.
- [ ] 3) Gate C: order/payment coordination flow.
- [ ] 4) Gate D: read models and operator visibility.
- [ ] 5) Gate E: tests and regression verification.

## Handoff and Operational Notes
- [ ] This milestone requires schema changes; stop after schema edits for migration generation/run from `HeyAlan.Initializer`.
- [ ] If WebAPI contract changes affect generated clients, hand off for `yarn openapi-ts`.
- [ ] This milestone intentionally does not implement proactive order-status notifications.

