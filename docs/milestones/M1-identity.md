# Milestone M1: Identity Integration & Secure Auth

## Goal
Integrate ASP.NET Core Identity into the existing `StoryBoard` solution so parents/admins can sign in via `StoryBoard.WebApp` (Next.js App Router) while `StoryBoard.WebApi` owns user state. Use cookie-based sessions through the existing `/api` proxy and support local email/password plus Google OAuth.

## Scope
- **Backend (`StoryBoard.WebApi`):** Integrate Identity with `MainDataContext` (PostgreSQL via Npgsql) and expose minimal API auth endpoints at `/auth`.
- **Frontend (`StoryBoard.WebApp`):** Implement login/registration UI and route protection using the existing `/api` rewrite to the WebApi.
- **Protocol:** HttpOnly cookie session management shared through the proxy with `SameSite=Lax`.
- **Features:** Local login, registration, logout.

## Non-Goals (Out of Scope)
- Mobile App API (OpenIddict/Tokens deferred to M2).
- Two-Factor Authentication (2FA).
- Forgot Password / Email Confirmation flows (basic placeholders only).
- User Profile management UI (Edit profile, change password).
- Google OAuth (later milestone).

## Gate A: Identity Foundation (Backend)
- [x] Create `ApplicationUser` (inherits from `IdentityUser`) in `StoryBoard.Data`.
- [x] Update `MainDataContext` to inherit from `IdentityDbContext<ApplicationUser>` and preserve existing entity mappings.
- [x] Add EF Core migration from `StoryBoard.Initializer` for the Identity schema (PostgreSQL).
- [x] Configure Identity services in `StoryBoard.WebApi/Program.cs` and map Identity API endpoints via `app.MapGroup("/auth").MapIdentityApi<ApplicationUser>()`.
- [x] Seed an admin user via `StoryBoard.Initializer` after migrations; only create when the user does not exist (use `ADMIN_EMAIL`/`ADMIN_PASSWORD`).
- [x] Pass `ADMIN_EMAIL` and `ADMIN_PASSWORD` from `StoryBoard.AppHost` to the initializer; fail fast if missing.
- [x] Configure Cookie Policy for interoperability:
    - `SameSite=Lax` (Essential for OAuth redirects to work)
    - `SecurePolicy=Always`

## Gate B: Local Login (Email/Password)
- [x] **Frontend:** Create Login Page (`/login`) with Email/Password form.
- [x] **Frontend:** Use `/api/auth/*` calls (rewritten by `StoryBoard.WebApp/src/proxy.ts`) for login/register/logout.
- [x] **Backend:** Use the built-in Identity API endpoints (ex: `/auth/manage/info`) for session hydration; no custom `/auth/me`.
- [x] **Frontend:** Add `proxy.ts` guard to protect `/admin` routes (redirect to `/login` if auth cookie is missing).
- [x] **Frontend:** Verify logout clears cookie via `/api/auth/logout`.

## Gate C: Implement IEmailSender 
- [x] Add IEmailSender to the StoryBoard.WebApi project, for development cases just send the email to log info stream. 

## Risks & Notes
- **Cookie Domains:** The `/api` proxy keeps cookies same-origin in dev. In production with separate domains, set the cookie `Domain` accordingly.
- **Data Protection:** Persist keys (volume or Redis) so auth cookies survive container restarts.
- **Next.js 16 proxy:** `middleware.ts` conflicts with `proxy.ts`; auth guard must live in `src/proxy.ts`.
- **Login page build:** `useSearchParams()` requires Suspense; keep return URL parsing in the server `page.tsx` and pass to a client form component.
- **Session hydration:** `POST /auth/manage/info` returns email + confirmation state; UI should not depend on custom `/auth/me` fields.
