# SendGrid Setup

## What is `SENDGRID_NEWSLETTER_LIST_ID`
It is the SendGrid Marketing Contacts list ID used for confirmed newsletter subscribers.

Set:
- `SENDGRID_API_KEY`
- `SENDGRID_EMAIL_FROM`
- `SENDGRID_TEMPLATE_IDENTITY_CONFIRMATION_LINK`
- `SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_LINK`
- `SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_CODE`
- `SENDGRID_TEMPLATE_NEWSLETTER_CONFIRMATION`
- `SENDGRID_NEWSLETTER_LIST_ID`
- `NEWSLETTER_CONFIRM_TOKEN_TTL_MINUTES` (optional, defaults to `1440`)

## Create a List in the UI
1. Open SendGrid.
2. Go to `Marketing` -> `Contacts`.
3. Create a new list (example: `HeyAlan Newsletter`).
4. Save.

## Create a List via API
```bash
curl -X POST "https://api.sendgrid.com/v3/marketing/lists" \
  -H "Authorization: Bearer $SENDGRID_API_KEY" \
  -H "Content-Type: application/json" \
  -d "{\"name\":\"HeyAlan Newsletter\"}"
```

The response contains the new list `id`.

## Get List ID
```bash
curl -X GET "https://api.sendgrid.com/v3/marketing/lists" \
  -H "Authorization: Bearer $SENDGRID_API_KEY"
```

Find your list by `name` and use the matching `id` as `SENDGRID_NEWSLETTER_LIST_ID`.

## Configure Transactional Templates
Create dynamic transactional templates in SendGrid for:
- newsletter confirmation with `confirmation_url`
- identity confirmation link with `confirmation_url`
- identity password reset link with `reset_url`
- identity password reset code with `reset_code`

Use the template IDs (`d-...`) as:
- `SENDGRID_TEMPLATE_NEWSLETTER_CONFIRMATION`
- `SENDGRID_TEMPLATE_IDENTITY_CONFIRMATION_LINK`
- `SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_LINK`
- `SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_CODE`

Example template body link:
```html
<a href="{{confirmation_url}}">Confirm newsletter subscription</a>
```

## Multi-App Setup
If one SendGrid account is shared by multiple apps, create one list per app (for example `HeyAlan Newsletter`, `Foo Newsletter`) and set each app to its own list ID.
