# Milestone M21: Square Service Consolidation

## Summary
Consolidate Square integration logic in `HeyAlan` and `HeyAlan.WebApi` into one domain service (`ISquareService` / `SquareService`) so outbound Square communication, token lifecycle, and connect/disconnect orchestration are owned by a single implementation.

This milestone explicitly excludes Microsoft Identity Square authorization provider behavior.

## Findings Baseline (Current State Inventory)
- [x] `HeyAlan.WebApi/SquareIntegration/SquareConnectionEndpoints.cs` does not call Square directly; it delegates to `ISubscriptionSquareConnectionService`.
- [x] Outbound Square communication for subscription connection flow currently happens only in:
  - [x] `HeyAlan/SquareIntegration/SquareOAuthClient.cs`
  - [x] `HeyAlan/SquareIntegration/SquareTokenService.cs`
  - [x] `HeyAlan/SquareIntegration/SubscriptionSquareConnectionService.cs` (authorize URL construction)
- [x] Current Square operations observed:
  - [x] OAuth authorize URL construction
  - [x] OAuth code exchange
  - [x] Token status fallback (scope resolution)
  - [x] Token refresh
  - [x] Token revoke
  - [x] Token persistence/encryption/decryption
- [x] Shared scope/base-url logic is duplicated today across Square services and onboarding checks.
- [x] `HeyAlan/Identity/IdentityBuilderExtensions.cs` contains Square OAuth provider code for Microsoft Identity login and is out of scope for this milestone.
- [x] No additional direct Square SDK/API usage found in `./HeyAlan` and `./HeyAlan.WebApi` outside the files above.

## Findings (Post-Refactor)
- [x] Subscription Square integration is now consolidated behind `ISquareService` / `SquareService`.
- [x] Legacy split services (`ISubscriptionSquareConnectionService`, `ISquareOAuthClient`, `ISquareTokenService` and implementations) were removed.
- [x] Shared Square rules are centralized in one place (`SquareIntegrationRules`) and reused by connect + onboarding completion checks.
- [x] Subscription Square callback path is standardized to `/api/subscriptions/square/callback`.
- [x] Identity Square auth callback remains separate and out of scope (`/api/auth/providers/square/callback`).
- [x] WebApp onboarding flow still referenced a removed generated-client symbol (`postOnboardingSubscriptionsBySubscriptionIdSquareConnectAuthorize`) while generated SDK currently exports `postSubscriptionsBySubscriptionIdSquareAuthorize`.
- [x] Runtime symptom observed in onboarding: `(0 , _lib_api__WEBPACK_IMPORTED_MODULE_4__.postOnboardingSubscriptionsBySubscriptionIdSquareConnectAuthorize) is not a function`.
- [x] Onboarding step 1 currently surfaces `Missing subscription context.` when active subscription context is absent or not yet resolved.

## User Decisions (Locked)
- [x] Consolidation mode: **Full Merge**.
- [x] Keep existing WebApi endpoint contracts unchanged.
- [x] Keep existing error code vocabulary unchanged.
- [x] Keep Identity provider Square auth code out of scope.

## Gate A - Single Square Service Contract
- [x] Introduce `ISquareService` as the only Square integration interface used by WebApi handlers.
- [x] Move connect/disconnect orchestration methods under `ISquareService`.
- [x] Move token lifecycle operations (store, resolve, refresh) under `ISquareService`.
- [x] Move outbound Square OAuth operations (exchange, status fallback, revoke, refresh) under `ISquareService`.
- [x] Register only the consolidated service in DI for Square integration behavior.

### Gate A Acceptance Criteria
- [x] `SquareConnectionEndpoints` depends on `ISquareService` only.
- [x] No remaining runtime dependency on `ISquareOAuthClient`, `ISquareTokenService`, or `ISubscriptionSquareConnectionService`.
- [x] One service boundary owns all Square API communication (excluding Identity provider auth flow).

## Gate B - Consolidate Shared Square Rules
- [x] Centralize required Square scopes in one place and reuse in connect + onboarding completion checks.
- [x] Centralize sandbox/production base URL resolution in one place.
- [x] Centralize callback path usage for connect callback handling.
- [x] Remove duplicated helper code for scope parsing/normalization and expiry parsing where possible.

### Gate B Acceptance Criteria
- [x] Scope requirements used for connect completion and onboarding progression come from a single source.
- [x] No duplicated Square base URL construction remains across consolidated flow.

## Gate C - WebApi Integration Refactor
- [x] Refactor `SquareConnectionEndpoints` to call `ISquareService`.
- [ ] Keep all existing endpoint routes unchanged:
  - [x] `POST /subscriptions/{subscriptionId}/square/authorize`
  - [x] `GET /subscriptions/square/callback`
  - [x] `DELETE /subscriptions/{subscriptionId}/square/connection`
- [x] Keep existing DTO request/response payload shapes unchanged.
- [x] Keep existing error-code-to-status-code mapping unchanged.

### Gate C Acceptance Criteria
- [x] OpenAPI-facing shape for Square connection endpoints is unchanged.
- [x] Existing frontend call expectations remain compatible.

## Gate D - Regression and Behavior Tests
- [x] Add unit tests for consolidated service:
  - [x] Start connect URL generation (sandbox/prod) and returnUrl validation.
  - [x] Complete connect: invalid state, denied OAuth, missing code, missing scopes, exchange failure.
  - [x] Token lifecycle: valid token reuse, decrypt failure, refresh success, refresh reconnect-required, refresh failure.
  - [x] Disconnect: missing connection, revoke success, already revoked, revoke failure.
- [x] Add/update endpoint-level tests for `SquareConnectionEndpoints` to verify status and payload mapping are unchanged.
- [x] Keep onboarding recompute behavior validated after connect/disconnect.

### Gate D Acceptance Criteria
- [x] Tests cover all critical success/failure branches above.
- [x] No behavior regressions in Square connect/disconnect API flow.

## Gate E - Cleanup and Housekeeping
- [x] Remove obsolete Square service interfaces/classes after call sites migrate.
- [x] Remove unused named HTTP client registrations if not needed by consolidated service.
- [x] Confirm no logs expose access tokens, refresh tokens, or PII.
- [x] Confirm no changes are made to Identity Square auth provider flow.

### Gate E Acceptance Criteria
- [x] Only consolidated Square service remains for subscription Square integration.
- [x] Security posture is preserved (least privilege, no secret leakage in logs).

## Gate F - Onboarding Square Connect Refactoring
- [x] Refactor onboarding WebApp Square connect action to use the current generated SDK operation (`postSubscriptionsBySubscriptionIdSquareAuthorize`).
- [x] Add a temporary compatibility alias export in non-generated API wrapper (`src/lib/api/index.ts`) to map legacy onboarding function name to the new SDK operation.
- [x] Keep onboarding redirect/query behavior unchanged (`returnUrl=/onboarding`, `squareConnect`, `squareConnectError`).
- [x] Improve step-1 subscription context handling to distinguish loading-context vs missing-context and avoid misleading `Missing subscription context.` during initialization.
- [x] Ensure no `.gen.ts` files are edited manually.

### Gate F Acceptance Criteria
- [x] Onboarding Square connect button no longer throws `...is not a function`.
- [x] Clicking Connect Square triggers `POST /subscriptions/{subscriptionId}/square/authorize` and redirects to returned `authorizeUrl`.
- [x] Existing onboarding callback UX/messages remain compatible.
- [x] Temporary alias keeps legacy imports functional until all callsites are migrated.

## Implementation Sequence (Handoff Order)
- [x] 1) Introduce `ISquareService` contract with existing connect/disconnect method signatures first.
- [ ] 2) Implement `SquareService` by moving logic from:
  - [x] `SubscriptionSquareConnectionService` (workflow/orchestration)
  - [x] `SquareOAuthClient` (exchange/revoke/token-status)
  - [x] `SquareTokenService` (store/resolve/refresh lifecycle)
- [x] 3) Switch DI registrations and endpoint dependency from `ISubscriptionSquareConnectionService` to `ISquareService`.
- [x] 4) Centralize shared constants/helpers (required scopes, base URL resolution, callback path, scope normalization).
- [ ] 5) Remove obsolete interfaces/classes only after all call sites compile and tests pass.
- [ ] 6) Run regression tests for endpoint mapping + service behavior + onboarding recompute interactions.
- [ ] 7) Complete onboarding client refactor + compatibility alias and validate runtime connect flow.

## Notes
- Database schema changes are not planned in this milestone.
- If any schema change is introduced unexpectedly, stop and hand off for migration creation per repository rule.
- Existing endpoint routes and DTO contracts must remain stable throughout this milestone.
