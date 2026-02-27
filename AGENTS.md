# Repository Guidelines

## SquareBuddy Overview
An autonomous AI agent that knows your inventory. Engage customers via Text, WhatsApp, and Telegram. Drive upsells, process payments, and schedule shipping - all through natural language.

## RFC 2119 Language
All instructions in this document use RFC-style terms (MUST, MUST NOT, SHOULD, SHOULD NOT, MAY) and must be interpreted accordingly.

## Terms 
- Solution: the Visual Studio Solution containing all SquareBuddy projects, ./SquareBuddy.slnx 
- Project: a .NET project within the SquareBuddy solution, e.g. SquareBuddy.WebApp, SquareBuddy.WebApi, SquareBuddy.Initializer, SquareBuddy.Data, SquareBuddy

## Package Documentation & External References
- You MUST use the context7 MCP server to search documentation of external packages and libraries before making assumptions.
- You MUST prefer context7 for Helm chart values and external service docs when relevant.
- You MAY use context7 for upgrade planning and compatibility checks.
- You MAY use the helm CLI to inspect chart values and documentation when needed.

## Project Structure & Module Organization
- SquareBuddy uses the Aspire host to run in development mode.

## Security & Foresight
- You MUST prioritize security, privacy, and least-privilege access.
- You MUST avoid logging PII or sensitive data and SHOULD call out any existing logging that violates this.
- You SHOULD think ahead about data boundaries, multi-tenant isolation, and upgrade impact when changing code or infrastructure.

## Coding Style & Naming Conventions
- You MUST first read the ./docs/CODE_GUIDELINE_DOTNET.md when writing .net C# code
- You MUST first read the ./docs/CODE_GUIDELINE_NODE.md when writing node/typescript code
- You MUST first read the project README before working in that project (e.g., `SquareBuddy.WebApp/README.md`, `SquareBuddy.WebApi/README.md`, `SquareBuddy.Initializer/README.md`, `SquareBuddy.Data/README.md`, `SquareBuddy/README.md`)

## Dependencies & Package Management
- When working with `package.json` dependencies or making upgrades, you MUST use `npm view` to check the latest package version.
- When working with NuGet dependencies or making upgrades, you MUST check the latest package version on `nuget.org`.
- You MAY use context7 for package documentation when upgrading or when behavior is unclear.

## Milestones, Gates, and Scratchpads
- We usually work in milestones (M1, M2, M2.5, M3, ...) with gates (Gate A, Gate B, ...) that include tickable checklists (`[ ]`) by default in `docs/milestones/*.md`.
- A milestone MAY have its own scratchpad for notes and decisions, e.g. `docs/milestones/M1-identity-scratchpad.md`.
- Default work mode: when the developer explicitly requests work without referring to a milestone, you MUST proceed without creating or updating milestone artifacts unless asked. In this mode, you MUST tick off tasks you completed in existing milestones without waiting for additional confirmation from the developer.
- Working mode: creating/refining milestones. When the developer asks for a new milestone (or refers to this mode), you MUST act as a sparring partner to refine the feature before drafting the milestone file. You SHOULD propose a draft, ask clarifying questions, and iterate on scope until the developer is satisfied. Once approved, you MUST flesh out the milestone (gates with `[ ]` checklists) and initialize an empty scratchpad.

## Agent Expectations
- You MUST behave like an expert in .NET, TypeScript and DevOps, with strong architecture and software design skills.
- You SHOULD transform unclear or legacy code into readable, maintainable code while preserving behavior.
- You MUST report any unusual findings, housekeeping opportunities, or side quests discovered during implementation and SHOULD propose follow-up actions when relevant.

## Out of Scope for Agents
- You MUST NOT use `package.json` scripts such as `build`, `dev`, etc.
- You MUST NOT start `docker-compose` files.
- After changing the database schema, you MUST stop and hand off so the developer can create migrations and run `dotnet ef migrations add Init --context MainDataContext -o .\Migrations` from `SquareBuddy.Initializer`. Resume only after explicit confirmation.
- You MUST NOT create git commits. You MAY use git to review history.
- `swagger.json` files are auto-generated and derived from API annotations in the NestJS backend. You MUST NOT edit them manually.
- You MUST NOT touch `.gen.ts` files as they are auto-generated.
- You MUST NOT edit `.env` or `.env.example` files without reading their existing contents first; you MUST preserve existing keys and secrets when adding new entries.
