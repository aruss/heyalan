# Milestone M7: Google External Auth Endpoints (Custom `/auth` Flow)

## Goal
Add custom 3rd-party identity endpoints for Google login in `HeyAlan.WebApi` without using `MapIdentityApi<T>`, while preserving the project's cookie-based authentication model behind the frontend proxy.

All auth-related callbacks MUST be under `/auth/*`.

## Scope
- **Backend (`HeyAlan.WebApi`)**
  - Register Google external auth conditionally from `AppOptions`.
  - Add provider discovery endpoint for frontend login button rendering.
  - Add external login start endpoint.
  - Add external callback completion endpoint.
  - Keep final user authentication cookie-based via ASP.NET Core Identity.
- **Shared (`HeyAlan`)**
  - Extend `AppOptions` for Google credentials.
  - Extend options validation so Google credentials are optional as a pair.
- **Frontend interaction contract**
  - Login page uses `returnUrl` query string and forwards it into external login start.
  - Callback redirects to that `returnUrl` if valid; fallback to `/admin`.

## Non-Goals (Out of Scope)
- Reintroducing full `MapIdentityApi<T>` surface.
- Additional providers beyond Google.
- 2FA and advanced account-link management UI.
- Bearer-token auth mode for this flow.

## Configuration Contract

### Required existing key
- `PUBLIC_BASE_URL` (already required and validated).

### New optional keys (pair rule)
- `GOOGLE_CLIENT_ID`
- `GOOGLE_CLIENT_SECRET`

Validation rules:
- [x] If both Google keys are missing: valid (Google login disabled).
- [x] If one key is present and the other missing: invalid startup configuration.
- [x] If both keys are present: valid (Google login enabled).

## HTTP Surface

### 1) Get available external providers
`GET /auth/providers`

Behavior:
- [x] Return only enabled/configured external providers.
- [x] If Google is not configured, omit it.

Result DTOs:
- [x] `ExternalLoginProviderItem`
  - `Name`
  - `DisplayName`
- [x] `GetExternalLoginProvidersResult`
  - `Providers: ExternalLoginProviderItem[]`

### 2) Start external login
`GET /auth/providers/{provider}/start?returnUrl=/admin/...`

Behavior:
- [x] Validate provider exists in external auth schemes.
- [x] Sanitize `returnUrl` (local path only).
- [x] Fallback `returnUrl` to `/admin` when missing/invalid.
- [x] Use `SignInManager.ConfigureExternalAuthenticationProperties(...)`.
- [x] Return challenge result for selected provider.

Error behavior:
- [x] Unknown provider returns `404`.

### 3) External callback completion
`GET /auth/external-callback?returnUrl=/admin/...&remoteError=...`

Behavior:
- [x] If `remoteError` is present, redirect to sanitized return target with error marker.
- [x] Read external login info via `SignInManager.GetExternalLoginInfoAsync()`.
- [x] Attempt `ExternalLoginSignInAsync(...)`.
- [x] If linked login exists and succeeds: sign in cookie flow completes and redirect.
- [x] If linked login does not exist: auto-provision local user, add external login link, sign in, redirect.
- [x] If no/invalid `returnUrl`, redirect to `/admin`.

Auto-provision rules:
- [x] Require email claim for new-user provisioning.
- [x] Map `ApplicationUser.Email` and `ApplicationUser.UserName` from external email.
- [x] Map `ApplicationUser.DisplayName` from name claim if available, otherwise fallback to email-derived value.
- [x] Persist link via `UserManager.AddLoginAsync(user, info)`.

Failure behavior:
- [x] Missing external info or missing required claim for provisioning returns redirect with error marker.

## Callback Path Requirements (`/auth` only)

Google middleware callback path:
- [x] Configure `GoogleOptions.CallbackPath = "/auth/google/callback"`.

Flow:
- [x] Browser starts at `/auth/providers/google/authorize`.
- [x] Google redirects to `/auth/providers/google/callback`.
- [x] Middleware finalizes external auth handshake and returns to `/auth/external-callback`.
- [x] When running behind a proxy path prefix (for example `/api`), callback redirects preserve that prefix (`/api/auth/...`).

## Security Requirements
- [x] `returnUrl` MUST be local path only (`/something`) to prevent open redirect attacks.
- [x] Reject absolute external URLs and protocol-relative URLs.
- [x] Do not log external tokens or Google credentials.
- [x] Continue using secure cookie settings (`HttpOnly`, `Secure`, `SameSite=Lax`) already configured.
- [x] Configure Identity application/external cookie path to `/` so proxy `/api` callbacks and `/admin` routes share the same session cookie.

## Implementation Plan by Gate

## Gate A: AppOptions + Validation
- [x] Extend `HeyAlan/Configuration/AppOptions.cs` with nullable Google credential properties.
- [x] Extend `TryGetAppOptions()` to read and trim `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET`.
- [x] Add explicit pair validation logic for Google credentials.
- [x] Preserve existing `PUBLIC_BASE_URL` validation behavior.

## Gate B: Identity Registration
- [x] Update `HeyAlan/Identity/IdentityBuilderExtensions.cs` to conditionally register Google auth only when both credentials exist.
- [x] Configure callback path to `/auth/google/callback`.
- [x] Ensure Identity external sign-in temp cookie flow remains compatible with `SignInManager`.

## Gate C: Custom Identity Endpoints
- [x] Update `HeyAlan.WebApi/Identity/IdentityEndpoints.cs` to add:
  - [x] `GET /auth/providers`
  - [x] `GET /auth/providers/{provider}/start`
  - [x] `GET /auth/external-callback`
- [x] Keep existing `/auth/me` and `/auth/logout`.
- [x] Add returnUrl sanitization helper and `/admin` fallback.
- [x] Add operation-centric DTOs in `HeyAlan.WebApi/Identity`:
  - [x] `GetExternalLoginProvidersResult`
  - [x] `ExternalLoginProviderItem`

## Gate D: Tests
- [x] Add `AppOptions` unit tests for Google pair validation and existing base URL behavior.
- [ ] Add endpoint tests for `/auth/providers` enabled/disabled behavior.
- [ ] Add endpoint tests for external start:
  - [ ] Known provider challenges.
  - [ ] Unknown provider returns `404`.
- [ ] Add callback tests:
  - [ ] Successful linked login redirects correctly.
  - [ ] Missing/invalid returnUrl falls back to `/admin`.
  - [ ] `remoteError` path redirects with failure marker.
  - [ ] New user auto-provision creates user + external login and signs in.

## Gate E: External Login Security Hardening
- [x] Update `GET /auth/external-callback` linking flow to prevent account takeover via unverified external emails.
- [x] Introduce provider email-verification validation before linking to an existing local user by email.
  - [x] Require provider email claim to be present.
  - [x] Require provider email-verified indicator claim to be true before any existing-user email link.
  - [x] If verification signal is missing/false, redirect with explicit failure marker (for example: `authError=external_email_not_verified`).
- [x] Require local account email confirmation when auto-linking an existing user by email.
  - [x] If local email is unconfirmed, redirect with explicit failure marker (for example: `authError=local_email_not_confirmed`).
- [x] Keep new-user auto-provision constrained to verified external email only.
- [x] Add explicit code comments documenting why email verification checks are required before linking.

## Gate F: Post-Login Authorization & Onboarding Enforcement
- [x] Enforce authentication by default for private API surfaces.
  - [x] Enable authorization on conversation endpoints (remove temporary unauthenticated behavior).
  - [x] Preserve anonymous access only for intended public endpoints (`/auth/providers`, `/auth/providers/*`, webhook endpoints as needed).
- [x] Apply onboarding policy to private admin/API route groups where business rules require completed onboarding.
  - [x] Use existing `OnboardedOnly` policy for protected groups.
  - [ ] Confirm policy expectations for service/webhook endpoints that should remain outside onboarding requirements.
- [x] Add post-login routing guard in external callback:
  - [x] If user is authenticated but not onboarded, redirect to onboarding route (local URL) rather than requested admin route.
  - [x] Preserve safe local `returnUrl` normalization and error marker behavior.
- [x] Ensure claims used by onboarding policy are present and consistent at sign-in time.
  - [x] If onboarding claim issuance is deferred/not implemented, block by policy source of truth and document interim behavior.

## Gate G: Regression and Security Tests
- [ ] Add callback security tests for account-linking rules:
  - [ ] Existing local user + unverified external email must not auto-link.
  - [ ] Existing local user + verified external email + confirmed local email can auto-link.
  - [ ] Existing local user + verified external email + unconfirmed local email must not auto-link.
  - [ ] New-user auto-provision is rejected when external email is missing or unverified.
- [ ] Add authorization tests:
  - [ ] Conversation/private endpoints return `401` when unauthenticated.
  - [ ] Onboarding-protected endpoints return `403` for authenticated users without `onboarded=true`.
  - [ ] Onboarding-protected endpoints succeed for authenticated users with `onboarded=true`.
- [ ] Add callback redirect tests:
  - [ ] Non-onboarded user callback redirects to onboarding route.
  - [ ] Onboarded user callback redirects to normalized requested route.
  - [x] Unit tests cover redirect-target decision helper for onboarded/non-onboarded outcomes.


## Gate H: Login Page Auth Error Display (UI-Only, Current API State)
  - [x] Surface auth callback errors on `/login` via `authError` query parameter.
    - [x] Parse `authError` from `searchParams` and render a single top banner on the login page.
    - [x] If multiple `authError` values are present, display the first value only.
    - [x] Keep `returnUrl` safety logic unchanged.
  - [x] Map all currently emitted API error codes to user-facing copy.
    - [x] `external_provider_error`
    - [x] `external_login_info_missing`
    - [x] `user_not_allowed`
    - [x] `user_locked_out`
    - [x] `email_claim_missing`
    - [x] `external_email_not_verified`
    - [x] `user_create_failed`
    - [x] `local_email_not_confirmed`
    - [x] `external_login_link_failed`
    - [x] Unknown code fallback: generic sign-in failure message.
  - [x] Implement unconfirmed-email guidance behavior.
    - [x] For `local_email_not_confirmed`, tell user to confirm email first.
    - [x] Show “or use another provider” guidance only when more than one provider is available on the page.
  - [x] Preserve existing provider rendering behavior.
    - [x] Auth error banner should display independently of provider load/empty states.
    - [x] Existing provider-load error handling remains intact.
  - [x] Add tests for UI error behavior.
    - [x] Mapping/parsing tests for known and unknown codes.
    - [x] Multiple `authError` values select the first one.
    - [x] `local_email_not_confirmed` message variant changes based on provider count.


## Gate I: Google UserInfo Verification Claim Normalization
  - [x] Update Google auth registration in `HeyAlan/Identity/IdentityBuilderExtensions.cs` to explicitly source verification data from
  Google UserInfo endpoint.
    - [x] Configure/confirm `GoogleOptions.UserInformationEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo"`.
    - [x] Update `WithError(...)` logic in `HeyAlan.WebApi/Identity/IdentityEndpoints.cs` to replace existing `authError` rather than
  append duplicates.
    - [x] Missing/false verification still rejects deterministically.
    - [x] `authError` deduplication emits a single value in redirect URL.

## Acceptance Criteria
- [ ] Frontend can call `/auth/providers` and render Google login only when configured.
- [ ] Google sign-in starts at `/auth/providers/google/authorize` and callback traffic stays under `/auth/*`.
- [ ] Successful login ends with Identity cookie authentication and redirects to frontend `returnUrl`.
- [ ] Missing or invalid `returnUrl` redirects to `/admin`.
- [ ] Partial Google credential configuration fails fast at startup with clear config error.
- [ ] Existing local accounts are never linked from external login unless external email is verified and local email is confirmed.
- [ ] External callback rejects missing/unverified email cases with deterministic `authError` markers.
- [ ] Private conversation/admin APIs are not reachable anonymously.
- [ ] Onboarding-protected APIs are reachable only when user has satisfied onboarding policy.
- [ ] External callback sends non-onboarded users to onboarding route and onboarded users to normalized target route.


## Notes
- `docs/IdentityApiEndpointRouteBuilderExtensions.cs` is currently empty and is not used for implementation.
- This milestone intentionally customizes only the required Identity endpoint subset and keeps API-specific request/response contracts.
- Security baseline for this milestone: email-based account linking MUST NOT rely on `ClaimTypes.Email` alone.
- TEMP-DIAG-REMOVE: Development-only raw Google profile logging and external claim dump were added to diagnose `external_email_not_verified` outcomes and should be removed after root-cause confirmation.

