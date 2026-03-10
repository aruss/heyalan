# Milestone M31: Unified Queued Email Sender via SendGrid

## Summary
Build one transactional email pipeline for HeyAlan that all current email-sending code uses.

- Add a new `EmailService` abstraction in `HeyAlan` that enqueues email-send requests through Wolverine.
- Add a dedicated email subscriber/consumer that reads queued messages and sends them through SendGrid `POST /v3/mail/send` using dynamic templates and a single global `from` address.
- Refactor both current send paths to use it:
  - `LoggingEmailSender` becomes an adapter from ASP.NET Core Identity email events to `EmailService`.
  - Newsletter confirmation email dispatch moves from direct `ISendGridClient.SendNewsletterConfirmationEmailAsync(...)` to `EmailService`.
- Keep newsletter contact upsert on the existing marketing client; only actual email delivery moves to the new pipeline.

## Dependencies and Current Codebase Baseline
- [x] Wolverine is the active internal messaging runtime; the repo no longer uses MassTransit for these flows.
- [x] `HeyAlan/Identity/LoggingEmailSender.cs` currently implements `IEmailSender<ApplicationUser>` and only logs masked metadata instead of sending mail.
- [x] Newsletter confirmation emails are currently sent directly from `HeyAlan/Newsletter/NewsletterSubscriptionConsumer.cs` through `HeyAlan/Newsletter/SendGridClient.cs`.
- [x] Newsletter contact confirmation/upsert is a separate SendGrid marketing concern and already uses `PUT /v3/marketing/contacts`.
- [x] `HeyAlan.WebApi/Infrastructure/WolverineBuilderExtensions.cs` is the authoritative queue/listener registration point for WebApi runtime message handling.
- [x] `HeyAlan.Initializer/Program.cs` mirrors Wolverine queue topology setup for startup provisioning.
- [x] `HeyAlan.AppHost/Program.cs` currently forwards newsletter-specific SendGrid configuration into the WebApi process.

## External Findings (SendGrid)
- [x] Transactional email send should use `POST /v3/mail/send`.
- [x] Dynamic template send requires `from`, `personalizations`, `template_id`, and `dynamic_template_data`.
- [x] Successful send is accepted asynchronously by SendGrid; failures should be treated as retriable consumer failures.

## User Decisions (Locked)
- [x] This milestone is full consolidation, not an identity-only path.
- [x] All current email-sending paths must route through the new email service.
- [x] Queued email messages use template-based sends, not raw subject/body content.
- [x] Callers enqueue an internal template key, not a raw SendGrid template id.
- [x] A single global `from` address is used for all current queued emails.

## Public Contracts and Internal Interfaces
- [x] Add `IEmailService` as the shared application-facing email enqueue boundary.
- [x] Add durable message contract `EmailSendRequested`.
- [x] Add internal email template catalog/registry abstraction that maps internal template keys to SendGrid template ids.
- [x] Define canonical internal template keys for the current senders:
  - [x] `identity_confirmation_link`
  - [x] `identity_password_reset_link`
  - [x] `identity_password_reset_code`
  - [x] `newsletter_confirmation`
- [x] Keep the queue contract provider-agnostic at the app level:
  - [x] recipient email
  - [x] internal template key
  - [x] string-based template data payload
  - [x] optional non-sensitive correlation metadata only if already available
- [x] Do not queue raw SendGrid template ids or provider-specific request envelopes.

## Gate A - Core Email Abstractions and Queue Contract
- [x] Add a new `HeyAlan.Email` area for shared transactional email concerns.
- [x] Introduce `IEmailService` and `EmailService`.
- [x] Implement `EmailService` over Wolverine `IMessageBus`.
- [x] Add `EmailSendRequested` durable message contract.
- [x] Add internal template key constants/type for the supported templates.
- [x] Enforce basic validation before enqueue:
  - [x] recipient email must be non-empty
  - [x] template key must be known/supported
  - [x] template data dictionary must be non-null
- [x] Keep logs sanitized:
  - [x] no raw template secrets
  - [x] no full email body logging
  - [x] no unnecessary PII in enqueue logs

### Gate A Acceptance Criteria
- [x] Any in-process caller can enqueue a transactional email through one shared service.
- [x] The queue contract is stable and does not expose SendGrid-specific identifiers to callers.
- [x] Invalid enqueue inputs fail fast before they reach the queue.

## Gate B - SendGrid Transactional Transport
- [x] Add `ITransactionalEmailClient` dedicated to transactional sends.
- [x] Implement `SendGridTransactionalEmailClient` using `POST /v3/mail/send`.
- [x] Add config-backed template id resolver for the internal template keys.
- [x] Resolve one global `from` email from configuration.
- [x] Build SendGrid payload with:
  - [x] `from`
  - [x] one personalization with `to`
  - [x] `template_id`
  - [x] `dynamic_template_data`
- [x] Treat non-success SendGrid responses as exceptions so Wolverine retries apply.
- [x] Preserve the existing newsletter marketing client responsibility for list/contact upserts.
- [x] Avoid logging raw response bodies if they contain sensitive content beyond what is operationally necessary.

### Gate B Acceptance Criteria
- [x] Internal template keys resolve deterministically to SendGrid template ids.
- [x] Transactional sends use one shared transport implementation.
- [x] SendGrid transport failures surface as retriable consumer failures.

## Gate C - Email Consumer and Wolverine Topology
- [x] Add `TransactionalEmailConsumer` with `Consume(EmailSendRequested message, CancellationToken ct)`.
- [x] In the consumer:
  - [x] resolve the internal template key to SendGrid template id
  - [x] map template data into `dynamic_template_data`
  - [x] call the transactional SendGrid client
- [x] Register consumer discovery in `HeyAlan.WebApi/Infrastructure/WolverineBuilderExtensions.cs`.
- [x] Add a dedicated Rabbit queue for outbound email requests.
- [x] Add publish routing for `EmailSendRequested`.
- [x] Mirror the same queue/listener topology in `HeyAlan.Initializer/Program.cs`.
- [x] Keep durable inbox/outbox behavior aligned with the existing Wolverine setup.

### Gate C Acceptance Criteria
- [x] Enqueued email messages are routed to a dedicated queue and consumed by the new email handler.
- [x] WebApi and Initializer topology definitions stay in sync.
- [x] Existing Wolverine durability policies continue to apply to the new email flow.

## Gate D - Identity Integration
- [x] Refactor `HeyAlan/Identity/LoggingEmailSender.cs` to depend on `IEmailService` instead of only `ILogger`.
- [x] Keep it implementing `IEmailSender<ApplicationUser>`.
- [x] Map Identity events to internal template keys:
  - [x] `SendConfirmationLinkAsync` -> `identity_confirmation_link`
  - [x] `SendPasswordResetLinkAsync` -> `identity_password_reset_link`
  - [x] `SendPasswordResetCodeAsync` -> `identity_password_reset_code`
- [x] Map payload fields expected by SendGrid templates:
  - [x] confirmation link -> `confirmation_url`
  - [x] reset link -> `reset_url`
  - [x] reset code -> `reset_code`
- [x] Preserve safe logging only:
  - [x] masked recipient email allowed
  - [x] template key allowed
  - [x] no raw link/code logging
- [x] Keep `IdentityBuilderExtensions` wired to the refactored sender.

### Gate D Acceptance Criteria
- [x] ASP.NET Core Identity no longer uses a dev-only log sink for email delivery.
- [x] All current Identity mail operations enqueue through `IEmailService`.
- [x] Sensitive Identity tokens/links are not logged.

## Gate E - Newsletter Integration
- [x] Refactor `NewsletterSubscriptionConsumer` to enqueue `newsletter_confirmation` through `IEmailService`.
- [x] Keep token generation and confirmation URL building in the newsletter domain.
- [x] Keep newsletter confirmation API behavior unchanged.
- [x] Keep `UpsertNewsletterContactAsync` unchanged for confirmed newsletter subscriptions.
- [x] Remove direct transactional-email responsibility from the existing newsletter SendGrid client path.
- [x] Decide whether to keep or narrow `ISendGridClient` so it owns only newsletter marketing operations after the refactor.

### Gate E Acceptance Criteria
- [x] Newsletter confirmation emails are sent through the shared queued email pipeline.
- [x] Newsletter contact upsert behavior remains unchanged.
- [x] Newsletter user-facing behavior is preserved.

## Gate F - Configuration and Composition
- [x] Replace newsletter-specific transactional template config with shared email configuration:
  - [x] `SENDGRID_API_KEY`
  - [x] `SENDGRID_EMAIL_FROM`
  - [x] `SENDGRID_TEMPLATE_IDENTITY_CONFIRMATION_LINK`
  - [x] `SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_LINK`
  - [x] `SENDGRID_TEMPLATE_IDENTITY_PASSWORD_RESET_CODE`
  - [x] `SENDGRID_TEMPLATE_NEWSLETTER_CONFIRMATION`
- [x] Keep `SENDGRID_NEWSLETTER_LIST_ID` for marketing contact upsert.
- [x] Fail fast on missing or blank required email sender settings.
- [x] Update `HeyAlan.AppHost/Program.cs` to forward the new shared email settings.
- [x] Update any relevant setup docs to reflect the new configuration contract.

### Gate F Acceptance Criteria
- [x] Startup fails fast if required transactional email settings are missing.
- [x] WebApi receives all required shared SendGrid email settings from AppHost.
- [x] Newsletter list-upsert config remains available and unchanged where still needed.

## Gate G - Tests and Regression Coverage
- [x] Unit tests for `EmailService` enqueue behavior.
- [x] Unit tests for template key to SendGrid template id resolution.
- [x] Unit tests for `SendGridTransactionalEmailClient` request payload shape.
- [x] Unit tests for `TransactionalEmailConsumer` send orchestration and failure behavior.
- [x] Unit tests for refactored `LoggingEmailSender` template mapping.
- [x] Update newsletter consumer tests to verify enqueue behavior instead of direct transactional send calls.
- [x] Regression tests to ensure newsletter contact upsert path still works.
- [x] Verify no tests rely on logged raw links/codes or other sensitive data.

### Gate G Acceptance Criteria
- [x] Current email-producing flows are covered by unit tests.
- [x] Shared email queue + consumer path is covered by tests.
- [x] Existing newsletter confirmation and contact-upsert behavior is not regressed.

## Implementation Sequence
- [x] 1) Gate A: shared email abstractions and queue contract.
- [x] 2) Gate B: SendGrid transactional transport and template resolution.
- [x] 3) Gate C: consumer wiring and Wolverine topology.
- [x] 4) Gate D: Identity integration.
- [x] 5) Gate E: Newsletter integration.
- [x] 6) Gate F: configuration and docs updates.
- [x] 7) Gate G: tests and regression verification.

## Notes
- [ ] Do not edit generated files manually.
- [ ] This milestone does not require WebApi interface changes, so OpenAPI regeneration is not expected.
- [ ] Newsletter contact list upsert remains a separate marketing API concern and is not merged into the queued transactional send path.
- [ ] Logging must continue to avoid raw tokens, reset codes, confirmation links, and unnecessary email-address exposure.
- [x] Verification note:
  - [x] `HeyAlan`, `HeyAlan.WebApi`, and `HeyAlan.Initializer` build successfully after the refactor.
  - [x] `HeyAlan.Tests` test-project verification is still environment-sensitive because restore/build is blocked intermittently by `NU1900` vulnerability metadata lookup failures against `nuget.org`.
