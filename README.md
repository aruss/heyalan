# SquareBuddy

Autonomous conversational commerce middleware integrated with Square.

**Core Architecture & I/O**
- Supported Protocols: SMS, WhatsApp, Telegram.
- Data Synchronization: Real-time ingestion of Square inventory and product catalog metadata.
- Compliance: Hardcoded opt-in/opt-out state handling (Zero-Spam Policy).

**Customer-Facing Execution**
- NLP Query Resolution: Processes complex inquiries regarding material specifications, sizing, and real-time availability.
- Algorithmic Upselling: Dynamic recommendation engine for complementary items and alternatives.
- Transaction Processing: Secure, in-thread checkout and billing execution.
- Logistics Automation: Modifies delivery parameters, schedules shipping, and dispatches real-time tracking webhooks.

**Admin Control Plane**
- Manual Override (Takeover Protocol): Live session monitoring with instantaneous agent-pause functionality for human intervention.
- Attribution Telemetry: Direct mapping of chat histories to specific transaction IDs and live order states.
- Behavioral Tuning: Modifiable LLM parameters dictating conversational tone, upselling aggressiveness, and fallback thresholds.

## Technology
- .NET (ASP.NET Core, EF Core)
- Next.js (App Router, React)
- Tailwind CSS
- OpenTelemetry
- Docker

## Data Migrations
Run inside the `SquareBuddy.Initializer` project folder:

    dotnet ef migrations add Init --context MainDataContext -o .\Migrations


## Run following to start the 3rd party services (now includes observability)

    docker-compose -f docker-compose.local.yaml -p squarebuddy up -d

## Run Project 

    dotnet watch run --project .\SquareBuddy.AppHost\SquareBuddy.AppHost.csproj
