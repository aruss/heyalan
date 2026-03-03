# Milestone M14: Onboarding Guard And Sidebar Reminder

## Goal
Prevent already-onboarded users from accessing onboarding pages (including browser back navigation), expose onboarding state via `CurrentUserResult.isOnboarded`, and surface a clear admin sidebar reminder for users who still need to complete onboarding.

## Scope
- **Backend (`HeyAlan.WebApi`)**
  - Extend `/auth/me` payload with `isOnboarded`.
  - Compute onboarding completion from current onboarding state.
- **Frontend (`HeyAlan.WebApp`)**
  - Guard `/onboarding` in proxy routing.
  - Redirect onboarded users from onboarding to `/admin`.
  - Show `Proceed onboarding` callout above user profile in admin sidebar when not onboarded.

## Non-Goals
- Forcing non-onboarded users out of `/admin`.
- Changing onboarding completion semantics beyond existing required steps.
- Editing auto-generated `*.gen.ts` files manually.

## User Decisions (Locked)
- [x] Onboarded user opening `/onboarding` is redirected to `/admin`.
- [x] Sidebar reminder is a clickable box linking to `/onboarding`.

## Gate A: Backend Contract + Onboarding Flag
- [x] Add `IsOnboarded` to `CurrentUserResult`.
- [x] Update `GET /auth/me` to include onboarding status.
- [x] Keep onboarding truth aligned with existing completed onboarding state logic.

## Gate B: Route Guard For Onboarding
- [x] Extend proxy matcher to include `/onboarding/:path*`.
- [x] Require auth cookie + successful `/auth/me` for onboarding route access.
- [x] Redirect onboarded users away from onboarding to `/admin`.
- [x] Preserve existing `/admin` and `/api` proxy behavior.

## Gate C: Sidebar Reminder UX
- [x] Read session user onboarding state in `AppSidebar`.
- [x] Render `Proceed onboarding` box above the user profile menu when `isOnboarded !== true`.
- [x] Keep profile dropdown and existing sidebar behavior intact.

## Gate D: Verification
- [ ] Automated .NET tests pass for touched areas.
- [ ] Manual verification of back-navigation and sidebar reminder in browser.

## Implementation Notes
- Added a client-side defensive redirect in onboarding page to `/admin` when session indicates onboarded.
- Session context was extended with optional `isOnboarded` for compatibility until regenerated OpenAPI client includes the new field.

## Handoff Notes
- Because `/auth/me` contract changed, regenerate webapp client from updated OpenAPI after backend is running:
  - `yarn openapi-ts`
- Re-run frontend type checks/e2e once regenerated.
