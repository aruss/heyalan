# Milestone M30: Human Handoff and Manual State Control

## Summary
Build the operator takeover workflow so a human can assume control of a conversation, correct structured state, reply as the agent, and hand control back without losing continuity.

This milestone introduces explicit conversation ownership, handoff audit history, operator state editing, and human-authored message sending through the same customer-facing identity the agent uses.

This milestone is the operational control layer on top of the core agent loop. It does not redefine the underlying LLM orchestration, checkout, or order status models.

## Dependencies and Preconditions
- [ ] M26 conversation state and ownership fields are available.
- [ ] M27 runtime orchestration respects conversation ownership.
- [ ] Existing inbox/conversation APIs and outbound messaging pipeline are available.
- [ ] M28 and M29 read models are available for operator visibility during takeover.

## User Decisions (Locked)
- [x] A conversation always has one active owner: `agent` or `human`.
- [x] When human-owned, the agent must not auto-respond.
- [x] Human operators may manually edit structured conversation state.
- [x] Human operators may send messages that impersonate the agent identity.
- [x] Ownership changes and manual state edits must be auditable.
- [x] Returning control to the agent should preserve all manual corrections.

## Public API and Contract Changes
- [ ] Add authenticated handoff endpoints:
  - [ ] `POST /agents/{agentId}/conversations/{conversationId}/handoff`
  - [ ] `POST /agents/{agentId}/conversations/{conversationId}/resume`
- [ ] Add authenticated state-edit endpoint:
  - [ ] `PATCH /agents/{agentId}/conversations/{conversationId}/state`
- [ ] Add authenticated operator-send endpoint:
  - [ ] `POST /agents/{agentId}/conversations/{conversationId}/messages`
- [ ] Add authenticated audit/history endpoint:
  - [ ] `GET /agents/{agentId}/conversations/{conversationId}/handoff-history`
- [ ] Add internal `IConversationOwnershipService`:
  - [ ] transition ownership,
  - [ ] validate allowed actions by current owner,
  - [ ] persist audit history.

## Authoritative Ownership Rules
- [ ] Ownership transitions:
  - [ ] `agent -> human` requires handoff reason,
  - [ ] `human -> agent` records resume reason and timestamp,
  - [ ] repeated transition to same owner is idempotent.
- [ ] Human-owned conversation rules:
  - [ ] automated reply generation is suppressed,
  - [ ] manual operator messages are allowed,
  - [ ] manual state edits are allowed.
- [ ] Agent-owned conversation rules:
  - [ ] normal runtime orchestration resumes,
  - [ ] prior human edits remain authoritative unless later changed.

## Gate A - Ownership and Audit Persistence
- [ ] Add `ConversationHandoffRecord` persistence model.
- [ ] Persist:
  - [ ] conversation id,
  - [ ] previous owner,
  - [ ] new owner,
  - [ ] reason,
  - [ ] actor user id,
  - [ ] created timestamp.
- [ ] Extend state/audit support as needed for operator edits and manual sends.
- [ ] Register EF mappings in `MainDataContext`.
- [ ] Stop and hand off for migration generation/run from `HeyAlan.Initializer` per repo rule.

### Gate A Acceptance Criteria
- [ ] Ownership transitions and operator interventions are durably auditable.
- [ ] Schema supports complete takeover/resume history per conversation.

## Gate B - Ownership Service and Runtime Enforcement
- [ ] Implement `IConversationOwnershipService`.
- [ ] Add tenant-safe ownership transition methods.
- [ ] Integrate ownership checks into the runtime/orchestrator path so human-owned conversations suppress automatic replies.
- [ ] Add deterministic error codes:
  - [ ] `conversation_handoff_invalid`
  - [ ] `conversation_resume_invalid`
  - [ ] `conversation_human_owned`
  - [ ] `conversation_agent_owned`

### Gate B Acceptance Criteria
- [ ] Ownership transitions are safe, auditable, and enforced by runtime behavior.
- [ ] Human-owned conversations cannot receive automatic agent replies.

## Gate C - Manual State Editing
- [ ] Add operator state-edit endpoint with typed editable fields:
  - [ ] customer identity fields,
  - [ ] shipping/delivery fields,
  - [ ] candidate order items,
  - [ ] workflow flags,
  - [ ] handoff-related notes.
- [ ] Route edits through `IConversationStateService` so audit metadata remains consistent.
- [ ] Validate operator edits and reject unsafe cross-tenant or invalid provider-link changes.

### Gate C Acceptance Criteria
- [ ] Operators can correct structured state without bypassing validation or audit.
- [ ] Manual edits remain visible to later agent runtime turns.

## Gate D - Operator Message Sending
- [ ] Add operator-send endpoint for manual replies.
- [ ] Reuse existing outbound delivery pipeline and conversation persistence behavior.
- [ ] Ensure manual sends appear in conversation history as agent-authored customer-facing messages while preserving internal audit of human actor.
- [ ] Prevent duplicate auto-send around manual intervention timing.

### Gate D Acceptance Criteria
- [ ] Human operators can reply as the agent through existing channels.
- [ ] Manual replies are persisted consistently and do not trigger duplicate automated responses.

## Gate E - Inbox/Admin Visibility
- [ ] Add handoff history and current owner to conversation read models.
- [ ] Expose latest structured state snapshot, order/payment snapshot, and notification snapshot for operator context during takeover.
- [ ] Keep authorization consistent with existing subscription membership rules.

### Gate E Acceptance Criteria
- [ ] Operators can see whether a conversation is human-owned and why.
- [ ] Operators have sufficient state visibility to resume or hand back conversations safely.

## Gate F - Testing and Regression Coverage
- [ ] Unit tests:
  - [ ] ownership transition rules,
  - [ ] runtime suppression when human-owned,
  - [ ] manual state edit validation,
  - [ ] manual send audit behavior.
- [ ] Integration tests:
  - [ ] handoff from agent to human,
  - [ ] manual state correction while human-owned,
  - [ ] operator reply path,
  - [ ] resume back to agent and subsequent automated reply path.
- [ ] Regression tests:
  - [ ] M27 runtime remains active for agent-owned conversations,
  - [ ] M28/M29 projections remain visible and unchanged by ownership transfer,
  - [ ] no duplicate messages are emitted during handoff/resume transitions.

### Gate F Acceptance Criteria
- [ ] Human takeover, manual correction, reply, and resume flows are covered end to end.
- [ ] Existing automated behaviors remain intact outside human-owned conversations.

## Implementation Sequence
- [ ] 1) Gate A: ownership/audit schema and migration handoff.
- [ ] 2) Gate B: ownership service and runtime enforcement.
- [ ] 3) Gate C: manual state editing.
- [ ] 4) Gate D: operator message sending.
- [ ] 5) Gate E: visibility/read models.
- [ ] 6) Gate F: tests and regression verification.

## Handoff and Operational Notes
- [ ] This milestone requires schema changes; stop after schema edits for migration generation/run from `HeyAlan.Initializer`.
- [ ] If WebAPI contract changes affect generated clients, hand off for `yarn openapi-ts`.
- [ ] This milestone should keep audit records secret-safe and avoid logging sensitive customer data beyond what operators already manage.

