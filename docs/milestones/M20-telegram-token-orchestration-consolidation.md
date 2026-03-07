# Milestone M20: Telegram Token Orchestration Consolidation

## Summary
Consolidate duplicated Telegram token-change orchestration currently split across `SubscriptionAgentService` and `SubscriptionOnboardingService` into a single shared method in `ITelegramService`/`TelegramService`.

Goal: keep domain services focused on business flow while centralizing Telegram webhook registration trigger + Telegram exception-to-error-code mapping in one place.

## User Decisions (Locked)
- [x] Consolidate steps 1/2/3 only (token-changed check, webhook registration call, Telegram error mapping).
- [x] Keep service-specific rollback/reaction logic in each domain service.
- [x] Keep behavior and existing error codes unchanged.

## Gate A - Shared Telegram Orchestration API
- [x] Add `RegisterWebhookIfTokenChangedAsync(previousBotToken, nextBotToken, ct)` to `ITelegramService`.
- [x] Add a small typed result (`WasAttempted`, `ErrorCode`) under `HeyAlan.TelegramIntegration`.
- [x] Implement the method in `TelegramService` with centralized token comparison and exception mapping.

### Gate A Acceptance Criteria
- [x] Unchanged/empty target token returns success without webhook call.
- [x] Changed token attempts webhook registration once through existing retry path.
- [x] `ApiRequestException` 401 maps to `telegram_bot_token_invalid`.
- [x] Other failures map to `telegram_webhook_registration_failed`.

## Gate B - Refactor Domain Services to Use Shared Method
- [x] Refactor `SubscriptionAgentService` to call shared orchestration method.
- [x] Refactor `SubscriptionOnboardingService` to call shared orchestration method.
- [x] Remove duplicated Telegram exception mapping helpers from both services.
- [x] Preserve existing rollback behavior and existing service result contracts.

### Gate B Acceptance Criteria
- [x] No Telegram-specific try/catch mapping duplication remains in domain services.
- [x] Agent and onboarding flows still roll back persisted channel changes on webhook failure.
- [x] Returned error codes remain exactly the same as before.

## Gate C - Test Coverage and Regression
- [x] Add/adjust `TelegramServiceTests` for new orchestration method:
  - [x] unchanged token
  - [x] changed token success
  - [x] changed token invalid (401)
  - [x] changed token generic failure
- [x] Update test stubs implementing `ITelegramService` where interface changed.
- [ ] Run focused test suite for Telegram + Agent + Onboarding behavior.

### Gate C Acceptance Criteria
- [ ] Existing rollback and error-mapping tests for agent/onboarding continue to pass.
- [x] New orchestration method behavior is covered by unit tests.

## Notes
- Inbound webhook endpoint handling in `HeyAlan.WebApi` stays unchanged for this milestone.
- Outgoing message consumer behavior stays unchanged for this milestone.
