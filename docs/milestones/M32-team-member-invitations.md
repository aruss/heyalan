# Milestone M32: Team Member Invitations

## Summary
Implement subscription-scoped team invitations across onboarding, member settings, invitation redemption, and the existing queued email pipeline.

- Add a real `SubscriptionInvitation` domain model for pending invites.
- Generate invitation link tokens through a reusable `ITokenService`.
- Use Square team members as onboarding suggestions via a new `SquareService` team-members read operation.
- Show and manage invitations and current members in the admin settings members page.
- Send invitation emails through the shared queued mail pipeline from M31.
- Redeem invitation links into `SubscriptionUser` memberships and switch the accepting user into the invited subscription context.

## Dependencies and Current Codebase Baseline
- [x] `SubscriptionUser` and `SubscriptionUserRole` already exist in `BuyAlan/Data/Entities/SubscriptionUser.cs`.
- [x] Square connection and token handling already exist in `BuyAlan/SquareIntegration/SquareService.cs`.
- [x] Onboarding already has an invitations step and endpoint, but `POST /onboarding/subscriptions/{subscriptionId}/members/invitations` currently only completes the step and does not create invitations.
- [x] The onboarding UI already contains a placeholder invite step in `BuyAlan.WebApp/src/app/onboarding/page.tsx`.
- [x] The members settings route already exists at `BuyAlan.WebApp/src/app/admin/settings/members/page.tsx` but is still a placeholder.
- [x] Shared queued transactional email infrastructure already exists from M31 via `IEmailQueuingService`, `EmailSendRequested`, and `ITransactionalEmailService`.
- [x] The current session model exposes `activeSubscriptionId`, but the backend currently derives it from membership ordering rather than persisted user preference.
- [x] There is currently no subscription switcher UI, so invitation acceptance must explicitly land the user in the invited subscription.
- [x] Current auth redirect behavior still forces non-onboarded users to `/onboarding`, so invite return URLs are not honored yet.

## User Decisions (Locked)
- [x] Invitation role support for this milestone matches the existing enum: `Owner` and `Member`.
- [x] If an invited user is not signed in, the invite link must redirect to login first and then return to the invite URL using the existing `returnUrl` mechanism.
- [x] After accepting an invite, the user must switch into the invited subscription immediately.
- [x] Invitation email delivery must use the existing mail pipeline from M31, not a separate sender.
- [x] Implementing the transport itself is not part of this milestone; this milestone consumes the shared mail boundary.
- [x] Invitation links use a random opaque token stored directly on `SubscriptionInvitation`.
- [x] Invitation tokens are not hashed and not encrypted.
- [x] Invitation tokens are generated through a reusable `ITokenService`, not inline in invitation logic.
- [x] Invitation acceptance requires authenticated user email to match the invitation email after normalization.
- [x] Accepted and revoked invitations remain stored for auditability instead of being deleted inline.
- [x] Invite-driven first login must not auto-create a personal owner subscription.
- [x] Resend reuses the currently stored invitation token and re-sends the same link.
- [x] Copy invitation link returns the currently stored invitation URL for the active invitation.

## Public Contracts and Internal Interfaces
- [x] Add `SubscriptionInvitation` entity to the core data model.
- [x] Add a persisted active subscription selector to `ApplicationUser`, e.g. `ActiveSubscriptionId`.
- [x] Extend `Subscription` with invitation navigation.
- [ ] Add reusable `ITokenService` to generate high-entropy URL-safe opaque tokens for invitation links and future link-based flows.
- [ ] Extend `ISquareService` with a `GetTeamMembersAsync` read operation returning minimal team-member data.
- [x] Add invitation-oriented DTOs following the existing `Input` / `Result` naming pattern:
  - [x] create invitation
  - [x] list invitations and members
  - [x] resend invitation
  - [x] copy invitation link
  - [x] revoke invitation
  - [x] accept invitation
  - [x] update member role
  - [x] delete member
- [x] Extend onboarding state/read DTOs so step 4 can render:
  - [x] current invitations
  - [x] current members
  - [x] Square team-member suggestions
  - [x] available roles
- [ ] Extend `EmailTemplateKey` and SendGrid template resolution with a new invitation template key.
  - [ ] Keep SendGrid mapping optional; supported invitation keys may fall back to the generic template id when no dedicated config is present.

## Gate A - Data Model and Membership Context
- [x] Add `SubscriptionInvitation` entity with at least:
  - [x] `Id`
  - [x] `SubscriptionId`
  - [x] `Email`
  - [x] `Role`
  - [x] `Token`
  - [x] unique index on `Token`
  - [x] `InvitedByUserId`
  - [x] `SentAtUtc`
  - [x] `AcceptedAtUtc`
  - [x] `RevokedAtUtc`
  - [x] `ExpiresAtUtc`
  - [x] audit fields
- [x] Add `DbSet<SubscriptionInvitation>` and EF mapping in `MainDataContext`.
- [x] Add invitation navigation to `Subscription`.
- [x] Add persisted `ActiveSubscriptionId` to `ApplicationUser`.
- [x] Update current-user active-subscription resolution to prefer `ApplicationUser.ActiveSubscriptionId` when valid.
- [x] Fall back to current membership-order behavior when the persisted active subscription is null or no longer valid.
- [ ] Keep authorization and multi-tenant boundaries subscription-scoped.

### Gate A Acceptance Criteria
- [ ] Invitations can be stored, queried, revoked, expired, and accepted without relying on ephemeral state.
- [ ] Invitation tokens are unique and directly look-upable by token.
- [ ] Active subscription is explicitly persisted and no longer depends only on membership ordering.

## Gate B - Token Service, Invitation Domain Service, and Email Enqueue
- [x] Add a reusable `ITokenService` in `BuyAlan` to own opaque token generation.
- [x] Add a dedicated invitation service in `BuyAlan` to own invitation creation, resend, copy-link lookup, revoke, lookup, and acceptance rules.
- [x] Ensure invitation creation and any future invitation token rotation go through `ITokenService`, not ad hoc random generation.
- [x] Enforce invitation validation rules:
  - [x] email must be non-empty and normalized
  - [x] role must be supported
  - [x] duplicate active invites must reuse the existing active invitation row for the same subscription and normalized email
  - [x] a subscription member cannot be re-invited
  - [x] accepting revoked, expired, invalid, or already-accepted invites is handled deterministically
  - [x] accepting user email must match invitation email
- [x] Generate invite links against the public WebApp base URL.
- [x] Enqueue invitation emails through `IEmailQueuingService` using `EmailSendRequested`.
- [x] Add a new email template key such as `subscription_invitation`.
- [x] Keep dedicated SendGrid template configuration optional; if no invitation template id is configured, the existing generic template fallback is acceptable for this milestone.
- [x] Keep invitation template payload minimal for this milestone:
  - [x] `invitation_url`
  - [x] subscription display text
- [x] Keep logging sanitized:
  - [x] no raw invite tokens in logs
  - [x] no full invite links in logs
  - [x] no unnecessary PII beyond masked email metadata
- [x] Treat invitation expiry as effectively indefinite for this milestone:
  - [x] do not expire invitations by age in domain logic
  - [x] persist a far-future `ExpiresAtUtc` sentinel because the column is required

### Gate B Acceptance Criteria
- [x] Creating or resending an invitation enqueues a transactional email through the shared mail service.
- [x] Invitation business rules live in one domain service rather than controllers.
- [x] Token generation is reusable through `ITokenService`.
- [x] Sensitive invitation data is not logged.
- [x] Invitation emails work without requiring a dedicated SendGrid template mapping.

## Gate C - Square Team Member Read Model
- [x] Extend `ISquareService` and `SquareService` with `GetTeamMembersAsync`.
- [x] Use the existing Square connection/token resolution path for authenticated reads.
- [x] Return only minimal onboarding-safe fields:
  - [x] display name
  - [x] email
- [x] Filter out unusable suggestion rows that do not have an email address.
- [x] Treat missing Square connection, unsupported Team API states, and empty team lists as safe non-fatal states for onboarding UI.

### Gate C Acceptance Criteria
- [x] Onboarding can load Square team-member suggestions from the connected merchant account.
- [x] Team-member reads reuse the consolidated Square service instead of duplicating client logic.

## Gate D - WebApi Invitation and Member Management
- [x] Add owner-authorized subscription member-management endpoints for:
  - [x] listing invitations and current members
  - [x] creating invitations
  - [x] resending invitations
  - [x] copying invitation links
  - [x] revoking invitations
  - [x] updating member role
  - [x] deleting a member
- [x] Add public or anonymous-safe invite lookup endpoint by token.
- [x] Add authenticated invite acceptance endpoint by token.
- [x] Extend onboarding state endpoint payload with invitation-step read data.
- [x] Replace the current onboarding invitations placeholder behavior with real invitation work plus explicit onboarding-step completion semantics.
- [x] Keep endpoint error mapping consistent with current WebApi patterns.
- [x] Prevent role changes or deletions that would remove the last owner from a subscription.

### Gate D Acceptance Criteria
- [x] Owners can fully manage invitations and members through stable WebApi contracts.
- [x] Invite acceptance is API-backed and idempotent enough for browser retries.
- [x] Onboarding step 4 now performs real invitation work instead of placeholder completion only.
- [x] Owner-safety rules are enforced by the API.

## Gate E - Auth and Invitation Redemption Flow
- [x] Add a WebApp invite route, e.g. `/invite/[token]`.
- [x] If unauthenticated, redirect to `/login` with the invite URL as `returnUrl`.
- [x] Update auth redirect behavior so invite return URLs are honored instead of always forcing `/onboarding` for non-onboarded users.
- [x] Suppress personal owner-subscription auto-provisioning when first login was initiated from an invite flow.
- [x] Preserve current onboarding behavior for normal logins outside the invite path.
- [x] On accept:
  - [x] create `SubscriptionUser` membership if missing
  - [x] mark invitation accepted
  - [x] set `ApplicationUser.ActiveSubscriptionId` to the invited subscription
  - [x] refresh session state so the frontend sees the new active subscription
  - [x] reject acceptance when signed-in user email does not match invitation email

### Gate E Acceptance Criteria
- [x] Users can open an invite link, sign in, return to the invite route, and accept successfully.
- [x] Accepted users land in the invited subscription context immediately.
- [x] Invite flow does not strand users on an auto-created personal owner subscription.
- [x] Forwarded or mismatched-account invite acceptance is blocked by email-match checks.

## Gate F - Onboarding Invitations Step
- [x] Replace the disabled placeholder UI in onboarding step 4 with real invitation UX.
- [ ] Render:
  - [x] Square team-member suggestions
  - [x] pending invitations
  - [x] current members
  - [x] invite form with email and role
- [x] Submit invitation creation through the real invitation/member-management API.
- [x] Keep the step dependency that invitations require completed Square connection.
- [x] Make onboarding-step completion explicit instead of depending on the old placeholder endpoint semantics.
- [x] Align the onboarding step message and behavior so docs, UI, and backend dependencies match.
- [x] Allow continuing to finalize after invitation handling is done.

### Gate F Acceptance Criteria
- [x] Onboarding step 4 can create invitations and reflect the resulting state.
- [x] Square suggestions reduce manual re-entry for team invites.
- [x] Finalize still works after the invitations step completes.

## Gate G - Admin Settings Members Page
- [x] Build `BuyAlan.WebApp/src/app/admin/settings/members/page.tsx` using existing admin primitives.
- [x] Add "invite team member" button that opens a drawer with:
  - [x] email input
  - [x] role dropdown
- [x] Add invitations table with actions:
  - [x] delete
  - [x] resend invitation
  - [x] copy invitation link
- [x] Implement delete-invitation confirmation in a drawer.
- [x] Add current-members table with actions:
  - [x] change role
  - [x] delete member
- [x] Wire all mutations to refresh the page state cleanly.

### Gate G Acceptance Criteria
- [x] The members settings page is fully functional for invitation and membership management.
- [x] Drawer-based create and delete flows match the requested UX.
- [x] Current members and pending invitations are clearly separated.

## Gate H - Tests and Regression Coverage
- [ ] Unit tests for `ITokenService` generation invariants and reasonable uniqueness.
- [ ] Unit tests for invitation service create/resend/revoke/accept behavior.
- [ ] Unit tests for token lookup, email-match enforcement, and duplicate acceptance behavior.
- [ ] Unit tests for active-subscription switching after acceptance.
- [ ] Unit tests for invitation email enqueue payload and template-key usage.
- [ ] API tests for owner authorization and forbidden behavior for non-owners.
- [ ] API tests for invalid, expired, revoked, already-accepted, and mismatched-email invite scenarios.
- [ ] Auth tests for invite-driven first login versus normal first login behavior.
- [ ] WebApp tests for onboarding invite flow.
- [ ] WebApp tests for admin members page drawer flows and table actions.
- [ ] WebApp tests for invite acceptance and session refresh behavior.

### Gate H Acceptance Criteria
- [ ] Core invitation lifecycle behavior is covered by automated tests.
- [ ] Authorization boundaries are regression-protected.
- [ ] Frontend invite management and acceptance paths are regression-protected.

## Implementation Sequence
- [ ] 1) Gate A: data model and persisted active subscription.
- [ ] 2) Gate B: reusable token service, invitation domain service, and email enqueue integration.
- [x] 3) Gate C: Square team-member read support.
- [x] 4) Gate D: WebApi invitation and member-management endpoints.
- [x] 5) Gate E: auth and invite redemption flow.
- [x] 6) Gate F: onboarding invitations step.
- [x] 7) Gate G: admin settings members page.
- [ ] 8) Gate H: tests and regression verification.

## Notes
- [ ] Use the shared queued mail service from M31 for invitation delivery.
- [ ] Do not edit generated files manually.
- [ ] After schema changes, stop for developer-created migrations per repo rules.
- [ ] After WebApi interface changes that affect the generated client, hand off for OpenAPI client regeneration.
- [ ] Keep logs free of raw invite tokens, full invite links, and unnecessary PII.
- [ ] Retention cleanup for old accepted/revoked invitations is future work and not part of M32.

