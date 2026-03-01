# Milestone M8: Square OAuth Auth + Subscription-Scoped Connection

## Goal
Align Square external authentication with the working Google `/auth` flow, establish a subscription-scoped Square OAuth connection that supports server-to-server Square API access without an active user session, and deliver a two-layer onboarding backend flow with one endpoint per step.

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

## Non-Goals (Out of Scope)
- Multi-Square-account per agent support.
- Many-to-many agent-to-Square-account relationship.
- Admin settings UI implementation for connect/disconnect management.
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
- `SQUARE_CLIENT_ID`
- `SQUARE_CLIENT_SECRET`

### Validation rules
- [ ] If both Square keys are missing: valid (Square disabled).
- [ ] If one key is present and the other missing: invalid startup configuration.
- [ ] If both keys are present: valid (Square enabled).

## HTTP Surface

### External auth endpoints (existing shape, Square parity)
- `GET /auth/providers`
- `GET /auth/providers/{provider}/authorize`
- `GET /auth/external-callback`

Square-specific behavior:
- [ ] Provider name is `square`.
- [ ] Callback path is `/auth/providers/square/callback`.
- [ ] Callback completion returns through `/auth/external-callback`.

### Onboarding connect endpoint(s)
- [ ] Add/extend onboarding route to start full-scope Square authorization for a subscription.
- [ ] Persist/replace `SubscriptionSquareConnection` on successful authorization.
- [ ] Return deterministic error codes for connect failures.

### Onboarding step endpoints (backend)
- [ ] `GET /onboarding/subscriptions/{subscriptionId}/state` returns big onboarding state for resumable first-login onboarding.
- [ ] `POST /onboarding/subscriptions/{subscriptionId}/square/connect/authorize` starts Square full-scope connect flow.
- [ ] `POST /onboarding/subscriptions/{subscriptionId}/agents` creates the first draft agent used in big onboarding.
- [ ] `PATCH /onboarding/agents/{agentId}/profile` updates name and `Personality`.
- [ ] `PATCH /onboarding/agents/{agentId}/channels` updates the three channel fields on `Agent`.
- [ ] `POST /onboarding/subscriptions/{subscriptionId}/members/invitations` exists as placeholder endpoint and returns deterministic "not implemented in M8" semantics.
- [ ] `POST /onboarding/subscriptions/{subscriptionId}/finalize` validates required steps and marks subscription onboarding complete.

### Admin settings connection management API (no UI work in this milestone)
- [ ] Add backend endpoint(s) used by Admin Settings to create/connect a `SubscriptionSquareConnection`.
- [ ] Add backend endpoint(s) used by Admin Settings to remove/disconnect a `SubscriptionSquareConnection`.
- [ ] Enforce authorization so only permitted subscription members can connect/disconnect for their subscription.

## Data Model

### New entity: `SubscriptionSquareConnection`
- [ ] `SubscriptionId` (FK -> `Subscription`, unique)
- [ ] `SquareMerchantId`
- [ ] `EncryptedAccessToken`
- [ ] `EncryptedRefreshToken`
- [ ] `AccessTokenExpiresAtUtc`
- [ ] `Scopes`
- [ ] `ConnectedByUserId` (FK -> `ApplicationUser`)
- [ ] `CreatedAt`
- [ ] `UpdatedAt`
- [ ] Optional lifecycle fields as needed (`DisconnectedAtUtc`, status flags)

### New entity: `SubscriptionOnboardingState`
- [ ] `SubscriptionId` (FK -> `Subscription`, unique)
- [ ] `Status` (`Draft`, `InProgress`, `Completed`)
- [ ] `CurrentStep` (big onboarding step pointer)
- [ ] `PrimaryAgentId` (nullable FK -> `Agent`)
- [ ] `StartedAt`
- [ ] `UpdatedAt`
- [ ] `CompletedAt` (nullable)

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
- [ ] Unique index on `SubscriptionId` for 1:1 square connection cardinality.
- [ ] Index on `SquareMerchantId`.
- [ ] Unique index on `SubscriptionOnboardingState.SubscriptionId`.

### `Agent` extensions
- [ ] Add `Personality` field.
- [ ] Add/confirm the three channel fields used by onboarding channels step:
  - [ ] `TwilioPhoneNumber` (existing)
  - [ ] `TelegramBotToken` (existing)
  - [ ] `WhatsappNumber` (new)

## Service Design

### New module
- [ ] Create `ShelfBuddy/SquareIntegration` folder.
- [ ] Add `ISquareTokenService` + implementation in `ShelfBuddy` project.

### `ISquareTokenService` responsibilities
- [ ] Store tokens from successful Square connect/login callback.
- [ ] Return valid access token for a given `SubscriptionId`.
- [ ] Refresh access token when expired or near expiry.
- [ ] Rotate refresh token when Square returns a new one.
- [ ] Return typed failures for revoked/invalid/reauthorize-required states.

### Security requirements
- [ ] Do not expose tokens in API responses.
- [ ] Do not place tokens in redirect query strings.
- [ ] Encrypt token values at rest.
- [ ] Keep least-privilege scope model through two-step consent.

## Gate A: Square Auth Foundation (Config + Provider Parity)
- [ ] Add Square pair validation in `AppOptionsValidator`.
- [ ] Ensure Square provider registration is conditional and consistent with Google pair behavior.
- [ ] Fix Square callback path to `/auth/providers/square/callback`.
- [ ] Keep `/auth/providers/{provider}/authorize` + `/auth/external-callback` flow unchanged and functional for Square login with minimal scope (`MERCHANT_PROFILE_READ`).

## Gate B: Subscription-Scoped Persistence Foundation
- [ ] Add `SubscriptionSquareConnection` entity and EF configuration.
- [ ] Add `SubscriptionOnboardingState` entity and EF configuration.
- [ ] Add `DbSet<SubscriptionSquareConnection>` and `DbSet<SubscriptionOnboardingState>` in `MainDataContext`.
- [ ] Add constraints/indexes and audit behavior.
- [ ] Extend `Agent` with `Personality` and channel fields (`TwilioPhoneNumber`, `TelegramBotToken`, `WhatsappNumber`).
- [ ] Update initializer assumptions/documentation for new schema object.
- [ ] Stop and hand off for migration creation/run from `ShelfBuddy.Initializer` per repository rule.

## Gate C: Shared Square Token Lifecycle Service
- [ ] Add `ShelfBuddy/SquareIntegration/ISquareTokenService.cs`.
- [ ] Add implementation and supporting models/errors.
- [ ] Implement token storage/read/update against `SubscriptionSquareConnection`.
- [ ] Add Square OAuth token refresh client logic (`/oauth2/token`).
- [ ] Implement deterministic typed outcomes for refresh failure/reconnect-required states.
- [ ] Register services in DI from WebApi composition root.

## Gate D: Square Connection Management APIs (Backend)
- [ ] Add backend endpoint(s) used by Admin Settings to create/connect a `SubscriptionSquareConnection` (UI excluded).
- [ ] Add backend endpoint(s) used by Admin Settings to remove/disconnect a `SubscriptionSquareConnection` (UI excluded).
- [ ] Add big onboarding connect flow requesting this exact full-scope set: `ITEMS_READ`, `CUSTOMERS_READ`, `CUSTOMERS_WRITE`, `ORDERS_READ`, `ORDERS_WRITE`, `PAYMENTS_WRITE`.
- [ ] On successful connect, persist/replace `SubscriptionSquareConnection` through `ISquareTokenService`.
- [ ] Enforce subscription membership/authorization for connect/disconnect actions.
- [ ] Return deterministic error codes for connect/disconnect failures.

## Gate E: Runtime Token Consumption
- [ ] Backend Square API callers resolve tokens via `ISquareTokenService.GetValidAccessTokenAsync(subscriptionId)`.
- [ ] Automatic refresh on expiry works end-to-end in live caller path.
- [ ] Revoked/invalid refresh states produce deterministic reconnect-required behavior.
- [ ] Avoid duplicate concurrent refresh races (single-flight or transaction-safe update).

## Gate F: Onboarding Flow Refactor (State + UX Parity)
- [ ] Rework onboarding into a multi-step flow while preserving current onboarding UX behavior (no regression).
- [ ] Big onboarding Step 1 (functional): connect `SubscriptionSquareConnection`.
- [ ] Step 2 (functional): set agent profile (name + `Personality`).
- [ ] Step 3 (functional): configure channels (persist `TwilioPhoneNumber`, `TelegramBotToken`, and `WhatsappNumber` on `Agent`).
- [ ] Step 4 (placeholder): invite members endpoint exists but invitation logic remains non-functional in M8.
- [ ] Onboarding state is resumable per subscription.
- [ ] Define step completion criteria and allowed transitions between steps.
- [ ] Implement per-step onboarding endpoints and state transitions for big onboarding:
  - [ ] `GET /onboarding/subscriptions/{subscriptionId}/state`
  - [ ] `POST /onboarding/subscriptions/{subscriptionId}/agents`
  - [ ] `PATCH /onboarding/agents/{agentId}/profile`
  - [ ] `PATCH /onboarding/agents/{agentId}/channels`
  - [ ] `POST /onboarding/subscriptions/{subscriptionId}/members/invitations` (placeholder semantics)
  - [ ] `POST /onboarding/subscriptions/{subscriptionId}/finalize`
- [ ] Implement onboarding state response contract (`status`, `currentStep`, `steps[]`, `primaryAgentId`, `canFinalize`) across step endpoints.
- [ ] Keep `onboarded` claim behavior, but derive it from persisted onboarding completion state.
- [ ] Enforce recomputation-on-read/write so persisted data is authoritative over cached step pointers.

## Gate G: Diagnostics and Observability
- [ ] Keep development-only verbose diagnostics for external claims/profile payload.
- [ ] Add structured token lifecycle logs (store/refresh/failure) without changing requested dev verbosity rule.
- [ ] Include trace correlation IDs in auth/connect logs.

## Gate H: Tests
- [ ] Gate A tests: Square pair validation, provider listing enabled/disabled, square authorize/callback path behavior.
- [ ] Gate B tests: `SubscriptionSquareConnection` and `SubscriptionOnboardingState` mapping/constraints/indexes, plus `Agent` new field mappings (`Personality`, `TwilioPhoneNumber`, `TelegramBotToken`, `WhatsappNumber`).
- [ ] Gate C tests: `ISquareTokenService` token store/read, refresh success, refresh token rotation, reconnect-required failures.
- [ ] Gate D tests: subscription connect/disconnect API authorization and deterministic error paths.
- [ ] Gate E tests: caller path gets valid subscription token and refreshes transparently.
- [ ] Gate F tests: per-step onboarding endpoints, big onboarding transitions, resumable state, channels persistence, invitation placeholder response, and UX parity checks for existing behavior.
- [ ] State machine tests: earliest-incomplete-step recomputation, invalidation fallback from `Completed` to `InProgress`, finalize guard behavior, and claim synchronization on finalize/login.
- [ ] Gate G tests: diagnostics/logging emits expected events and excludes redirect/query token leakage.

## Acceptance Criteria
- [ ] Square login behaves like Google external auth through `/auth/*` endpoints.
- [ ] Square callback path is correctly configured and functional.
- [ ] First-time Square login can complete with minimal identity scope.
- [ ] Onboarding escalates to the locked full scope set: `ITEMS_READ`, `CUSTOMERS_READ`, `CUSTOMERS_WRITE`, `ORDERS_READ`, `ORDERS_WRITE`, `PAYMENTS_WRITE`.
- [ ] Big onboarding works end-to-end: connect square -> profile/personality -> channels -> invitations placeholder -> finalize.
- [ ] Channels step persists `TwilioPhoneNumber`, `TelegramBotToken`, and `WhatsappNumber` on `Agent`.
- [ ] Invitations step endpoint exists and is intentionally non-functional with deterministic placeholder behavior.
- [ ] `GET /onboarding/subscriptions/{subscriptionId}/state` deterministically returns authoritative `currentStep` and per-step statuses based on persisted data.
- [ ] `onboarded` claim is synchronized from persisted onboarding completion state at login and finalize.
- [ ] Subscription-scoped Square connection is persisted and used for server-to-server calls.
- [ ] Backend can call Square APIs without requiring an active user session.
- [ ] Token refresh is automatic and resilient.
- [ ] If refresh is invalid/revoked, system returns deterministic reconnect-required state.

## Implementation Notes / Handoff Rule
- [ ] After Gate B schema changes are implemented, stop and hand off for migration creation/run from `ShelfBuddy.Initializer` per repository rule:
  - `dotnet ef migrations add Init --context MainDataContext -o .\\Migrations`

## Open Follow-Ups
- [x] Decide final full-scope list for onboarding connect (orders, inventory, customers, etc.) per concrete feature matrix.
- [x] Define disconnect behavior: Admin Settings supports create/remove connection; backend removes `SubscriptionSquareConnection` and invalidates runtime token availability for that subscription.
- [ ] Define small onboarding contract for additional agents in settings flow (outside M8 implementation scope).
- [ ] Implement Square team-member fetch and invitation workflow (deferred; invitations endpoint is placeholder in M8).
