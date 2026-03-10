# Milestone M32: Team Member Invitations

## Summary
Implement subscription-scoped team invitations across onboarding, member settings, invitation redemption, and the existing queued email pipeline.

- Add a real `SubscriptionInvitation` domain model for pending invites.
- Use Square team members as onboarding suggestions via a new `SquareService` team-members read operation.
- Show and manage invitations and current members in the admin settings members page.
- Send invitation emails through the shared `IEmailService` and SendGrid template pipeline from M31.
- Redeem invitation links into `SubscriptionUser` memberships and switch the accepting user into the invited subscription context.

## Dependencies and Current Codebase Baseline
- [x] `SubscriptionUser` and `SubscriptionUserRole` already exist in `HeyAlan/Data/Entities/SubscriptionUser.cs`.
- [x] Square connection and token handling already exist in `HeyAlan/SquareIntegration/SquareService.cs`.
- [x] Onboarding already has an invitations step and endpoint, but it currently only marks the step complete.
- [x] The onboarding UI already contains a placeholder invite step in `HeyAlan.WebApp/src/app/onboarding/page.tsx`.
- [x] The members settings route already exists at `HeyAlan.WebApp/src/app/admin/settings/members/page.tsx` but is still a placeholder.
- [x] Shared queued transactional email infrastructure already exists from M31 via `HeyAlan.Email.IEmailService`.
- [x] The current session model exposes `activeSubscriptionId`, but the backend currently derives it from membership ordering rather than persisted user preference.
- [x] There is currently no subscription switcher UI, so invitation acceptance must explicitly land the user in the invited subscription.

## User Decisions (Locked)
- [x] Invitation role support for this milestone matches the existing enum: `Owner` and `Member`.
- [x] If an invited user is not signed in, the invite link must redirect to login first and then return to the invite URL using the existing `returnUrl` mechanism.
- [x] After accepting an invite, the user must switch into the invited subscription immediately.
- [x] Invitation email delivery must use the existing mail service from M31, not a separate sender.
- [x] Implementing the transport itself is not part of this milestone; this milestone consumes the shared email service boundary.

## Public Contracts and Internal Interfaces
- [ ] Add `SubscriptionInvitation` entity to the core data model.
- [ ] Add a persisted active subscription selector to `ApplicationUser`, e.g. `ActiveSubscriptionId`.
- [ ] Extend `Subscription` with invitation navigation.
- [ ] Extend `ISquareService` with a `GetTeamMembersAsync` read operation returning minimal team-member data.
- [ ] Add invitation-oriented DTOs following the existing `Input` / `Result` naming pattern:
  - [ ] create invitation
  - [ ] list invitations and members
  - [ ] resend invitation
  - [ ] revoke invitation
  - [ ] accept invitation
  - [ ] update member role
  - [ ] delete member
- [ ] Extend onboarding state/read DTOs so step 4 can render:
  - [ ] current invitations
  - [ ] current members
  - [ ] Square team-member suggestions
  - [ ] available roles
- [ ] Extend `EmailTemplateKey` and SendGrid template resolution with a new invitation template key.

## Gate A - Data Model and Membership Context
- [ ] Add `SubscriptionInvitation` entity with at least:
  - [ ] `Id`
  - [ ] `SubscriptionId`
  - [ ] `Email`
  - [ ] `Role`
  - [ ] secure token storage (`TokenHash` or equivalent non-plaintext token representation)
  - [ ] `InvitedByUserId`
  - [ ] `SentAtUtc`
  - [ ] `AcceptedAtUtc`
  - [ ] `RevokedAtUtc`
  - [ ] `ExpiresAtUtc`
  - [ ] audit fields
- [ ] Add `DbSet<SubscriptionInvitation>` and EF mapping in `MainDataContext`.
- [ ] Add invitation navigation to `Subscription`.
- [ ] Add persisted `ActiveSubscriptionId` to `ApplicationUser`.
- [ ] Update current-user active-subscription resolution to prefer `ApplicationUser.ActiveSubscriptionId` when valid.
- [ ] Keep authorization and multi-tenant boundaries subscription-scoped.

### Gate A Acceptance Criteria
- [ ] Invitations can be stored, queried, revoked, and accepted without relying on ephemeral state.
- [ ] Invite tokens are not stored in plaintext.
- [ ] Active subscription is explicitly persisted and no longer depends only on membership ordering.

## Gate B - Invitation Domain Service and Email Enqueue
- [ ] Add a dedicated invitation service in `HeyAlan` to own invitation creation, resend, revoke, lookup, and acceptance rules.
- [ ] Enforce invitation validation rules:
  - [ ] email must be non-empty and normalized
  - [ ] role must be supported
  - [ ] duplicate active invite behavior is deterministic
  - [ ] accepting revoked, expired, invalid, or already-accepted invites is handled deterministically
- [ ] Generate invite links against the public WebApp base URL.
- [ ] Enqueue invitation emails through `IEmailService` using `EmailSendRequested`.
- [ ] Add a new email template key such as `subscription_invitation`.
- [ ] Add SendGrid template configuration for the invitation template in the same shared config area used by M31.
- [ ] Keep logging sanitized:
  - [ ] no raw invite tokens
  - [ ] no full invitation links in logs
  - [ ] no unnecessary PII beyond masked email metadata

### Gate B Acceptance Criteria
- [ ] Creating or resending an invitation enqueues a transactional email through the shared mail service.
- [ ] Invitation business rules live in one domain service rather than controllers.
- [ ] Sensitive invitation data is not logged.

## Gate C - Square Team Member Read Model
- [ ] Extend `ISquareService` and `SquareService` with `GetTeamMembersAsync`.
- [ ] Use the existing Square connection/token resolution path for authenticated reads.
- [ ] Return only minimal onboarding-safe fields:
  - [ ] display name
  - [ ] email
- [ ] Treat missing Square connection and empty team lists as safe non-fatal states for onboarding UI.

### Gate C Acceptance Criteria
- [ ] Onboarding can load Square team-member suggestions from the connected merchant account.
- [ ] Team-member reads reuse the consolidated Square service instead of duplicating client logic.

## Gate D - WebApi Invitation and Member Management
- [ ] Add owner-authorized subscription member-management endpoints for:
  - [ ] listing invitations and current members
  - [ ] creating invitations
  - [ ] resending invitations
  - [ ] revoking invitations
  - [ ] updating member role
  - [ ] deleting a member
- [ ] Add public or anonymous-safe invite lookup endpoint by token.
- [ ] Add authenticated invite acceptance endpoint.
- [ ] Replace the current onboarding invitations endpoint implementation so it creates invitations instead of only completing the step.
- [ ] Extend onboarding state endpoint payload with invitation-step read data.
- [ ] Keep endpoint error mapping consistent with current WebApi patterns.

### Gate D Acceptance Criteria
- [ ] Owners can fully manage invitations and members through stable WebApi contracts.
- [ ] Invite acceptance is API-backed and idempotent enough for browser retries.
- [ ] Onboarding step 4 now performs real invitation work.

## Gate E - Auth and Invitation Redemption Flow
- [ ] Add a WebApp invite route, e.g. `/invite/[token]`.
- [ ] If unauthenticated, redirect to `/login` with the invite URL as `returnUrl`.
- [ ] Update auth redirect behavior so invite return URLs are honored instead of always forcing `/onboarding` for non-onboarded users.
- [ ] On accept:
  - [ ] create `SubscriptionUser` membership if missing
  - [ ] mark invitation accepted
  - [ ] set `ApplicationUser.ActiveSubscriptionId` to the invited subscription
  - [ ] refresh session state so the frontend sees the new active subscription
- [ ] Preserve current onboarding behavior for normal logins outside the invite path.

### Gate E Acceptance Criteria
- [ ] Users can open an invite link, sign in, return to the invite route, and accept successfully.
- [ ] Accepted users land in the invited subscription context immediately.
- [ ] Invite flow does not strand users on an auto-created personal owner subscription.

## Gate F - Onboarding Invitations Step
- [ ] Replace the disabled placeholder UI in onboarding step 4 with real invitation UX.
- [ ] Render:
  - [ ] Square team-member suggestions
  - [ ] pending invitations
  - [ ] current members
  - [ ] invite form with email and role
- [ ] Submit invitation creation through the onboarding invitation endpoint.
- [ ] Keep the step dependency that invitations require completed Square connection.
- [ ] Allow continuing to finalize after invitation handling is done.

### Gate F Acceptance Criteria
- [ ] Onboarding step 4 can create invitations and reflect the resulting state.
- [ ] Square suggestions reduce manual re-entry for team invites.
- [ ] Finalize still works after the invitations step completes.

## Gate G - Admin Settings Members Page
- [ ] Build `HeyAlan.WebApp/src/app/admin/settings/members/page.tsx` using existing admin primitives.
- [ ] Add "invite team member" button that opens a drawer with:
  - [ ] email input
  - [ ] role dropdown
- [ ] Add invitations table with actions:
  - [ ] delete
  - [ ] resend invitation
  - [ ] copy invitation link
- [ ] Implement delete-invitation confirmation in a drawer.
- [ ] Add current-members table with actions:
  - [ ] change role
  - [ ] delete member
- [ ] Wire all mutations to refresh the page state cleanly.

### Gate G Acceptance Criteria
- [ ] The members settings page is fully functional for invitation and membership management.
- [ ] Drawer-based create and delete flows match the requested UX.
- [ ] Current members and pending invitations are clearly separated.

## Gate H - Tests and Regression Coverage
- [ ] Unit tests for invitation service create/resend/revoke/accept behavior.
- [ ] Unit tests for token validation and duplicate acceptance behavior.
- [ ] Unit tests for active-subscription switching after acceptance.
- [ ] Unit tests for invitation email enqueue payload and template-key usage.
- [ ] API tests for owner authorization and forbidden behavior for non-owners.
- [ ] API tests for invalid, expired, revoked, and already-accepted invite scenarios.
- [ ] WebApp tests for onboarding invite flow.
- [ ] WebApp tests for admin members page drawer flows and table actions.
- [ ] WebApp tests for invite acceptance and session refresh behavior.

### Gate H Acceptance Criteria
- [ ] Core invitation lifecycle behavior is covered by automated tests.
- [ ] Authorization boundaries are regression-protected.
- [ ] Frontend invite management and acceptance paths are regression-protected.

## Implementation Sequence
- [ ] 1) Gate A: data model and persisted active subscription.
- [ ] 2) Gate B: invitation domain service and email enqueue integration.
- [ ] 3) Gate C: Square team-member read support.
- [ ] 4) Gate D: WebApi invitation and member-management endpoints.
- [ ] 5) Gate E: auth and invite redemption flow.
- [ ] 6) Gate F: onboarding invitations step.
- [ ] 7) Gate G: admin settings members page.
- [ ] 8) Gate H: tests and regression verification.

## Notes
- [ ] Use the shared queued mail service from M31 for invitation delivery.
- [ ] Do not edit generated files manually.
- [ ] After schema changes, stop for developer-created migrations per repo rules.
- [ ] After WebApi interface changes that affect the generated client, hand off for OpenAPI client regeneration.
- [ ] Keep logs free of raw invite tokens, full invite links, and unnecessary PII.
