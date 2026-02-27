# ShelfBuddy

ShelfBuddy is an autonomous AI sales and support agent for merchants using Square.
It engages customers over messaging channels (SMS, WhatsApp, and Telegram), helps drive upsells, and supports payment, order, and shipping workflows through natural conversation.

## System Overview

ShelfBuddy is a multi-tenant platform built around a subscription account model.

Each `Subscription` represents one tenant account and includes:
- Team members (`SubscriptionUsers`) with roles (Owner, Member)
- Billing and credit metadata for the ShelfBuddy plan
- A list of configured agents (data model supports multiple agents; current UI supports one)

## Integrations and Runtime Communication

ShelfBuddy currently integrates with:
- **Square**: product/catalog context, inventory awareness, and order-management workflows the agent can act on
- **Twilio**: messaging channel integration (including SMS and WhatsApp delivery paths)
- **Telegram**: messaging channel integration
- **Stripe**: billing for ShelfBuddy subscription plans (platform billing only)

For dashboard runtime communication, ShelfBuddy uses **SignalR** to stream real-time events, including:
- Chat message updates
- Notifications and activity events

## Admin and Operations

The dashboard is the control plane for operators and team members:
- Subscription-level team access and collaboration
- Agent configuration and prompt management
- Channel credential and endpoint configuration
- Human takeover and manual intervention when needed
- Live updates for conversations and operational notifications via SignalR

## POC vs Future Onboarding

Current POC onboarding is manual:
- Merchant provides required phone numbers and bot tokens
- Merchant configures Twilio and Telegram in their own accounts

Future onboarding is planned to be automated:
- Platform-managed channel provisioning and setup
- Reduced manual provider configuration for merchants

## Technology
- .NET (ASP.NET Core, EF Core)
- Next.js (App Router, React)
- Tailwind CSS
- SignalR
- OpenTelemetry
- Docker

## Run Project

    dotnet watch run --project .\ShelfBuddy.AppHost\ShelfBuddy.AppHost.csproj

## Local Webhooks via ngrok

To expose local `webapi` for Telegram/Twilio webhooks through Aspire:

1. Set `ENABLE_NGROK=true` in your local `.env`.
2. Set `NGROK_AUTHTOKEN=<your-token>` in your local `.env`.
3. Set `NGROK_DOMAIN=<your-reserved-domain>` (for example `gobbler-bright-llama.ngrok-free.app`).
4. Set `PUBLIC_BASE_URL=https://<your-reserved-domain>`.
5. Start AppHost.

AppHost passes `PUBLIC_BASE_URL` to both `initializer` and `webapi` as environment variables.
When ngrok is enabled, AppHost runs ngrok with `--url=<NGROK_DOMAIN>` so that exact domain is brought online.
Note: the tunnel targets `http://host.docker.internal:5000` (WebApi dev URL from launch settings).
