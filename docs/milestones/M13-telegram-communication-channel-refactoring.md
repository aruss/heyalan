# Milestone M13: Telegram Communication Channel Refactoring

## Goal
Refactor the Telegram communication path so multiple bots/subscriptions work reliably by enforcing unique bot tokens, making webhook registration mission-critical during onboarding, surfacing actionable onboarding errors, and routing outgoing replies to Telegram `chat.id` instead of sender user id.

## Scope
- **Backend (`HeyAlan.WebApi`, `HeyAlan`)**
  - Enforce uniqueness for `Agent.TelegramBotToken`.
  - Register Telegram webhook during onboarding channels step and fail onboarding if registration fails.
  - Add retry policy for transient webhook registration failures.
  - Improve Telegram ingress diagnostics (`401/404/publish success`).
  - Use Telegram `message.chat.id` as outgoing reply target.
- **Frontend (`HeyAlan.WebApp`)**
  - Reuse existing onboarding channels error rendering to display Telegram-specific actionable messages.

## Non-Goals (Out of Scope)
- New onboarding UI flows beyond current channels-step message area.
- Introducing background jobs/workers for deferred webhook registration.
- Supporting duplicate token ownership or automatic token reassignment.
- Changes to Twilio/WhatsApp behavior in this milestone.

## User Decisions (Locked)
- [x] Telegram bot tokens MUST be unique.
- [x] Duplicate token policy: reject with clear error.
- [x] Webhook registration is mission critical in onboarding channels step.
- [x] Retry webhook registration using Polly-style bounded retry.
- [x] Outgoing Telegram recipient should use chat id (`chat.id`) instead of user id.

## Findings from Repository Analysis

### Confirmed functional gap: token save without webhook registration
- [x] Onboarding channels update persists `agent.TelegramBotToken` but does not call `ITelegramService.RegisterWebhookAsync`.
  - `HeyAlan/Onboarding/SubscriptionOnboardingService.cs`
- [x] Existing webhook registration callsite exists only in commented initializer seed block.
  - `HeyAlan.Initializer/Program.cs`

### Webhook ingress strictness explains empty queues
- [x] Ingress endpoint requires matching secret token header and returns `401` when invalid.
  - `HeyAlan.WebApi/TelegramIntegration/TelegramSecretTokenFilter.cs`
- [x] Ingress endpoint resolves agent by exact bot token and returns `404` when no match.
  - `HeyAlan.WebApi/TelegramIntegration/TelegramWebhookEndpoints.cs`
- [x] Bot token lookup is exact string match; stored token vs webhook route token mismatch (including subtle formatting differences) can produce `404` and prevent queue publish.
- [x] If ingress fails before publish, RabbitMQ incoming/outgoing queues remain empty.

### Data-model risk: duplicate tokens not prevented
- [x] `Agent.TelegramBotToken` currently has non-unique filtered index.
  - `HeyAlan/Data/MainDataContext.cs`
- [x] Webhook lookup uses `SingleOrDefaultAsync`; duplicates can cause runtime exceptions.
  - `HeyAlan.WebApi/TelegramIntegration/TelegramWebhookEndpoints.cs`

### Routing gap: outgoing recipient source
- [x] Telegram ingress currently maps `IncomingMessage.From` from `message.from.id` (sender user id).
- [x] Outgoing consumer expects `OutgoingTelegramMessage.To` to be parseable chat id (`long`).
  - `HeyAlan/Messaging/OutgoingTelegramMessageConsumer.cs`
- [x] Correct Telegram delivery target should be `message.chat.id`.

### Onboarding UX readiness
- [x] Onboarding channels step already shows backend-provided error text (`resolveApiErrorMessage` + step message state).
  - `HeyAlan.WebApp/src/app/onboarding/page.tsx`

## Architecture Decisions (Locked)
- [x] Telegram webhook registration executes synchronously in onboarding channels update flow.
- [x] Onboarding channels call returns success only if Telegram webhook registration succeeds.
- [x] Retry strategy is bounded and fast (Polly-style exponential backoff).
- [x] Token uniqueness is global across all agents/subscriptions.
- [x] Telegram outgoing target source for inbound conversations is `chat.id`.

## Implementation Plan by Gate

## Gate A: Enforce Unique Telegram Bot Tokens
- [x] Update EF model index for `Agent.TelegramBotToken` to filtered unique index (`IS NOT NULL`).
- [x] Handle unique-constraint violations in onboarding channels flow and map to deterministic error code.
- [x] Add onboarding error code/message for duplicate token (e.g., `telegram_bot_token_already_in_use`).

### Gate A Acceptance Criteria
- [x] Duplicate non-null Telegram token cannot be persisted.
- [x] API returns deterministic onboarding error payload for duplicate token (not generic 500).

## Gate B: Mission-Critical Webhook Registration in Onboarding Channels Step
- [x] Introduce app-layer orchestration for channels update + webhook registration.
- [x] When Telegram token is present: persist/validate token, register webhook, and only then return success.
- [x] If webhook registration fails after retries, return onboarding failure response with actionable message.
- [x] Preserve existing membership/authorization checks.

### Gate B Acceptance Criteria
- [x] Channel step returns success only when webhook registration is confirmed.
- [x] Failures are surfaced immediately to onboarding caller.

## Gate C: Polly-Based Retry for Webhook Registration
- [x] Add bounded retry policy (recommended baseline: 3 attempts, exponential backoff).
- [x] Retry transient failures (network/timeout/5xx-style request failures).
- [x] Avoid excessive retries for non-transient Telegram API errors (e.g., invalid token cases).
- [x] Log each retry attempt with contextual metadata (agent/subscription, attempt count).

### Gate C Acceptance Criteria
- [x] Transient registration failures retry automatically.
- [x] Final failure returns clear onboarding error.

## Gate D: Use `chat.id` for Outgoing Telegram Messages
- [x] Extend webhook input DTO to include Telegram `message.chat.id`.
- [x] Map inbound `IncomingMessage.From` (Telegram channel) to `chat.id` string.
- [x] Keep outgoing message pipeline unchanged structurally, but now fed with chat id-compatible `To`.

### Gate D Acceptance Criteria
- [x] Telegram replies route to `chat.id` and send successfully.
- [x] No regressions in existing outbound consumer parsing (`long.TryParse`).

## Gate E: Error Contracts + Onboarding UX
- [x] Add Telegram-specific onboarding error codes/messages:
  - [x] duplicate token
  - [x] webhook registration failed
  - [x] optional invalid token-specific mapping
- [x] Ensure channels step shows backend message directly via existing UI message rendering.
- [x] Ensure backend messages include actionable guidance (e.g., verify bot token/BotFather config).

### Gate E Acceptance Criteria
- [x] User sees immediate actionable error in onboarding channels step when registration fails.
- [x] No silent partial success where token is stored but webhook inactive.

## Gate F: Diagnostics for Queue-Empty Incidents
- [x] Add structured logs for Telegram ingress outcomes (`401`, `404`, publish success).
- [x] Add structured logs for webhook registration start/success/retry/failure.
- [x] Include RabbitMQ `_error` queue inspection in triage guidance (main queues may be empty while faults accumulate).
- [x] Document quick triage checklist in milestone notes.

### Gate F Acceptance Criteria
- [x] Operators can distinguish ingress rejection from queue/consumer issues using logs.

## Test Cases and Scenarios
- [x] Channels update with valid unique token registers webhook and returns success.
- [x] Channels update with duplicate token returns `telegram_bot_token_already_in_use`.
- [x] Channels update with invalid/misconfigured token returns clear actionable failure.
- [x] Transient webhook registration failures retry and eventually succeed/fail deterministically.
- [x] Telegram inbound with `chat.id` results in outgoing send to same chat id.
- [x] Ingress `401`/`404` paths are observable and do not publish messages.
- [ ] Two distinct subscriptions/tokens both ingest and reply correctly.

## Risks & Notes
- Database schema change required for unique token enforcement.
- Per repo rules: after schema change implementation, pause for developer-managed migration creation/execution before continuing dependent work.
- Existing generated API client files are out of scope for manual edits.

## Quick Triage Checklist
- Confirm incoming webhook auth failures: check logs for `Rejected Telegram webhook request with invalid secret token` (`401` path).
- Confirm bot token route mapping failures: check logs for `bot token not found in database` (`404` path).
- Confirm successful ingress publish: check logs for `Published Telegram incoming message`.
- Confirm onboarding webhook setup: check logs for registration start/success/retry/failure by subscription and agent IDs.
- Inspect RabbitMQ `<queue>_error` queues when primary incoming/outgoing queues appear empty; faults often accumulate there.

## Assumptions and Defaults
- Retry baseline: 3 attempts with exponential backoff (e.g., 1s, 2s, 4s).
- Token uniqueness scope is global across all agents.
- Telegram `IncomingMessage.From` for routing semantics will represent `chat.id` after refactor.
- Onboarding UI keeps current rendering model and relies on backend-provided message text.
