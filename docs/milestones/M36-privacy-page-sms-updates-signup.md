# M36 - Privacy Page SMS Updates Signup

## Summary
Adapt [`docs/phone-number.html`](/mnt/c/projects/software/atlas-delivery/buyalan/docs/phone-number.html) into a new standalone BuyAlan landing component rendered near the bottom of the privacy page content. Add a new anonymous WebAPI endpoint and database persistence so submitted phone numbers and SMS consent selections are stored locally for future operational use and compliance review.

## Scope
- WebApp:
  - Create a new standalone SMS signup component based on `docs/phone-number.html`.
  - Place the form UI, client-side validation, submit logic, and success/error states inside the standalone component.
  - Render the component inside the `/privacy` page content near the bottom of the page body.
  - Preserve the form fields and core interaction model from the source artifact:
    - required phone number
    - optional transactional consent checkbox
    - optional marketing consent checkbox
    - disclosure text with Terms and Privacy links
    - success and error feedback after submit
- WebAPI:
  - Add a new anonymous SMS signup endpoint separate from newsletter code.
  - Validate required input and persist the signup record into the project database.
- Data:
  - Add a new entity and EF mapping for SMS signup/consent storage.
  - Store submitted phone numbers as-is without normalization or dedupe behavior.

## Non-Goals
- Reusing or modifying the footer newsletter form.
- Integrating with SendGrid newsletter flows.
- Sending outbound SMS as part of this milestone.
- Editing generated OpenAPI client files manually.
- Creating EF migrations in this milestone implementation pass.

## Locked Decisions
- [x] The design/source artifact is `docs/phone-number.html`.
- [x] The new component is standalone and MUST NOT touch the footer.
- [x] The component is rendered inside the privacy page content near the bottom of the page body.
- [x] The backend stores a full consent record, not just a bare phone number.
- [x] Terms and Privacy links in the adapted component point to BuyAlan routes (`/terms`, `/privacy`) instead of Propane PHP pages.
- [x] `consentSource` is stored as `privacy-page` and assigned by the server.
- [x] Submitted phone numbers are stored as provided, without normalization or dedupe.

## Planned API and Contracts
- [x] New endpoint:
  - `POST /sms/subscribe`
- [x] New request DTO:
  - `CreateSmsConsentInput`
- [x] New response DTO:
  - `CreateSmsConsentResult`
- [x] Request payload fields:
  - `phoneNumber`
  - `transactionalConsent`
  - `marketingConsent`
- [x] Response payload:
  - `accepted`

## Planned Data Model
- [x] New entity for SMS update subscriptions/consents.
- [x] Fields:
  - `Id`
  - `PhoneNumber`
  - `TransactionalConsent`
  - `MarketingConsent`
  - `ConsentSource`
  - `CreatedAt`
  - `UpdatedAt`
- [x] `CreatedAt` serves as the submission timestamp for each stored record.

## Validation and Behavior Rules
- [x] Phone number is the only required user input.
- [x] The UI validates phone number format before submit using permissive, user-friendly checks rather than strict E.164 validation.
- [x] The server requires a non-empty phone number and stores it as submitted.
- [x] Missing or invalid required input returns `400 Problem`.
- [x] Missing checkbox values are treated as `false`.
- [x] The server stores `consentSource: "privacy-page"`.
- [x] The UI uses BuyAlan/Atlas Delivery Software, Inc. branding in adapted copy.
- [x] Success UX is inline on the same component after a successful submit.

## Gate A - WebApp Component Adaptation
- [x] Create a new landing component adapted from `docs/phone-number.html`.
- [x] Convert the source artifact into repo-compliant Next.js/React/Tailwind code.
- [x] Replace Propane-specific branding/copy targets where needed for BuyAlan legal pages.
- [x] Keep all form rendering, validation, and submit logic inside the standalone component.
- [x] Add permissive client-side phone validation and inline validation messaging.
- [x] Add submit pending, success, and error states.
- [x] Render the component from `BuyAlan.WebApp/src/app/(landing)/privacy/page.tsx` inside the privacy page content near the bottom of the page body.

### Gate A Acceptance Criteria
- [x] Privacy page renders the new SMS signup card inside the page content near the bottom.
- [x] Footer/newsletter code remains unchanged.
- [x] Empty or obviously invalid phone input is blocked client-side with visible feedback.
- [x] Successful submission shows inline success state without navigating away.

## Gate B - WebAPI Endpoint
- [x] Add a new endpoint group or endpoint mapping for SMS update subscriptions.
- [x] Add request/response DTOs following repo naming conventions.
- [x] Allow anonymous access.
- [x] Require a non-empty phone number and return `400 Problem` for missing or invalid required input.
- [x] Persist a new SMS signup record using the submitted phone number as-is.
- [x] Assign `consentSource` as `privacy-page` on the server.

### Gate B Acceptance Criteria
- [x] Valid requests return accepted response.
- [x] Invalid phone input returns `400`.
- [x] Endpoint does not disclose unnecessary internal state.

## Gate C - Data Persistence
- [x] Add the new SMS signup entity in `BuyAlan/Data`.
- [x] Register `DbSet<>` and EF model configuration in `MainDataContext`.
- [x] Add consent fields and audit-compatible fields.
- [x] Store each submission as a new record without normalization or dedupe rules.

### Gate C Acceptance Criteria
- [x] A valid submit is stored in the database with consent flags and source.
- [x] Submitted phone numbers are stored exactly as provided.
- [x] Persistence follows existing audit/id conventions in `MainDataContext`.

## Gate D - Verification
- [x] WebApp test covers empty or obviously invalid phone validation.
- [x] WebApp test covers successful submission payload and success state.
- [x] WebApp test covers failed submission error state.
- [ ] WebAPI tests cover valid create behavior.
- [ ] WebAPI tests cover missing/invalid phone rejection.
- [x] Data tests cover entity mapping and raw phone-number persistence assumptions.

### Gate D Acceptance Criteria
- [ ] Form behavior is covered end-to-end at the component/API boundary.
- [ ] Persisted consent fields match the submitted checkbox values.
- [x] Privacy page uses the standalone component and the footer remains unchanged.

## Handoffs and Repo Rules
- [x] After changing the database schema, stop and hand off for developer-run EF migration creation.
- [x] After changing the WebAPI interface/openapi surface, hand off for developer-run client generation:
  - `yarn openapi-ts`
- [x] Do not edit `swagger.json` or `.gen.ts` files manually.

## Risks and Notes
- Client-side phone validation should be permissive enough to avoid blocking plausible user input while still catching obvious junk values.
- SMS consent text is compliance-sensitive; copy changes should stay close to the source artifact while using BuyAlan legal links/branding.
- This milestone stores consent data only; any future outbound SMS workflow should reuse this persisted record instead of redefining consent capture.
