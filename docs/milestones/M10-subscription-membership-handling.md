# Milestone M10: Subscription Membership Handling

## Goal
Ensure every newly created external-auth user has a subscription membership so onboarding can always resolve an active subscription and proceed.

## Scope
- **Backend (`ShelfBuddy.WebApi`)**
  - Update external login completion flow to provision subscription membership for first-time users.
  - Keep existing auth endpoint surface (`/auth/*`) and response contracts unchanged.
- **Data (`ShelfBuddy.Data`)**
  - Reuse existing `Subscription` + `SubscriptionUser` entities and mappings.
  - No schema changes for this milestone.
- **Tests (`ShelfBuddy.Tests`)**
  - Add/extend tests for new-user provisioning behavior and failure handling.

## Non-Goals (Out of Scope)
- Backfilling existing orphan users with no memberships (explicitly deferred; database reset expected).
- Users created via invitations and invitation acceptance handling.
- Changing credits behavior beyond existing defaults.
- Changing onboarding endpoint contracts.
- Adding new migration files.

## User Decisions (Locked)
- [x] New-user flow should auto-create workspace membership.
- [x] No backfill strategy required for existing data because database will be dropped.
- [x] Credits system is currently unused; subscription can use default values.
- [x] Provisioning must be atomic in a single DB transaction to avoid partially linked users getting stuck without membership.

## Findings from Repository Analysis

### Confirmed gap in current flow
- [x] External callback creates `ApplicationUser` but does not create `Subscription` or `SubscriptionUser`.
  - `ShelfBuddy.WebApi/Identity/IdentityEndpoints.cs`
  - Relevant sections: user creation branch + `AddLoginAsync` path.
- [x] Onboarding relies on `SubscriptionUsers` membership and returns not-found/forbidden when missing.
  - `ShelfBuddy.WebApi/Onboarding/OnboardingEndpoints.cs`
  - `ShelfBuddy/Onboarding/SubscriptionOnboardingService.cs`

### Current failure manifestations
- [x] `GET /onboarding/subscriptions/active` returns `subscription_membership_not_found` when user has no membership.
- [x] Onboarding service returns `subscription_member_required` in all step mutations/reads when membership is absent.

### Existing seed behavior does not cover normal runtime signups
- [x] Initializer seeds one admin subscription membership only for seeded admin data.
  - `ShelfBuddy.Initializer/Program.cs`
- [x] There is no runtime provisioning path for brand-new external users.

### Nuances to preserve during implementation
- [x] `CompleteExternalLoginAsync` currently short-circuits for users found via `FindByLoginAsync`; if a login is linked but membership creation fails, later logins bypass new-user logic and can remain broken.
  - `ShelfBuddy.WebApi/Identity/IdentityEndpoints.cs`
- [x] Login page currently has explicit mappings for known auth error codes; unknown codes fall back to a generic message.
  - `ShelfBuddy.WebApp/src/app/login/auth-error.ts`
- [x] Existing test suite is mostly unit/service focused; direct callback flow tests require either helper extraction or endpoint integration tests.
  - `ShelfBuddy.Tests/IdentityEndpointsSecurityTests.cs`
  - `ShelfBuddy.Tests/SubscriptionSquareConnectionServiceTests.cs`

## Architecture Decisions (Locked)
- [x] Provision subscription and owner membership during first successful external signup.
- [x] Keep provisioning inside auth callback transaction flow (same request lifecycle).
- [x] Scope provisioning to new-user branch only (no legacy/orphan auto-repair in this milestone).
- [x] Use one transaction for: user create (if needed), external login link, subscription create, and owner membership create.
- [x] On any failure in that unit of work, rollback and return deterministic auth error.
- [x] Subscription defaults for this milestone: `SubscriptionCreditBalance = 0`, `TopUpCreditBalance = 0`.
- [x] Preserve current redirect behavior:
  - [x] Not onboarded users still go to `/onboarding`.
  - [x] Onboarded users continue to requested return URL.

## Implementation Plan by Gate

## Gate A: Provisioning Path in External Login (Self-Contained)
- [x] Add a dedicated provisioning routine in `IdentityEndpoints` (or small internal helper/service) that creates:
  - [x] `Subscription`
  - [x] `SubscriptionUser` with `Role = Owner` for the newly created user.
- [x] Execute user create (if needed), login link, subscription create, and owner membership create in one transaction.
- [x] Keep provisioning scoped to first-time external sign-up path (new user branch).
- [x] Ensure provisioning does not run for existing users.
- [x] Ensure resulting user can resolve `GET /onboarding/subscriptions/active` immediately after first login.

### Gate A Acceptance Criteria
- [ ] First successful external signup creates exactly one subscription and one owner membership for that user.
- [ ] Existing users do not get duplicate subscriptions on subsequent logins.
- [ ] Onboarding active subscription lookup succeeds for newly created users.

## Gate B: Error Handling and Deterministic Auth Outcome (Self-Contained)
- [x] Add deterministic auth error handling for provisioning failures (for example `subscription_provision_failed`).
- [x] Keep external cookie cleanup and current redirect/error patterns intact.
- [x] Ensure partial-failure behavior is explicit and safe (no silent success when provisioning fails).
- [x] Add a dedicated login UI message mapping for `subscription_provision_failed` in `ShelfBuddy.WebApp/src/app/login/auth-error.ts`.

### Gate B Acceptance Criteria
- [ ] Provisioning exceptions are surfaced via deterministic login redirect auth error.
- [ ] No token/PII leakage in error responses or logs.
- [ ] External auth callback behavior remains compatible with existing login page error handling.
- [ ] Login page shows a custom message for `subscription_provision_failed` (not generic fallback).

## Gate C: Verification Tests (Self-Contained)
- [ ] Add/extend tests covering:
  - [x] New external user provisioning unit creates `Subscription` + `SubscriptionUser(Owner)`.
  - [x] Existing-user duplicate-provision prevention is verified at provisioning unit boundary.
  - [x] Provisioning failure code usage is verified through redirect builder output for `subscription_provision_failed`.
  - [ ] New user can resolve active subscription endpoint after login.
- [x] Lock test strategy for callback flow:
  - [x] Option A: extract provisioning unit into testable internal method/service and add unit tests.
  - [ ] Option B: add integration tests around `/auth/external-callback` using test host.
- [x] Keep tests focused on behavior regression guardrails for auth + onboarding handoff.

### Gate C Acceptance Criteria
- [ ] Test suite includes coverage for all four scenarios above.
- [ ] No regressions in existing identity security tests.

## Public Interfaces / Types
- [ ] No API contract changes planned.
- [ ] No DTO/type shape changes planned.
- [ ] Introduce an internal helper/service if needed to keep callback logic testable and transaction-aware.

## Test Scenarios (Authoritative)
1. **New user external login happy path**
   - Identity user is created.
   - External login is linked.
   - Subscription + owner membership are created.
   - Subscription uses default values (`SubscriptionCreditBalance = 0`, `TopUpCreditBalance = 0`).
   - User is redirected to `/onboarding` (not onboarded yet).
2. **Existing user external login happy path**
   - No new subscription records are created.
   - Existing behavior remains unchanged.
3. **Provisioning failure path**
   - Login redirect includes deterministic auth error.
   - User is not treated as successfully provisioned.
   - No partial linked-login-without-membership state remains after failure.
4. **Onboarding handoff path**
   - `GET /onboarding/subscriptions/active` succeeds for newly created users after first login.
## Risks / Notes
- [ ] Ensure idempotency around repeated callback attempts to avoid duplicate subscription creation.
- [ ] Keep membership role deterministic (`Owner`) for creator user.
- [ ] Preserve least-privilege and avoid additional claims/permissions in this milestone.

## Handoff Notes for New Context Window
- [ ] Start at `ShelfBuddy.WebApi/Identity/IdentityEndpoints.cs` (`CompleteExternalLoginAsync`).
- [ ] Implement Gate A before Gate B/C; Gate A unblocks everything else.
- [ ] Validate against onboarding membership checks in:
  - [ ] `ShelfBuddy.WebApi/Onboarding/OnboardingEndpoints.cs`
  - [ ] `ShelfBuddy/Onboarding/SubscriptionOnboardingService.cs`
- [ ] Keep schema untouched; no migration work required for this milestone.
