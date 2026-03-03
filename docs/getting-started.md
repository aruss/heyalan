# Getting Started

This guide describes the fastest local path to run HeyAlan and access the core services.

## Prerequisites

- .NET SDK (matching the solution target)
- Node.js and Yarn (for `HeyAlan.WebApp`)
- Docker Desktop (for local infrastructure containers)

## setup git

   git config --global --add safe.directory .

## codex setup 

   codex mcp add chrome-devtools -- npx chrome-devtools-mcp@latest


## Local Configuration

1. Ensure local environment values are present in `.env` (solution root).
2. Set key runtime values such as:
   - `PUBLIC_BASE_URL`
   - connection strings required by AppHost-managed services
3. If using webhook testing, also configure ngrok values (see [Setup Webhooks](./setup-webhooks.md)).

## Run the Solution

From the repository root:

```powershell
dotnet watch run --project .\HeyAlan.AppHost\HeyAlan.AppHost.csproj
```

This starts the Aspire host and orchestrates local services.

## What to Open

- AppHost dashboard (from console output)
- Web app and API endpoints exposed by the local host environment

## Troubleshooting Pointers

- Configuration contract details: [Configuration Reference](./configuration-reference.md)
- Integration-specific credential setup: [Square App Setup](./square-app-setup.md)
- Webhook tunnel setup: [Setup Webhooks](./setup-webhooks.md)

