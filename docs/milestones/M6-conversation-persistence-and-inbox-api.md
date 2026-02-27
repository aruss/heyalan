# M6 - Conversation Persistence, Read State, and REST Inbox APIs

## Summary
Add persistent conversation/message storage to the existing M5 ingestion/reply pipeline, then expose inbox APIs for the UI.

Core behavior:
1. Every inbound and outbound message is persisted.
2. Conversations are grouped by `AgentId + IncomingMessage.From + Channel`.
3. `Channel` is stored on `Conversation` and is immutable for that conversation.
4. Unread applies only to inbound (customer) messages.
5. Read-state updates are explicit via dedicated APIs (GET endpoints are side-effect free).
6. APIs are tenant-safe: only users belonging to the agent's subscription can access data.

## Data Model Changes

### New entity: `Conversation`
File target: `ShelfBuddy/Data/Entities/Conversation.cs`

Fields:
- `Guid Id`
- `Guid AgentId` (FK -> `Agent`)
- `Agent Agent`
- `string ParticipantExternalId` (from incoming `From`; exact string, no normalization)
- `MessageChannel Channel`
- `string? LastMessagePreview`
- `DateTimeOffset? LastMessageAt`
- `MessageRole? LastMessageRole`
- `int UnreadCount` (inbound unread count for inbox list performance)
- `DateTime CreatedAt`
- `DateTime UpdatedAt`
- `ICollection<ConversationMessage> Messages`

Constraints/indexes:
- Unique: `(AgentId, ParticipantExternalId, Channel)`
- Index: `(AgentId, LastMessageAt DESC)` for inbox ordering
- Optional index: `(AgentId, UnreadCount)` for unread filtering later

### New entity: `ConversationMessage`
File target: `ShelfBuddy/Data/Entities/ConversationMessage.cs`

Fields:
- `Guid Id`
- `Guid ConversationId` (FK -> `Conversation`)
- `Conversation Conversation`
- `Guid AgentId` (denormalized for efficient scoped queries; FK -> `Agent`)
- `MessageRole Role`
- `string Content`
- `string From`
- `string To`
- `DateTimeOffset OccurredAt`
- `bool IsRead`
- `DateTimeOffset? ReadAt`
- `DateTime CreatedAt`
- `DateTime UpdatedAt`

Constraints/indexes:
- Index: `(ConversationId, OccurredAt DESC, Id DESC)` for message timeline pagination
- Index: `(ConversationId, IsRead, Role)` for unread operations
- Rule: outbound (`Role = Agent`) persisted with `IsRead = true`, `ReadAt = OccurredAt`
- Rule: inbound (`Role = Customer`) persisted with `IsRead = false`, `ReadAt = null`

### DbContext updates
File target: `ShelfBuddy/Data/MainDataContext.cs`
- Add `DbSet<Conversation> Conversations`
- Add `DbSet<ConversationMessage> ConversationMessages`
- Configure relationships and indexes in `OnModelCreating`
- Keep existing naming convention helper so prefixed snake_case naming applies automatically

## Pipeline / Domain Flow Changes

### Incoming persistence
File target: `ShelfBuddy/Consumers/IncomingMessageConsumer.cs`

Before channel-specific branching:
1. Resolve or create conversation by `(AgentId, From, Channel)`.
2. Insert inbound `ConversationMessage` from `IncomingMessage`.
3. Update conversation summary:
- `LastMessageAt = message.ReceivedAt`
- `LastMessagePreview = truncated Content` (e.g. first 160 chars)
- `LastMessageRole = message.Role`
- `UnreadCount += 1` for inbound customer message

Then keep current routing behavior:
- Telegram still publishes `OutgoingTelegramMessage` placeholder reply.

### Outgoing persistence (Telegram)
File target: `ShelfBuddy/Consumers/IncomingMessageConsumer.cs` (`OutgoingTelegramMessageConsumer`)

After successful Telegram send:
1. Resolve conversation by `(AgentId, To, Telegram)` where `To` is recipient external id in outgoing message.
2. If missing, create conversation (defensive for out-of-band publish cases).
3. Insert outbound `ConversationMessage` with:
- `Role = Agent`
- `From = bot id/token-derived safe identifier if available else empty`
- `To = outgoing.To`
- `IsRead = true`
4. Update conversation summary from this outbound message.
5. Do not increment `UnreadCount`.

Note:
- Persist outbound after successful send to avoid showing unsent messages as delivered.
- Retry behavior remains MassTransit-driven; persistence happens on successful processing path.

## REST API Surface (new)

Create endpoint module:
- `ShelfBuddy.WebApi/Core/ConversationEndpoints.cs`
- Map in `Program.cs` via `app.MapConversationEndpoints();`

Route group:
- `/agents/{agentId:guid}/conversations`
- `.RequireAuthorization()`
- `.WithTags("Conversations")`

Authorization rule for every endpoint:
- Get authenticated user id (`ClaimsPrincipalExtensions.GetUserId()`).
- Verify membership via join:
  `Agent.SubscriptionId == SubscriptionUser.SubscriptionId && SubscriptionUser.UserId == currentUserId`
- If agent not found or not accessible: return `404` (tenant-safe non-disclosing behavior).

### 1) List conversations for agent
`GET /agents/{agentId}/conversations?skip=0&take=30`

Input parameters:
- Route/query params: `AgentId`, `Skip`, `Take`

Result DTOs:
- `ConversationListItem`:
  - `ConversationId`
  - `ParticipantExternalId`
  - `Channel`
  - `LastMessagePreview`
  - `LastMessageAt`
  - `LastMessageRole`
  - `UnreadCount`
  - `HasUnread` (derived from `UnreadCount > 0`)
- `GetAgentConversationsResult : CursorList<ConversationListItem>`

Query:
- Ordered by `LastMessageAt DESC`, then `Id DESC`
- Projection via `Select(...)` then `ToCursorListAsync(...)`
- Default `take` clamp (e.g. 1..100)

### 2) List messages for conversation
`GET /agents/{agentId}/conversations/{conversationId}/messages?skip=0&take=50`

Input parameters:
- Route/query params: `AgentId`, `ConversationId`, `Skip`, `Take`

Result DTOs:
- `ConversationMessageItem`:
  - `MessageId`
  - `Role`
  - `Content`
  - `From`
  - `To`
  - `OccurredAt`
  - `IsRead`
  - `ReadAt`
- `GetConversationMessagesResult : CursorList<ConversationMessageItem>`

Query:
- Scope by `AgentId + ConversationId`
- Ordered by `OccurredAt DESC`, then `Id DESC`
- GET remains read-only (no auto mark-read)

### 3) Mark one message as read
`PATCH /agents/{agentId}/conversations/{conversationId}/messages/{messageId}/read`

Input parameters:
- Path params only: `AgentId`, `ConversationId`, `MessageId`

Behavior:
- Only affects inbound unread messages (`Role = Customer`, `IsRead = false`)
- Idempotent: already read returns `200`
- On success:
  - set `IsRead = true`, `ReadAt = now`
  - decrement `Conversation.UnreadCount` safely (`max(0, count-1)`)

Result DTO:
- `MarkConversationMessageReadResult`:
  - `MessageId`
  - `IsRead`
  - `ReadAt`
  - `ConversationUnreadCount`

### 4) Mark all unread inbound messages in conversation as read
`PATCH /agents/{agentId}/conversations/{conversationId}/read`

Input parameters:
- Path params only: `AgentId`, `ConversationId`

Behavior:
- Bulk update all messages in conversation where `Role = Customer && IsRead = false`
- Set each to read with shared `ReadAt = now`
- Set `Conversation.UnreadCount = 0`
- Return number of updated messages

Result DTO:
- `MarkConversationReadResult`:
  - `ConversationId`
  - `MarkedCount`
  - `ConversationUnreadCount` (always `0`)

## Public Interfaces / Contracts Affected

### No change to transport contracts required for this milestone
Current contracts already include required fields to persist:
- `IncomingMessage`: `AgentId`, `Channel`, `Role`, `Content`, `From`, `To`, `ReceivedAt`
- `OutgoingTelegramMessage`: `AgentId`, `Content`, `To`

### Optional (future, not in this milestone)
- Add provider message identifiers (Telegram update id / Twilio SID) for dedup across provider retries.

## Implementation Steps (Decision-Complete)

1. Add entities:
- `Conversation`, `ConversationMessage` in `ShelfBuddy/Data/Entities`.

2. Extend `MainDataContext`:
- Add DbSets and model configuration (FKs, indexes, uniqueness).

3. Add a small persistence service (recommended to keep consumers thin):
- `ShelfBuddy/Core/Conversations/IConversationStore.cs`
- `ShelfBuddy/Core/Conversations/ConversationStore.cs`
- Methods:
  - `UpsertIncomingMessageAsync(IncomingMessage, ct)`
  - `AppendOutgoingTelegramMessageAsync(OutgoingTelegramMessage, occurredAt, ct)`
- Register service in `ShelfBuddy.WebApi/Core/CoreBuilderExtensions.cs`.

4. Update `IncomingMessageConsumer`:
- Inject `IConversationStore`.
- Persist inbound before routing branch.
- Keep Telegram outgoing publish logic unchanged.

5. Update `OutgoingTelegramMessageConsumer`:
- After successful `SendMessageAsync`, call store append for outbound.

6. Add conversation endpoints module + DTO files:
- `ShelfBuddy.WebApi/Core/ConversationEndpoints.cs`
- DTO files in same folder following `*Input` / `*Result` naming.

7. Map endpoint group in `Program.cs`.

8. Tests in `ShelfBuddy.Tests`:
- Add consumer tests for persistence behavior.
- Add endpoint tests for list/read operations and auth scoping.

9. Migration workflow gate:
- After schema changes, stop and hand off for developer migration creation as required by repo policy:
  - `dotnet ef migrations add <Name> --context MainDataContext -o .\Migrations` from `ShelfBuddy.Initializer`
- Resume implementation only after explicit confirmation.

## Test Cases and Scenarios

### Consumer tests
1. Incoming message creates new conversation when absent.
2. Incoming message reuses existing conversation by `(AgentId, From, Channel)`.
3. Incoming messages from same sender on different channels create separate conversations.
4. Incoming inbound message increments unread count.
5. Telegram outgoing successful send appends outbound message and does not increment unread.
6. Conversation summary fields update on every append.
7. Outgoing send failure does not persist outbound message.

### API tests
1. `GET conversations` returns cursor payload with unread metadata ordered by recency.
2. `GET messages` returns timeline items with read flags, scoped by agent+conversation.
3. `PATCH message read` marks one inbound unread message and decrements unread count.
4. `PATCH conversation read` marks all inbound unread messages and sets unread count to zero.
5. Mark-read endpoints are idempotent.
6. Non-member user cannot access another subscription's agent data (`404`).
7. Unknown conversation/message returns `404`.

## Assumptions and Defaults Chosen
- Conversation identity is strictly `AgentId + IncomingMessage.From + Channel`.
- Channel is stored on conversation records only and is immutable per conversation.
- Unread applies only to inbound customer messages.
- GET endpoints never mutate read state.
- Both read APIs are included: single-message and bulk mark-all.
- Cursor pagination uses existing `ToCursorListAsync(skip, take)` utility.
- Tenant privacy favored over explicit authorization failure details (`404` for inaccessible resources).
- Initial scope covers all current channels flowing through `IncomingMessage` (Telegram + SMS/Twilio placeholders) with separate conversations per channel.
