# Milestone M29: Order Status Tracking and Proactive Customer Updates

## Summary
Build the post-checkout order tracking layer so HeyAlan can answer status questions and proactively inform customers when meaningful order or delivery changes happen.

This milestone introduces local synchronization and projection of order, payment, and fulfillment state from Square, plus the outbound notification policy that decides when to send customer updates through the existing messaging pipeline.

This milestone does not own checkout completion or human takeover behavior.

## Dependencies and Preconditions
- [ ] M28 local order and payment projections are available.
- [ ] Existing channel delivery pipeline is available for outbound customer messaging.
- [ ] Square connection and credential flows remain operational.

## User Decisions (Locked)
- [x] Order/payment/fulfillment state should be projected locally for fast status answers.
- [x] Customers can ask for status at any time; the agent must answer from current known state without rebuilding checkout context.
- [x] Proactive notifications should be sent only on meaningful state changes.
- [x] Duplicate external events must not produce duplicate customer messages.
- [x] Square remains the v1 source of truth for external order state.

## Public API and Contract Changes
- [ ] Add internal `IOrderStatusSyncService`:
  - [ ] refresh one order projection from Square,
  - [ ] process provider-triggered status updates,
  - [ ] persist normalized order/payment/fulfillment states.
- [ ] Add internal `ICustomerNotificationPolicy`:
  - [ ] decide whether a state transition should notify customer,
  - [ ] build notification intent payload for outbound delivery.
- [ ] Add read-only endpoints:
  - [ ] `GET /agents/{agentId}/conversations/{conversationId}/order-status`
  - [ ] `GET /agents/{agentId}/conversations/{conversationId}/notifications`
- [ ] Add normalized local enums or canonical status mapping models for:
  - [ ] order status,
  - [ ] payment status,
  - [ ] fulfillment/delivery status.

## Authoritative Status Responsibilities
- [ ] Status sync must track:
  - [ ] order lifecycle,
  - [ ] payment lifecycle,
  - [ ] fulfillment/delivery lifecycle,
  - [ ] last synced timestamp,
  - [ ] last customer-notified transition.
- [ ] Proactive notifications must:
  - [ ] trigger only for meaningful transitions,
  - [ ] be idempotent,
  - [ ] use existing channel identity for the conversation,
  - [ ] record notification outcome for audit/debugging.
- [ ] Customer status answers must:
  - [ ] use local projection first,
  - [ ] optionally refresh under controlled policy when stale.

## Gate A - Status Projection Model and Mapping
- [ ] Extend local order/payment projections with normalized status fields and sync metadata.
- [ ] Add notification audit model for customer status messages:
  - [ ] conversation id,
  - [ ] order projection id,
  - [ ] transition key,
  - [ ] channel,
  - [ ] sent outcome,
  - [ ] timestamps.
- [ ] Register EF mappings in `MainDataContext`.
- [ ] Stop and hand off for migration generation/run from `HeyAlan.Initializer` per repo rule.

### Gate A Acceptance Criteria
- [ ] Local projections can represent current status plus notification history.
- [ ] Schema supports idempotent transition notification tracking.

## Gate B - Status Sync Service
- [ ] Implement `IOrderStatusSyncService`.
- [ ] Define sync triggers:
  - [ ] on-demand status refresh for direct customer inquiries,
  - [ ] provider webhook/event-driven updates where available,
  - [ ] background refresh for tracked active orders if needed.
- [ ] Normalize Square provider status data into canonical local states.
- [ ] Persist sync timestamps and transition metadata.

### Gate B Acceptance Criteria
- [ ] Local order status can be refreshed and normalized deterministically.
- [ ] Repeated identical provider updates do not create duplicate local transitions.

## Gate C - Customer Notification Policy and Delivery
- [ ] Implement `ICustomerNotificationPolicy`.
- [ ] Define meaningful transition set for v1 proactive updates.
- [ ] Build customer-facing notification intents from normalized status changes.
- [ ] Route outbound notifications through the existing messaging delivery pipeline.
- [ ] Record sent/skipped decisions with deterministic reasons.

### Gate C Acceptance Criteria
- [ ] Meaningful status changes generate at most one customer notification per transition.
- [ ] Skipped transitions are explainable through persisted audit data.

## Gate D - Runtime Status Answer Integration
- [ ] Integrate order-status reads into agent runtime context and reply flow.
- [ ] Support direct customer status question path:
  - [ ] load local projection,
  - [ ] refresh if policy says stale,
  - [ ] answer with current normalized status summary.
- [ ] Ensure the runtime can answer status without rebuilding the original checkout flow.

### Gate D Acceptance Criteria
- [ ] Customer can ask for order status at any time and receive a coherent response.
- [ ] Runtime status answers remain fast and deterministic.

## Gate E - Testing and Regression Coverage
- [ ] Unit tests:
  - [ ] provider-to-local status mapping,
  - [ ] stale/refresh policy behavior,
  - [ ] notification decision rules,
  - [ ] duplicate transition suppression.
- [ ] Integration tests:
  - [ ] on-demand status refresh path,
  - [ ] proactive notification on meaningful transition,
  - [ ] duplicate provider event path,
  - [ ] status answer flow through runtime.
- [ ] Regression tests:
  - [ ] M28 checkout projections remain intact,
  - [ ] existing outbound delivery pipeline behavior remains intact,
  - [ ] no duplicate customer messages on repeated sync events.

### Gate E Acceptance Criteria
- [ ] Status tracking and proactive notification paths are covered across refresh, event, and duplicate scenarios.
- [ ] Existing checkout and message delivery features are not regressed.

## Implementation Sequence
- [ ] 1) Gate A: projection extension and migration handoff.
- [ ] 2) Gate B: status sync and normalization.
- [ ] 3) Gate C: customer notification policy and outbound delivery.
- [ ] 4) Gate D: runtime status-answer integration.
- [ ] 5) Gate E: tests and regression verification.

## Handoff and Operational Notes
- [ ] This milestone requires schema changes; stop after schema edits for migration generation/run from `HeyAlan.Initializer`.
- [ ] If WebAPI contract changes affect generated clients, hand off for `yarn openapi-ts`.
- [ ] This milestone intentionally does not implement manual operator takeover.

