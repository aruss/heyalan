# M4 - Telegram Webhook Registration Service

## Summary
Add a dedicated Telegram application service in `ShelfBuddy.WebApi` that can register a webhook per bot token using `Telegram.Bot`.

This milestone is service-only: no onboarding/admin/agent-save caller integration yet.

## Gate A - Service Contract and Implementation
- [ ] Add `ITelegramService` in `ShelfBuddy.WebApi/Telegram`.
- [ ] Add `TelegramService` implementation with `RegisterWebhookAsync(string botToken, CancellationToken ct = default)`.
- [ ] Validate `botToken` (required, trimmed, non-whitespace).
- [ ] Build webhook URL using `AppOptions.PublicBaseUrl` + `/webhooks/telegram/{botToken}`.
- [ ] Use `TelegramClientFactory.GetClient(botToken)` and call Telegram `setWebhook`.
- [ ] Send configured secret token when registering webhook.
- [ ] Restrict allowed updates to message updates only.
- [ ] Fail fast by bubbling Telegram/API exceptions to caller.

## Gate B - DI and Startup Wiring
- [ ] Keep `AppOptions` sourced from DI singleton registered in `ShelfBuddy.WebApi/Core/CoreBuilderExtensions.cs`.
- [ ] Do not re-register `AppOptions` in Telegram registrations.
- [ ] Register `TelegramOptions`, `TelegramClientFactory`, and `ITelegramService` in Telegram builder extensions.
- [ ] Ensure `builder.AddTelegram()` is called in `ShelfBuddy.WebApi/Program.cs`.

## Gate C - Security and Logging
- [ ] Ensure no logs expose raw Telegram bot tokens.
- [ ] Keep webhook secret-token validation in inbound endpoint filter unchanged.
- [ ] Keep webhook path shape unchanged: `POST /webhooks/telegram/{botToken}`.

## Gate D - Configuration Contracts
- [ ] Continue requiring `TELEGRAM_SECRETTOKEN` for Telegram webhook registration.
- [ ] Reuse `PUBLIC_BASE_URL` from `AppOptions` (already validated by `TryGetAppOptions`).
- [ ] Ensure startup fails fast on invalid/missing required configuration.

## Gate E - Verification
- [ ] Unit test: URL composition from `AppOptions.PublicBaseUrl` (with/without trailing slash).
- [ ] Unit test: `RegisterWebhookAsync` calls `setWebhook` with expected URL, secret token, and allowed updates.
- [ ] Unit test: empty/whitespace `botToken` throws.
- [ ] Unit test: Telegram API failure propagates (fail-fast behavior).
- [ ] Smoke check: existing webhook ingestion endpoint remains functional and mapped.

## Notes
- This milestone intentionally excludes API/UI integration that triggers `RegisterWebhookAsync`.
- A follow-up milestone should wire registration into agent channel configuration flows.