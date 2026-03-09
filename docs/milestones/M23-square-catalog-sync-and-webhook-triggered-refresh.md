# Milestone M23: Square Catalog Sync Cache + Webhook-Triggered Refresh

## Summary
Build a subscription-scoped Square catalog cache so agent reply paths use local product data instead of calling Square on each incoming message.

Add Square webhook-driven refresh (`catalog.version.updated`) that triggers immediate sync and resets that subscription's periodic sync window to 15 minutes from webhook receipt.

Extend this milestone so the synced subscription catalog is shared across all agents, while each agent can either:
- sell all products (default mode), or
- sell a filtered subset of assigned products.

Also extend this milestone with zip-code-based regional restrictions:
- each agent can define an allowlist of zip codes it can sell to,
- customer address-to-zip lookup happens upstream (out of scope here),
- runtime product availability is filtered by both agent product access and agent zip allowlist.

## User Decisions (Locked)
- [x] Service split is explicit:
  - [x] `ISubscriptionCatalogSyncService` fetches from Square and persists local cache.
  - [x] `ISubscriptionCatalogReadService` reads synced catalog data internally.
  - [x] `ISquareService` remains token lifecycle only.
- [x] Sync trigger strategy is hybrid:
  - [x] Initial full sync on connect/reconnect.
  - [x] Periodic incremental sync every 15 minutes.
  - [x] Manual sync endpoint.
  - [x] Webhook-triggered sync for catalog updates.
- [x] Catalog storage shape is normalized minimal for agent usage.
- [x] Catalog scope for v1 is sellable product data (`ITEM` + `ITEM_VARIATION` focused).
- [x] Location policy is per-location records.
- [x] v1 is catalog-only (no inventory quantity/count sync).
- [x] Webhook timer policy: accepted catalog webhook resets periodic timer for that subscription.
- [x] Agent catalog access mode uses implicit relation state:
  - [x] No product assignments for agent => all products are accessible.
  - [x] One or more assignments => only assigned products are accessible.
- [x] Admin API for agent-product access is agent-scoped.
- [x] Regional restriction model is agent zip allowlist based.
- [x] Zip matching rule is exact normalized match.
- [x] Empty agent zip allowlist means no regional restriction (all zips allowed).
- [x] Product-to-zip association model is intersection-based (no direct zip->product mapping table):
  - [x] Product eligibility comes from agent product access mode.
  - [x] Zip eligibility comes from agent zip allowlist mode.
  - [x] Final eligibility is `productAllowed && zipAllowed`.

## Architecture Notes
- [ ] All runtime agent/product lookups MUST read local DB cache only (no per-message Square API calls).
- [ ] Sync implementation MUST reuse `ISquareService.GetValidAccessTokenAsync(subscriptionId)`.
- [ ] Sync runs MUST be single-flight per subscription and idempotent.
- [ ] Webhook processing MUST be durable, deduplicated, and async (ack quickly, process via Wolverine).
- [ ] Logging MUST avoid tokens, signatures, and PII.
- [ ] Agent runtime catalog reads MUST be agent-aware.
- [ ] Product-to-agent assignment MUST be isolated by subscription boundary.
- [ ] Runtime availability MUST apply both filters:
  - [ ] product access filter (default-all or subset),
  - [ ] zip-code filter (if agent zip allowlist exists).

## Runtime Availability Resolution (Authoritative)
- [ ] Inputs:
  - [ ] `subscriptionId`
  - [ ] `agentId`
  - [ ] `customerZipNormalized` (from upstream address->zip lookup, out of scope here)
  - [ ] candidate product(s) from synced subscription catalog
- [ ] Product permission evaluation:
  - [ ] If agent has zero product assignments, all catalog products are product-allowed.
  - [ ] If agent has one or more product assignments, only assigned products are product-allowed.
- [ ] Zip permission evaluation:
  - [ ] If agent has zero zip allowlist entries, all zips are zip-allowed.
  - [ ] If agent has one or more zip allowlist entries, zip is allowed only on exact normalized match.
- [ ] Final decision:
  - [ ] `canSellProduct = productAllowed && zipAllowed`
- [ ] Mapping note:
  - [ ] There is no direct zip->product relation in this milestone.
  - [ ] Association is computed at read-time via intersection of independent agent-product and agent-zip filters.

## Gate A - Data Model Foundation (Catalog Cache + Sync State + Webhook Dedupe)
- [ ] Add `SubscriptionCatalogSyncState` entity keyed by subscription:
  - [ ] Last successful watermark (`LastSyncedBeginTimeUtc` or equivalent)
  - [ ] `NextScheduledSyncAtUtc`
  - [ ] `LastSyncStartedAtUtc`
  - [ ] `LastSyncCompletedAtUtc`
  - [ ] `LastTriggerSource` (`periodic|webhook|manual|connect`)
  - [ ] `SyncInProgress`
  - [ ] `PendingResync`
  - [ ] Last error code/message (sanitized)
- [ ] Add `SubscriptionCatalogProduct` normalized variation-level cache entity:
  - [ ] `SubscriptionId`
  - [ ] `SquareItemId`
  - [ ] `SquareVariationId`
  - [ ] Item/variation names
  - [ ] Description
  - [ ] SKU
  - [ ] Base price/currency
  - [ ] Sellable/deleted flags
  - [ ] Square `UpdatedAt` / `Version`
  - [ ] Search text column for internal lookup
- [ ] Add `SubscriptionCatalogProductLocation` entity for per-location overrides:
  - [ ] `SubscriptionId`
  - [ ] `SquareVariationId`
  - [ ] `LocationId`
  - [ ] Effective price override
  - [ ] Availability/sold-out flags from catalog location fields
- [ ] Add `SquareWebhookReceipt` dedupe entity:
  - [ ] Unique `EventId`
  - [ ] Event type
  - [ ] Merchant id
  - [ ] Received at
  - [ ] Processed status
- [ ] Add `AgentCatalogProductAccess` relation entity:
  - [ ] `AgentId`
  - [ ] `SubscriptionId`
  - [ ] Catalog product key (`SubscriptionCatalogProductId` or stable product key)
  - [ ] `CreatedAt`
  - [ ] `UpdatedAt`
- [ ] Add `AgentSalesZipCode` relation entity:
  - [ ] `AgentId`
  - [ ] `SubscriptionId`
  - [ ] `ZipCodeNormalized`
  - [ ] `CreatedAt`
  - [ ] `UpdatedAt`
- [ ] Enforce relation constraints/indexes:
  - [ ] Unique assignment per agent/product pair
  - [ ] Unique zip entry per agent/zip pair
  - [ ] Tenant-safe indexes with `SubscriptionId` prefix
  - [ ] Prevent cross-subscription assignment joins
- [ ] Register new `DbSet<>` mappings and indexes/constraints in `MainDataContext`.
- [ ] Add tenant-safe composite indexes (`SubscriptionId` first) for all read paths.
- [ ] Stop and hand off for migration creation/run per repository rule.

### Gate A Acceptance Criteria
- [ ] Schema supports incremental sync watermarking, schedule reset, and webhook dedupe.
- [ ] No cross-subscription reads are possible by model/query shape.
- [ ] Schema supports agent default-all and filtered-subset product access semantics.
- [ ] Schema supports exact zip allowlist checks per agent.

## Gate B - Catalog Sync Services (Square Pull + Local Upsert)
- [ ] Introduce `ISubscriptionCatalogSyncService` contract and implementation in `HeyAlan`.
- [ ] Introduce `ISubscriptionCatalogReadService` for internal read/query use by agent flows.
- [ ] Extend `ISubscriptionCatalogReadService` with agent-aware reads:
  - [ ] Agent-aware list/search/get operations take `subscriptionId` + `agentId` + `customerZipNormalized` (optional).
  - [ ] Default mode behavior: no assignments for agent returns all active products.
  - [ ] Filter mode behavior: assigned rows restrict returned products.
  - [ ] Zip mode behavior:
    - [ ] no agent zip rows => no zip restriction,
    - [ ] one or more agent zip rows => `customerZipNormalized` must match exactly.
- [ ] Implement sync modes:
  - [ ] Full sync (connect/manual force)
  - [ ] Incremental sync (`begin_time` from watermark)
- [ ] Use Square SDK Catalog API (`client.Catalog.SearchAsync`) with:
  - [ ] `cursor` pagination loop
  - [ ] `object_types` constrained for v1 scope
  - [ ] `include_deleted_objects = true` for incremental updates
  - [ ] `limit` configured within API bounds
- [ ] Map Square catalog objects to normalized entities:
  - [ ] Flatten item + variation fields
  - [ ] Persist per-location overrides
  - [ ] Handle deletes as tombstone or soft-delete semantics
- [ ] Implement idempotent upsert/delete behavior.
- [ ] Enforce one sync at a time per subscription (single-flight lock).
- [ ] Update sync state (`last*`, watermark, errors, trigger source).
- [ ] Ensure sync does not clear or mutate agent-product assignments.
- [ ] Register services in DI via builder extensions.

### Gate B Acceptance Criteria
- [ ] A connected subscription can complete full and incremental sync without duplicate rows.
- [ ] Sync failures are recorded with deterministic error codes and no secret leakage.
- [ ] Agent-aware reads correctly enforce default-all and filtered-subset behavior.
- [ ] Agent-aware reads correctly enforce zip allowlist behavior.

## Gate C - Triggering and Scheduling (15-Minute Cadence + Reset Semantics)
- [ ] Add durable message contract `SquareCatalogSyncRequested`.
- [ ] Add Wolverine consumer that executes sync by subscription id.
- [ ] Add periodic background scheduler (1-minute tick is sufficient) that:
  - [ ] Selects subscriptions where `UtcNow >= NextScheduledSyncAtUtc`
  - [ ] Enqueues periodic sync
- [ ] Implement reset semantics:
  - [ ] On accepted webhook event: set `NextScheduledSyncAtUtc = UtcNow + 15m`
  - [ ] On periodic run start: set next due to `UtcNow + 15m`
  - [ ] If sync is in progress and a trigger arrives: set `PendingResync = true`
  - [ ] After run completes, if `PendingResync` true: enqueue one follow-up run and clear flag
- [ ] Add connect/reconnect hook:
  - [ ] After successful Square connect completion, enqueue full sync for that subscription
- [ ] Add manual sync API endpoint:
  - [ ] `POST /subscriptions/{subscriptionId}/square/catalog/sync`
  - [ ] Auth + subscription membership checks

### Gate C Acceptance Criteria
- [ ] Periodic sync runs every 15 minutes per connected subscription.
- [ ] Webhook-triggered sync occurs promptly and resets that subscription timer.
- [ ] Bursty triggers coalesce safely without concurrent sync overlap.

## Gate D - Square Catalog Webhook Endpoint (Secure Ingest + Dedupe + Route)
- [ ] Add anonymous endpoint:
  - [ ] `POST /webhooks/square/catalog`
- [ ] Add app config key:
  - [ ] `SQUARE_WEBHOOK_SIGNATURE_KEY`
- [ ] Implement signature validation using raw request body + notification URL + Square signature header.
- [ ] Parse event envelope and handle only `catalog.version.updated`.
- [ ] Resolve subscription via `SubscriptionSquareConnection.SquareMerchantId`.
- [ ] Persist dedupe receipt (`SquareWebhookReceipt`) before enqueueing.
- [ ] Publish `SquareCatalogSyncRequested` with trigger source `webhook`.
- [ ] Return `200` quickly for valid/ignored/duplicate events; reject bad signature with `401`.

### Gate D Acceptance Criteria
- [ ] Valid catalog webhook triggers sync enqueue and timer reset.
- [ ] Duplicate deliveries do not trigger duplicate processing.
- [ ] Invalid signatures are rejected.

## Gate E - Internal Catalog Read Integration for Agent Replies
- [ ] Integrate `ISubscriptionCatalogReadService` into incoming message processing path.
- [ ] Ensure reply generation path uses cached catalog products for product mention/lookup context.
- [ ] Ensure reads are filtered by agent access rules (agent id required on runtime reads).
- [ ] Ensure reads are filtered by agent zip allowlist rules when customer zip is available.
- [ ] Ensure no direct Square API call is made in incoming message hot path.
- [ ] Add graceful fallback when cache is empty/stale:
  - [ ] Continue response behavior
  - [ ] Optionally enqueue background sync without blocking current message

### Gate E Acceptance Criteria
- [ ] Agent reply flow can retrieve product context from local cache.
- [ ] Incoming message latency is decoupled from Square API availability.
- [ ] Agent only sees products allowed by assignment mode.
- [ ] Agent only sees products when customer zip is eligible under agent zip rules.

## Gate H - Agent Product Access Control Domain
- [ ] Implement domain service methods for assignment management:
  - [ ] Replace assignment set atomically for an agent.
  - [ ] Clear assignment set (revert to default-all mode).
  - [ ] Get assignment state for agent.
- [ ] Validate all requested product IDs belong to same subscription as agent.
- [ ] Keep deterministic error codes for invalid assignments.

### Gate H Acceptance Criteria
- [ ] Assignment updates are atomic and tenant-safe.
- [ ] Clearing assignments reliably returns agent to default-all mode.

## Gate J - Agent Zip Allowlist Domain
- [ ] Implement domain service methods for zip allowlist management:
  - [ ] Replace zip allowlist atomically for an agent.
  - [ ] Clear zip allowlist (revert to no regional restriction).
  - [ ] Get zip allowlist state for agent.
- [ ] Implement zip normalization utility and validation rules.
- [ ] Keep deterministic error codes for invalid zip entries.

### Gate J Acceptance Criteria
- [ ] Zip allowlist updates are atomic and tenant-safe.
- [ ] Empty allowlist reliably means no zip restriction.
- [ ] Exact normalized zip matching behavior is deterministic.

## Gate I - Admin Dashboard Endpoints for Agent Product Access
- [ ] Add agent-scoped APIs:
  - [ ] `GET /agents/{agentId}/catalog/products` (paged, include assignment state per product)
  - [ ] `PUT /agents/{agentId}/catalog/products` (replace assignment set)
  - [ ] `DELETE /agents/{agentId}/catalog/products` (clear assignments)
- [ ] Apply auth and subscription membership checks consistently.
- [ ] Use DTO naming per guideline (`*Input`, `*Result`) and concrete list result schema shape.
- [ ] Add deterministic API error codes for:
  - [ ] `agent_not_found`
  - [ ] `subscription_member_required`
  - [ ] `catalog_product_not_found`
  - [ ] `agent_catalog_assignment_invalid`
- [ ] Add agent zip allowlist APIs:
  - [ ] `GET /agents/{agentId}/sales-zips`
  - [ ] `PUT /agents/{agentId}/sales-zips` (replace full list)
  - [ ] `DELETE /agents/{agentId}/sales-zips` (clear list)
- [ ] Add deterministic zip-related API error codes:
  - [ ] `agent_sales_zip_invalid`
  - [ ] `agent_sales_zip_conflict`

### Gate I Acceptance Criteria
- [ ] Admin can assign subset and observe assignment state.
- [ ] Admin can clear assignments and return to default-all mode.
- [ ] Admin can configure and clear agent zip allowlist.
- [ ] Endpoint contracts are stable for OpenAPI client generation.

## Gate F - Observability, Security, and Operational Controls
- [ ] Add structured logs/metrics:
  - [ ] Sync started/completed/failed with subscription id and trigger source
  - [ ] Processed object counts and duration
  - [ ] Webhook accepted/ignored/rejected counters
- [ ] Add health/diagnostic API endpoints:
  - [ ] `GET /subscriptions/{subscriptionId}/square/catalog/sync-state`
  - [ ] `GET /subscriptions/{subscriptionId}/square/catalog/products` (paged)
- [ ] Confirm no logs contain:
  - [ ] Access/refresh tokens
  - [ ] Webhook signature key
  - [ ] Raw sensitive payload fragments
- [ ] Ensure least-privilege scope posture remains unchanged for v1 (`ITEMS_READ` only for catalog reads).
- [ ] Ensure regional restriction relies on internal agent zip allowlists (not external geolocation services in this milestone).

### Gate F Acceptance Criteria
- [ ] Operators can observe sync health and troubleshoot failures without sensitive leakage.

## Gate K - WebApp Inventory Sync Operations UI
- [ ] Implement inventory settings UI in `HeyAlan.WebApp/src/app/admin/settings/inventory/page.tsx`.
- [ ] Align layout and interaction style with existing pages in `HeyAlan.WebApp/src/app/admin/settings`.
- [ ] Add manual sync action UI:
  - [ ] `Sync now` button invokes `POST /subscriptions/{subscriptionId}/square/catalog/sync`.
  - [ ] Disable sync action while request is in-flight.
  - [ ] Show deterministic success and failure feedback.
- [ ] Add sync-state dashboard UI:
  - [ ] Read from `GET /subscriptions/{subscriptionId}/square/catalog/sync-state`.
  - [ ] Display key sync fields (status/trigger/timestamps/last error where present).
  - [ ] Poll sync state every 30 seconds.
  - [ ] Add manual refresh action.
- [ ] Add minimal cached catalog product snapshot:
  - [ ] Read from `GET /subscriptions/{subscriptionId}/square/catalog/products` (paged).
  - [ ] Render compact table with essential fields for operational verification.
  - [ ] Include empty and error states.
- [ ] Guard UI behavior for missing subscription context and unauthorized errors.
- [ ] Keep client usage generated-only (`.gen.ts` files remain untouched).
- [ ] If WebAPI contract changes are required, hand off for `yarn openapi-ts` per repository rule.

### Gate K Acceptance Criteria
- [ ] Admin can trigger manual sync from settings inventory page.
- [ ] Admin can see current sync state without leaving the page.
- [ ] Sync-state panel updates automatically at 30-second cadence and via manual refresh.
- [ ] Admin can inspect a minimal paged snapshot of cached catalog products.
- [ ] UX is visually consistent with existing settings pages.

## Gate G - Testing and Regression Coverage
- [ ] Unit tests:
  - [ ] Mapper flattening and sellable/deleted handling
  - [ ] Pagination and incremental watermark behavior
  - [ ] Single-flight + pending-resync logic
  - [ ] Timer reset semantics on webhook acceptance
  - [ ] Agent access semantics (default-all vs filtered-subset)
  - [ ] Agent zip semantics (empty list => unrestricted, populated => exact match required)
- [ ] Endpoint tests:
  - [ ] Webhook signature valid/invalid paths
  - [ ] Duplicate `EventId` dedupe behavior
  - [ ] Manual sync auth/membership behavior
  - [ ] Agent product access endpoint auth/validation/update paths
  - [ ] Agent zip allowlist endpoint auth/validation/update paths
- [ ] Integration tests:
  - [ ] Webhook -> message enqueue -> sync state update pipeline
  - [ ] Connect success triggers initial full sync enqueue
  - [ ] Runtime agent reads respect assignment filtering
  - [ ] Runtime agent reads respect zip filtering
- [ ] Regression tests:
  - [ ] Existing Square connect/disconnect/token tests remain green
  - [ ] Existing onboarding state recompute behavior unchanged

### Gate G Acceptance Criteria
- [ ] Critical success/failure branches for sync and webhook flows are covered.
- [ ] Existing Square integration behavior is not regressed.

## Implementation Sequence (Handoff Order)
- [ ] 1) Gate A schema/entities and migration handoff.
- [ ] 2) Gate B sync/read services + mapping/upsert logic.
- [ ] 3) Gate C scheduler + trigger message orchestration.
- [ ] 4) Gate D secure webhook ingest + dedupe + routing.
- [ ] 5) Gate H agent product access domain behavior.
- [ ] 6) Gate J agent zip allowlist domain behavior.
- [ ] 7) Gate I admin endpoints for assignment management.
- [ ] 8) Gate E agent read-path integration.
- [ ] 9) Gate F observability/diagnostics.
- [ ] 10) Gate K WebApp inventory sync operations UI.
- [ ] 11) Gate G complete test pass and regression verification.

## Notes
- [ ] Do not edit auto-generated files (`swagger.json`, `.gen.ts`) manually.
- [ ] If WebAPI interface changes require client regeneration, hand off for `yarn openapi-ts`.
- [ ] This milestone intentionally excludes inventory quantity/count sync (`INVENTORY_READ` scope expansion) for v1.
- [ ] Customer address-to-zip lookup is explicitly out of scope for this milestone; this milestone consumes normalized zip as input only.

## Out of Scope Features
- [ ] WebApp/Admin implementation for product assignment management screens (backend APIs only in this milestone).
- [ ] WebApp/Admin implementation for agent zip allowlist management screens (backend APIs only in this milestone).
- [ ] Conversation/chat UI collection or editing flow for customer delivery address/zipcode.
- [ ] Customer address-to-zipcode lookup implementation (this milestone only consumes normalized zipcode input).
