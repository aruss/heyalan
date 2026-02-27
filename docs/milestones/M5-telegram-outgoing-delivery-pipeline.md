# M5 - Telegram Outgoing Delivery Pipeline

## Summary
Implement end-to-end Telegram message delivery across webhook ingestion, message contracts, queue routing, and outbound dispatch.

Incoming Telegram webhooks must resolve the owning `Agent` by bot token, enrich `IncomingMessage` with `AgentId` and `SubscribtionId`, emit a placeholder business reply, and dispatch through `OutgoingTelegramMessageConsumer` using `ITelegramService`.

## Gate A - Message Contract and Telegram Service API
- [x] Extend `IncomingMessage` with required `Guid AgentId`.
- [x] Define `OutgoingTelegramMessage` with required fields: `SubscribtionId`, `AgentId`, `Content`, `To`.
- [x] Add `SendMessageAsync(string botToken, long chatId, string text, CancellationToken ct = default)` to `ITelegramService`.
- [x] Implement `SendMessageAsync` in `TelegramService` using `TelegramClientFactory.GetClient(botToken)` and Telegram.Bot send API.

## Gate B - Telegram Webhook Ingestion Enrichment
- [x] Update `SquareBuddy.WebApi/TelegramIntegration/TelegramWebhookEndpoints.cs` to inject `MainDataContext`.
- [x] Resolve `Agent` by exact `TelegramBotToken == botToken`.
- [x] Return `NotFound` when no matching agent exists.
- [x] Publish `IncomingMessage` with populated `SubscribtionId` and `AgentId` from resolved agent.
- [x] Preserve current behavior for non-text updates (`Ok` without publish).

## Gate C - Incoming Routing and Placeholder Business Reply
- [x] In `IncomingMessageConsumer`, keep inbound logging and include `AgentId` in logs.
- [x] Add deterministic placeholder reply text for current mock business logic.
- [x] For `MessageChannel.Telegram`, publish `OutgoingTelegramMessage` (not `IncomingMessage`).
- [x] Set outgoing recipient to incoming Telegram sender id (`message.From`).

## Gate D - Outgoing Telegram Consumer Delivery
- [x] Complete `OutgoingTelegramMessageConsumer` consume logic.
- [x] Inject and use `MainDataContext` to load `Agent` by `AgentId`.
- [x] Validate agent existence and non-empty `TelegramBotToken`; fault when invalid.
- [x] Parse outgoing `To` into `long chatId`; fault on parse errors.
- [x] Send message via `ITelegramService.SendMessageAsync` using consume cancellation token.

## Gate E - MassTransit Topology and Registrations
- [x] Register `OutgoingTelegramMessageConsumer` in `SquareBuddy.WebApi/Infrastructure/MassTransitBuilderExtensions.cs`.
- [x] Register `OutgoingTelegramMessageConsumer` in `SquareBuddy.Initializer/Program.cs` topology deployment section.
- [x] Keep endpoint auto-configuration via `cfg.ConfigureEndpoints(context)`.

## Gate F - Channel Compatibility and Current Placeholders
- [x] Update `TwilioWebhookEndpoints` to set required `AgentId` placeholder (same temporary style as `SubscribtionId`).
- [x] Keep current Twilio behavior otherwise unchanged.

## Gate G - Verification
- [ ] Test: Telegram webhook publishes enriched `IncomingMessage` with resolved `AgentId` and `SubscribtionId`.
- [ ] Test: Telegram webhook returns `404` for unknown bot token.
- [ ] Test: Telegram webhook non-text updates return `200` without publish.
- [ ] Test: `IncomingMessageConsumer` emits one `OutgoingTelegramMessage` with fixed placeholder content for Telegram channel.
- [ ] Test: `OutgoingTelegramMessageConsumer` resolves agent token and calls `ITelegramService.SendMessageAsync`.
- [ ] Test: missing agent/token and invalid chat id paths fault as expected.
- [x] Test: `TelegramService.SendMessageAsync` sends expected payload and propagates Telegram API failures.

## Assumptions and Defaults
- `SubscribtionId` spelling remains unchanged in this milestone for compatibility.
- `AgentId` is required and non-null on `IncomingMessage`.
- Outgoing bus messages do not carry Telegram bot token secrets.
- Unknown Telegram bot token returns HTTP `404`.
- Placeholder business response is a single fixed deterministic string.
