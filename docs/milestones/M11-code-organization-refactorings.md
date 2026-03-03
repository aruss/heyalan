# Milestone M11: Code Organization Refactorings

## Goal
Improve code discoverability and maintainability in `HeyAlan` by reorganizing around feature folders (flat within each feature), making data access store-centric, and removing boundary leaks where transport concerns bleed into data access/domain code.

## Scope
- **Primary target (`HeyAlan`)**
  - Reorganize files/namespaces into feature-first folders.
  - Keep each feature folder as flat as practical.
  - Introduce and standardize store-centric data access shape (EF-backed stores).
- **Required dependency updates (no broader redesign)**
  - Update direct references in `HeyAlan.WebApi`, `HeyAlan.Initializer`, and `HeyAlan.Tests` caused by moved/renamed types.

## Non-Goals (Out of Scope)
- Database schema changes.
- New product behavior/features.
- Refactoring all existing EF access into stores in one pass (only establish pattern and apply to Messaging now).
- Renaming integration feature roots `SquareIntegration` and `TelegramIntegration`.

## User Decisions (Locked)
- [x] Use feature-first organization.
- [x] Keep each feature folder flat (minimal subfolders).
- [x] Perform broad cleanup in one pass (structure + naming + boundary cleanup).
- [x] Apply hard namespace/type updates now (no temporary compatibility shims).
- [x] Prioritize messaging/channels first.
- [x] Include semantic renames now (including typo fixes).
- [x] Treat stores like `IConversationStore` as EF-backed data access stores.
- [x] Keep integration folder names `SquareIntegration` and `TelegramIntegration` to avoid namespace collisions with official SDKs.

## Findings from Repository Analysis

### Structural findings
- [x] `HeyAlan` already mixes feature folders and technical folders:
  - `Consumers`, `Core`, `Onboarding`, `Identity`, `SquareIntegration`, `TelegramIntegration`, `Data`, `Configuration`, `Extensions`, `Collections`.
- [x] `HeyAlan/README.md` project structure is stale versus actual files.
- [x] `Consumers/IncomingMessageConsumer.cs` combines multiple responsibilities and types in one file:
  - `IncomingMessage`
  - `OutgoingTelegramMessage`
  - `IncomingMessageConsumer`
  - `OutgoingTelegramMessageConsumer`
- [x] `Core/Conversations` depends on consumer contracts (`using HeyAlan.Consumers`), creating boundary inversion.
- [x] Cross-folder coupling confirms Messaging flow is split across `Consumers`, `Core/Conversations`, and integration folders.

### Naming and quality findings
- [x] Typo appears in transport contracts and usages: `SubscribtionId` should be `SubscriptionId`.
- [x] Some extension class/file naming is inconsistent (`TBuilderExtensions`, `IConfigurationBuilderExtensions` declared in global namespace).
- [x] Several files contain multiple records/contracts in one file, making feature navigation harder.

### Dependency and blast-radius findings
- [x] Message contract and consumer types are referenced in:
  - `HeyAlan.WebApi/Infrastructure/MassTransitBuilderExtensions.cs`
  - `HeyAlan.WebApi/TelegramIntegration/TelegramWebhookEndpoints.cs`
  - `HeyAlan.WebApi/TwilioIntegration/TwilioWebhookEndpoints.cs`
  - `HeyAlan.Initializer/Program.cs`
- [x] Conversation store types are referenced in:
  - `HeyAlan.WebApi/Core/CoreBuilderExtensions.cs`
  - `HeyAlan.Initializer/Program.cs`
- [x] Any namespace/type move in Messaging requires direct updates in WebApi/Initializer/Tests.

## Target Organization (Authoritative)

### Top-level folders in `HeyAlan`
- `Messaging` (new, flat)
- `Identity` (existing, flat)
- `Onboarding` (existing, flat)
- `SquareIntegration` (keep name)
- `TelegramIntegration` (keep name)
- `Data` (existing EF infrastructure/entities)
- `Configuration` (shared)
- `Extensions` (shared)
- `Common` (shared primitives/utilities)

### Messaging target files (flat)
- `Messaging/IncomingMessage.cs`
- `Messaging/OutgoingTelegramMessage.cs`
- `Messaging/IncomingMessageConsumer.cs`
- `Messaging/OutgoingTelegramMessageConsumer.cs`
- `Messaging/IConversationStore.cs`
- `Messaging/ConversationStore.cs`
- `Messaging/MessagingBuilderExtensions.cs`

## Architecture Decisions (Locked)
- [x] Stores are data access abstractions (EF-backed), not transport/domain orchestration components.
- [x] `IConversationStore` / `ConversationStore` belongs to Messaging and is the first store-pattern baseline.
- [x] Consumers and endpoint handlers orchestrate; stores encapsulate DB persistence/query logic.
- [x] Integration roots keep suffix naming:
  - [x] `SquareIntegration`
  - [x] `TelegramIntegration`
- [x] Namespaces should align with physical feature locations after moves.

## Gate A: Messaging Feature Consolidation (Self-Contained)
- [x] Create `HeyAlan/Messaging` feature folder.
- [x] Split current combined consumer/contracts file into one-type-per-file.
- [x] Move conversation store interface/implementation from `Core/Conversations` to `Messaging`.
- [x] Add `MessagingBuilderExtensions` to centralize messaging DI registration.
- [x] Remove obsolete `Consumers` and `Core/Conversations` usage paths after replacement.

### Gate A Acceptance Criteria
- [x] All messaging contracts, consumers, and conversation store types live under `HeyAlan/Messaging`.
- [x] No files in Messaging contain unrelated feature responsibilities.
- [x] Feature is flat and discoverable by file name.

## Gate B: Naming and Namespace Cleanup (Self-Contained)
- [x] Rename `SubscribtionId` -> `SubscriptionId` in all messaging contracts and usages.
- [x] Update logging templates and variable names to match corrected naming.
- [x] Align namespaces with final folder paths.
- [x] Normalize one-type-per-file where refactor touched files.

### Gate B Acceptance Criteria
- [x] No remaining `Subscribtion*` symbols in solution.
- [ ] Build compiles with updated namespaces across all affected projects.
- [x] Naming consistency improves scanability in Messaging and related call-sites.

## Gate C: Store-Centric Boundary Cleanup (Self-Contained)
- [x] Ensure `ConversationStore` is explicitly treated/used as EF-backed store.
- [x] Keep consumers thin: map message contracts and delegate DB operations to store.
- [x] Keep DB query/persistence logic in store, not in consumers.
- [x] Prepare pattern for future feature stores (onboarding/square/etc.) without implementing all of them now.

### Gate C Acceptance Criteria
- [x] Messaging DB operations are encapsulated in `ConversationStore`.
- [x] Consumers avoid direct DB persistence logic except required orchestration dependencies.
- [x] Store abstraction is clear and reusable for follow-up refactors.

## Gate D: Shared/Common Discoverability Cleanup (Self-Contained)
- [x] Create `Common` shared folder for primitives/utilities touched by this refactor.
- [x] Move root shared primitives (`Constants`, `Enums`) to clearer shared location if part of cleanup diff.
- [x] Move paging/query helper placement from `Collections` to a clearer shared location if touched.
- [x] Keep `Configuration` and `Extensions` as cross-feature shared roots.
- [x] Update stale `HeyAlan/README.md` structure section to match real project layout.

### Gate D Acceptance Criteria
- [x] Shared primitives are easier to locate from top-level structure.
- [x] README structure reflects current organization.
- [x] No behavior changes introduced by shared-file relocation.

## Gate E: Cross-Project Reference Update (Self-Contained)
- [x] Update `HeyAlan.WebApi` references for moved Messaging types/usings.
- [x] Update `HeyAlan.Initializer` references for moved Messaging types/usings.
- [x] Update `HeyAlan.Tests` references for moved/renamed symbols.
- [x] Verify DI registration call-sites with new messaging composition extension.

### Gate E Acceptance Criteria
- [ ] No compile errors due to missing old namespaces/types.
- [ ] All projects reference new feature locations cleanly.

## Test Plan (Authoritative)
1. Build validation
   - [ ] `HeyAlan`
   - [ ] `HeyAlan.WebApi`
   - [ ] `HeyAlan.Initializer`
   - [ ] `HeyAlan.Tests`
2. Existing tests
   - [ ] Run current test suite and fix namespace/type fallout.
3. Messaging flow sanity
   - [ ] Telegram webhook publishes inbound message using renamed `SubscriptionId`.
   - [ ] Inbound consumer persists conversation message through `IConversationStore`.
   - [ ] Outbound telegram consumer sends and appends conversation message.
4. DI sanity
   - [ ] Messaging DI registration composes correctly in WebApi and Initializer.

## Public Interfaces / Contracts Affected
- [x] `IncomingMessage.SubscribtionId` -> `IncomingMessage.SubscriptionId`
- [x] `OutgoingTelegramMessage.SubscribtionId` -> `OutgoingTelegramMessage.SubscriptionId`
- [x] Namespace moves for Messaging contracts/consumers/store types.
- [ ] No intentional runtime behavior changes in message handling semantics.

## Risks and Mitigations
- [ ] Risk: large namespace/type move causes broad compile breakage.
  - Mitigation: perform gates in order and run full solution compile after each gate.
- [ ] Risk: hidden references in tests or bootstrap code missed.
  - Mitigation: solution-wide symbol search for old namespaces and old property names before finalizing.
- [ ] Risk: accidental behavior drift during file splits.
  - Mitigation: no logic rewrites unless required for boundary clarity; validate with messaging flow tests.

## Handoff Notes
- [x] This milestone intentionally establishes the store pattern in Messaging first.
- [ ] Future milestone should move more direct `MainDataContext` access into per-feature stores.
- [x] Keep `SquareIntegration` and `TelegramIntegration` naming untouched during this milestone.
