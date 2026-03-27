# Square App Setup

BuyAlan uses one Square app and one shared credential pair for both external sign-in and subscription connection.

## Why One Square App

BuyAlan has two distinct OAuth flows:

1. External sign-in flow (`/auth/*`)
2. Subscription connection flow (onboarding/admin)

Both flows now broker through the same public callback URL and route internally based on the OAuth `state` value. This removes the need for a second Square app and avoids proxy-sensitive callback reconstruction in production.

## Credential Set

- `SQUARE_CLIENT_ID`
- `SQUARE_CLIENT_SECRET`
- Redirect URL:
  - `<PUBLIC_BASE_URL>/api/subscriptions/square/callback`

## Broker Behavior

- Square redirects to `<PUBLIC_BASE_URL>/api/subscriptions/square/callback`.
- BuyAlan completes the subscription connect flow when `state` is a protected Square connect payload.
- BuyAlan forwards other Square auth callbacks internally to `/auth/providers/square/callback`.

## Setup Checklist

1. Create or configure one Square app in the Square Developer Console.
2. Assign `<PUBLIC_BASE_URL>/api/subscriptions/square/callback` as the Square redirect URL.
3. Populate `SQUARE_CLIENT_ID` and `SQUARE_CLIENT_SECRET`.
4. Confirm `PUBLIC_BASE_URL` is the externally reachable base URL used for callbacks.

Square Developer Console:
- https://developer.squareup.com/console/en/apps

## Related Docs

- [Configuration Reference](./configuration-reference.md)
- [Getting Started](./getting-started.md)
