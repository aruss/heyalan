# Milestone M8: Square OAuth Auth + Subscription-Scoped Connection

## Goal
Align Square external authentication with the working Google `/auth` flow, establish a subscription-scoped Square OAuth connection that supports server-to-server Square API access without an active user session, and deliver end-to-end onboarding (backend + web UI) for first login.

The onboarding outcome for M8 is:
- a valid `SubscriptionSquareConnection` for the active subscription, and
- a configured primary `Agent` (name, `Personality`, `TwilioPhoneNumber`, `TelegramBotToken`, `WhatsappNumber`).

## Scope
- **Backend (`ShelfBuddy.WebApi`)**
  - Keep external auth endpoint surface under `/auth/*`.
  - Support Square external login parity with Google flow.
  - Persist and use Square OAuth tokens for backend/subscription operations.
  - Implement onboarding step endpoints for big onboarding (first login) and small onboarding (future additional agent flow).
- **Shared (`ShelfBuddy`)**
  - Add `SquareIntegration` module with `ISquareTokenService` and implementation.
  - Centralize Square token lifecycle (get valid token, refresh, rotate, typed failures).
- **Data (`ShelfBuddy.Data`)**
  - Introduce subscription-scoped Square connection persistence (`SubscriptionSquareConnection`).
  - Introduce persisted onboarding state at subscription scope.
  - Extend `Agent` with onboarding/profile fields, including `Personality`.
- **Frontend (`ShelfBuddy.WebApp`)**
  - Implement `/onboarding` as a backend-driven multi-step flow for big onboarding.
  - Integrate each step with onboarding APIs (`/onboarding/*`) and Square connect callback handling.
  - Keep team step present as a dummy/placeholder step in M8 (no invitation side effects).
  - Use `react-hook-form` with `zod` validation (via `@hookform/resolvers/zod`) for onboarding forms.
  - Support resumable onboarding by rehydrating from `GET /onboarding/subscriptions/{subscriptionId}/state`.

## Non-Goals (Out of Scope)
- Multi-Square-account per agent support.
- Many-to-many agent-to-Square-account relationship.
- Admin settings UI implementation for connect/disconnect management (onboarding UI is in scope; admin settings UI is not).
- Functional invitation/member-sync logic (including Square dashboard member discovery/listing).
- Replacing existing Google auth behavior.

## Architecture Decisions (Locked)
- [x] Boundary for Square server token storage is **subscription-scoped**.
- [x] Persistence model is a **dedicated table** (`SubscriptionSquareConnection`) rather than `AspNetUserTokens`.
- [x] Cardinality for this milestone is **1 subscription -> 1 Square account**.
- [x] Scope strategy is **two-step consent**:
  - [x] Login step requests minimal scope (`MERCHANT_PROFILE_READ`) for identity.
  - [x] Onboarding/connect step requests full operational scopes.
- [x] Onboarding is **two-layer**:
  - [x] Big onboarding on first login: connect Square at subscription level, configure first agent, then finalize onboarding.
  - [x] Small onboarding for future additional-agent creation: agent profile + channels setup only (not implemented in M8).
- [x] Each onboarding step must have its own backend endpoint.
- [x] Development diagnostics can log full claims/profile payloads as explicitly requested.

## Square SDK Usage Policy (Authoritative)
- [x] Official `Square` SDK (`Square` `43.0.0`) is the default integration path for all server-to-server Square API calls in M8.
- [x] Any new Square API interaction added in Gates C-H MUST use official SDK clients first.
- [x] Direct `HttpClient` calls to Square endpoints MAY be used only if SDK support is missing/insufficient for a concrete use case.
- [x] Every direct-HTTP exception MUST be documented in the Exception Registry below with reason, owner, and review date.
- [x] API contracts and deterministic error codes MUST remain stable even when implementation moves from HTTP to SDK.

### Exception Registry
| Area | Exception | Reason | Owner | Review Date | Status |
|---|---|---|---|---|---|
| Gate A external login flow (`/auth/*`) | ASP.NET Core OAuth middleware is used instead of Square SDK | Browser redirect + external cookie handshake is framework-native and stable; SDK is used for server-to-server flows | Platform Team | 2026-06-01 | Approved |

## Square Scope Set (Locked)

### Login scope (identity-only)
- `MERCHANT_PROFILE_READ`

### Onboarding connect full operational scopes
- `ITEMS_READ` (read products/catalog)
- `CUSTOMERS_READ` (read customer information)
- `CUSTOMERS_WRITE` (update customer information)
- `ORDERS_READ` (read order state and shipping/fulfillment details)
- `ORDERS_WRITE` (create orders)
- `PAYMENTS_WRITE` (required for payment links with order operations)

## Configuration Contract

### Existing keys used
- `AUTH_GOOGLE_CLIENT_ID`
- `AUTH_GOOGLE_CLIENT_SECRET`
- `AUTH_SQUARE_CLIENT_ID`
- `AUTH_SQUARE_CLIENT_SECRET`
- `SQUARE_CLIENT_ID`
- `SQUARE_CLIENT_SECRET`

### Validation rules
- [x] `AUTH_GOOGLE_CLIENT_ID` and `AUTH_GOOGLE_CLIENT_SECRET` must both be set or both be missing.
- [x] `AUTH_SQUARE_CLIENT_ID` and `AUTH_SQUARE_CLIENT_SECRET` must both be set or both be missing.
- [x] `SQUARE_CLIENT_ID` and `SQUARE_CLIENT_SECRET` must both be set or both be missing.
- [x] Auth-provider keys (`AUTH_*`) and Square-connection keys (`SQUARE_*`) are independent.

## HTTP Surface

### External auth endpoints (existing shape, Square parity)
- `GET /auth/providers`
- `GET /auth/providers/{provider}/authorize`
- `GET /auth/external-callback`

Square-specific behavior:
- [x] Provider name is `square`.
- [x] Callback path is `/auth/providers/square/callback`.
- [x] Callback completion returns through `/auth/external-callback`.

### Onboarding connect endpoint(s)
- [x] Add/extend onboarding route to start full-scope Square authorization for a subscription.
- [x] Persist/replace `SubscriptionSquareConnection` on successful authorization.
- [x] Return deterministic error codes for connect failures.

### Onboarding step endpoints (backend)
- [x] `GET /onboarding/subscriptions/{subscriptionId}/state` returns big onboarding state for resumable first-login onboarding.
- [x] `POST /onboarding/subscriptions/{subscriptionId}/square/connect/authorize` starts Square full-scope connect flow.
- [x] `POST /onboarding/subscriptions/{subscriptionId}/agents` creates the first draft agent used in big onboarding.
- [x] `PATCH /onboarding/agents/{agentId}/profile` updates name and `Personality`.
- [x] `PATCH /onboarding/agents/{agentId}/channels` updates the three channel fields on `Agent`.
- [x] `POST /onboarding/subscriptions/{subscriptionId}/members/invitations` exists as placeholder endpoint and returns deterministic "not implemented in M8" semantics.
- [x] `POST /onboarding/subscriptions/{subscriptionId}/finalize` validates required steps and marks subscription onboarding complete.

### Admin settings connection management API (no UI work in this milestone)
- [x] Add backend endpoint(s) used by Admin Settings to create/connect a `SubscriptionSquareConnection`.
- [x] Add backend endpoint(s) used by Admin Settings to remove/disconnect a `SubscriptionSquareConnection`.
- [x] Enforce authorization so only permitted subscription members can connect/disconnect for their subscription.

## Data Model

### New entity: `SubscriptionSquareConnection`
- [x] `SubscriptionId` (FK -> `Subscription`, unique)
- [x] `SquareMerchantId`
- [x] `EncryptedAccessToken`
- [x] `EncryptedRefreshToken`
- [x] `AccessTokenExpiresAtUtc`
- [x] `Scopes`
- [x] `ConnectedByUserId` (FK -> `ApplicationUser`)
- [x] `CreatedAt`
- [x] `UpdatedAt`
- [x] Optional lifecycle fields as needed (`DisconnectedAtUtc`, status flags)

### New entity: `SubscriptionOnboardingState`
- [x] `SubscriptionId` (FK -> `Subscription`, unique)
- [x] `Status` (`Draft`, `InProgress`, `Completed`)
- [x] `CurrentStep` (big onboarding step pointer)
- [x] `PrimaryAgentId` (nullable FK -> `Agent`)
- [x] `StartedAt`
- [x] `UpdatedAt`
- [x] `CompletedAt` (nullable)

## Onboarding State Machine (Authoritative)

### Canonical steps
- `square_connect`
- `profile`
- `channels`
- `invitations`
- `finalize`

### Canonical status
- `Draft`: onboarding record exists but first actionable step not completed.
- `InProgress`: at least one step completed and onboarding not finalized.
- `Completed`: finalize step completed successfully.

### Step completion rules (server-side source of truth)
- `square_connect` is complete when:
  - `SubscriptionSquareConnection` exists for `SubscriptionId`, and
  - connection has non-empty token material and required full scopes for onboarding (`ITEMS_READ`, `CUSTOMERS_READ`, `CUSTOMERS_WRITE`, `ORDERS_READ`, `ORDERS_WRITE`, `PAYMENTS_WRITE`).
- `profile` is complete when:
  - `PrimaryAgentId` is set, and
  - referenced `Agent` has non-empty `Name`, and
  - referenced `Agent` has non-empty `Personality`.
- `channels` is complete when:
  - referenced `Agent` exists, and
  - channel fields for this milestone are persisted on `Agent` (`TwilioPhoneNumber`, `TelegramBotToken`, `WhatsappNumber`) according to endpoint validation rules.
- `invitations` is complete in M8 when:
  - invitations placeholder endpoint is called successfully (no invitation side effects required).
- `finalize` is complete when:
  - `square_connect`, `profile`, and `channels` are complete, and
  - `SubscriptionOnboardingState.Status` is set to `Completed`, and
  - `CompletedAt` is set.

### Transition rules
- Allowed forward transitions only:
  - `square_connect -> profile -> channels -> invitations -> finalize`
- Backward navigation from UI is allowed, but `CurrentStep` returned by API is the earliest incomplete required step.
- Any required-step data invalidation (for example connection removed, profile cleared, channel fields invalidated) moves `Status` to `InProgress` and recomputes `CurrentStep` to earliest incomplete required step.
- `finalize` must fail with deterministic validation errors if required steps are incomplete.

### State recomputation policy
- Backend recomputes step completion from persisted data on:
  - `GET /onboarding/subscriptions/{subscriptionId}/state`
  - every step mutation endpoint
  - finalize endpoint
- Persisted `CurrentStep` is advisory cache only; recomputed result is authoritative.

### Endpoint response contract for state
- `GET /onboarding/subscriptions/{subscriptionId}/state` returns:
  - `status`
  - `currentStep`
  - `steps[]` with per-step status (`not_started | in_progress | completed | blocked`)
  - `primaryAgentId` (nullable)
  - `canFinalize` (boolean)
- Step mutation endpoints return updated onboarding state payload using the same contract.

### First-login and resume behavior
- On authenticated entry, backend checks subscription onboarding state.
- If `Status != Completed`, caller is treated as onboarding-required and routed to `currentStep`.
- If `Status == Completed`, caller is treated as onboarded and normal app routing applies.

### Claim synchronization rule
- Existing `onboarded` claim remains in use for authorization/routing.
- Claim value is derived from persisted onboarding state:
  - `Completed` -> `onboarded=true`
  - otherwise -> `onboarded=false` (or absent)
- Claim must be refreshed on login and after finalize.

### Index/constraints
- [x] Unique index on `SubscriptionId` for 1:1 square connection cardinality.
- [x] Index on `SquareMerchantId`.
- [x] Unique index on `SubscriptionOnboardingState.SubscriptionId`.

### `Agent` extensions
- [x] Add `Personality` field.
- [ ] Add/confirm the three channel fields used by onboarding channels step:
  - [x] `TwilioPhoneNumber` (existing)
  - [x] `TelegramBotToken` (existing)
  - [x] `WhatsappNumber` (new)

## Service Design

### New module
- [x] Create `ShelfBuddy/SquareIntegration` folder.
- [x] Add `ISquareTokenService` + implementation in `ShelfBuddy` project.

### `ISquareTokenService` responsibilities
- [x] Store tokens from successful Square connect/login callback.
- [x] Return valid access token for a given `SubscriptionId`.
- [x] Refresh access token when expired or near expiry.
- [x] Rotate refresh token when Square returns a new one.
- [x] Return typed failures for revoked/invalid/reauthorize-required states.

### Security requirements
- [x] Do not expose tokens in API responses.
- [x] Do not place tokens in redirect query strings.
- [x] Encrypt token values at rest.
- [ ] Keep least-privilege scope model through two-step consent.

## Gate A: Square Auth Foundation (Config + Provider Parity)
- [x] Add Square pair validation in `AppOptionsValidator`.
- [x] Ensure Square provider registration is conditional and consistent with Google pair behavior.
- [x] Fix Square callback path to `/auth/providers/square/callback`.
- [x] Keep `/auth/providers/{provider}/authorize` + `/auth/external-callback` flow unchanged and functional for Square login with minimal scope (`MERCHANT_PROFILE_READ`).
- [x] Gate A explicitly uses framework OAuth middleware as a documented SDK-policy exception (see Exception Registry).
- [x] Split auth-provider config from connection config:
  - [x] Identity providers read only `AUTH_*` keys.
  - [x] Subscription Square connection services read only `SQUARE_*` keys.
  - [x] No fallback to legacy key names.

## Gate B: Subscription-Scoped Persistence Foundation
- [x] Add `SubscriptionSquareConnection` entity and EF configuration.
- [x] Add `SubscriptionOnboardingState` entity and EF configuration.
- [x] Add `DbSet<SubscriptionSquareConnection>` and `DbSet<SubscriptionOnboardingState>` in `MainDataContext`.
- [x] Add constraints/indexes and audit behavior.
- [x] Extend `Agent` with `Personality` and channel fields (`TwilioPhoneNumber`, `TelegramBotToken`, `WhatsappNumber`).
- [x] Update initializer assumptions/documentation for new schema object.
- [ ] Stop and hand off for migration creation/run from `ShelfBuddy.Initializer` per repository rule.

## Gate C: Shared Square Token Lifecycle Service
- [x] Add `ShelfBuddy/SquareIntegration/ISquareTokenService.cs`.
- [x] Add implementation and supporting models/errors.
- [x] Implement token storage/read/update against `SubscriptionSquareConnection`.
- [x] Add Square OAuth token refresh client logic (`/oauth2/token`).
- [x] Implement deterministic typed outcomes for refresh failure/reconnect-required states.
- [x] Register services in DI from WebApi composition root.
- [x] Use official Square SDK OAuth client for refresh (`ObtainTokenAsync`, `grant_type=refresh_token`) while preserving existing typed outcomes.

## Gate D: Square Connection Management APIs (Backend)
- [x] Add backend endpoint(s) used by Admin Settings to create/connect a `SubscriptionSquareConnection` (UI excluded).
- [x] Add backend endpoint(s) used by Admin Settings to remove/disconnect a `SubscriptionSquareConnection` (UI excluded).
- [x] Add big onboarding connect flow requesting this exact full-scope set: `ITEMS_READ`, `CUSTOMERS_READ`, `CUSTOMERS_WRITE`, `ORDERS_READ`, `ORDERS_WRITE`, `PAYMENTS_WRITE`.
- [x] On successful connect, persist/replace `SubscriptionSquareConnection` through `ISquareTokenService`.
- [x] Enforce subscription membership/authorization for connect/disconnect actions.
- [x] Return deterministic error codes for connect/disconnect failures.
- [x] Use official Square SDK for OAuth code exchange/revoke and merchant probe operations in connection management flows.
- [x] Map OAuth callback deny/error outcomes (including `access_denied`) to deterministic `squareConnectError` codes.

## Gate E: Runtime Token Consumption
- [x] Backend Square API callers resolve tokens via `ISquareTokenService.GetValidAccessTokenAsync(subscriptionId)`.
- [x] Automatic refresh on expiry works end-to-end in live caller path.
- [x] Revoked/invalid refresh states produce deterministic reconnect-required behavior.
- [x] Avoid duplicate concurrent refresh races (single-flight or transaction-safe update).
- [x] Live caller path uses SDK-backed Square API clients and SDK-backed token refresh path.
- [x] Connection callback scope validation uses SDK token status (`RetrieveTokenStatus`) when exchange response scopes are missing.

## SDK Findings (March 1, 2026)
- [x] `Square` NuGet package is installed in `ShelfBuddy` (`Square` `43.0.0`).
- [x] M8 Gate D/E implementation now uses official SDK clients for OAuth and merchant probe flows:
  - [x] `client.OAuth.ObtainTokenAsync(...)`
  - [x] `client.OAuth.RevokeTokenAsync(...)`
  - [x] `client.Merchants.GetAsync(...)`
- [x] `SquareTokenService` refresh path now uses SDK OAuth `ObtainTokenAsync` (`grant_type=refresh_token`) instead of direct HTTP token calls.
- [x] Direct raw HTTP parsing for `/oauth2/token`, `/oauth2/revoke`, and `/v2/merchants/me` has been removed from the M8 runtime path.
- [x] Official SDK capabilities verified from local package docs (`Square.xml`):
  - [x] `client.OAuth.ObtainTokenAsync(...)`
  - [x] `client.OAuth.RevokeTokenAsync(...)`
  - [x] `client.Merchants.GetAsync(...)` / `client.Merchants.ListAsync(...)`
- [x] Deterministic error contracts were preserved while migrating to SDK-backed implementations.

## SDK Alignment Follow-Up (M8 Extension)
- [x] Refactor `SquareOAuthClient` to use SDK OAuth client (`ObtainTokenAsync` / `RevokeTokenAsync`) instead of manual HTTP calls.
- [x] Refactor `SquareMerchantClient` to use SDK merchants client instead of direct `/v2/merchants/me` HTTP call.
- [x] Refactor `SquareTokenService` refresh path to use SDK OAuth `ObtainTokenAsync` (`grant_type=refresh_token`).
- [x] Preserve existing API surface and deterministic error codes during SDK migration (no contract drift).
- [x] Add/adjust tests to validate SDK-based mappings for success, reconnect-required, and failure outcomes.

## Gate F: Onboarding Flow Refactor (State + UX Parity)
- [x] Rework onboarding into a multi-step flow while preserving current onboarding UX behavior (no regression).
- [x] Big onboarding Step 1 (functional): connect `SubscriptionSquareConnection`.
- [x] Step 2 (functional): set agent profile (name + `Personality`).
- [x] Step 3 (functional): configure channels (persist `TwilioPhoneNumber`, `TelegramBotToken`, and `WhatsappNumber` on `Agent`).
- [x] Step 4 (placeholder): invite members endpoint exists but invitation logic remains non-functional in M8.
- [x] Onboarding state is resumable per subscription.
- [x] Define step completion criteria and allowed transitions between steps.
- [x] Implement per-step onboarding endpoints and state transitions for big onboarding:
  - [x] `GET /onboarding/subscriptions/{subscriptionId}/state`
  - [x] `POST /onboarding/subscriptions/{subscriptionId}/agents`
  - [x] `PATCH /onboarding/agents/{agentId}/profile`
  - [x] `PATCH /onboarding/agents/{agentId}/channels`
  - [x] `POST /onboarding/subscriptions/{subscriptionId}/members/invitations` (placeholder semantics)
  - [x] `POST /onboarding/subscriptions/{subscriptionId}/finalize`
- [x] Implement onboarding state response contract (`status`, `currentStep`, `steps[]`, `primaryAgentId`, `canFinalize`) across step endpoints.
- [x] Keep `onboarded` claim behavior, but derive it from persisted onboarding completion state.
- [x] Enforce recomputation-on-read/write so persisted data is authoritative over cached step pointers.
- [ ] Any new Square API interactions introduced in onboarding steps MUST use official Square SDK clients (unless logged in Exception Registry).

## Gate G: Diagnostics and Observability
- [ ] Keep development-only verbose diagnostics for external claims/profile payload.
- [ ] Add structured token lifecycle logs (store/refresh/failure) without changing requested dev verbosity rule.
- [ ] Include trace correlation IDs in auth/connect logs.
- [ ] Add structured logs that identify SDK operation names and mapped deterministic outcome codes (without token leakage).

## Gate H: Tests
- [x] Gate A tests: Square pair validation, provider listing enabled/disabled, square authorize/callback path behavior.
- [x] Gate B tests: `SubscriptionSquareConnection` and `SubscriptionOnboardingState` mapping/constraints/indexes, plus `Agent` new field mappings (`Personality`, `TwilioPhoneNumber`, `TelegramBotToken`, `WhatsappNumber`).
- [x] Gate C tests: `ISquareTokenService` token store/read, refresh success, refresh token rotation, reconnect-required failures.
- [ ] Gate D tests: subscription connect/disconnect API authorization and deterministic error paths.
- [ ] Gate E tests: caller path gets valid subscription token and refreshes transparently.
- [x] Add `SquareOAuthClient` tests for exchange-scope parsing and token-status scope fallback behavior.
- [x] Add callback error-path service test for deterministic `access_denied` mapping.
- [ ] Gate F tests: per-step onboarding endpoints, big onboarding transitions, resumable state, channels persistence, invitation placeholder response, and UX parity checks for existing behavior.
- [x] Add onboarding service tests for channels step semantics: single-channel success and all-empty validation failure (`channels_at_least_one_required`).
- [ ] State machine tests: earliest-incomplete-step recomputation, invalidation fallback from `Completed` to `InProgress`, finalize guard behavior, and claim synchronization on finalize/login.
- [ ] Gate G tests: diagnostics/logging emits expected events and excludes redirect/query token leakage.
- [ ] Add endpoint-level tests for `SquareConnectionEndpoints` to confirm HTTP-layer authorization/status-code mapping over the existing service-level coverage.
- [ ] SDK path tests: assert all M8 server-side Square API calls use SDK-backed integrations unless listed in Exception Registry.
- [ ] Deterministic mapping tests: verify SDK exceptions/responses map to the same stable error codes used by existing API contracts.
- [ ] Gate I tests (WebApp): onboarding UI step flow bound to API state, including callback/resume behavior.
- [ ] Gate I form tests: `react-hook-form` + `zod` validation for profile/channels/team inputs.
- [ ] Gate I e2e happy path: square connect -> profile -> channels -> team dummy -> finalize.
- [ ] Gate I e2e skip path: skip required setup with warnings and verify finalize remains blocked until backend-required steps are completed.

## Gate I: Web Onboarding UI (End-to-End)
- [ ] Implement `/onboarding` UI flow in `ShelfBuddy.WebApp` using backend onboarding state as source of truth.
- [ ] Step 1 integrates Square connect start endpoint and handles OAuth callback return.
- [ ] Step 2 integrates primary agent creation/profile update (`name` + `Personality`).
- [x] Step 3 integrates channels update (`TwilioPhoneNumber`, `TelegramBotToken`, `WhatsappNumber`).
- [ ] Step 4 remains a team/invitations dummy step and calls placeholder endpoint only.
- [ ] Step 5 finalizes onboarding and routes to post-onboarding app destination.
- [ ] Use `react-hook-form` + `zod` + `@hookform/resolvers/zod` for form validation.
- [ ] Validation depth for M8:
  - [ ] required fields + basic formatting checks
  - [ ] `Agent.Name` required
  - [ ] `Personality` required
  - [x] phone values use basic E.164-like validation when provided
  - [x] channels may be empty individually, but at least one of `TwilioPhoneNumber` / `TelegramBotToken` / `WhatsappNumber` is required
  - [x] channels aggregate "at least one" validation error is shown on Telegram field
  - [x] channels field order in onboarding UI is `Telegram`, `Phone Number (SMS/Voice)`, `WhatsApp`
  - [ ] team emails (if entered) basic email format validation only
- [x] Remove onboarding completion JSON/debug configuration output.
- [ ] Keep skip navigation behavior with warning messaging, but do not allow successful finalize until required steps are complete.
- [ ] Re-fetch onboarding state after each step mutation and after Square callback to keep UI in sync with backend recomputation.
- [ ] Render progress and current step from API state contract (`status`, `currentStep`, `steps[]`, `primaryAgentId`, `canFinalize`).

### Gate I Findings (March 1, 2026)
- [x] Product decision: onboarding UI/UX is approved and MUST remain visually/structurally unchanged.
- [x] Exact preservation includes:
  - [x] button placement,
  - [x] skip buttons and their helper text placement under the actions,
  - [x] form layout/visual styling,
  - [x] existing step visuals and flow presentation.
- [x] Error messaging in onboarding should follow login-style placement and appear below continue/skip actions (no redesigned banner layout).
- [x] `ShelfBuddy.WebApp/src/lib/api` already contains generated onboarding endpoints; Gate I MUST use generated API client methods.
- [x] `ShelfBuddy.WebApp/src/lib/onboarding-api.ts` custom wrapper is not required for Gate I and should be removed in corrective pass.
- [x] `GET /onboarding/subscriptions/active` exists in backend and is the active-subscription resolution endpoint for onboarding UI.
- [ ] Corrective pass required: rebase `ShelfBuddy.WebApp/src/app/onboarding/page.tsx` on `docs/onboaring_page.tsx` and wire backend behavior without visual changes.

### Gate I UX Findings (March 2, 2026)
- [x] Channels step validation changed from "all three required" to "at least one channel required".
- [x] Aggregate channels validation error must be attached to Telegram field path for stable message placement.
- [x] Channels input order in onboarding Step 3 is `Telegram`, `Phone Number (SMS/Voice)`, `WhatsApp`.
- [x] Onboarding completion page no longer renders raw JSON/debug configuration output.
- [x] Backend channels endpoint validation is aligned with UI semantics and now returns `channels_at_least_one_required` when all channel values are empty.

### OAuth Redirect Findings (March 1, 2026)
- [x] Square OAuth app supports a single redirect URL, which conflicts with dual callback paths (`/auth/providers/square/callback` and `/onboarding/square/connect/callback`) when using one app.
- [x] Product decision: keep both callback flows and use separate Square app credentials.
- [x] Config decision:
  - [x] `AUTH_SQUARE_CLIENT_ID` / `AUTH_SQUARE_CLIENT_SECRET` for external auth provider (`/auth/*` flow).
  - [x] `SQUARE_CLIENT_ID` / `SQUARE_CLIENT_SECRET` for onboarding/admin subscription connection flow.

### OAuth Token Findings (March 1, 2026)
Sources reviewed:
- [x] https://developer.squareup.com/docs/oauth-api/receive-and-manage-tokens
- [x] https://developer.squareup.com/docs/oauth-api/best-practices

Findings applied to M8:
- [x] `ObtainToken` response is not a reliable source of granted scopes for onboarding scope-gate checks.
- [x] M8 connect callback should use `RetrieveTokenStatus` (with the exchanged access token) as authoritative scope source when token exchange scope fields are missing/empty.
- [x] Callback flow must explicitly handle deny/error query outcomes (for example `error=access_denied`) with deterministic error mapping.
- [x] Continue strict `state` validation as CSRF protection for all callback outcomes.
- [x] Authorization code exchange must happen immediately after callback because auth codes are short-lived.
- [x] Token handling must remain server-side only; no token values in redirects, client payloads, or logs.
- [x] Storage must remain resilient to long/JWT-format token lengths; token length must never be used as a validity check.
- [x] Runtime should keep deterministic handling for expired/revoked/unauthorized token states and map reconnect-required outcomes consistently.
- [ ] Follow-up (post-unblock): evaluate proactive scheduled refresh cadence aligned with Square guidance (<= 7 days) in addition to on-demand refresh.

## Acceptance Criteria
- [ ] Square login behaves like Google external auth through `/auth/*` endpoints.
- [ ] Square callback path is correctly configured and functional.
- [ ] First-time Square login can complete with minimal identity scope.
- [ ] Onboarding escalates to the locked full scope set: `ITEMS_READ`, `CUSTOMERS_READ`, `CUSTOMERS_WRITE`, `ORDERS_READ`, `ORDERS_WRITE`, `PAYMENTS_WRITE`.
- [ ] Onboarding callback validates required scopes from authoritative token status when exchange payload scopes are missing/empty.
- [ ] OAuth callback deny/error outcomes (including `access_denied`) are mapped to deterministic user-visible error codes.
- [ ] Big onboarding works end-to-end: connect square -> profile/personality -> channels -> invitations placeholder -> finalize.
- [ ] Channels step persists `TwilioPhoneNumber`, `TelegramBotToken`, and `WhatsappNumber` on `Agent`.
- [ ] Invitations step endpoint exists and is intentionally non-functional with deterministic placeholder behavior.
- [ ] `GET /onboarding/subscriptions/{subscriptionId}/state` deterministically returns authoritative `currentStep` and per-step statuses based on persisted data.
- [ ] `onboarded` claim is synchronized from persisted onboarding completion state at login and finalize.
- [ ] Subscription-scoped Square connection is persisted and used for server-to-server calls.
- [ ] Backend can call Square APIs without requiring an active user session.
- [ ] Token refresh is automatic and resilient.
- [ ] If refresh is invalid/revoked, system returns deterministic reconnect-required state.
- [ ] Token storage and handling are robust for long/JWT-format OAuth token values (no token-length assumptions).
- [ ] All M8 server-to-server Square API interactions use official Square SDK (`43.0.0`) unless explicitly documented in Exception Registry.
- [ ] `/onboarding` web UI supports end-to-end completion of big onboarding using backend endpoints.
- [ ] Onboarding forms use `react-hook-form` + `zod` validation with field-level errors.
- [ ] Channels step allows empty individual fields but blocks continue when all three channel fields are empty.
- [ ] Channels endpoint returns deterministic `channels_at_least_one_required` when all channel values are empty/whitespace.
- [ ] Onboarding completion page does not render JSON/debug configuration dump.
- [ ] Team step is visible in UI but remains intentionally non-functional beyond placeholder endpoint semantics in M8.
- [ ] Users may navigate/skip with warnings, but onboarding cannot finalize until required steps (`square_connect`, `profile`, `channels`) are complete.
- [ ] UI resumes deterministically from backend onboarding state (`currentStep`) after reload/callback.
- [ ] Completing onboarding results in both:
  - [ ] persisted subscription square connection, and
  - [ ] persisted configured primary agent profile + channels.

## Implementation Notes / Handoff Rule
- [ ] After Gate B schema changes are implemented, stop and hand off for migration creation/run from `ShelfBuddy.Initializer` per repository rule:
  - `dotnet ef migrations add Init --context MainDataContext -o .\\Migrations`
- [ ] Rework existing `ShelfBuddy.WebApp/src/app/onboarding/page.tsx` from local-only wizard state to backend-driven onboarding state + mutation flow while preserving the approved UI exactly (`docs/onboaring_page.tsx` as visual source of truth).

## Open Follow-Ups
- [x] Decide final full-scope list for onboarding connect (orders, inventory, customers, etc.) per concrete feature matrix.
- [x] Define disconnect behavior: Admin Settings supports create/remove connection; backend removes `SubscriptionSquareConnection` and invalidates runtime token availability for that subscription.
- [ ] Define small onboarding contract for additional agents in settings flow (outside M8 implementation scope).
- [ ] Implement Square team-member fetch and invitation workflow (deferred; invitations endpoint is placeholder in M8).
- [ ] Evaluate and design proactive Square token refresh scheduling (<= 7 days cadence) while preserving current deterministic reconnect-required behavior.
