# Milestone M34: Inbox Real Data Wiring

## Summary
Fill the existing inbox UI with real conversation data from the Web API without changing the current design or UI behavior.

This milestone wires the `ConversationListPanel` and `ChatPanel` to the existing conversation endpoints through the generated web client. It keeps the current layout, mobile transitions, search interaction, chat header actions, and disabled send-box behavior intact.

This milestone does not redesign the inbox, does not introduce new inbox actions, and does not change read-state behavior. The right-side `ChatInfoPanel` remains mock-backed until dedicated backend contracts exist for that data.

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

## Public API and Contract Changes
- [ ] No backend API changes.
- [ ] No OpenAPI regeneration required as part of this milestone.
- [ ] Add WebApp-only adapter logic to map generated DTOs into existing inbox UI props:
  - [ ] conversation list item mapping,
  - [ ] message item mapping,
  - [ ] timestamp formatting for existing short labels,
  - [ ] fallback chat-info model for conversations without mock details.

## Gate A - Agent Resolution and Conversation List Query
- [ ] Resolve the active subscription from the existing session context.
- [ ] Resolve the current agent using the same admin convention already used elsewhere:
  - [ ] fetch `getAgents` by active subscription,
  - [ ] use the first returned agent as the inbox agent.
- [ ] Fetch conversations with the generated query helpers for `/agents/{agentId}/conversations`.
- [ ] Continue loading additional conversation pages until exhausted so client-side search works across the full loaded set.
- [ ] Merge paginated conversation pages deterministically by `conversationId` without duplicates.
- [ ] Preserve existing list behavior:
  - [ ] local search filters the loaded list,
  - [ ] first available filtered conversation becomes active,
  - [ ] empty state remains stable when no conversations exist.

### Gate A Acceptance Criteria
- [ ] Inbox loads the real conversation list for the active agent.
- [ ] Search behavior remains client-side and unchanged from the user’s perspective.
- [ ] Conversation selection remains stable when the filter changes.

## Gate B - Selected Conversation Message Query
- [ ] Fetch messages for the selected conversation with the generated query helpers for `/agents/{agentId}/conversations/{conversationId}/messages`.
- [ ] Map API message roles into the existing chat sender model used by `ChatPanel`.
- [ ] Reorder API results from newest-first to oldest-first before rendering so the visible chat timeline matches the current UI.
- [ ] Preserve the existing chat header and footer behavior:
  - [ ] current channel icon behavior,
  - [ ] local `agentActive` toggle only,
  - [ ] existing disabled send-box behavior when AI is active,
  - [ ] no message sending implementation in this milestone.
- [ ] Keep mobile list/chat/info navigation exactly as it behaves today.

### Gate B Acceptance Criteria
- [ ] Selecting a conversation renders its real message history in the current chat UI.
- [ ] No visible layout or interaction regression is introduced in desktop or mobile views.
- [ ] Switching between conversations does not leak stale message state.

## Gate C - Chat Info Compatibility and Fallbacks
- [ ] Keep `ChatInfoPanel` connected to the current mock dataset for now.
- [ ] Continue using mock chat info when a real conversation id matches existing mock entries.
- [ ] Introduce a stable empty fallback chat-info object for real conversations with no mock match.
- [ ] Do not change the panel’s structure, headings, buttons, or visual treatment.

### Gate C Acceptance Criteria
- [ ] The right-side panel continues to render without blocking the real inbox integration.
- [ ] Real conversations without mock details do not crash the page or alter the layout.

## Gate D - Loading, Error, and State Coordination
- [ ] Follow existing admin-page patterns for loading and error handling.
- [ ] Handle missing session, missing active subscription, missing agent, and failed conversation/message requests without breaking the page shell.
- [ ] Prevent selection and query race conditions when:
  - [ ] the active conversation changes quickly,
  - [ ] search removes the currently active conversation,
  - [ ] paginated loads finish out of order.
- [ ] Keep unread state display-only:
  - [ ] show API unread indicators in the list,
  - [ ] do not call mark-message-read,
  - [ ] do not call mark-conversation-read.

### Gate D Acceptance Criteria
- [ ] The inbox remains stable through loading, empty, and failure states.
- [ ] Unread indicators reflect API state without changing existing behavior.

## Gate E - Tests and Regression Coverage
- [ ] Add tests for DTO-to-UI mapping:
  - [ ] conversation mapping,
  - [ ] message mapping,
  - [ ] timestamp formatting,
  - [ ] fallback chat-info generation.
- [ ] Add component or page-level tests for:
  - [ ] rendering a real conversation list,
  - [ ] selecting a conversation updates the chat panel,
  - [ ] message order is oldest-to-newest in the rendered chat,
  - [ ] client-side search filters loaded conversations,
  - [ ] fallback behavior when no chat-info mock entry exists.
- [ ] Add regression coverage for:
  - [ ] mobile list/chat/info navigation,
  - [ ] local agent toggle behavior,
  - [ ] no read-state mutation calls are issued.

### Gate E Acceptance Criteria
- [ ] Inbox wiring is covered by tests at the adapter and UI integration level.
- [ ] Existing inbox visuals and interactions remain unchanged.

## Implementation Sequence
- [ ] 1) Gate A: resolve current agent and load the full conversation list.
- [ ] 2) Gate B: load selected-conversation messages and map them into the existing chat UI.
- [ ] 3) Gate C: keep the chat info panel compatible with real conversations through fallback data.
- [ ] 4) Gate D: finalize loading/error coordination and preserve passive unread behavior.
- [ ] 5) Gate E: add tests and regression verification.

## Handoff and Operational Notes
- [ ] This milestone is WebApp-only unless backend contract gaps are discovered.
- [ ] If any WebAPI contract changes become necessary after implementation starts, stop and hand off for `yarn openapi-ts` per repo rule.
- [ ] Read-state mutation is intentionally deferred to a later milestone so the existing UX remains unchanged.
