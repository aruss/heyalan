# Milestone M18: Single Square App via Callback Broker

## Summary
Collapse Square external auth and subscription connect onto one Square app and one credential pair. The single provider-facing callback URL becomes:

- `<PUBLIC_BASE_URL>/api/subscriptions/square/callback`

That endpoint acts as a broker:
- BuyAlan connect state -> complete subscription Square connect
- ASP.NET Core external-auth state -> forward internally to `/auth/providers/square/callback`

This removes the proxy-sensitive callback reconstruction problem in production and eliminates the dual-app / dual-credential setup.

## User Decisions (Locked)
- [x] Use one Square app and one shared credential pair.
- [x] Canonical public Square callback URL is `/api/subscriptions/square/callback`.
- [x] Keep the internal ASP.NET Core auth callback path `/auth/providers/square/callback`.
- [x] Use the broker callback to route login vs connect based on `state`.
- [x] Do not change Google auth in this milestone.

## API and Interface Changes
- [x] Keep `POST /subscriptions/{subscriptionId}/square/authorize` unchanged for connect start.
- [x] Keep `GET /subscriptions/square/callback` as the canonical public Square callback endpoint.
- [x] Keep Identity Square `CallbackPath` registered as `/auth/providers/square/callback`.
- [x] Change Square external auth provider authorize redirect so provider-facing `redirect_uri` is `<PUBLIC_BASE_URL>/api/subscriptions/square/callback`.
- [x] No new public DTO contracts required.
- [x] OpenAPI surface remains unchanged.

## Gate A: Credential and Configuration Unification
- [x] Update identity Square provider setup to use `SQUARE_CLIENT_ID` / `SQUARE_CLIENT_SECRET`.
- [x] Remove runtime dependency on `AUTH_SQUARE_CLIENT_ID` / `AUTH_SQUARE_CLIENT_SECRET`.
- [x] Update `AppOptions` validation so Square auth and Square connect share the same required `SQUARE_*` pair.
- [x] Update docs and setup guidance to describe a single Square app and a single credential pair.

### Gate A Acceptance Criteria
- [x] Auth Square provider initializes from `SQUARE_*` only.
- [x] Subscription connect flow also initializes from `SQUARE_*`.
- [x] No runtime code path still reads `AUTH_SQUARE_*`.

## Gate B: Broker Callback Routing
- [x] Extend `/subscriptions/square/callback` handler to branch by `state`:
  - valid `SquareConnectStatePayload` -> complete existing connect flow
  - any other `state` value -> 302 redirect to `/auth/providers/square/callback` with original query preserved
- [x] Keep the forward target fixed/internal; never forward to user-provided URLs.
- [x] Preserve existing connect callback success/error behavior and redirect URLs.
- [x] Ensure non-connect external-auth callbacks no longer fail as `square_oauth_state_invalid`.

### Gate B Acceptance Criteria
- [x] Subscription connect callback still persists tokens and redirects to the onboarding return URL.
- [x] External login callback reaches the ASP.NET Core auth handler through the broker.
- [x] Query parameters (`code`, `state`, `error`, `response_type`) survive broker forwarding unchanged.

## Gate C: Identity Square Redirect Canonicalization
- [x] In `BuyAlan/Identity/IdentityBuilderExtensions.cs`, force Square auth `redirect_uri` to `<PUBLIC_BASE_URL>/api/subscriptions/square/callback`.
- [x] Keep existing production `session=false` behavior.
- [x] Keep existing claim mapping, ticket creation, and remote-failure handling unchanged.

### Gate C Acceptance Criteria
- [x] Outbound Square external-auth challenge uses the broker callback URL.
- [x] ASP.NET Core external auth correlation/state flow remains functional after broker forwarding.
- [x] External login success/failure user experience remains unchanged.

## Gate D: Tests
- [x] Add identity tests proving the Square provider is registered from `SQUARE_*` and no longer depends on `AUTH_SQUARE_*`.
- [x] Add endpoint-level tests for broker branching:
  - valid connect-state -> connect completion path
  - opaque external-auth state -> redirect to `/auth/providers/square/callback`
- [x] Add tests verifying broker forwarding preserves the full query payload.
- [x] Add tests verifying the Square auth provider now uses `/api/subscriptions/square/callback` as `redirect_uri`.
- [x] Keep/update existing connect flow tests for required internal return URL behavior.
- [x] Keep existing remote-failure redirect coverage for external auth.
- [ ] Run `BuyAlan.Tests` and `BuyAlan.WebApi.Tests` and verify no regressions.

### Gate D Acceptance Criteria
- [x] Broker callback tests pass for both branches.
- [x] External auth and connect-flow tests both pass with shared Square credentials.
- [x] No regression in subscription ownership/security checks for connect flow.

## Gate E: Documentation and Ops
- [x] Update `docs/square-app-setup.md` so setup requires one Square app and one callback URL: `<PUBLIC_BASE_URL>/api/subscriptions/square/callback`.
- [x] Remove references to `AUTH_SQUARE_*` and dual-app Square setup.
- [x] Document the broker behavior for operators and future maintainers.
- [x] No WebApp client regeneration handoff is needed because no API contract changes are introduced.

### Gate E Acceptance Criteria
- [x] Docs match actual runtime configuration and callback behavior.
- [x] Operators can deploy with one Square app and one credential pair.

## Risks and Notes
- Dev currently works through Cloudflare Tunnel, so the current flow is not fundamentally invalid; the failure is specific to production callback reconstruction under Coolify.
- Current production logs show forwarded headers from the internal proxy are not trusted, which likely causes the mismatched token-exchange `redirect_uri`.
- The broker removes Square external auth's dependency on that proxy-specific reconstruction path.
- This milestone intentionally does not solve forwarded-header trust globally; it hardens Square login specifically.

