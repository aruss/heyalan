# Milestone M26: Conversation State and Customer Identity Core

## Summary
Build the durable workflow state layer that turns message history into actionable agent state.

This milestone extends existing conversation persistence so each conversation has a structured state object the agent runtime can read and update across turns. It also introduces customer identity resolution so the agent can look up existing customers, create missing customers, and reuse prior order history during future interactions.

This milestone does not implement LLM orchestration, automated order creation, or human handoff behavior. It provides the state and customer foundations those later milestones depend on.

## Dependencies and Preconditions
- [ ] M6 conversation persistence and inbox APIs are available.
- [ ] M23 subscription catalog cache is available for product references in state.
- [ ] M25 Square operation skills are not required for initial state persistence, but customer/order projections should remain compatible with them.

## User Decisions (Locked)
- [x] `ConversationState` is a durable first-class aggregate, not an ephemeral prompt artifact.
- [x] State is scoped to one conversation and tenant-safe through `Conversation -> Agent -> Subscription`.
- [x] Customer lookup happens before customer creation whenever sufficient identifiers exist.
- [x] Square is the v1 system of record for customer identity and order history.
- [x] State stores normalized workflow facts, not raw prompt transcripts.
- [x] Message history remains the canonical transcript; `ConversationState` stores structured derived facts and workflow progress.
- [x] Human handoff ownership fields may exist in state now, but automated handoff behavior is out of scope for this milestone.

## Public API and Contract Changes
- [ ] Add internal `IConversationStateService`:
  - [ ] Load state by conversation id.
  - [ ] Initialize state when conversation is created.
  - [ ] Apply typed state mutations with audit metadata.
- [ ] Add internal `ICustomerResolutionService`:
  - [ ] Resolve existing Square customer from known identifiers.
  - [ ] Create Square customer when resolution fails and minimum fields are available.
  - [ ] Load recent order history summary for resolved customer.
- [ ] Add conversation-state read models for inbox/admin usage:
  - [ ] current checkout stage,
  - [ ] known customer snapshot,
  - [ ] known shipping/delivery facts,
  - [ ] candidate order items,
  - [ ] payment/order linkage fields,
  - [ ] ownership/handoff flags.
- [ ] Add authenticated conversation-state endpoints:
  - [ ] `GET /agents/{agentId}/conversations/{conversationId}/state`
  - [ ] `GET /agents/{agentId}/conversations/{conversationId}/customer-history`
- [ ] Keep all secrets and provider tokens out of DTOs and logs.

## Authoritative State Shape
- [ ] `ConversationState` must include:
  - [ ] identity block:
    - [ ] `SquareCustomerId`
    - [ ] customer name snapshot
    - [ ] phone/email snapshot where known
    - [ ] confidence / source metadata for resolved identity
  - [ ] checkout block:
    - [ ] requested items
    - [ ] normalized item references to cached catalog products where possible
    - [ ] quantity notes
    - [ ] special instructions
  - [ ] fulfillment block:
    - [ ] normalized shipping address
    - [ ] zip code
    - [ ] delivery appointment/preferences
  - [ ] order block:
    - [ ] local order projection id
    - [ ] Square order id
    - [ ] payment link id/url
    - [ ] current known order/payment status snapshot
  - [ ] workflow block:
    - [ ] `CurrentStage`
    - [ ] `MissingFields`
    - [ ] `LastAgentIntent`
    - [ ] `NeedsHumanAttention`
    - [ ] `CurrentOwner` (`agent|human`)
  - [ ] audit block:
    - [ ] created/updated timestamps
    - [ ] last mutation source (`system|agent_runtime|human_operator`)

## Gate A - Persistence Model and State Aggregate
- [ ] Add `ConversationState` persistence model linked 1:1 to `Conversation`.
- [ ] Add owned/related persistence structures needed for:
  - [ ] customer snapshot,
  - [ ] fulfillment details,
  - [ ] candidate order items,
  - [ ] workflow metadata,
  - [ ] provider linkage ids.
- [ ] Add indexes and constraints:
  - [ ] unique one state row per conversation,
  - [ ] tenant-safe query indexes through `ConversationId` and `AgentId`,
  - [ ] no cross-subscription joins by query shape.
- [ ] Register EF mappings in `MainDataContext`.
- [ ] Stop and hand off for migration generation/run from `HeyAlan.Initializer` per repo rule.

### Gate A Acceptance Criteria
- [ ] Every persisted conversation can have exactly one structured state.
- [ ] State can store customer, checkout, fulfillment, workflow, and provider linkage data without schema ambiguity.
- [ ] Schema is tenant-safe and migration-ready.

## Gate B - State Service and Mutation Rules
- [ ] Implement `IConversationStateService`.
- [ ] Add initialize-on-demand behavior when conversation state is first requested or first mutated.
- [ ] Define typed mutation methods for:
  - [ ] customer identity updates,
  - [ ] shipping/delivery updates,
  - [ ] candidate line item replace/update,
  - [ ] workflow stage and missing-field updates,
  - [ ] provider id/status linkage updates,
  - [ ] ownership flag updates.
- [ ] Enforce deterministic merge behavior:
  - [ ] normalized values replace prior normalized values,
  - [ ] unknown fields are preserved,
  - [ ] null/empty input does not accidentally clear required prior data unless explicitly requested by trusted caller.
- [ ] Persist mutation audit metadata without storing raw prompts or secrets.

### Gate B Acceptance Criteria
- [ ] State initialization and updates are deterministic.
- [ ] Concurrent updates do not corrupt workflow facts.
- [ ] Audit metadata exists for each state mutation.

## Gate C - Customer Resolution and History Projection
- [ ] Implement `ICustomerResolutionService`.
- [ ] Define v1 customer lookup strategy:
  - [ ] prefer explicit linked `SquareCustomerId`,
  - [ ] otherwise try phone,
  - [ ] otherwise try email,
  - [ ] otherwise fall back to create when minimum required fields are available.
- [ ] Persist customer linkage back into `ConversationState`.
- [ ] Build a compact customer-history projection:
  - [ ] recent order count,
  - [ ] recent order summaries/statuses,
  - [ ] last known fulfillment address when available.
- [ ] Keep provider reads tenant-scoped and secret-safe.

### Gate C Acceptance Criteria
- [ ] Existing customers are reused when resolvable.
- [ ] Missing customers can be created through a controlled service boundary.
- [ ] Conversation state can expose a customer-history snapshot suitable for agent runtime context.

## Gate D - Inbox/Admin Read APIs
- [ ] Add read-only endpoints for conversation state and customer history.
- [ ] Enforce subscription membership checks consistently with existing conversation APIs.
- [ ] Return stable DTOs for:
  - [ ] conversation workflow state,
  - [ ] customer identity snapshot,
  - [ ] order/payment linkage ids and statuses,
  - [ ] customer history summary.
- [ ] Keep error surfaces deterministic:
  - [ ] `conversation_not_found`
  - [ ] `conversation_state_not_found`
  - [ ] `customer_history_unavailable`
  - [ ] `subscription_member_required`

### Gate D Acceptance Criteria
- [ ] Authorized operators can inspect structured state without reading raw provider payloads.
- [ ] DTOs are stable enough for later inbox/handoff UI work.

## Gate E - Testing and Regression Coverage
- [ ] Unit tests:
  - [ ] state initialization,
  - [ ] mutation merge behavior,
  - [ ] workflow missing-field updates,
  - [ ] customer resolution decision order,
  - [ ] customer creation path and linkage persistence.
- [ ] Integration tests:
  - [ ] conversation -> state creation flow,
  - [ ] existing customer resolution by linked id / phone / email,
  - [ ] customer history projection for resolved customer,
  - [ ] state endpoints auth and tenant scoping.
- [ ] Regression tests:
  - [ ] M6 conversation history behavior remains unchanged,
  - [ ] M23 catalog behavior remains unaffected,
  - [ ] no sensitive data leaks to logs or API payloads.

### Gate E Acceptance Criteria
- [ ] Structured conversation state and customer resolution flows are covered by tests.
- [ ] Existing conversation and catalog features are not regressed.

## Implementation Sequence
- [ ] 1) Gate A: schema and migration handoff.
- [ ] 2) Gate B: state aggregate service and mutation rules.
- [ ] 3) Gate C: customer resolution and history projection.
- [ ] 4) Gate D: read APIs for inbox/admin consumers.
- [ ] 5) Gate E: tests and regression verification.

## Handoff and Operational Notes
- [ ] This milestone requires schema changes; stop after schema edits for migration generation/run from `HeyAlan.Initializer`.
- [ ] If WebAPI contract changes affect generated clients, hand off for `yarn openapi-ts`.
- [ ] This milestone intentionally does not implement LLM prompt orchestration or automated human handoff behavior.

