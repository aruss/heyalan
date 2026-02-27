# M3 - Breadcrumbs: Route Map + Context Override

## Summary
Make breadcrumb titles render immediately for admin routes using a static route map, and allow pages to override via `BreadcrumbProvider` when dynamic data (e.g., customer names) becomes available. Use static fallback labels for dynamic routes until overrides arrive.

## Gate A - Route Map Foundation
- [ ] Add admin breadcrumb map module (`ShelfBuddy.WebApp/src/lib/admin-breadcrumbs.ts`) with a named export for resolving items from a pathname.
- [ ] Ensure static admin routes (`/admin/home`, `/admin/inbox`, `/admin/settings`) resolve to their existing breadcrumb items.
- [ ] Include static fallback labels for dynamic routes (e.g., `Customer`).

## Gate B - Breadcrumbs Component Update
- [ ] Update `ShelfBuddy.WebApp/src/components/admin/ui/navigation/Breadcrumbs.tsx` to derive items from pathname using the route map.
- [ ] If context items are present, use them as an override; otherwise, use route-map items.
- [ ] Keep the title derived from the last breadcrumb item (same behavior).

## Gate C - Dynamic Override Support
- [ ] Keep `BreadcrumbProvider` in layout and expose `useBreadcrumbs` for overrides.
- [ ] Document how pages should call `setItems` when dynamic data is ready (e.g., after fetch).

## Gate D - Page Cleanup
- [ ] Remove breadcrumb `useEffect` setters from:
  - `ShelfBuddy.WebApp/src/app/admin/home/page.tsx`
  - `ShelfBuddy.WebApp/src/app/admin/inbox/page.tsx`
  - `ShelfBuddy.WebApp/src/app/admin/settings/page.tsx`

## Gate E - Verification
- [ ] Manual check: `/admin/home`, `/admin/inbox`, `/admin/settings` show title immediately on navigation.
- [ ] Manual check: dynamic page shows static fallback first, then updates to dynamic title when override arrives.

## Notes
- The override mechanism should not introduce blocking/hydration delays.
