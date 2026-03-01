# Milestone M9: Admin Sidebar Authenticated User Profile via Session Context

## Goal
Display the current authenticated user in the admin sidebar profile menu using the generated API client (`/auth/me`), with session state provided through a mounted session context in the admin route group.

## Scope
- **Frontend (`ShelfBuddy.WebApp`)**
  - Mount query/session providers for `/admin`.
  - Load authenticated user from generated `getAuthMeOptions()`.
  - Bind sidebar user profile button/dropdown to session user data.
  - Apply explicit display-name/email fallback rules.

## Non-Goals (Out of Scope)
- Changing backend `/auth/me` contract.
- Adding new auth redirect logic from profile components.
- Expanding session error UX beyond minimal fallback.
- Adding test coverage in this pass.

## User Decisions (Locked)
- [x] Provider mounting scope is **admin-only**.
- [x] Use **generated API client** and generated types (`CurrentUserResult`).
- [x] Profile fields:
  - [x] Button uses display name when meaningful.
  - [x] Dropdown label uses email.
  - [x] If `displayName` equals `email`, show only email in the button.
- [x] Loading state uses **placeholder/skeleton-like UI**.
- [x] No additional error UX work now ("don't worry about it now").
- [x] No tests added in this milestone pass.

## Findings from Repository Analysis

### Existing implementation bits already present
- [x] `SessionProvider` exists and already uses generated `getAuthMeOptions()`:
  - `ShelfBuddy.WebApp/src/lib/session-context.tsx`
- [x] `ReactQueryProvider` exists:
  - `ShelfBuddy.WebApp/src/lib/react-query-provider.tsx`
- [x] Generated auth query options and endpoint client exist:
  - `ShelfBuddy.WebApp/src/lib/api/@tanstack/react-query.gen.ts`
  - `ShelfBuddy.WebApp/src/lib/api/types.gen.ts`
  - `ShelfBuddy.WebApp/src/lib/api/sdk.gen.ts`
- [x] Backend `/auth/me` exists and returns `CurrentUserResult`:
  - `ShelfBuddy.WebApi/Identity/IdentityEndpoints.cs`
  - `ShelfBuddy.WebApi/Identity/CurrentUserResult.cs`

### Gaps identified
- [x] `SessionProvider` is **not mounted** anywhere.
- [x] `ReactQueryProvider` is **not mounted** anywhere.
- [x] Sidebar profile UI is hardcoded with static user values:
  - `ShelfBuddy.WebApp/src/components/admin/ui/navigation/UserProfile.tsx`
  - `ShelfBuddy.WebApp/src/components/admin/ui/navigation/DropdownUserProfile.tsx`

### Current auth/session routing context
- [x] Admin route gating is handled in `proxy.ts` with cookie + `/auth/me` check before allowing `/admin`.
- [x] Unauthorized redirect interceptor exists in `hey-api-client-auth.ts`.
- [x] Admin layout currently mixes server and client concerns and does not include query/session providers.

## Architecture/Design Decisions (Locked)
- [x] Keep `/admin/layout.tsx` as a **server component** to read sidebar cookie state.
- [x] Introduce a dedicated **client shell component** for admin provider composition.
- [x] Compose providers in this order (inside admin shell):
  - [x] `ThemeProvider`
  - [x] `ReactQueryProvider`
  - [x] `SessionProvider`
  - [x] `BreadcrumbProvider`
  - [x] `SidebarProvider`
- [x] Session context user type is generated `CurrentUserResult` (not local manual type guard).
- [x] No generated files (`*.gen.ts`) will be edited.

## UI Behavior Contract (Authoritative)

### Profile button text
- [x] Resolve a `displayLabel` from session user:
  - [x] If `displayName` is empty -> use `email`.
  - [x] If normalized `displayName` equals normalized `email` -> use `email`.
  - [x] Else use `displayName`.

### Dropdown label
- [x] Show `email` when available.
- [x] If unavailable (no user), show neutral fallback text.

### Loading behavior
- [x] Show placeholder avatar + `"Loading..."` in profile button until query resolves.

### No-user/error behavior (current pass)
- [x] Show neutral fallback profile label (`"Account"`), keep menu usable.
- [x] Keep existing sign-out action.
- [x] Do not implement additional retry/error controls now.

## Implementation Plan by Gate

## Gate A: Session Context Type Alignment
- [x] Update `ShelfBuddy.WebApp/src/lib/session-context.tsx`:
  - [x] Replace local `SessionUser` with generated `CurrentUserResult` import from `@/lib/api`.
  - [x] Keep existing query pattern using `getAuthMeOptions()` and `retry: false`.
  - [x] Keep `refresh()`, `isLoading`, and `errorMessage` contract.
  - [x] Remove manual runtime shape guard no longer needed with generated type contract.

## Gate B: Admin Provider Mounting
- [x] Add client component (new file) for admin provider composition, e.g.:
  - [x] `ShelfBuddy.WebApp/src/app/admin/admin-shell.tsx`
- [x] Move client-only provider tree and sidebar/inset rendering into admin shell.
- [x] Keep `defaultOpen` as prop from server layout.
- [x] Update `ShelfBuddy.WebApp/src/app/admin/layout.tsx`:
  - [x] Keep server-side cookies read.
  - [x] Render admin shell with `defaultOpen`.
  - [x] Preserve existing font/html/body attributes.

## Gate C: Sidebar Profile Data Binding
- [x] Update `ShelfBuddy.WebApp/src/components/admin/ui/navigation/UserProfile.tsx`:
  - [x] Consume `useSession()`.
  - [x] Replace hardcoded initials/name with resolved session-driven values.
  - [x] Implement deterministic display-label and initials helpers.
  - [x] Render loading placeholder state.
  - [x] Render neutral fallback when no session user.
- [x] Update `ShelfBuddy.WebApp/src/components/admin/ui/navigation/DropdownUserProfile.tsx`:
  - [x] Accept `emailLabel?: string | null` prop.
  - [x] Replace hardcoded email label with prop/fallback text.
  - [x] Keep existing theme submenu and sign-out behavior unchanged.

## Gate D: Regression Verification
- [ ] Manually verify authenticated admin flow:
  - [ ] Loading placeholder appears then resolves to real profile data.
  - [ ] `displayName != email` => button shows display name, dropdown shows email.
  - [ ] `displayName == email` => button shows email only.
  - [ ] Sign out still calls `/api/auth/logout` and redirects to `/login`.
  - [ ] Sidebar/theme interactions unchanged.

## Public Interface / Type Changes
- [x] Session context contract update:
  - [x] `currentUser` type becomes `CurrentUserResult | null`.
- [x] Dropdown component contract update:
  - [x] `DropdownUserProfileProps` adds `emailLabel?: string | null`.

## Acceptance Criteria
- [x] Admin sidebar profile renders authenticated user from generated `/auth/me` client.
- [x] Session state is provided via mounted session context in admin route group.
- [x] Display-name/email rule is implemented exactly as specified.
- [x] Loading placeholder is shown before session resolves.
- [x] Hardcoded profile identity values are removed.
- [x] Existing logout/theme behavior remains intact.
- [x] No `.gen.ts` files are modified.

## Risks / Notes
- [ ] Provider mounting in admin layout introduces client shell refactor; ensure no hydration regressions.
- [ ] Keep changes localized to admin route group to avoid unnecessary session fetching on public pages.
- [ ] Existing global unauthorized behavior already exists; avoid duplicating redirect logic in profile components.

## Handoff Notes for New Session
- [ ] Start from Gate A through Gate D in order.
- [ ] Do not change backend contracts for this milestone.
- [ ] If additional UX for session error/retry is desired later, treat as a follow-up milestone extension.
