# Milestone M34: Inbox Real Data Wiring

## Summary
Wire the existing inbox UI to real conversation and message data from the Web API without changing the current design or UI behavior.

The inbox list and chat panels already consume generated API DTO field names and already apply local presentation formatting for timestamps and message roles. This milestone only covers replacing the current DTO-shaped mock data with live WebAPI queries for:
- the conversation list
- the selected conversation message list

This milestone does not redesign the inbox, does not introduce new inbox actions, and does not change read-state behavior. The right-side `ChatInfoPanel` remains mock-backed until dedicated backend contracts exist for that data. Conversation and message paging are deferred; this milestone loads up to `1000` records per request.

## Dependencies and Preconditions
- [ ] M6 conversation persistence and inbox APIs are available.
- [ ] Generated WebApp client already contains the conversation endpoints and is current.
- [ ] Existing admin session and active-subscription resolution remain the source of truth for the current agent.

## User Decisions (Locked)
- [x] `ConversationListPanel` must use real conversation data.
- [x] `ChatPanel` must use real message data for the selected conversation.
- [x] Existing design and UI behavior must remain unchanged.
- [x] Generated client must be used; no manual fetch wrapper for these endpoints.
- [x] `ChatInfoPanel` stays mock-backed in this milestone.
- [x] Read-state endpoints are not used in this milestone; unread is display-only.
- [x] Conversation loading uses a single `skip: 0, take: 1000` request in this milestone.
- [x] Message loading uses a single `skip: 0, take: 1000` request in this milestone.

## Public API and Contract Changes
- [x] No backend API changes.
- [x] No OpenAPI regeneration required as part of this milestone.
- [x] Inbox list and chat panels already consume generated DTO field names directly:
  - [x] `conversationId`, `participantExternalId`, `lastMessagePreview`, `lastMessageAt`, `hasUnread`
  - [x] `messageId`, `role`, `content`, `occurredAt`
- [x] Local inbox presentation mapping already exists and remains client-side:
  - [x] timestamp formatting for short labels
  - [x] `Customer` / `Agent` / `Operator` message rendering behavior
- [x] Preserve the current search input markup in commented source instead of deleting it.

## Gate A - Agent Resolution and Live Conversation Query
- [x] Resolve the active subscription from the existing session context.
- [x] Resolve the current agent using the same admin convention already used elsewhere:
  - [x] fetch `getAgents` by active subscription,
  - [x] use the first returned agent as the inbox agent.
- [x] Fetch conversations with the generated query helpers for `/agents/{agentId}/conversations` using `skip: 0` and `take: 1000`.
- [x] Feed API conversation items directly into the current conversation list state without introducing a legacy adapter model.
- [x] Preserve existing list behavior:
  - [x] first available conversation becomes active,
  - [x] empty state remains stable when no conversations exist.

### Gate A Acceptance Criteria
- [x] Inbox loads the real conversation list for the active agent.
- [x] The search input is not rendered, but its code is preserved in comments.
- [x] Conversation selection remains stable when the loaded list changes.

## Gate B - Live Selected Conversation Message Query
- [x] Fetch messages for the selected conversation with the generated query helpers for `/agents/{agentId}/conversations/{conversationId}/messages` using `skip: 0` and `take: 1000`.
- [x] Feed API message items directly into the current chat panel state without introducing a legacy sender model.
- [x] Confirm API message ordering against the current UI and only reverse client-side if necessary.
- [x] Preserve the existing chat presentation rules already implemented locally:
  - [x] current channel icon behavior,
  - [x] local timestamp formatting,
  - [x] `Customer` renders as the user-side bubble,
  - [x] `Agent` renders as the AI-side bubble with bot icon,
  - [x] `Operator` renders as the non-user-side bubble without bot icon,
  - [x] local `agentActive` toggle only,
  - [x] existing disabled send-box behavior when AI is active,
  - [x] no message sending implementation in this milestone.
- [x] Keep mobile list/chat/info navigation exactly as it behaves today.

### Gate B Acceptance Criteria
- [x] Selecting a conversation renders its real message history in the current chat UI.
- [x] No visible layout or interaction regression is introduced in desktop or mobile views.
- [x] Switching between conversations does not leak stale message state.

## Gate C - Loading, Error, and State Coordination
- [x] Follow existing admin-page patterns for loading and error handling.
- [x] Handle missing session, missing active subscription, missing agent, and failed conversation/message requests without breaking the page shell.
- [x] Prevent selection and query race conditions when:
  - [x] the active conversation changes quickly,
  - [x] the selected conversation is no longer present in the loaded list.
- [x] Keep unread state display-only:
  - [x] show API unread indicators in the list,
  - [x] do not call mark-message-read,
  - [x] do not call mark-conversation-read.

### Gate C Acceptance Criteria
- [x] The inbox remains stable through loading, empty, and failure states.
- [x] Unread indicators reflect API state without changing existing behavior.

## Gate D - Tests and Regression Coverage
- [ ] Add tests for current inbox presentation behavior:
  - [ ] timestamp formatting
  - [ ] `Customer` / `Agent` / `Operator` message rendering
  - [ ] preserved commented search markup does not become visible
- [ ] Add component or page-level tests for:
  - [ ] rendering a real conversation list,
  - [ ] selecting a conversation updates the chat panel,
  - [ ] message order matches the expected visible chat timeline,
  - [ ] loading, empty, and error states for live list/message queries.
- [ ] Add regression coverage for:
  - [ ] mobile list/chat/info navigation,
  - [ ] local agent toggle behavior,
  - [ ] no read-state mutation calls are issued.

### Gate D Acceptance Criteria
- [ ] Inbox live wiring is covered by tests at the query integration and UI state level.
- [ ] Existing inbox visuals and interactions remain unchanged.

## Implementation Sequence
- [x] 1) Resolve current subscription and current agent for the inbox context.
- [x] 2) Replace conversation mock sourcing with a live conversation query using `take: 1000`.
- [x] 3) Replace selected-conversation message mock sourcing with a live message query using `take: 1000`.
- [x] 4) Finalize loading, empty, error, and selection-state coordination.
- [ ] 5) Add tests and regression verification.

## Handoff and Operational Notes
- [x] DTO-shaped mock data and UI-side presentation mapping are already in place and are not the remaining focus of this milestone.
- [x] This milestone is WebApp-only unless backend contract gaps are discovered.
- [ ] If any WebAPI contract changes become necessary after implementation starts, stop and hand off for `yarn openapi-ts` per repo rule.
- [ ] Read-state mutation is intentionally deferred to a later milestone so the existing UX remains unchanged.
- [ ] Conversation and message paging are intentionally deferred to a later milestone.
