# M36 - Privacy Page SMS Updates Signup

## Summary
Adapt [`docs/phone-number.html`](/mnt/c/projects/software/atlas-delivery/buyalan/docs/phone-number.html) into a new standalone BuyAlan landing component rendered at the bottom of the privacy page. Add a new anonymous WebAPI endpoint and database persistence so submitted phone numbers and SMS consent selections are stored locally for future operational use and compliance review.

## Scope
- WebApp:
  - Create a new standalone SMS signup component based on `docs/phone-number.html`.
  - Render the component at the bottom of `/privacy`.
  - Preserve the form fields and core interaction model from the source artifact:
    - required phone number
    - optional transactional consent checkbox
    - optional marketing consent checkbox
    - disclosure text with Terms and Privacy links
    - success and error feedback after submit
- WebAPI:
  - Add a new anonymous SMS signup endpoint separate from newsletter code.
  - Validate input and persist the signup record into the project database.
- Data:
  - Add a new entity and EF mapping for SMS signup/consent storage.
  - Support dedupe/upsert by normalized phone number.

## Non-Goals
- Reusing or modifying the footer newsletter form.
- Integrating with SendGrid newsletter flows.
- Sending outbound SMS as part of this milestone.
- Editing generated OpenAPI client files manually.
- Creating EF migrations in this milestone implementation pass.

## Locked Decisions
- [x] The design/source artifact is `docs/phone-number.html`.
- [x] The new component is standalone and MUST NOT touch the footer.
- [x] The component is rendered at the very bottom of the privacy page.
- [x] The backend stores a full consent record, not just a bare phone number.
- [x] Terms and Privacy links in the adapted component point to BuyAlan routes (`/terms`, `/privacy`) instead of Propane PHP pages.
- [x] `consentSource` is stored as `privacy-page`.
- [x] Repeat submissions for the same normalized phone number update the existing record instead of creating duplicates.

## Planned API and Contracts
- [x] New endpoint:
  - `POST /sms/subscribe`
- [x] New request DTO:
  - `CreateSmsubscriptionInput`
- [x] New response DTO:
  - `CreateSmsSubscriptionResult`
- [x] Request payload fields:
  - `phoneNumber`
  - `transactionalConsent`
  - `marketingConsent`
  - `consentSource`
- [x] Response payload:
  - `accepted`

## Planned Data Model
- [x] New entity for SMS update subscriptions/consents.
- [x] Fields:
  - `Id`
  - `PhoneNumber`
  - `PhoneNumberNormalized`
  - `TransactionalConsent`
  - `MarketingConsent`
  - `ConsentSource`
  - `SubmittedAtUtc`
  - `CreatedAt`
  - `UpdatedAt`
- [x] Index on `PhoneNumberNormalized`.
- [x] `PhoneNumberNormalized` is the dedupe key for upsert behavior.

## Validation and Behavior Rules
- [x] Phone number is the only required user input.
- [x] Server validates and normalizes phone number before persistence.
- [x] Invalid phone input returns `400 Problem`.
- [x] Missing checkbox values are treated as `false`.
- [x] The UI submits `consentSource: "privacy-page"`.
- [x] The UI uses BuyAlan/Atlas Delivery Software, Inc. branding in adapted copy.
- [x] Success UX is inline on the same component after a successful submit.

## Gate A - WebApp Component Adaptation
- [ ] Create a new landing component adapted from `docs/phone-number.html`.
- [ ] Convert the source artifact into repo-compliant Next.js/React/Tailwind code.
- [ ] Replace Propane-specific branding/copy targets where needed for BuyAlan legal pages.
- [ ] Add client-side phone validation and inline validation messaging.
- [ ] Add submit pending, success, and error states.
- [ ] Render the component at the bottom of `BuyAlan.WebApp/src/app/(landing)/privacy/page.tsx`.

### Gate A Acceptance Criteria
- [ ] Privacy page renders the new SMS signup card at the bottom.
- [ ] Footer/newsletter code remains unchanged.
- [ ] Invalid phone input is blocked client-side with visible feedback.
- [ ] Successful submission shows inline success state without navigating away.

## Gate B - WebAPI Endpoint
- [ ] Add a new endpoint group or endpoint mapping for SMS update subscriptions.
- [ ] Add request/response DTOs following repo naming conventions.
- [ ] Allow anonymous access.
- [ ] Validate incoming phone numbers and return `400 Problem` for invalid input.
- [ ] Persist or update the SMS signup record using normalized phone number dedupe.

### Gate B Acceptance Criteria
- [ ] Valid requests return accepted response.
- [ ] Invalid phone input returns `400`.
- [ ] Endpoint does not disclose unnecessary internal state.

## Gate C - Data Persistence
- [ ] Add the new SMS signup entity in `BuyAlan.Data`.
- [ ] Register `DbSet<>` and EF model configuration in `MainDataContext`.
- [ ] Add normalized-phone index and audit-compatible fields.
- [ ] Ensure repeat submits update the existing record instead of inserting duplicates.

### Gate C Acceptance Criteria
- [ ] A valid submit is stored in the database with consent flags and source.
- [ ] Duplicate normalized phone submissions update the same logical record.
- [ ] Persistence follows existing audit/id conventions in `MainDataContext`.

## Gate D - Verification
- [ ] WebApp test covers invalid phone validation.
- [ ] WebApp test covers successful submission payload and success state.
- [ ] WebApp test covers failed submission error state.
- [ ] WebAPI tests cover valid create/update behavior.
- [ ] WebAPI tests cover invalid phone rejection.
- [ ] Data tests cover entity mapping and normalized-phone dedupe assumptions.

### Gate D Acceptance Criteria
- [ ] Form behavior is covered end-to-end at the component/API boundary.
- [ ] Persisted consent fields match the submitted checkbox values.

## Handoffs and Repo Rules
- [ ] After changing the database schema, stop and hand off for developer-run EF migration creation.
- [ ] After changing the WebAPI interface/openapi surface, hand off for developer-run client generation:
  - `yarn openapi-ts`
- [x] Do not edit `swagger.json` or `.gen.ts` files manually.

## Risks and Notes
- Phone normalization rules should be strict enough to reject malformed inputs but pragmatic enough for the intended SMS capture flow.
- SMS consent text is compliance-sensitive; copy changes should stay close to the source artifact while using BuyAlan legal links/branding.
- This milestone stores consent data only; any future outbound SMS workflow should reuse this persisted record instead of redefining consent capture.
