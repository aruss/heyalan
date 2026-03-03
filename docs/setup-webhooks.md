# Setup Webhooks

Use this guide to expose local `HeyAlan.WebApi` endpoints for Telegram and Twilio webhook testing.

## Local Tunnel via ngrok

HeyAlan AppHost supports ngrok-driven local webhook exposure.

Set these values in your local `.env`:

- `ENABLE_NGROK=true`
- `NGROK_AUTHTOKEN=<your-token>`
- `NGROK_DOMAIN=<your-reserved-domain>`
- `PUBLIC_BASE_URL=https://<your-reserved-domain>`

Example:

- `NGROK_DOMAIN=gobbler-bright-llama.ngrok-free.app`
- `PUBLIC_BASE_URL=https://gobbler-bright-llama.ngrok-free.app`

## Start the Environment

Run AppHost from the solution root:

```powershell
dotnet watch run --project .\HeyAlan.AppHost\HeyAlan.AppHost.csproj
```

When enabled, AppHost passes `PUBLIC_BASE_URL` to services that need callback/webhook URLs and starts ngrok with the configured reserved domain.

## Provider Configuration Notes

- Telegram webhook endpoint format: `/webhooks/telegram/{botToken}`
- Twilio webhook paths are configured under the WebApi webhook routes
- Ensure provider-side webhook URL points to your `PUBLIC_BASE_URL`

## Related Docs

- [Getting Started](./getting-started.md)
- [Square App Setup](./square-app-setup.md)
- [Configuration Reference](./configuration-reference.md)
