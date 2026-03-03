# M2 - Admin Dashboard (Tremor Shell)

## Summary
Build a basic Tremor-based admin dashboard in `HeyAlan.WebApp` with:
- Top bar (notifications dropdown + user menu dropdown)
- Sidebar with example navigation links
- Main content area with sample dashboard cards
- Minimal `/login` placeholder route so `/admin` redirect flow has a valid destination

## Gate A - Dependencies and Foundation
- [x] Verify latest package versions with `npm view <package> version` for all new dependencies
- [x] Install dependencies using exact versions via Yarn (`yarn add -E ...`)
- [x] Add Tremor-style utility helper in `src/lib/utils.ts`
- [x] Add required global dropdown animation/theme CSS in `src/app/globals.css`

## Gate B - UI Primitives
- [x] Add `src/components/ui/dropdown-menu.tsx` using Tremor/Radix pattern
- [x] Add `src/components/ui/button.tsx`
- [x] Add `src/components/ui/card.tsx`
- [x] Add `src/components/ui/badge.tsx`

## Gate C - Admin Dashboard Shell
- [x] Add `src/components/admin/admin-sidebar.tsx` with example links
- [x] Add `src/components/admin/admin-topbar.tsx` with notification and user dropdowns
- [x] Add `src/components/admin/admin-overview.tsx` with sample cards/widgets
- [x] Add `src/components/admin/admin-shell.tsx` composing sidebar/topbar/content
- [x] Add `src/app/admin/page.tsx` route rendering the shell

## Gate D - Auth Redirect Compatibility
- [x] Add `src/app/login/page.tsx` minimal placeholder page
- [x] Keep `src/proxy.ts` `/admin` auth guard unchanged
- [x] Confirm unauthenticated `/admin` redirects to `/login?returnUrl=...` (by route availability and unchanged guard logic)

## Gate E - Verification
- [x] Validate dropdown keyboard/click behavior implementation (Radix primitives)
- [x] Validate responsive layout implementation (desktop/mobile classes)
- [x] Validate strict typing (no `any` introduced)
- [x] Run non-mutating verification checks (type/lint)

## Notes
- Installed packages (exact versions): `@radix-ui/react-dropdown-menu@2.1.16`, `@remixicon/react@4.9.0`, `clsx@2.1.1`, `tailwind-merge@3.5.0`.
- Menu actions are placeholder-only by design in this milestone.
