# CivicFlow Entra ID Dual-Mode Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add optional Microsoft Entra ID (MSAL SPA + dual JwtBearer API + app roles) while keeping seed-user JWT for Docker and CI.

**Architecture:** Local HS256 JWT stays the default scheme. When `AzureAd:TenantId` and `AzureAd:ClientId` are set, a second JwtBearer scheme validates Entra access tokens. A synchronizer upserts `Users` by `oid` and rewrites claims to CivicFlow `UserId` + role so existing `[Authorize(Roles)]` and `CurrentUser` do not change. Tests use a stand-in issuer/key (`AzureAd:SigningKey`) so CI never calls Microsoft.

**Tech Stack:** .NET 9, JwtBearer, EF Core SQL Server, xUnit, React 19, `@azure/msal-browser`.

## Global Constraints

- Target framework: `net9.0` (nullable enable, implicit usings).
- Error JSON: `{ "status": 404, "message": "...", "traceId": "..." }` (403/409 use the same shape via `AppException`).
- Roles (exact strings): `Citizen`, `Employee`, `Supervisor`, `Administrator`.
- Seed password (local/dev): `CivicFlow!dev1`.
- Password sentinel for Entra-provisioned users: `ENTRA`.
- 403 no/unknown/multiple app roles: `An Entra app role of Citizen, Employee, Supervisor, or Administrator is required.`
- 403 inactive: `This account is inactive.`
- 409 email collision: `An account with this email already exists.`
- 403 admin role change on Entra user: `Role is managed in Entra ID.`
- Password login for Entra user: 401 `Invalid email or password.` (do not reveal Entra).
- Do not call `login.microsoftonline.com` from tests.
- Do not invent Blob, groups, BFF, live tenant, or Entra-only mode.
- Commit after every task on `feat/phase2-entra-id`. Do not commit `.superpowers/`.
- `GET /api/auth/me` is required: the SPA stores the Entra access token, which does not contain CivicFlow `UserId`; after MSAL login the client must load profile from this endpoint.

---

## File map

```text
src/CivicFlow.Domain/Entities/User.cs                    (+ EntraObjectId)
src/CivicFlow.Infrastructure/CivicFlowDbContext.cs       (filtered unique index)
src/CivicFlow.Infrastructure/Migrations/*EntraObjectId*  (new)
src/CivicFlow.Application/Auth/EntraIdentity.cs          (record)
src/CivicFlow.Application/Auth/EntraUserSynchronizer.cs  (JIT upsert)
src/CivicFlow.Application/Auth/AuthService.cs            (block Entra password login)
src/CivicFlow.Application/Admin/AdminDtos.cs             (+ IsEntraUser)
src/CivicFlow.Application/Admin/AdminService.cs          (403 Entra role change)
src/CivicFlow.Api/Auth/AuthenticationExtensions.cs       (dual JwtBearer)
src/CivicFlow.Api/Auth/EntraUserSyncMiddleware.cs
src/CivicFlow.Api/Controllers/AuthController.cs          (+ GET me)
src/CivicFlow.Api/Program.cs                             (wire auth + middleware)
tests/CivicFlow.Tests/EntraAuthTests.cs
tests/CivicFlow.Tests/EntraTokenHelper.cs
tests/CivicFlow.Tests/EntraApiFactory.cs
frontend/src/auth/msal.ts
frontend/src/auth/AuthContext.tsx
frontend/src/pages/LoginPage.tsx
frontend/src/pages/admin/UsersPage.tsx
frontend/package.json                                    (+ @azure/msal-browser)
infra/main.bicep                                         (optional AzureAd settings)
README.md
```

---

### Task 1: EntraObjectId column and migration

**Files:**
- Modify: `src/CivicFlow.Domain/Entities/User.cs`
- Modify: `src/CivicFlow.Infrastructure/CivicFlowDbContext.cs` (User entity config)
- Create: EF migration `AddUserEntraObjectId` under `src/CivicFlow.Infrastructure/Migrations/`
- Test: existing `dotnet test CivicFlow.sln`

**Interfaces:**
- Consumes: current `User` + `HasIndex(Email)` unique
- Produces: `User.EntraObjectId` (`string?`, max 64); filtered unique index `IX_Users_EntraObjectId` where not null

- [ ] **Step 1: Add the property**

```csharp
public string? EntraObjectId { get; set; }
```

on `User` after `PasswordHash`.

In `CivicFlowDbContext` User config, after the Email index:

```csharp
entity.Property(x => x.EntraObjectId).HasMaxLength(64);
entity.HasIndex(x => x.EntraObjectId)
    .IsUnique()
    .HasFilter("[EntraObjectId] IS NOT NULL");
```

- [ ] **Step 2: Add the migration**

From `C:\Users\derri\Projects\CivicFlow`:

```powershell
dotnet ef migrations add AddUserEntraObjectId --project src/CivicFlow.Infrastructure --startup-project src/CivicFlow.Api
```

Expected: new migration file that `AddColumn` `EntraObjectId` nvarchar(64) null and creates the filtered unique index. Do not hand-edit the snapshot except via this command.

- [ ] **Step 3: Run tests**

```powershell
dotnet test CivicFlow.sln
```

Expected: existing suite PASS (migrate-on-startup applies the column).

- [ ] **Step 4: Commit**

```powershell
git add src/CivicFlow.Domain/Entities/User.cs src/CivicFlow.Infrastructure/CivicFlowDbContext.cs src/CivicFlow.Infrastructure/Migrations
git commit -m "feat: add Users.EntraObjectId for Entra JIT linking"
```

---

### Task 2: Block password login for Entra users (TDD)

**Files:**
- Modify: `src/CivicFlow.Application/Auth/AuthService.cs`
- Modify: `tests/CivicFlow.Tests/AuthApiTests.cs` (or new `EntraLoginGuardTests.cs` using SqlServer collection)
- Test: `dotnet test --filter Login_entra_user_password_returns_401`

**Interfaces:**
- Consumes: `User.EntraObjectId`, `AuthService.LoginAsync`
- Produces: if `user.EntraObjectId` is not null, throw `UnauthorizedException("Invalid email or password.")` **before** `VerifyHashedPassword`

- [ ] **Step 1: Failing test**

Insert an Entra-linked user in the test DB via the fixture factory’s services, then POST login. Add to `tests/CivicFlow.Tests/AuthApiTests.cs`:

The fixture only exposes `Client`. Extend `SqlServerFixture` to also expose `Factory` (it already has `Factory` public). Use:

```csharp
[Fact]
public async Task Login_entra_provisioned_user_returns_401()
{
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CivicFlowDbContext>();
    var entraUser = new User
    {
        FirstName = "Entra",
        LastName = "Citizen",
        Email = "entra-citizen@example.test",
        PasswordHash = EntraUserSynchronizer.PasswordSentinel,
        EntraObjectId = "11111111-1111-1111-1111-111111111111",
        RoleId = (int)RoleName.Citizen,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsActive = true
    };
    db.Users.Add(entraUser);
    await db.SaveChangesAsync();

    var response = await _client.PostAsJsonAsync("/api/auth/login", new
    {
        email = "entra-citizen@example.test",
        password = "CivicFlow!dev1"
    });
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

`AuthApiTests` currently only has `_client`. Change the constructor to also keep `SqlServerFixture.Factory` as `_factory`.

Until Task 3, `EntraUserSynchronizer.PasswordSentinel` does not exist — in **this** task use the literal `"ENTRA"` in the test. Task 3 will introduce the constant and this test can switch, or Task 3 uses the same literal. Prefer defining the constant in this task in a tiny static class to avoid churn:

Create `src/CivicFlow.Application/Auth/EntraAuthConstants.cs`:

```csharp
namespace CivicFlow.Application.Auth;

public static class EntraAuthConstants
{
    public const string PasswordSentinel = "ENTRA";
}
```

Use that in the test.

- [ ] **Step 2: FAIL**

```powershell
dotnet test tests/CivicFlow.Tests/CivicFlow.Tests.csproj --filter Login_entra_provisioned_user_returns_401
```

Expected: FAIL (login may 500 if Identity hasher rejects `ENTRA`, or 200 if it somehow verifies). The test must not pass until the EntraObjectId short-circuit exists.

- [ ] **Step 3: Implement**

In `LoginAsync`, after the null/inactive check and **before** `VerifyHashedPassword`:

```csharp
if (!string.IsNullOrEmpty(user.EntraObjectId))
{
    throw new UnauthorizedException("Invalid email or password.");
}
```

- [ ] **Step 4: PASS**

Same filter. Expected: PASS. Then `dotnet test CivicFlow.sln` — all existing tests PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/CivicFlow.Application/Auth src/CivicFlow.Tests/AuthApiTests.cs tests/CivicFlow.Tests/AuthApiTests.cs
git commit -m "feat: reject password login for Entra-provisioned users"
```

(Stage the actual test path `tests/CivicFlow.Tests/AuthApiTests.cs` only.)

---

### Task 3: EntraUserSynchronizer (TDD)

**Files:**
- Create: `src/CivicFlow.Application/Auth/EntraIdentity.cs`
- Create: `src/CivicFlow.Application/Auth/EntraUserSynchronizer.cs`
- Create: `tests/CivicFlow.Tests/EntraUserSynchronizerTests.cs`
- Test: `dotnet test --filter EntraUserSynchronizer`

**Interfaces:**
- Consumes: `IAppDbContext`, `EntraAuthConstants.PasswordSentinel`, `RoleName`, `User`, `ConflictException`, `ForbiddenException`
- Produces:

```csharp
namespace CivicFlow.Application.Auth;

public sealed record EntraIdentity(
    string ObjectId,
    string Email,
    string FirstName,
    string LastName,
    RoleName Role);

public sealed class EntraUserSynchronizer(IAppDbContext db)
{
    public async Task<User> SyncAsync(EntraIdentity identity, CancellationToken cancellationToken = default)
}
```

`SyncAsync` rules:
1. Normalize email trim; `ObjectId` trim; names trim; if first/last empty after trim, use `"Entra"` / `"User"`.
2. Find `Users` including `Role` where `EntraObjectId == identity.ObjectId`.
3. If found: if `!IsActive` throw `ForbiddenException("This account is inactive.")`. Update Email, FirstName, LastName, RoleId from identity; `UpdatedAt = UtcNow`; SaveChanges; return user with Role loaded.
4. If not found: try insert new user (`PasswordHash = EntraAuthConstants.PasswordSentinel`, `EntraObjectId = oid`, `IsActive = true`, `DepartmentId = null`). Catch EF unique email failure **or** pre-check `AnyAsync(Email == email)`: if a row exists with that email, throw `ConflictException("An account with this email already exists.")`.
5. Return the user with `Role` included.

- [ ] **Step 1: Failing tests** in `tests/CivicFlow.Tests/EntraUserSynchronizerTests.cs`

Use `[Collection("SqlServer")]` and resolve `CivicFlowDbContext` from `fixture.Factory.Services`. Tests:

1. `Sync_new_oid_creates_user_with_citizen_role`
2. `Sync_same_oid_does_not_duplicate`
3. `Sync_updates_role_from_identity`
4. `Sync_inactive_throws_403`
5. `Sync_email_collision_with_seed_throws_409` (use `citizen@civicflow.local`)

Construct `EntraUserSynchronizer` with the scoped `CivicFlowDbContext` (it implements `IAppDbContext`).

- [ ] **Step 2: FAIL** — class missing.

- [ ] **Step 3: Implement** the record + class as specified.

- [ ] **Step 4: PASS** `dotnet test --filter EntraUserSynchronizer` then full `dotnet test CivicFlow.sln`.

- [ ] **Step 5: Commit** `feat: add Entra user JIT synchronizer`

---

### Task 4: Dual JwtBearer, sync middleware, GET /api/auth/me (TDD)

**Files:**
- Create: `src/CivicFlow.Api/Auth/AuthenticationExtensions.cs`
- Create: `src/CivicFlow.Api/Auth/EntraUserSyncMiddleware.cs`
- Modify: `src/CivicFlow.Api/Program.cs`
- Modify: `src/CivicFlow.Api/Controllers/AuthController.cs`
- Create: `tests/CivicFlow.Tests/EntraTokenHelper.cs`
- Create: `tests/CivicFlow.Tests/EntraApiFactory.cs`
- Create: `tests/CivicFlow.Tests/EntraAuthTests.cs`
- Test: `dotnet test --filter EntraAuth`

**Interfaces:**
- Scheme names: `"CivicFlow"` (local, also default) and `"Entra"`.
- Local JWT stays `JwtBearerDefaults` **or** named `"CivicFlow"` with `ForwardDefaultSelector` reading unsigned `iss`. Use:

```csharp
public static class CivicFlowAuthSchemes
{
    public const string Local = "CivicFlow";
    public const string Entra = "Entra";
}
```

`AddCivicFlowAuthentication(IServiceCollection, IConfiguration)`:
- Always add policy scheme default authenticate that selects:
  - if Authorization bearer token `iss` equals `Jwt:Issuer` (or missing Entra config) → Local
  - else → Entra
- Always `AddJwtBearer(Local, …)` with today’s HS256 parameters (`RoleClaimType = ClaimTypes.Role`, `NameClaimType = ClaimTypes.NameIdentifier`).
- If `AzureAd:TenantId` and `AzureAd:ClientId` both non-empty:
  - `AddJwtBearer(Entra, …)`
  - If `AzureAd:SigningKey` is set (tests): do **not** set Authority; `TokenValidationParameters` ValidIssuer = `AzureAd:Issuer`, ValidAudience = `AzureAd:Audience`, IssuerSigningKey = symmetric key from SigningKey, RoleClaimType = `"roles"` **and** `ClaimTypes.Role`, NameClaimType = `"oid"`.
  - Else (production): `Authority = (AzureAd:Instance ?? "https://login.microsoftonline.com/") + TenantId + "/v2.0"`, `Audience = AzureAd:Audience`.
- Register `EntraUserSynchronizer` as scoped.
- `AddAuthorization`

`EntraUserSyncMiddleware`: if user is unauthenticated, next(). If authenticated via Local scheme (or `iss` is CivicFlow), next(). If Entra: parse claims:

- oid: `oid` or `http://schemas.microsoft.com/identity/claims/objectidentifier`
- email: `email` or `preferred_username`
- given/family or split `name`
- roles: all `ClaimTypes.Role` and `"roles"` values

If not exactly one parseable `RoleName` among {Citizen, Employee, Supervisor, Administrator}: write 403 envelope with spec message, do not call next.
Else call `EntraUserSynchronizer.SyncAsync`. On `AppException`, map status/message/traceId. On success, replace `HttpContext.User` with a new `ClaimsPrincipal` containing `ClaimTypes.NameIdentifier` = `user.UserId.ToString()`, `ClaimTypes.Role` and `"role"` = role name, `email`. Then next().

Place middleware **after** `UseAuthentication` and **before** `UseAuthorization`.

`GET /api/auth/me` `[Authorize]`: return `AuthResponse` **without minting a new token** (Token can be empty string). Fields from DB via `CurrentUser.GetUserId` after sync. Add `AuthService.GetCurrentAsync(int userId)`.

`EntraTokenHelper.CreateAccessToken(role, oid, email, first, last)` using the test issuer/audience/key.

`EntraApiFactory` : like `CivicFlowApiFactory` plus:

```
AzureAd:TenantId = test
AzureAd:ClientId = civicflow-test-api
AzureAd:Audience = api://civicflow-test
AzureAd:Issuer = https://login.microsoftonline.com/test/v2.0
AzureAd:SigningKey = TEST_ENTRA_SIGNING_KEY_32CHARS_MIN!!
```

Use database `CivicFlow_Entra_Test` (or append `_Entra` to the LocalDB name) so it does not race the default collection. New `[Collection("EntraSql")]`.

Tests:
1. `Entra_citizen_token_me_returns_profile_and_persists_user`
2. `Entra_token_without_roles_returns_403`
3. `Entra_token_unknown_role_returns_403`
4. `Entra_second_request_same_oid_does_not_duplicate`
5. `Local_seed_login_still_works_on_entra_factory`

Also add a test on the **default** factory (AzureAd unset): `Login_seeded_citizen_returns_token` still passes (already exists). Optional: document that Entra scheme is absent — skip asserting internals.

Default `Program.cs` currently always AddJwtBearer unnamed. Replace that block with `builder.Services.AddCivicFlowAuthentication(builder.Configuration);` plus `AddScoped<EntraUserSynchronizer>()`.

- [ ] **Step 1: Write failing EntraAuthTests** (factory + helper + tests). FAIL until schemes/middleware/me exist.
- [ ] **Step 2: FAIL**
- [ ] **Step 3: Implement extensions, middleware, `/api/auth/me`, Program.cs wiring**
- [ ] **Step 4: PASS** Entra filter + full `dotnet test CivicFlow.sln`
- [ ] **Step 5: Commit** `feat: accept Entra JWT beside local JWT and sync users`

---

### Task 5: Admin cannot change Entra roles (TDD)

**Files:**
- Modify: `src/CivicFlow.Application/Admin/AdminDtos.cs` (`bool IsEntraUser`)
- Modify: `src/CivicFlow.Application/Admin/AdminService.cs` (`ListUsersAsync` projection + `UpdateUserRoleAsync` guard)
- Modify: `frontend/src/pages/admin/UsersPage.tsx` (hide role `<select>` and save when `isEntraUser`; show text “Managed in Entra ID”)
- Test: `tests/CivicFlow.Tests/AdminEntraRoleTests.cs`

**Interfaces:**
- Consumes: `User.EntraObjectId`
- Produces: `AdminUserDto.IsEntraUser` = `EntraObjectId != null`; update throws `ForbiddenException("Role is managed in Entra ID.")`

- [ ] **Step 1: Failing test** using Entra collection or SqlServer collection: insert Entra user, login as `admin@civicflow.local`, `PUT /api/admin/users/{id}/role` `{ "role": "Employee" }` → 403 and that message.
- [ ] **Step 2: FAIL**
- [ ] **Step 3: Implement DTO + service guard + list flag; UI hide**
- [ ] **Step 4: PASS** + `npm run build` in `frontend`
- [ ] **Step 5: Commit** `feat: keep Entra app roles out of admin role editor`

---

### Task 6: MSAL login button (config-gated)

**Files:**
- Modify: `frontend/package.json` (dependency `@azure/msal-browser`)
- Create: `frontend/src/auth/msal.ts`
- Modify: `frontend/src/api/client.ts` (`AUTH_SOURCE_KEY = 'civicflow.authSource'`, get/set helpers)
- Modify: `frontend/src/auth/AuthContext.tsx`
- Modify: `frontend/src/pages/LoginPage.tsx`

**Interfaces:**
- Consumes: `VITE_ENTRA_CLIENT_ID`, `VITE_ENTRA_TENANT_ID`, `VITE_ENTRA_API_SCOPE`; `GET /api/auth/me` → `{ userId, email, role, firstName, lastName, token? }`
- Produces: `isEntraConfigured(): boolean`; `signInWithMicrosoft(): Promise<AuthUser>`; logout clears token + MSAL if `civicflow.authSource === 'entra'`

`msal.ts`:

```ts
export function isEntraConfigured(): boolean {
  return Boolean(
    import.meta.env.VITE_ENTRA_CLIENT_ID &&
      import.meta.env.VITE_ENTRA_TENANT_ID &&
      import.meta.env.VITE_ENTRA_API_SCOPE,
  )
}
```

If not configured, `getMsal()` returns null and LoginPage does not render the Microsoft button.

If configured, `PublicClientApplication` with `auth: { clientId, authority: https://login.microsoftonline.com/${tenantId}, redirectUri: window.location.origin }`. `loginPopup({ scopes: [VITE_ENTRA_API_SCOPE] })` then `acquireTokenSilent` / `acquireTokenPopup`. `setStoredToken(accessToken)`; `localStorage.setItem('civicflow.authSource', 'entra')`; `apiFetch<AuthUser>('/api/auth/me')` then set `user` with that profile **and** `token: accessToken`.

`AuthContext.login` (password) sets `civicflow.authSource` to `local`.

`decodeUserFromToken` remains for local JWT restore on refresh. On init, if `civicflow.authSource === 'entra'` and a token exists, call `/api/auth/me` instead of decoding (Entra `sub`/`oid` is not an int). If me fails, clear storage.

LoginPage: if `isEntraConfigured()`, a button **Sign in with Microsoft** above the demo accounts.

- [ ] **Step 1: `npm install @azure/msal-browser` in `frontend`**
- [ ] **Step 2: Implement msal.ts, client helpers, AuthContext, LoginPage**
- [ ] **Step 3: `npm run build` in `frontend` — expected succeed (no Vite Entra vars; button omitted)**
- [ ] **Step 4: Commit** `feat: add optional MSAL Sign in with Microsoft`

---

### Task 7: README and Bicep placeholders

**Files:**
- Modify: `README.md` (Entra section after Azure deploy; dual-mode; do not claim live tenant)
- Modify: `infra/main.bicep` (API app settings `AzureAd__TenantId`, `AzureAd__ClientId`, `AzureAd__Audience` with empty string values or parameters default `''`)
- Modify: `.github/workflows/azure-deploy.yml` only if adding optional `VITE_ENTRA_*` workflow inputs with defaults empty — **do not require them**

**Interfaces:**
- Produces: documented two-app registration steps from spec §9; talking points from spec §13

- [ ] **Step 1: Write README + Bicep settings (empty defaults)**
- [ ] **Step 2: Visual review — no live URL, no Entra-only claim**
- [ ] **Step 3: Commit** `docs: document optional Entra ID dual-mode setup`

---

## Execution notes

- TDD for Tasks 2–5. Tasks 1, 6, 7 do not need a red test first (schema/UI/docs).
- Task 4 must not fetch OpenID metadata in CI: `AzureAd:SigningKey` disables Authority.
- Do not add Microsoft.Identity.Web.
- After Task 4, `CurrentUser.GetUserId` must keep working for Entra requests because middleware rewrites `NameIdentifier` to the int.

## Self-review checklist (author)

1. Spec coverage: dual schemes, JIT oid, sentinel, 403/409 copy, admin lock, MSAL gated, README/Bicep, tests without Microsoft, `/api/auth/me` for SPA profile — each has a task.
2. Placeholders: none intended.
3. Names: `EntraUserSynchronizer`, `EntraIdentity`, `EntraAuthConstants.PasswordSentinel`, schemes `CivicFlow` / `Entra`.
