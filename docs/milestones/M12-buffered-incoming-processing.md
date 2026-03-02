# Milestone M12: Buffered Incoming Processing with Cancel/Restart Semantics

## Goal
Convert `ShelfBuddy/Messaging/IncomingMessageConsumer.cs` from per-message immediate handling to durable, per-conversation buffered processing that:
- waits for a configurable quiet window (constant for now, 5 seconds),
- executes business logic on a batch (not individual messages),
- cancels in-flight long-running work when a new message for the same conversation arrives,
- re-waits a full quiet window after cancellation,
- restarts with all pending messages,
- limits each processing run to a max of 100 messages (constant in the same file).

## Scope
- **Primary target:** `ShelfBuddy/Messaging/IncomingMessageConsumer.cs`
- **Behavior scope:** all `IncomingMessage` channels for buffering/long-running logic.
- **Outgoing scope:** channel-specific reply routing remains separate from channel-agnostic buffering.
- **Durability rule:** broker message ack must happen only after successful batch processing.

## Non-Goals (Out of Scope)
- No DB migrations or schema updates.
- No changes to webhook contracts or message DTO shape.
- No `.gen.ts`/`swagger.json` changes.
- No options/config plumbing beyond constants in this pass.
- No package upgrades.

## User Decisions (Locked)
- [x] Buffer by **conversation**, not globally.
- [x] Buffering and long-running logic are **independent of channel**.
- [x] On new message during processing: **cancel current run, then wait a fresh 5-second window**.
- [x] Fake long-running task duration: **~4 seconds**.
- [x] Buffer window: **at least 5 seconds** (constant for now).
- [x] Outgoing behavior for this prototype: **one reply per processed batch**.
- [x] Batch cap: **100 messages**, simple constant in same file.

## Findings from Repository Analysis

### Current consumer behavior and location
- [x] `ShelfBuddy/Messaging/IncomingMessageConsumer.cs` previously processed each message immediately:
  - logged message,
  - persisted one incoming message via `IConversationStore.UpsertIncomingMessageAsync`,
  - for Telegram published one `OutgoingTelegramMessage` with placeholder text.
- [x] Existing behavior was strictly per-message; no buffering/windowing/cancel-restart logic existed.

### Relevant messaging wiring
- [x] MassTransit consumer registration is in:
  - `ShelfBuddy.WebApi/Infrastructure/MassTransitBuilderExtensions.cs`
- [x] `IncomingMessageConsumer` and `OutgoingTelegramMessageConsumer` are auto-configured endpoints via `cfg.ConfigureEndpoints(context)`.

### Channel ingress shape
- [x] Telegram webhook publishes `IncomingMessage` with resolved `AgentId`, `SubscriptionId`, `Channel=Telegram`.
  - `ShelfBuddy.WebApi/TelegramIntegration/TelegramWebhookEndpoints.cs`
- [x] Twilio webhook publishes placeholder-routed `IncomingMessage` for SMS.
  - `ShelfBuddy.WebApi/TwilioIntegration/TwilioWebhookEndpoints.cs`

### Critical lifetime/architecture constraint
- [x] MassTransit consumers are scoped; storing in-memory buffer in consumer instance fields is not safe for cross-message state.
- [x] Buffer coordination must be done with shared/static state (or dedicated singleton service) if kept in-process.

### Documentation findings (MassTransit via Context7)
- [x] Batch options (`SetMessageLimit`, `SetTimeLimit`, `GroupBy`) exist for `IConsumer<Batch<T>>`.
- [x] Consume cancellation should use context cancellation token semantics.
- [x] Custom buffering in current consumer is used to satisfy explicit cancel/restart semantics requested.

### Test coverage gap
- [x] No existing tests target `IncomingMessageConsumer` behavior.
- [ ] New tests are needed for timing, cancellation, restart, and ack semantics.

## Implementation Plan by Gate

## Gate A: Per-Conversation Buffer Coordinator in Consumer
- [x] Introduce conversation key in `IncomingMessageConsumer` (`AgentId + Channel + From`).
- [x] Add shared in-process state keyed by conversation for pending messages and coordinator status.
- [x] Ensure one coordinator loop runs per conversation at a time.
- [x] Keep processing isolated across different conversations.

### Gate A Acceptance Criteria
- [x] Multiple messages for same conversation share the same pending queue.
- [x] Different conversation keys do not block each other.

## Gate B: Quiet Window + Fake Long-Running Task
- [x] Add constants in same file:
  - [x] `BufferQuietWindow = 5s`
  - [x] `FakeBusinessLogicDuration = 4s`
  - [x] `MaxBufferedMessagesPerBatch = 100`
- [x] Implement quiet-window timer reset on each newly arrived message.
- [x] After quiet window elapses, run fake long task (`Task.Delay(4s, token)`).

### Gate B Acceptance Criteria
- [x] No batch processing starts before a full 5-second quiet period.
- [x] Fake long task runs for approximately 4 seconds when not canceled.

## Gate C: Cancel In-Flight and Re-Wait Full Window
- [x] On new message arrival during in-flight processing for same conversation:
  - [x] cancel current processing token,
  - [x] keep all uncompleted messages pending,
  - [x] restart wait phase requiring full 5 seconds.
- [x] Ensure the message that triggered cancellation is included in next run.

### Gate C Acceptance Criteria
- [x] New message during long task aborts current run.
- [x] Next successful run includes previous pending + triggering message.

## Gate D: Durable Ack Semantics and Batch Completion
- [x] Keep each `Consume` call pending until its message is truly processed in a successful batch.
- [x] On successful batch:
  - [x] persist incoming messages (deterministic FIFO),
  - [x] complete waiting consume tasks.
- [x] On failure:
  - [x] fault consume tasks to allow broker retry/redelivery semantics.

### Gate D Acceptance Criteria
- [x] No early consume completion before batch success.
- [x] Failure paths propagate appropriately for retry.

## Gate E: Batch Cap Enforcement (100)
- [x] Enforce cap using constant in same file.
- [x] Process max 100 pending messages per run; remainder stays queued for next cycle.
- [x] Document overflow behavior in logs.

### Gate E Acceptance Criteria
- [x] Any single run processes at most 100 messages.
- [x] Messages beyond 100 are not dropped and are processed in subsequent cycle(s).

## Gate F: Outgoing Reply Routing per Batch
- [x] Replace per-message Telegram response with one response per processed batch.
- [x] Keep buffering/long-running logic channel-agnostic.
- [x] Keep channel-specific outgoing dispatch separated by channel routing.
- [x] For non-implemented channels, log and continue without failing batch persistence.

### Gate F Acceptance Criteria
- [x] Telegram sends exactly one placeholder outgoing message per successful batch.
- [x] Non-Telegram channels do not break batch processing.

## Gate G: Tests and Verification
- [ ] Add tests for:
  - [ ] quiet-window batching,
  - [ ] cancel/restart behavior,
  - [ ] inclusion of triggering message after cancel,
  - [ ] max-100-per-run behavior,
  - [ ] durable ack timing (consume completion after success),
  - [ ] single Telegram outgoing per batch.
- [ ] Run targeted tests for messaging behavior.

### Gate G Acceptance Criteria
- [ ] Automated tests cover success/cancel/restart/cap paths.
- [ ] No regressions in existing messaging-related tests.

## Risks and Notes
- In-process buffer is volatile across process restarts; durability here means "do not ack before successful processing," not cross-crash state persistence.
- Timing-sensitive tests can be flaky; use controlled synchronization primitives where possible.
- Per-conversation key uses `From` as participant identity; this must remain consistent across ingress sources.

## Explicit Assumptions and Defaults
- Conversation key is `(AgentId, Channel, From)`.
- Batch cap is hard-coded to 100 in `IncomingMessageConsumer.cs`.
- Buffer window and fake task duration are constants in same file.
- Buffering applies to all channels; outgoing dispatch decides channel-specific behavior.
- One outgoing reply per successful batch (Telegram implemented first).
