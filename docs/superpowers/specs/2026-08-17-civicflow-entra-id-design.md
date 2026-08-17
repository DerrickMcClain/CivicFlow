# CivicFlow Phase 2 — Entra ID (dual mode)

**Date:** 2026-08-17  
**Status:** Draft — pending user review  
**Repo location:** `C:\Users\derri\Projects\CivicFlow`  
**Depends on:** MVP (`docs/superpowers/specs/2026-08-16-civicflow-mvp-design.md`)  
**Next slice after this:** Azure Blob documents (separate spec)

## 1. Purpose

Add Microsoft Entra ID sign-in so CivicFlow can demonstrate the public-sector Microsoft identity pattern (SPA + API + app roles) without breaking the Docker / CI seed-user demo.

This slice is **code + documentation**. A live Entra tenant is not required to merge. Filling in tenant IDs later should not need a redesign.

## 2. Locked decisions

| Decision | Choice |
| --- | --- |
| Mode | Dual: seed JWT when `AzureAd` is unset; Entra when TenantId + ClientId are set |
| Roles | Entra **app roles** named exactly `Citizen`, `Employee`, `Supervisor`, `Administrator` |
| SPA | MSAL.js public client; `Authorization: Bearer` to the API |
| API auth | Two JwtBearer schemes; select by token `iss` |
| User row | JIT upsert `Users` by Entra `oid`; SQL `UserId` remains the FK for cases |
| Live tenant | Out of this slice (README + Bicep placeholders only) |
| Blob / groups / BFF / Key Vault | Out of this slice |

Rejected alternatives: Entra-only (breaks Docker/CI), SQL-owned roles for Entra users, cookie BFF, `Microsoft.Identity.Web` owning the pipeline, one custom handler that tries both tokens.

## 3. Scope

### 3.1 In this slice

1. Optional `AzureAd` configuration on the API; second JwtBearer scheme validating Entra access tokens via JWKS when configured
2. Issuer-based scheme selection (`CivicFlow` issuer → local JWT; Entra `iss` → Entra scheme)
3. `Users.EntraObjectId` (nullable unique) + EF migration
4. Middleware: after Entra authentication, upsert user, sync role from token, rewrite `NameIdentifier` / role claims to CivicFlow `UserId` + `RoleName` so `CurrentUser` and existing `[Authorize(Roles = …)]` stay unchanged
5. Local `POST /api/auth/login` unchanged for seed users; refuse password login when `EntraObjectId` is set
6. Admin role-change API/UI does not override Entra users (403 or hide control)
7. React: MSAL only when Vite Entra vars are set; **Sign in with Microsoft** on the login page; sign-out calls MSAL logout for Entra sessions
8. `apiFetch` continues to send `Authorization: Bearer` from the stored access token
9. Bicep optional app settings for `AzureAd__*` and frontend `VITE_ENTRA_*`
10. README: two-app registration procedure (SPA + API), four app roles, scope, user assignment, env vars
11. Tests: existing suite green with `AzureAd` unset; new tests for “scheme absent when unset”, JIT upsert, missing/unknown role → 403, using a stand-in issuer/key (no call to Microsoft)

### 3.2 Explicitly out

- Creating or verifying a real Entra tenant
- Azure Blob / documents
- Entra security groups
- BFF / cookies
- Personal Microsoft accounts (MSA) as the demo path — app roles require a work/school (Entra ID) tenant
- Multi-tenant “sign in from any org”
- Mapping department from Entra; Entra-provisioned staff may have `DepartmentId = null`
- Changing workflow, RBAC matrix, or error JSON shape

## 4. Architecture

```text
Docker / CI (AzureAd unset)
  SPA demo buttons → POST /api/auth/login → CivicFlow JWT → existing pipeline

When AzureAd + VITE_ENTRA_* are set
  SPA MSAL.js → Entra authorize → access token (aud = API, roles = app roles)
        → API JwtBearer "Entra" (JWKS)
        → EntraUserSyncMiddleware (upsert Users, rewrite claims)
        → existing controllers / RequestService / AdminService
```

Units (one job each):

| Unit | Does | Depends on |
| --- | --- | --- |
| Local JwtBearer | Validates CivicFlow HS256 JWT | `Jwt:*` (already required) |
| Entra JwtBearer | Validates Entra access token (issuer, audience, JWKS) | `AzureAd:TenantId`, `AzureAd:ClientId`, `AzureAd:Audience` |
| Scheme selector | Picks handler from unsigned `iss` | Token header/payload `iss` |
| `EntraUserSyncMiddleware` | JIT user + claim rewrite; 403 if no/unknown role or inactive | `IAppDbContext`, Entra claims `oid`, `email`, `name`, `roles` |
| MSAL login button | Optional UI; stores access token for `apiFetch` | Vite `VITE_ENTRA_*` |
| README / Bicep | How to turn Entra on later | Placeholders only |

Existing `AuthService`, `JwtTokenService`, `CurrentUser`, and role attributes do not learn Entra APIs. After sync, claims look like today’s local JWT (`NameIdentifier` = int user id, `ClaimTypes.Role` = `Citizen` / …).

## 5. Token and scheme rules

- One request carries **one** bearer token. Never accept a CivicFlow JWT via the Entra scheme or the reverse.
- Local issuer remains `Jwt:Issuer` (today `CivicFlow`). Entra issuer is `https://login.microsoftonline.com/{tenantId}/v2.0`.
- Entra validation: `ValidateIssuer`, `ValidateAudience`, `ValidateIssuerSigningKey` via OpenID metadata JWKS. `AzureAd:Audience` is the API App ID URI `api://<api-client-id>` (must match access-token `aud`).
- `RoleClaimType` for the Entra scheme must read Entra `roles` (and map onto `ClaimTypes.Role` during sync).
- If `AzureAd:TenantId` or `AzureAd:ClientId` is missing/blank, **do not register** the Entra scheme. Selector only uses local JWT. No metadata call at startup.
- Unauthenticated requests still 401. Authenticated Entra token with zero recognized app roles → **403** with the standard `{ status, message, traceId }` envelope (not 401).
- Multiple recognized app roles on one token → **403** (exactly one CivicFlow role per user for this MVP-compatible model).
- Unknown role string → **403**.

## 6. Data

`Users` gains:

- `EntraObjectId` (`string?`, max 64, **unique** filtered index where not null)

Rules:

- Seed users: `EntraObjectId` null, `PasswordHash` remains a real Identity hash.
- Entra-provisioned users: `EntraObjectId` = `oid`, `PasswordHash` = the constant sentinel `ENTRA` (not a real Identity hash). `AuthService.LoginAsync` **returns 401** if `EntraObjectId` is set, before password verify.
- Lookup: `EntraObjectId == oid` only. If missing, **insert**. If insert hits the email unique index (seed user already owns that email) → **409** `An account with this email already exists.` Do not stamp `oid` onto a password user.
- On upsert: set `Email`, `FirstName`/`LastName` from `given_name`/`family_name` or split `name`; default `"Entra"` / `"User"` if missing. Set `RoleId` from the single app role. `IsActive = false` → 403 and do not process the request.
- Role from Entra **wins** on every request. Admin `PUT /api/admin/users/{id}/role` on a row with `EntraObjectId` set → **403** `Role is managed in Entra ID.` Admin UI hides the role editor for those rows.
- Email unique index stays. One EF migration. No new tables.

## 7. Frontend

- `VITE_ENTRA_CLIENT_ID`, `VITE_ENTRA_TENANT_ID`, `VITE_ENTRA_API_SCOPE` (example: `api://<api-client-id>/access_as_user`). All three required to show the Microsoft button; otherwise seed login only.
- MSAL `PublicClientApplication`: `redirectUri` = `window.location.origin`, `authority` = `https://login.microsoftonline.com/{tenantId}`.
- Sign-in uses `loginPopup` then `acquireTokenSilent` for the API scope, with `acquireTokenPopup` if silent fails. Store the **access token** in `civicflow.token` (same key `apiFetch` already reads). Store `civicflow.authSource=entra`. Sign-out calls `logoutPopup`, then clears storage.
- Seed login still uses `POST /api/auth/login` and overwrites the same token key with `civicflow.authSource=local`.
- Do not put MSAL in the Docker frontend build unless those Vite vars are passed at image build time. Default Compose remains seed-only.
- CORS: Entra mode on split Azure hosting already has `Cors:AllowedOrigin`. Login-with-Microsoft is browser-to-Entra then SPA-to-API; no extra CORS for Entra itself.

## 8. Configuration

API (`appsettings.json` keys; env override):

```text
AzureAd__TenantId
AzureAd__ClientId          // API application (client) ID
AzureAd__Audience          // API App ID URI or client ID; must match token aud
```

Optional: `AzureAd__Instance` default `https://login.microsoftonline.com/`.

Bicep: add these as app settings, values empty or parameters with defaults empty so current deploy docs still work. Frontend workflow already injects `VITE_API_BASE_URL`; add optional `VITE_ENTRA_*` inputs later without making them required.

JWT local settings stay required for Docker/CI even when Entra is on (seed login still exists).

## 9. Tenant setup (documentation only)

README steps (operator does this when they have a tenant):

1. Create **API** app registration: expose scope `access_as_user`; add app roles `Citizen`, `Employee`, `Supervisor`, `Administrator` (`value` = those strings, allowed member type User).
2. Create **SPA** app registration: SPA redirect `http://localhost` (Docker), `http://localhost:5173` (Vite), and the Azure frontend origin; grant delegated permission to the API scope; no client secret.
3. Assign demo users to the **API** app roles (Enterprise applications → Users and groups).
4. Set API `AzureAd__*` and rebuild/redeploy the SPA with `VITE_ENTRA_*`.

Single-tenant (`Accounts in this organizational directory only`).

## 10. Errors

| Case | HTTP | Message (stable) |
| --- | --- | --- |
| Missing/invalid token | 401 | existing JWT middleware behavior |
| Entra authenticated, no/unknown/multiple app roles | 403 | `An Entra app role of Citizen, Employee, Supervisor, or Administrator is required.` |
| Inactive user | 403 | `This account is inactive.` |
| Email collision with seed user | 409 | `An account with this email already exists.` |
| Admin role change on Entra user | 403 | `Role is managed in Entra ID.` |
| Password login for Entra-provisioned user | 401 | `Invalid email or password.` (do not leak that the account is Entra) |

Error JSON remains `{ status, message, traceId }`.

## 11. Tests

- Full existing `dotnet test` with `AzureAd` unset — current suite must stay green; Entra scheme not registered.
- New WebApplicationFactory tests with a **stand-in** Entra JwtBearer (test issuer + symmetric/RSA key, not Microsoft JWKS):  
  - valid token + `roles: ["Citizen"]` creates `Users` row and `GET /api/requests/my` returns 200  
  - token with no roles → 403  
  - token with `roles: ["NotARole"]` → 403  
  - second request same `oid` does not duplicate the user  
  - seed `POST /api/auth/login` still works in the same factory when local JWT is also configured
- No network calls to `login.microsoftonline.com` in CI.

## 12. Success criteria

- Docker Compose + GitHub Actions behave as today with no Entra config.
- With config present (or test stand-in), an Entra access token with one app role maps to a SQL user and the correct portal role.
- README is enough for a future tenant; this PR does not claim a live Microsoft sign-in.
- Blob remains a later spec.

## 13. Resume talking points (after implementation)

Safe to claim: optional Entra ID (MSAL SPA + JWT API + app roles), dual-mode so local demo still uses seeded JWT.  
Do not claim: Entra is required, live tenant verified, groups, BFF, or that seed users went away.
