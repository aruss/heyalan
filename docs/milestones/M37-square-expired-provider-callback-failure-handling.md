# Milestone M37: Square Expired Provider Callback Failure Handling

## Summary
Handle expired or otherwise invalid Square external-auth callback URLs as a controlled remote-auth failure instead of letting ASP.NET Core surface an unhandled `AuthenticationFailureException` that becomes a 500 response.

This milestone applies to the generic provider callback path in `BuyAlan/Identity/IdentityBuilderExtensions.cs` and the login completion flow in `BuyAlan.WebApi/Identity/IdentityEndpoints.cs`. It does not change the subscription Square connect callback flow under `/subscriptions/square/callback`.

## User Decisions (Locked)
- [x] Fix the external auth provider callback path, not the subscription Square connect callback.
- [x] Preserve the existing redirect-based login recovery UX.
- [x] Reuse `external_provider_error` instead of introducing a new Square-specific auth error code.
- [x] Keep API contracts and OpenAPI surface unchanged.

## API and Interface Changes
- [x] No new public endpoints.
- [x] No new DTOs or response schemas.
- [x] Add or reuse an internal helper that builds the `/auth/external-callback?remoteError=external_provider_error` redirect target from `PathBase`.

## Gate A - Provider Failure Interception
- [x] Add `OnRemoteFailure` handling to the Square OAuth provider registration in `BuyAlan/Identity/IdentityBuilderExtensions.cs`.
- [x] Redirect Square remote-auth failures to `/auth/external-callback?remoteError=external_provider_error`.
- [x] Call `context.HandleResponse()` so correlation failures and expired callback errors do not propagate to authentication middleware as unhandled exceptions.
- [x] Keep Google remote-failure behavior aligned with the same shared redirect pattern.
- [x] Leave `GlobalProblemExceptionHandler` unchanged because this failure must be handled before it reaches the global 500 path.

### Gate A Acceptance Criteria
- [x] Reusing an expired Square provider callback URL no longer returns a 500 problem-details response.
- [x] A Square correlation failure is converted into the existing external login error redirect flow.
- [x] Google external auth behavior remains unchanged.

## Gate B - Shared Failure Redirect Helper
- [x] Introduce or consolidate a small internal helper for building the external-provider failure callback URL from `PathBase`.
- [x] Use the helper from provider event handlers instead of duplicating callback-path and query-string assembly.
- [x] Keep the helper internal to avoid expanding public surface area.

### Gate B Acceptance Criteria
- [x] External provider failure redirect generation is defined in one place.
- [x] Redirect URLs remain correct for both root deployments and deployments with a base path or forwarded prefix.

## Gate C - Tests
- [x] Extend `BuyAlan.Tests/Identity/IdentityBuilderExtensionsTests.cs` to cover the new failure redirect helper.
- [x] Add a unit test that invokes the configured Square `OnRemoteFailure` event and verifies:
- [x] redirect response is returned,
- [x] redirect location points to `/auth/external-callback?remoteError=external_provider_error`,
- [x] `HandleResponse()` prevents exception propagation,
- [x] `PathBase` is preserved when present.
- [ ] Run `BuyAlan.Tests` and confirm no regressions.

### Gate C Acceptance Criteria
- [x] Unit tests pass for both root-path and base-path callback failure redirects.
- [x] The Square provider registration is covered by a regression test for remote failure handling.

## Notes and Risks
- The failing URL is the OAuth provider callback `/auth/providers/square/callback`, not the subscription callback `/subscriptions/square/callback`.
- This fix must not introduce provider-specific open redirects; the failure target must remain the fixed internal auth callback route.
- The redirect-based recovery path already exists in `CompleteExternalLoginAsync`; the change should feed into that path rather than adding a second error-handling flow.
