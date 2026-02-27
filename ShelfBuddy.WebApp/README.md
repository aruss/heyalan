# ShelfBuddy.WebApp

Web frontend that contains a landing page and a dashboard.

**Tech Stack**
- Next.js (App Router)
- React + TypeScript
- Tailwind CSS
- OpenTelemetry instrumentation
- Playwright (e2e)
- ESLint

**Project Structure**
- `src/app` routes (`/` landing, `/admin` dashboard, `/login`, `/register`)
- `src/components` UI composition
- `src/lib` shared helpers
- `src/instrumentation.ts` and `src/instrumentation.otel.ts` for telemetry setup

**Architecture Decisions**
- TBD (tracked here for future ADR-style notes)

