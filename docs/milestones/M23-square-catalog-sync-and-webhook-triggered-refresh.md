# Milestone M23: Square Catalog Sync Cache + Webhook-Triggered Refresh

## Summary
Build a subscription-scoped Square catalog cache so HeyAlan uses local product data instead of calling Square in customer-message processing paths.

This milestone gives the system a local product catalog, sync state, operational sync controls, and agent-level sellability boundaries. The synced subscription catalog is shared across all agents, while each agent can either:
- sell all products by default, or
- sell a filtered subset of assigned products.

This milestone also introduces zip-code-based regional restriction data:
- each agent can define an allowlist of zip codes it can sell to,
- customer address-to-zip lookup happens upstream and is out of scope here,
- M23 stores and exposes the agent zip allowlist,
- M23 does not enforce runtime zip-based sellability.

M23 is a prerequisite milestone. It does not own LLM response generation, conversation-state collection, checkout orchestration, or final order creation behavior.

## Dependencies and Current Codebase Baseline
- [ ] Existing Square connection and token lifecycle remain owned by `ISquareService`.
- [ ] Existing incoming message processing still uses buffered placeholder business logic in `HeyAlan/Messaging/IncomingMessageConsumer.cs`.
- [ ] Existing agent model has no product-assignment or sales-zip fields; M23 introduces those through relation tables.
- [ ] Existing inventory settings UI is a placeholder in `HeyAlan.WebApp/src/app/admin/settings/inventory/page.tsx`.
- [ ] Existing Square OAuth connection already uses the current broader v1 commerce scopes; M23 must not require additional scopes beyond that baseline.

## User Decisions (Locked)
- [x] Service split is explicit:
  - [x] `ISubscriptionCatalogSyncService` fetches from Square and persists local cache.
  - [x] `ISubscriptionCatalogReadService` reads synced catalog data internally.
  - [x] `ISquareService` remains token lifecycle only.
- [x] Sync trigger strategy is hybrid:
  - [x] Initial full sync on successful Square connection persistence.
  - [x] Periodic incremental sync every 15 minutes.
  - [x] Manual sync endpoint.
  - [x] Webhook-triggered sync for catalog updates.
- [x] Catalog storage shape is normalized minimal for agent usage.
- [x] Catalog scope for v1 is sellable product data (`ITEM` + `ITEM_VARIATION` focused).
- [x] Location policy is per-location records only.
- [x] M23 does not choose an authoritative selling location for checkout/order creation.
- [x] v1 is catalog-only (no inventory quantity/count sync).
- [x] Webhook timer policy: accepted catalog webhook resets periodic timer for that subscription.
- [x] Agent catalog access mode uses implicit relation state:
  - [x] No product assignments for agent => all products are accessible.
  - [x] One or more assignments => only assigned products are accessible.
- [x] Admin API for agent-product access is agent-scoped.
- [x] Regional restriction model is agent zip allowlist based.
- [x] Zip matching rule is exact normalized match.
- [x] Empty agent zip allowlist means no regional restriction metadata.
- [x] Runtime zip filtering is deferred to later runtime/skill milestones and is not implemented in M23 catalog read paths.
- [x] Product-to-zip association model is intersection-based and indirect:
  - [x] Product eligibility comes from agent product access mode.
  - [x] Zip eligibility comes from agent zip allowlist mode.
  - [x] Final eligibility remains `productAllowed && zipAllowed` in later enforcement milestones.
- [x] Empty or stale cache must not force synchronous Square reads in message processing paths.

## Architecture Notes
- [ ] All runtime-adjacent agent/product lookups MUST read local DB cache only; no per-message Square API calls.
- [ ] Sync implementation MUST reuse `ISquareService.GetValidAccessTokenAsync(subscriptionId)`.
- [ ] Sync runs MUST be single-flight per subscription and idempotent.
- [ ] Webhook processing MUST be durable, deduplicated, and async with fast acknowledgment via Wolverine.
- [ ] Logging MUST avoid tokens, signatures, and unnecessary PII.
- [ ] Agent catalog reads MUST be agent-aware.
- [ ] Product-to-agent assignment MUST be isolated by subscription boundary.
- [ ] Runtime product filtering in M23 MUST apply agent product access rules only.
- [ ] Runtime zip filtering MUST NOT be implemented in M23 read paths.
- [ ] M23 MUST add clear seams for current message processing and later LLM runtime context building, without claiming to implement final autonomous reply orchestration.

## Runtime Availability Resolution (Authoritative)
- [ ] Inputs:
  - [ ] `subscriptionId`
  - [ ] `agentId`
  - [ ] candidate product(s) from synced subscription catalog
- [ ] Product permission evaluation:
  - [ ] If agent has zero product assignments, all active catalog products are product-allowed.
  - [ ] If agent has one or more product assignments, only assigned products are product-allowed.
- [ ] Zip restriction data model for future enforcement:
  - [ ] If agent has zero zip allowlist entries, all zip codes are allowed for future enforcement.
  - [ ] If agent has one or more zip allowlist entries, future enforcement requires exact normalized match.
- [ ] M23 enforcement boundary:
  - [ ] M23 does not execute runtime zip filtering in catalog read services.
  - [ ] Zip-based sell eligibility enforcement belongs to later runtime/skill milestones.
- [ ] Cache availability behavior:
  - [ ] If cache is empty or stale, message processing must continue without blocking on a Square catalog call.
  - [ ] M23 may enqueue a background sync or surface stale-state diagnostics.
- [ ] Mapping note:
  - [ ] There is no direct zip->product relation in this milestone.
  - [ ] M23 stores independent agent-product and agent-zip rules for later intersection.

## Gate A - Data Model Foundation (Catalog Cache + Sync State + Webhook Dedupe)
- [x] Add `SubscriptionCatalogSyncState` entity keyed by subscription:
  - [x] Last successful watermark (`LastSyncedBeginTimeUtc` or equivalent)
  - [x] `NextScheduledSyncAtUtc`
  - [x] `LastSyncStartedAtUtc`
  - [x] `LastSyncCompletedAtUtc`
  - [x] `LastTriggerSource` (`periodic|webhook|manual|connect`)
  - [x] `SyncInProgress`
  - [x] `PendingResync`
  - [x] Last error code/message (sanitized)
- [x] Add `SubscriptionCatalogProduct` normalized variation-level cache entity:
  - [x] `SubscriptionId`
  - [x] `SquareItemId`
  - [x] `SquareVariationId`
  - [x] Item/variation names
  - [x] Description
  - [x] SKU
  - [x] Base price/currency
  - [x] Sellable/deleted flags
  - [x] Square `UpdatedAt` / `Version`
  - [x] Search text column for internal lookup
- [x] Add `SubscriptionCatalogProductLocation` entity for per-location overrides:
  - [x] `SubscriptionId`
  - [x] `SquareVariationId`
  - [x] `LocationId`
  - [x] Effective price override
  - [x] Availability/sold-out flags from catalog location fields
- [x] Add `SquareWebhookReceipt` dedupe entity:
  - [x] Unique `EventId`
  - [x] Event type
  - [x] Merchant id
  - [x] Received at
  - [x] Processed status
- [x] Add `AgentCatalogProductAccess` relation entity:
  - [x] `AgentId`
  - [x] `SubscriptionId`
  - [x] Catalog product key (`SubscriptionCatalogProductId` or stable product key)
  - [x] `CreatedAt`
  - [x] `UpdatedAt`
- [x] Add `AgentSalesZipCode` relation entity:
  - [x] `AgentId`
  - [x] `SubscriptionId`
  - [x] `ZipCodeNormalized`
  - [x] `CreatedAt`
  - [x] `UpdatedAt`
- [x] Enforce relation constraints/indexes:
  - [x] Unique assignment per agent/product pair
  - [x] Unique zip entry per agent/zip pair
  - [x] Tenant-safe indexes with `SubscriptionId` prefix
  - [x] Prevent cross-subscription assignment joins
- [x] Register new `DbSet<>` mappings and indexes/constraints in `MainDataContext`.
- [x] Add tenant-safe composite indexes (`SubscriptionId` first) for all read paths.
- [x] Stop and hand off for migration creation/run per repository rule.

### Gate A Acceptance Criteria
- [x] Schema supports incremental sync watermarking, schedule reset, and webhook dedupe.
- [x] No cross-subscription reads are possible by model/query shape.
- [x] Schema supports agent default-all and filtered-subset product access semantics.
- [x] Schema supports exact zip allowlist storage per agent.

## Gate B - Catalog Sync Services (Square Pull + Local Upsert)
- [x] Introduce `ISubscriptionCatalogSyncService` contract and implementation in `HeyAlan`.
- [x] Introduce `ISubscriptionCatalogReadService` for internal read/query use by current message processing paths and later agent runtime flows.
- [x] Extend `ISubscriptionCatalogReadService` with agent-aware reads:
  - [x] Agent-aware list/search/get operations take `subscriptionId` + `agentId`.
  - [x] Default mode behavior: no assignments for agent returns all active products.
  - [x] Filter mode behavior: assigned rows restrict returned products.
  - [x] Agent zip allowlist data is stored for future enforcement but is not applied by M23 catalog read methods.
  - [x] Internal reads expose or can be paired with freshness metadata through sync-state diagnostics.
- [x] Implement sync modes:
  - [x] Full sync (initial connect/manual force)
  - [x] Incremental sync (`begin_time` from watermark)
- [x] Use Square SDK Catalog API (`client.Catalog.SearchAsync`) with:
  - [x] `cursor` pagination loop
  - [x] `object_types` constrained for v1 scope
  - [x] `include_deleted_objects = true` for incremental updates
  - [x] `limit` configured within API bounds
- [x] Map Square catalog objects to normalized entities:
  - [x] Flatten item + variation fields
  - [x] Persist per-location overrides
  - [x] Handle deletes as tombstone or soft-delete semantics
- [x] Implement idempotent upsert/delete behavior.
- [x] Enforce one sync at a time per subscription.
- [x] Update sync state (`last*`, watermark, errors, trigger source).
- [x] Ensure sync does not clear or mutate agent-product assignments or zip allowlists.
- [x] Register services in DI via builder extensions.

### Gate B Acceptance Criteria
- [x] A connected subscription can complete full and incremental sync without duplicate rows.
- [x] Sync failures are recorded with deterministic error codes and no secret leakage.
- [x] Agent-aware reads correctly enforce default-all and filtered-subset behavior.
- [x] Agent zip allowlist data is preserved independently of catalog sync behavior.

## Gate C - Triggering and Scheduling (15-Minute Cadence + Reset Semantics)
- [x] Add durable message contract `SquareCatalogSyncRequested`.
- [x] Add Wolverine consumer that executes sync by subscription id.
- [x] Add periodic background scheduler (1-minute tick is sufficient) that:
  - [x] selects subscriptions where `UtcNow >= NextScheduledSyncAtUtc`
  - [x] enqueues periodic sync
- [x] Implement reset semantics:
  - [x] on accepted webhook event: set `NextScheduledSyncAtUtc = UtcNow + 15m`
  - [x] on periodic run start: set next due to `UtcNow + 15m`
  - [x] if sync is in progress and a trigger arrives: set `PendingResync = true`
  - [x] after run completes, if `PendingResync` is true: enqueue one follow-up run and clear flag
- [x] Add Square connection hook:
  - [x] after successful Square connection persistence, enqueue full sync for that subscription
  - [x] do not assume a separate reconnect endpoint or reconnect completion flow exists in current code
- [x] Add manual sync API endpoint:
  - [x] `POST /subscriptions/{subscriptionId}/square/catalog/sync`
  - [x] auth + subscription membership checks

### Gate C Acceptance Criteria
- [x] Periodic sync runs every 15 minutes per connected subscription.
- [x] Webhook-triggered sync occurs promptly and resets that subscription timer.
- [x] Bursty triggers coalesce safely without concurrent sync overlap.

## Gate D - Square Catalog Webhook Endpoint (Secure Ingest + Dedupe + Route)
  - [x] Add anonymous endpoint:
    - [x] `POST /webhooks/square/catalog`
  - [x] Add app config key:
    - [x] `SQUARE_WEBHOOK_SIGNATURE_KEY`
  - [x] Implement signature validation using raw request body + notification URL + Square signature header.
  - [x] Parse event envelope and handle only `catalog.version.updated`.
  - [x] Resolve subscription via `SubscriptionSquareConnection.SquareMerchantId`.
  - [x] Persist dedupe receipt (`SquareWebhookReceipt`) before enqueueing.
  - [x] Publish `SquareCatalogSyncRequested` with trigger source `webhook`.
  - [x] Return `200` quickly for valid/ignored/duplicate events; reject bad signature with `401`.
  - [x] Map the new webhook endpoint explicitly in WebAPI endpoint composition.

### Gate D Acceptance Criteria
  - [x] Valid catalog webhook triggers sync enqueue and timer reset.
  - [x] Duplicate deliveries do not trigger duplicate processing.
  - [x] Invalid signatures are rejected.

## Gate E - Internal Catalog Read Integration for Message Processing
- [ ] Integrate `ISubscriptionCatalogReadService` into the current incoming message processing path as a product-context seam.
- [ ] Ensure product lookup/context assembly uses cached catalog products.
- [ ] Ensure reads are filtered by agent access rules where agent context is available.
- [ ] Do not implement zip-based runtime filtering in this milestone.
- [ ] Ensure no direct Square catalog API call is made in the current message hot path.
- [ ] Add graceful fallback when cache is empty/stale:
  - [ ] continue processing with degraded product context
  - [ ] optionally enqueue background sync without blocking the current message
- [ ] Do not claim final autonomous reply orchestration in this milestone; this gate only introduces the catalog-read integration seam.

### Gate E Acceptance Criteria
- [ ] Current message processing can retrieve product context from local cache.
- [ ] Incoming message latency is decoupled from Square catalog API availability.
- [ ] Agent only sees products allowed by assignment mode.
- [ ] Zip restriction enforcement remains out of scope for M23.

## Gate H - Agent Product Access Control Domain
  - [x] Implement domain service methods for assignment management:
    - [x] replace assignment set atomically for an agent
    - [x] clear assignment set (revert to default-all mode)
    - [x] get assignment state for agent
  - [x] Validate all requested product IDs belong to the same subscription as the agent.
  - [x] Keep deterministic error codes for invalid assignments.

### Gate H Acceptance Criteria
  - [x] Assignment updates are atomic and tenant-safe.
  - [x] Clearing assignments reliably returns agent to default-all mode.

## Gate J - Agent Zip Allowlist Domain
  - [x] Implement domain service methods for zip allowlist management:
    - [x] replace zip allowlist atomically for an agent
    - [x] clear zip allowlist (revert to no regional restriction metadata)
    - [x] get zip allowlist state for agent
  - [x] Implement zip normalization utility and validation rules.
  - [x] Keep deterministic error codes for invalid zip entries.

### Gate J Acceptance Criteria
  - [x] Zip allowlist updates are atomic and tenant-safe.
  - [x] Empty allowlist reliably means no zip restriction metadata.
  - [x] Exact normalized zip matching behavior is deterministic.

## Gate I - Admin Endpoints for Agent Product Access and Zip Allowlist
  - [x] Add agent-scoped APIs:
    - [x] `GET /agents/{agentId}/catalog/products` (paged, include assignment state per product)
    - [x] `PUT /agents/{agentId}/catalog/products` (replace assignment set)
    - [x] `DELETE /agents/{agentId}/catalog/products` (clear assignments)
  - [x] Apply auth and subscription membership checks consistently.
  - [x] Use DTO naming per guideline (`*Input`, `*Result`) and concrete list result schema shape.
  - [x] Add deterministic API error codes for:
    - [x] `agent_not_found`
    - [x] `subscription_member_required`
    - [x] `catalog_product_not_found`
    - [x] `agent_catalog_assignment_invalid`
  - [x] Add agent zip allowlist APIs:
    - [x] `GET /agents/{agentId}/sales-zips`
    - [x] `PUT /agents/{agentId}/sales-zips` (replace full list)
    - [x] `DELETE /agents/{agentId}/sales-zips` (clear list)
  - [x] Add deterministic zip-related API error codes:
    - [x] `agent_sales_zip_invalid`
    - [x] `agent_sales_zip_conflict`
  - [x] Map all new catalog and zip-management endpoints explicitly in WebAPI endpoint composition.

### Gate I Acceptance Criteria
  - [x] Admin can assign subset and observe assignment state.
  - [x] Admin can clear assignments and return to default-all mode.
  - [x] Admin can configure and clear agent zip allowlist metadata.
  - [x] Endpoint contracts are stable for OpenAPI client generation.

## Gate F - Observability, Security, and Operational Controls
- [ ] Add structured logs/metrics:
  - [ ] sync started/completed/failed with subscription id and trigger source
  - [ ] processed object counts and duration
  - [ ] webhook accepted/ignored/rejected counters
- [ ] Add health/diagnostic API endpoints:
  - [x] `GET /subscriptions/{subscriptionId}/square/catalog/sync-state`
  - [x] `GET /subscriptions/{subscriptionId}/square/catalog/products` (paged)
- [ ] Confirm no logs contain:
  - [ ] access/refresh tokens
  - [ ] webhook signature key
  - [ ] raw sensitive payload fragments
- [ ] Ensure M23 does not require any additional Square OAuth scopes beyond the current connection baseline.
- [ ] Ensure regional restriction relies on internal agent zip allowlists, not external geolocation services.

### Gate F Acceptance Criteria
- [ ] Operators can observe sync health and troubleshoot failures without sensitive leakage.

## Gate K - WebApp Inventory Sync Operations UI
- [x] Implement inventory settings sync UI in `HeyAlan.WebApp/src/app/admin/settings/inventory/page.tsx`.
- [x] Align layout and interaction style with existing pages in `HeyAlan.WebApp/src/app/admin/settings`.
- [ ] Limit UI scope to sync operations and catalog diagnostics:
  - [x] manual sync action
  - [x] sync-state panel
  - [x] minimal cached catalog snapshot
- [ ] Add manual sync action UI:
  - [x] `Sync now` button invokes `POST /subscriptions/{subscriptionId}/square/catalog/sync`
  - [x] disable sync action while request is in-flight
  - [x] show deterministic success and failure feedback
- [ ] Add sync-state dashboard UI:
  - [x] read from `GET /subscriptions/{subscriptionId}/square/catalog/sync-state`
  - [x] display key sync fields (status/trigger/timestamps/last error where present)
  - [x] poll sync state every 30 seconds
  - [x] add manual refresh action
- [ ] Add minimal cached catalog product snapshot:
  - [x] read from `GET /subscriptions/{subscriptionId}/square/catalog/products` (paged)
  - [x] render compact table with essential fields for operational verification
  - [x] include empty and error states
- [x] Guard UI behavior for missing subscription context and unauthorized errors.
- [x] Keep client usage generated-only (`.gen.ts` files remain untouched).
- [ ] Because this milestone adds WebAPI endpoints, hand off for OpenAPI spec/client regeneration whenever those API contracts change.

### Gate K Acceptance Criteria
- [x] Admin can trigger manual sync from the settings inventory page.
- [x] Admin can see current sync state without leaving the page.
- [x] Sync-state panel updates automatically at 30-second cadence and via manual refresh.
- [x] Admin can inspect a minimal paged snapshot of cached catalog products.
- [x] UX is visually consistent with existing settings pages.

## Gate G - Testing and Regression Coverage
- [ ] Unit tests:
  - [ ] mapper flattening and sellable/deleted handling
  - [ ] pagination and incremental watermark behavior
  - [ ] single-flight + pending-resync logic
  - [ ] timer reset semantics on webhook acceptance
  - [ ] agent access semantics (default-all vs filtered-subset)
  - [ ] agent zip metadata semantics (empty list => unrestricted metadata, populated => exact match required for future enforcement)
- [ ] Endpoint tests:
  - [ ] webhook signature valid/invalid paths
  - [ ] duplicate `EventId` dedupe behavior
  - [ ] manual sync auth/membership behavior
  - [ ] agent product access endpoint auth/validation/update paths
  - [ ] agent zip allowlist endpoint auth/validation/update paths
- [ ] Integration tests:
  - [ ] webhook -> message enqueue -> sync state update pipeline
  - [ ] successful Square connection persistence triggers initial full sync enqueue
  - [ ] current message processing path can consume cache-backed product context
  - [ ] runtime-adjacent agent reads respect assignment filtering
  - [ ] zip allowlist data persists and is exposed for later enforcement
- [ ] Regression tests:
  - [ ] existing Square connect/disconnect/token lifecycle tests remain green
  - [ ] existing onboarding state recompute behavior unchanged
  - [ ] existing conversation persistence and buffered message behavior unchanged

### Gate G Acceptance Criteria
- [ ] Critical success/failure branches for sync and webhook flows are covered.
- [ ] Existing Square integration behavior is not regressed.

## Implementation Sequence (Handoff Order)
- [x] 1) Gate A schema/entities and migration handoff.
- [x] 2) Gate B sync/read services + mapping/upsert logic.
- [x] 3) Gate C scheduler + trigger message orchestration.
- [x] 4) Gate D secure webhook ingest + dedupe + routing.
- [x] 5) Gate H agent product access domain behavior.
- [x] 6) Gate J agent zip allowlist domain behavior.
- [x] 7) Gate I admin endpoints for assignment management.
- [ ] 8) Gate E current message-path integration seam.
- [ ] 9) Gate F observability/diagnostics.
- [ ] 10) Gate K WebApp inventory sync operations UI.
- [ ] 11) Gate G complete test pass and regression verification.

## Notes
- [ ] Do not edit auto-generated files (`swagger.json`, `.gen.ts`) manually.
- [ ] Hand off to regenerate the OpenAPI spec/client every time the WebAPI contract changes.
- [ ] This milestone intentionally excludes inventory quantity/count sync.
- [ ] This milestone does not require new Square OAuth scopes beyond the current connection baseline.
- [ ] Customer address-to-zip lookup is explicitly out of scope for this milestone; this milestone consumes normalized zip as input only.
- [ ] Runtime zip-based sellability enforcement is explicitly out of scope for M23 and belongs to later runtime/skill milestones.
- [ ] M23 stores per-location product data but does not select a final checkout location.
- [x] Handoff note after Gate B:
  - [x] `SubscriptionCatalogSyncService` and `SubscriptionCatalogReadService` are implemented in `HeyAlan/SquareIntegration`.
  - [x] Targeted tests for Gate B live in `HeyAlan.Tests/SubscriptionCatalogServicesTests.cs` and pass in isolation.
  - [x] Full solution test run still has unrelated pre-existing failures in `IdentityEndpointsSecurityTests`, `SubscriptionOnboardingServiceTests`, and `TelegramServiceTests`.
  - [x] Next context should continue with Gate C scheduler/trigger orchestration unless priorities change.
- [x] Handoff note after Gate C:
  - [x] Trigger orchestration now lives in `HeyAlan/SquareIntegration/SubscriptionCatalogSyncTriggerService.cs`, `SubscriptionCatalogSyncConsumer.cs`, and `SubscriptionCatalogSyncScheduler.cs`.
  - [x] Manual sync endpoint `POST /subscriptions/{subscriptionId}/square/catalog/sync` is mapped in `HeyAlan.WebApi/SquareIntegration/SquareConnectionEndpoints.cs`.
  - [x] Successful Square connect now enqueues an initial full catalog sync without failing the connect callback if enqueueing itself fails.
  - [x] Webhook timer-reset behavior is implemented in the trigger service, but the actual Square webhook ingest path remains Gate D work.
  - [x] Targeted tests passing in isolation: `SubscriptionCatalogServicesTests`, `SubscriptionCatalogSyncTriggerServiceTests`, and `SubscriptionSquareConnectionServiceTests`.

## Out of Scope Features
- [ ] LLM prompt strategy, conversation policy, and autonomous response generation logic.
- [ ] Checkout/session state collection and persistence.
- [ ] Order creation, payment-link creation, and order-status orchestration.
- [ ] Human handoff and manual state editing flows.
- [ ] WebApp/Admin implementation for product assignment management screens (backend APIs only in this milestone).
- [ ] WebApp/Admin implementation for agent zip allowlist management screens (backend APIs only in this milestone).
- [ ] Conversation/chat UI collection or editing flow for customer delivery address/zipcode.
- [ ] Customer address-to-zipcode lookup implementation.
