# Square App Setup

ShelfBuddy uses two separate Square app credential pairs because Square apps support a single redirect URL per app.

## Why Two Square Apps

ShelfBuddy has two distinct OAuth flows:

1. External sign-in flow (`/auth/*`)
2. Subscription connection flow (onboarding/admin)

To keep redirect URLs correct and isolated, each flow uses its own app credentials.

## Credential Sets

### External auth provider flow

- `AUTH_SQUARE_CLIENT_ID`
- `AUTH_SQUARE_CLIENT_SECRET`
- Redirect URL:
  - `<PUBLIC_BASE_URL>/auth/providers/square/callback`

### Subscription connection flow

- `SQUARE_CLIENT_ID`
- `SQUARE_CLIENT_SECRET`
- Redirect URL:
  - `<PUBLIC_BASE_URL>/onboarding/square/connect/callback`

## Setup Checklist

1. Create or configure two Square apps in the Square Developer Console.
2. Assign the correct redirect URL to each app.
3. Populate the matching environment variables.
4. Confirm `PUBLIC_BASE_URL` is the externally reachable base URL used for callbacks.

Square Developer Console:
- https://developer.squareup.com/console/en/apps

## Related Docs

- [Configuration Reference](./configuration-reference.md)
- [Getting Started](./getting-started.md)
