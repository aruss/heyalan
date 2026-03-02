# Square Developer

https://developer.squareup.com/console/en/apps

## OAuth App Setup

ShelfBuddy uses two Square app credential pairs:

- External auth provider flow (`/auth/*`):
  - `AUTH_SQUARE_CLIENT_ID`
  - `AUTH_SQUARE_CLIENT_SECRET`
  - Redirect URL: `<PUBLIC_BASE_URL>/auth/providers/square/callback`

- Subscription connection flow (onboarding/admin):
  - `SQUARE_CLIENT_ID`
  - `SQUARE_CLIENT_SECRET`
  - Redirect URL: `<PUBLIC_BASE_URL>/onboarding/square/connect/callback`

Square app redirects are single-URL per app, so these flows require separate Square apps.
