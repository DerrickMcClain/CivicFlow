# CivicFlow MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a demoable government case-management platform (ASP.NET Core + React) with enforced workflow, JWT RBAC + resource authorization, status history, audit logging, Docker local run, GitHub Actions CI, and Azure deployment.

**Architecture:** Clean layered backend (`Api` → `Application` → `Domain` ← `Infrastructure`) with a thin React + TypeScript SPA. Workflow transition rules live only in Domain and are unit-tested. Every mutating case operation writes entity + status history + audit in one EF Core transaction. Controllers do HTTP only; Application services enforce authorization.

**Tech Stack:** .NET 9, ASP.NET Core, EF Core + SQL Server, JWT Bearer, xUnit, React 18 + TypeScript + Vite + React Router + Tailwind, Docker Compose, GitHub Actions, Azure App Service + Azure SQL.

## Global Constraints

- Target framework: `net9.0` (nullable enable, implicit usings). Installed SDK on this machine is `9.0.300`.
- Illegal workflow transitions return **HTTP 409 Conflict** (never 400 for transition failures).
- Cross-citizen and role-forbidden actions return **HTTP 403**.
- Missing resources return **HTTP 404** with the standard error shape.
- Error JSON: `{ "status": 404, "message": "Service request not found.", "traceId": "..." }`.
- Request numbers: `CIV-{yyyy}-{sequence:D6}` (example `CIV-2026-000184`); sequence must be concurrency-safe (SQL Server `SEQUENCE`).
- Closed requests (`Completed`, `Cancelled`) are immutable.
- Citizens never receive notes with `IsInternal = true`.
- Employees cannot approve or reject.
- Supervisor approval/reject only from `SupervisorReview`.
- Auth MVP: local JWT + seeded role users; Entra ID is Phase 2.
- Documents / Blob / notifications / SLA / Power BI / RAG are out of MVP.
- Azure deployment is in MVP definition of done.
- Demo password (local/dev only): `CivicFlow!dev1`.
- Seed users: `citizen@civicflow.local`, `employee@civicflow.local`, `supervisor@civicflow.local`, `admin@civicflow.local`.
- Frontend styling: Tailwind CSS.
- Test runner: `dotnet test` (xUnit). Frontend typecheck: `npm run build`.
- Do not invent extra features beyond this plan and `docs/superpowers/specs/2026-08-16-civicflow-mvp-design.md`.

---

## File map

```text
CivicFlow/
├── CivicFlow.sln
├── global.json
├── .gitignore
├── .editorconfig
├── docker-compose.yml
├── README.md
├── .github/workflows/ci.yml
├── infra/main.bicep
├── src/
│   ├── CivicFlow.Api/
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── Controllers/AuthController.cs
│   │   ├── Controllers/RequestsController.cs
│   │   ├── Controllers/EmployeeRequestsController.cs
│   │   ├── Controllers/SupervisorController.cs
│   │   ├── Controllers/AdminController.cs
│   │   ├── Controllers/HealthController.cs
│   │   ├── Middleware/ExceptionHandlingMiddleware.cs
│   │   └── Properties/launchSettings.json
│   ├── CivicFlow.Application/
│   │   ├── Abstractions/IAppDbContext.cs
│   │   ├── Abstractions/IRequestNumberGenerator.cs
│   │   ├── Abstractions/IAuditLogger.cs
│   │   ├── Abstractions/IJwtTokenService.cs
│   │   ├── Auth/AuthService.cs
│   │   ├── Auth/LoginRequest.cs
│   │   ├── Auth/RegisterRequest.cs
│   │   ├── Auth/AuthResponse.cs
│   │   ├── Requests/RequestService.cs
│   │   ├── Requests/ServiceRequestDtos.cs
│   │   ├── Admin/AdminService.cs
│   │   ├── Admin/AdminDtos.cs
│   │   └── Common/AppException.cs
│   ├── CivicFlow.Domain/
│   │   ├── Enums/RoleName.cs
│   │   ├── Enums/RequestStatusName.cs
│   │   ├── Enums/Priority.cs
│   │   ├── Workflow/WorkflowPolicy.cs
│   │   ├── Workflow/RequestNumberFormatter.cs
│   │   └── Entities/*.cs
│   └── CivicFlow.Infrastructure/
│       ├── CivicFlowDbContext.cs
│       ├── RequestNumberGenerator.cs
│       ├── AuditLogger.cs
│       ├── JwtTokenService.cs
│       ├── Seed/DbSeeder.cs
│       └── DependencyInjection.cs
├── tests/CivicFlow.Tests/
│   ├── WorkflowPolicyTests.cs
│   ├── RequestNumberFormatterTests.cs
│   ├── AuthApiTests.cs
│   └── RequestAuthorizationTests.cs
└── frontend/
    ├── package.json
    ├── src/main.tsx
    ├── src/App.tsx
    ├── src/api/client.ts
    ├── src/auth/AuthContext.tsx
    └── src/pages/*.tsx
```

---

### Task 1: Solution scaffold

**Files:**
- Create: `global.json`, `.gitignore`, `.editorconfig`, `CivicFlow.sln`, `src/CivicFlow.Domain/CivicFlow.Domain.csproj`, `src/CivicFlow.Application/CivicFlow.Application.csproj`, `src/CivicFlow.Infrastructure/CivicFlow.Infrastructure.csproj`, `src/CivicFlow.Api/CivicFlow.Api.csproj`, `tests/CivicFlow.Tests/CivicFlow.Tests.csproj`
- Test: `dotnet build CivicFlow.sln`

**Interfaces:**
- Consumes: none
- Produces: five projects targeting `net9.0`; Application references Domain; Infrastructure references Application; Api references Infrastructure; Tests reference Api + Domain

- [ ] **Step 1: Write repo hygiene files**

`global.json`:
```json
{
  "sdk": {
    "version": "9.0.300",
    "rollForward": "latestFeature"
  }
}
```

`.gitignore` must ignore `bin/`, `obj/`, `.vs/`, `node_modules/`, `dist/`, `.env`, `appsettings.*.local.json`, user secrets, OS junk.

`.editorconfig`: `indent_size = 4` for `*.cs`, `indent_size = 2` for `*.json,*.yml,*.ts,*.tsx`.

- [ ] **Step 2: Create projects and references**

Run from `C:\Users\derri\Projects\CivicFlow`:

```powershell
dotnet new sln -n CivicFlow --force
dotnet new classlib -n CivicFlow.Domain -o src/CivicFlow.Domain -f net9.0
dotnet new classlib -n CivicFlow.Application -o src/CivicFlow.Application -f net9.0
dotnet new classlib -n CivicFlow.Infrastructure -o src/CivicFlow.Infrastructure -f net9.0
dotnet new webapi -n CivicFlow.Api -o src/CivicFlow.Api -f net9.0 --use-controllers
dotnet new xunit -n CivicFlow.Tests -o tests/CivicFlow.Tests -f net9.0
dotnet sln add src/CivicFlow.Domain src/CivicFlow.Application src/CivicFlow.Infrastructure src/CivicFlow.Api tests/CivicFlow.Tests
dotnet add src/CivicFlow.Application reference src/CivicFlow.Domain
dotnet add src/CivicFlow.Infrastructure reference src/CivicFlow.Application
dotnet add src/CivicFlow.Api reference src/CivicFlow.Infrastructure
dotnet add tests/CivicFlow.Tests reference src/CivicFlow.Api
dotnet add tests/CivicFlow.Tests reference src/CivicFlow.Domain
```

Delete template `Class1.cs` / `WeatherForecast*` files. Enable nullable in every csproj if not already.

- [ ] **Step 3: Verify the solution builds**

Run: `dotnet build CivicFlow.sln -c Release`
Expected: `Build succeeded` with 0 errors.

- [ ] **Step 4: Commit**

```powershell
git add global.json .gitignore .editorconfig CivicFlow.sln src tests
git commit -m "chore: scaffold clean-layered .NET 9 solution"
```

---

### Task 2: Workflow policy (TDD)

**Files:**
- Create: `src/CivicFlow.Domain/Enums/RoleName.cs`, `src/CivicFlow.Domain/Enums/RequestStatusName.cs`, `src/CivicFlow.Domain/Workflow/WorkflowPolicy.cs`
- Test: `tests/CivicFlow.Tests/WorkflowPolicyTests.cs`

**Interfaces:**
- Consumes: none
- Produces: `WorkflowPolicy.CanTransition(RequestStatusName from, RequestStatusName to, RoleName role, bool isOwner) -> bool` and `WorkflowPolicy.IsTerminal(RequestStatusName status) -> bool`

- [ ] **Step 1: Write the failing tests**

```csharp
using CivicFlow.Domain.Enums;
using CivicFlow.Domain.Workflow;
using Xunit;

namespace CivicFlow.Tests;

public class WorkflowPolicyTests
{
    [Fact]
    public void Citizen_owner_can_submit_draft()
    {
        Assert.True(WorkflowPolicy.CanTransition(
            RequestStatusName.Draft, RequestStatusName.Submitted, RoleName.Citizen, isOwner: true));
    }

    [Fact]
    public void Citizen_non_owner_cannot_submit()
    {
        Assert.False(WorkflowPolicy.CanTransition(
            RequestStatusName.Draft, RequestStatusName.Submitted, RoleName.Citizen, isOwner: false));
    }

    [Fact]
    public void Employee_cannot_approve()
    {
        Assert.False(WorkflowPolicy.CanTransition(
            RequestStatusName.SupervisorReview, RequestStatusName.Approved, RoleName.Employee, isOwner: false));
    }

    [Fact]
    public void Supervisor_can_approve_from_supervisor_review()
    {
        Assert.True(WorkflowPolicy.CanTransition(
            RequestStatusName.SupervisorReview, RequestStatusName.Approved, RoleName.Supervisor, isOwner: false));
    }

    [Fact]
    public void Completed_is_immutable()
    {
        Assert.False(WorkflowPolicy.CanTransition(
            RequestStatusName.Completed, RequestStatusName.UnderReview, RoleName.Supervisor, isOwner: false));
    }

    [Fact]
    public void Citizen_cannot_cancel_under_review()
    {
        Assert.False(WorkflowPolicy.CanTransition(
            RequestStatusName.UnderReview, RequestStatusName.Cancelled, RoleName.Citizen, isOwner: true));
    }

    [Fact]
    public void Supervisor_cannot_cancel_approved()
    {
        Assert.False(WorkflowPolicy.CanTransition(
            RequestStatusName.Approved, RequestStatusName.Cancelled, RoleName.Supervisor, isOwner: false));
    }

    [Fact]
    public void Citizen_owner_response_returns_to_under_review()
    {
        Assert.True(WorkflowPolicy.CanTransition(
            RequestStatusName.AdditionalInfoRequired, RequestStatusName.UnderReview, RoleName.Citizen, isOwner: true));
    }

    [Theory]
    [InlineData(RequestStatusName.Completed)]
    [InlineData(RequestStatusName.Cancelled)]
    public void Terminal_statuses(RequestStatusName status)
    {
        Assert.True(WorkflowPolicy.IsTerminal(status));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/CivicFlow.Tests --filter FullyQualifiedName~WorkflowPolicyTests -v n`
Expected: FAIL because `WorkflowPolicy` does not exist.

- [ ] **Step 3: Write minimal implementation**

`src/CivicFlow.Domain/Enums/RoleName.cs`:
```csharp
namespace CivicFlow.Domain.Enums;

public enum RoleName
{
    Citizen = 1,
    Employee = 2,
    Supervisor = 3,
    Administrator = 4
}
```

`src/CivicFlow.Domain/Enums/RequestStatusName.cs`:
```csharp
namespace CivicFlow.Domain.Enums;

public enum RequestStatusName
{
    Draft = 1,
    Submitted = 2,
    UnderReview = 3,
    AdditionalInfoRequired = 4,
    EmployeeRecommendation = 5,
    SupervisorReview = 6,
    Approved = 7,
    Rejected = 8,
    Completed = 9,
    Cancelled = 10
}
```

`src/CivicFlow.Domain/Workflow/WorkflowPolicy.cs`:
```csharp
using CivicFlow.Domain.Enums;

namespace CivicFlow.Domain.Workflow;

public static class WorkflowPolicy
{
    public static bool IsTerminal(RequestStatusName status) =>
        status is RequestStatusName.Completed or RequestStatusName.Cancelled;

    public static bool CanTransition(
        RequestStatusName from,
        RequestStatusName to,
        RoleName role,
        bool isOwner)
    {
        if (IsTerminal(from))
        {
            return false;
        }

        return (from, to, role) switch
        {
            (RequestStatusName.Draft, RequestStatusName.Submitted, RoleName.Citizen) => isOwner,
            (RequestStatusName.Submitted, RequestStatusName.UnderReview, RoleName.Employee or RoleName.Supervisor) => true,
            (RequestStatusName.UnderReview, RequestStatusName.AdditionalInfoRequired, RoleName.Employee or RoleName.Supervisor) => true,
            (RequestStatusName.AdditionalInfoRequired, RequestStatusName.UnderReview, RoleName.Employee or RoleName.Supervisor) => true,
            (RequestStatusName.AdditionalInfoRequired, RequestStatusName.UnderReview, RoleName.Citizen) => isOwner,
            (RequestStatusName.UnderReview, RequestStatusName.EmployeeRecommendation, RoleName.Employee or RoleName.Supervisor) => true,
            (RequestStatusName.EmployeeRecommendation, RequestStatusName.SupervisorReview, RoleName.Employee or RoleName.Supervisor) => true,
            (RequestStatusName.SupervisorReview, RequestStatusName.Approved, RoleName.Supervisor) => true,
            (RequestStatusName.SupervisorReview, RequestStatusName.Rejected, RoleName.Supervisor) => true,
            (RequestStatusName.Approved, RequestStatusName.Completed, RoleName.Employee or RoleName.Supervisor) => true,
            (RequestStatusName.Rejected, RequestStatusName.Completed, RoleName.Employee or RoleName.Supervisor) => true,
            (RequestStatusName.Draft, RequestStatusName.Cancelled, RoleName.Citizen) => isOwner,
            (RequestStatusName.Submitted, RequestStatusName.Cancelled, RoleName.Citizen) => isOwner,
            (_, RequestStatusName.Cancelled, RoleName.Supervisor or RoleName.Administrator)
                when from is not RequestStatusName.Approved and not RequestStatusName.Rejected => true,
            _ => false
        };
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/CivicFlow.Tests --filter FullyQualifiedName~WorkflowPolicyTests -v n`
Expected: all tests PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/CivicFlow.Domain tests/CivicFlow.Tests/WorkflowPolicyTests.cs
git commit -m "feat: enforce legal service-request status transitions"
```

---

### Task 3: Request number formatter (TDD)

**Files:**
- Create: `src/CivicFlow.Domain/Workflow/RequestNumberFormatter.cs`
- Test: `tests/CivicFlow.Tests/RequestNumberFormatterTests.cs`

**Interfaces:**
- Consumes: none
- Produces: `RequestNumberFormatter.Format(int year, int sequence) -> string`

- [ ] **Step 1: Write the failing test**

```csharp
using CivicFlow.Domain.Workflow;
using Xunit;

namespace CivicFlow.Tests;

public class RequestNumberFormatterTests
{
    [Fact]
    public void Formats_year_and_six_digit_sequence()
    {
        Assert.Equal("CIV-2026-000184", RequestNumberFormatter.Format(2026, 184));
    }

    [Fact]
    public void Rejects_non_positive_sequence()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RequestNumberFormatter.Format(2026, 0));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/CivicFlow.Tests --filter FullyQualifiedName~RequestNumberFormatterTests -v n`
Expected: FAIL because `RequestNumberFormatter` does not exist.

- [ ] **Step 3: Write minimal implementation**

```csharp
namespace CivicFlow.Domain.Workflow;

public static class RequestNumberFormatter
{
    public static string Format(int year, int sequence)
    {
        if (year is < 2000 or > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(year));
        }

        if (sequence < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }

        return $"CIV-{year}-{sequence:D6}";
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/CivicFlow.Tests --filter FullyQualifiedName~RequestNumberFormatterTests -v n`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/CivicFlow.Domain/Workflow/RequestNumberFormatter.cs tests/CivicFlow.Tests/RequestNumberFormatterTests.cs
git commit -m "feat: format CIV-year-sequence request numbers"
```

---

### Task 4: Domain entities and exceptions

**Files:**
- Create: `src/CivicFlow.Domain/Enums/Priority.cs`, `src/CivicFlow.Domain/Entities/Role.cs`, `src/CivicFlow.Domain/Entities/Department.cs`, `src/CivicFlow.Domain/Entities/User.cs`, `src/CivicFlow.Domain/Entities/RequestStatus.cs`, `src/CivicFlow.Domain/Entities/ServiceRequestType.cs`, `src/CivicFlow.Domain/Entities/ServiceRequest.cs`, `src/CivicFlow.Domain/Entities/RequestStatusHistory.cs`, `src/CivicFlow.Domain/Entities/CaseNote.cs`, `src/CivicFlow.Domain/Entities/AssignmentHistory.cs`, `src/CivicFlow.Domain/Entities/AuditLog.cs`
- Create: `src/CivicFlow.Application/Common/AppException.cs`
- Test: extend `tests/CivicFlow.Tests/WorkflowPolicyTests.cs` is not required; verify with `dotnet build`

**Interfaces:**
- Consumes: `RoleName`, `RequestStatusName`
- Produces: entity types listed below; `NotFoundException`, `ForbiddenException`, `ConflictException` in Application

Entity property names (lock these — later tasks depend on them):

- `User`: `UserId`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `RoleId`, `DepartmentId`, `CreatedAt`, `UpdatedAt`, `IsActive`
- `Role`: `RoleId`, `RoleName`
- `Department`: `DepartmentId`, `DepartmentName`, `Description`
- `ServiceRequestType`: `ServiceRequestTypeId`, `DepartmentId`, `Name`, `Description`, `IsActive`
- `ServiceRequest`: `RequestId`, `RequestNumber`, `CitizenId`, `RequestTypeId`, `AssignedEmployeeId`, `StatusId`, `Title`, `Description`, `Priority`, `CreatedAt`, `UpdatedAt`, `SubmittedAt`, `CompletedAt`
- `RequestStatus`: `StatusId`, `StatusName`, `IsTerminal`
- `RequestStatusHistory`: `HistoryId`, `RequestId`, `OldStatusId`, `NewStatusId`, `ChangedByUserId`, `Reason`, `ChangedAt`
- `CaseNote`: `NoteId`, `RequestId`, `AuthorId`, `NoteText`, `CreatedAt`, `IsInternal`
- `AssignmentHistory`: `AssignmentId`, `RequestId`, `AssignedFromUserId`, `AssignedToUserId`, `AssignedByUserId`, `AssignedAt`, `Reason`
- `AuditLog`: `AuditLogId`, `UserId`, `Action`, `EntityType`, `EntityId`, `OldValues`, `NewValues`, `IpAddress`, `Timestamp`

`Priority` enum: `Low = 1`, `Medium = 2`, `High = 3`.

`AppException` types:

```csharp
namespace CivicFlow.Application.Common;

public abstract class AppException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

public sealed class NotFoundException(string message) : AppException(404, message);
public sealed class ForbiddenException(string message) : AppException(403, message);
public sealed class ConflictException(string message) : AppException(409, message);
public sealed class ValidationException(string message) : AppException(400, message);
```

- [ ] **Step 1: Create entity classes**

`Priority.cs`:
```csharp
namespace CivicFlow.Domain.Enums;

public enum Priority
{
    Low = 1,
    Medium = 2,
    High = 3
}
```

`Role.cs`:
```csharp
using CivicFlow.Domain.Enums;

namespace CivicFlow.Domain.Entities;

public class Role
{
    public int RoleId { get; set; }
    public RoleName RoleName { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}
```

`Department.cs`:
```csharp
namespace CivicFlow.Domain.Entities;

public class Department
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<ServiceRequestType> RequestTypes { get; set; } = new List<ServiceRequestType>();
}
```

`User.cs`:
```csharp
namespace CivicFlow.Domain.Entities;

public class User
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public int? DepartmentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public Role Role { get; set; } = null!;
    public Department? Department { get; set; }
}
```

`RequestStatus.cs`:
```csharp
using CivicFlow.Domain.Enums;

namespace CivicFlow.Domain.Entities;

public class RequestStatus
{
    public int StatusId { get; set; }
    public RequestStatusName StatusName { get; set; }
    public bool IsTerminal { get; set; }
}
```

`ServiceRequestType.cs`:
```csharp
namespace CivicFlow.Domain.Entities;

public class ServiceRequestType
{
    public int ServiceRequestTypeId { get; set; }
    public int DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Department Department { get; set; } = null!;
}
```

`ServiceRequest.cs`:
```csharp
using CivicFlow.Domain.Enums;

namespace CivicFlow.Domain.Entities;

public class ServiceRequest
{
    public int RequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public int CitizenId { get; set; }
    public int RequestTypeId { get; set; }
    public int? AssignedEmployeeId { get; set; }
    public int StatusId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public User Citizen { get; set; } = null!;
    public ServiceRequestType RequestType { get; set; } = null!;
    public User? AssignedEmployee { get; set; }
    public RequestStatus Status { get; set; } = null!;
    public ICollection<RequestStatusHistory> StatusHistory { get; set; } = new List<RequestStatusHistory>();
    public ICollection<CaseNote> Notes { get; set; } = new List<CaseNote>();
    public ICollection<AssignmentHistory> Assignments { get; set; } = new List<AssignmentHistory>();
}
```

`RequestStatusHistory.cs`:
```csharp
namespace CivicFlow.Domain.Entities;

public class RequestStatusHistory
{
    public int HistoryId { get; set; }
    public int RequestId { get; set; }
    public int? OldStatusId { get; set; }
    public int NewStatusId { get; set; }
    public int ChangedByUserId { get; set; }
    public string? Reason { get; set; }
    public DateTime ChangedAt { get; set; }
    public ServiceRequest Request { get; set; } = null!;
    public RequestStatus? OldStatus { get; set; }
    public RequestStatus NewStatus { get; set; } = null!;
    public User ChangedByUser { get; set; } = null!;
}
```

`CaseNote.cs`:
```csharp
namespace CivicFlow.Domain.Entities;

public class CaseNote
{
    public int NoteId { get; set; }
    public int RequestId { get; set; }
    public int AuthorId { get; set; }
    public string NoteText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsInternal { get; set; }
    public ServiceRequest Request { get; set; } = null!;
    public User Author { get; set; } = null!;
}
```

`AssignmentHistory.cs`:
```csharp
namespace CivicFlow.Domain.Entities;

public class AssignmentHistory
{
    public int AssignmentId { get; set; }
    public int RequestId { get; set; }
    public int? AssignedFromUserId { get; set; }
    public int AssignedToUserId { get; set; }
    public int AssignedByUserId { get; set; }
    public DateTime AssignedAt { get; set; }
    public string? Reason { get; set; }
    public ServiceRequest Request { get; set; } = null!;
    public User? AssignedFromUser { get; set; }
    public User AssignedToUser { get; set; } = null!;
    public User AssignedByUser { get; set; } = null!;
}
```

`AuditLog.cs`:
```csharp
namespace CivicFlow.Domain.Entities;

public class AuditLog
{
    public int AuditLogId { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
    public User? User { get; set; }
}
```

- [ ] **Step 2: `dotnet build CivicFlow.sln` — expected succeed**
- [ ] **Step 3: Commit** `feat: add case-management domain entities`

---

### Task 5: EF Core, SQL sequence, seed data

**Files:**
- Create: `src/CivicFlow.Infrastructure/CivicFlowDbContext.cs`, `src/CivicFlow.Infrastructure/RequestNumberGenerator.cs`, `src/CivicFlow.Infrastructure/Seed/DbSeeder.cs`, `src/CivicFlow.Infrastructure/DependencyInjection.cs`, `src/CivicFlow.Application/Abstractions/IAppDbContext.cs`, `src/CivicFlow.Application/Abstractions/IRequestNumberGenerator.cs`
- Modify: `src/CivicFlow.Infrastructure/CivicFlow.Infrastructure.csproj` (EF SQL Server packages)
- Modify: `src/CivicFlow.Api/appsettings.Development.json` connection string
- Test: seeder and context compile via `dotnet build`; sequence behavior is covered by Task 7 create-request integration tests against SQL Server.

**Interfaces:**
- Consumes: domain entities
- Produces:
  - `IAppDbContext` with `DbSet<>` for every entity plus `Task<int> SaveChangesAsync(CancellationToken)`
  - `IRequestNumberGenerator.NextAsync(CancellationToken) -> Task<string>` using SQL sequence `dbo.ServiceRequestNumberSeq`
  - Seeded roles, statuses, department `Planning & Development`, type `Residential Permit`, four users with password `CivicFlow!dev1`

Connection string name: `CivicFlow`. Local default:

```
Server=localhost,1433;Database=CivicFlow;User Id=sa;Password=CivicFlow_Sql!23;TrustServerCertificate=True
```

Indexes (fluent API):
- `ServiceRequests(StatusId)`
- `ServiceRequests(AssignedEmployeeId)`
- `ServiceRequests(CitizenId)`
- unique `ServiceRequests(RequestNumber)`
- `AuditLogs(Timestamp)`
- `RequestStatusHistories(RequestId, ChangedAt)`

Password hashing: `PasswordHasher<User>` from `Microsoft.AspNetCore.Identity`.

- [ ] **Step 1: Add packages**

```powershell
dotnet add src/CivicFlow.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/CivicFlow.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add src/CivicFlow.Api package Microsoft.EntityFrameworkCore.Design
dotnet add src/CivicFlow.Infrastructure package Microsoft.AspNetCore.Identity
```

- [ ] **Step 2: Implement DbContext, sequence SQL in `OnModelCreating` via `modelBuilder.HasSequence<int>("ServiceRequestNumberSeq")`, generator, seeder, DI extension `AddCivicFlowInfrastructure(IConfiguration)`**
- [ ] **Step 3: `dotnet ef migrations add InitialCreate --project src/CivicFlow.Infrastructure --startup-project src/CivicFlow.Api`**
- [ ] **Step 4: `dotnet build` expected succeed**
- [ ] **Step 5: Commit** `feat: add EF Core schema, sequence, and demo seed`

---

### Task 6: JWT login and citizen register (TDD)

**Files:**
- Create: `src/CivicFlow.Application/Abstractions/IJwtTokenService.cs`, `src/CivicFlow.Application/Auth/AuthService.cs`, `src/CivicFlow.Application/Auth/LoginRequest.cs`, `src/CivicFlow.Application/Auth/RegisterRequest.cs`, `src/CivicFlow.Application/Auth/AuthResponse.cs`, `src/CivicFlow.Infrastructure/JwtTokenService.cs`, `src/CivicFlow.Api/Controllers/AuthController.cs`
- Modify: `src/CivicFlow.Api/Program.cs` (JWT bearer)
- Test: `tests/CivicFlow.Tests/AuthApiTests.cs` using `WebApplicationFactory<Program>` and a shared SQL Server test fixture (`CivicFlow_Test` on local Docker SQL, connection string from `ConnectionStrings__CivicFlow`). Do not use EF InMemory — JWT + seed + later sequence tests must hit SQL Server.

**Interfaces:**
- Consumes: `User`, `Role`, `PasswordHasher<User>`
- Produces:
  - `AuthService.LoginAsync(LoginRequest request) -> Task<AuthResponse>`
  - `AuthService.RegisterCitizenAsync(RegisterRequest request) -> Task<AuthResponse>`
  - `IJwtTokenService.CreateToken(User user) -> string`
  - HTTP `POST /api/auth/login`, `POST /api/auth/register`
  - JWT claims: `sub` = userId, `email`, `role` = `RoleName` string (`Citizen` etc.)

`LoginRequest`: `Email`, `Password`  
`RegisterRequest`: `FirstName`, `LastName`, `Email`, `Password`  
`AuthResponse`: `Token`, `UserId`, `Email`, `Role`, `FirstName`, `LastName`

JWT settings in config:

```json
"Jwt": {
  "Issuer": "CivicFlow",
  "Audience": "CivicFlow",
  "SigningKey": "DEV_ONLY_CHANGE_ME_32CHARS_MIN_KEY!!",
  "ExpiryMinutes": 480
}
```

- [ ] **Step 1: Write failing API tests**

```csharp
[Fact]
public async Task Login_seeded_citizen_returns_token()
{
    var response = await _client.PostAsJsonAsync("/api/auth/login", new
    {
        email = "citizen@civicflow.local",
        password = "CivicFlow!dev1"
    });
    response.EnsureSuccessStatusCode();
    var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
    Assert.Equal("Citizen", body!.Role);
    Assert.False(string.IsNullOrWhiteSpace(body.Token));
}

[Fact]
public async Task Login_bad_password_returns_401()
{
    var response = await _client.PostAsJsonAsync("/api/auth/login", new
    {
        email = "citizen@civicflow.local",
        password = "wrong"
    });
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

- [ ] **Step 2: Run tests — expected FAIL (404/route missing)**
- [ ] **Step 3: Implement JWT service, AuthService, AuthController, Program.cs authentication**
- [ ] **Step 4: Run `dotnet test --filter FullyQualifiedName~AuthApiTests` — expected PASS**
- [ ] **Step 5: Commit** `feat: add JWT login and citizen registration`

---

### Task 7: Citizen request create/list/get/respond

**Files:**
- Create: `src/CivicFlow.Application/Abstractions/IAuditLogger.cs`, `src/CivicFlow.Application/Requests/ServiceRequestDtos.cs`, `src/CivicFlow.Application/Requests/RequestService.cs`, `src/CivicFlow.Infrastructure/AuditLogger.cs`, `src/CivicFlow.Api/Controllers/RequestsController.cs`
- Test: `tests/CivicFlow.Tests/RequestAuthorizationTests.cs`

**Interfaces:**
- Consumes: `WorkflowPolicy`, `IRequestNumberGenerator`, `IAppDbContext`, `IAuditLogger`
- Produces:
  - `RequestService.CreateAsync(int citizenId, CreateRequestRequest dto, string? ip)`
  - `RequestService.ListMineAsync(int citizenId)`
  - `RequestService.GetAsync(int requestId, int userId, RoleName role)` — citizens get public notes only
  - `RequestService.RespondAsync(int requestId, int citizenId, string message, string? ip)` — owner + `AdditionalInfoRequired` → `UnderReview` + public note + history + audit `CASE_INFO_RESPONDED`
  - HTTP `POST /api/requests`, `GET /api/requests/my`, `GET /api/requests/{id}`, `POST /api/requests/{id}/responses`

DTOs:
- `CreateRequestRequest`: `RequestTypeId`, `Title`, `Description`, `Priority`
- `ServiceRequestDetailDto`: `RequestId`, `RequestNumber`, `Title`, `Description`, `Status`, `Priority`, `RequestTypeName`, `DepartmentName`, `AssignedEmployeeName`, `CreatedAt`, `SubmittedAt`, `CompletedAt`, `Notes` (`NoteDto` without internal notes for citizens), `History` (`StatusHistoryDto`)
- Create starts in `Submitted` (citizen submit is the create action for MVP; do **not** persist a user-visible Draft unless the UI later adds save-draft). Creating sets status `Submitted`, writes history `null → Submitted`, audit `CASE_CREATED`.

`IAuditLogger.WriteAsync(int? userId, string action, string entityType, string entityId, string? oldValues, string? newValues, string? ip, CancellationToken ct)`.

Current user: read `sub` and `role` claims in controllers; pass into services. Do not trust role from the body.

- [ ] **Step 1: Write failing tests**

```csharp
[Fact]
public async Task Citizen_creates_request_201_and_audit() { /* login, POST /api/requests, expect 201, RequestNumber starts with CIV- */ }

[Fact]
public async Task Citizen_A_cannot_read_citizen_B_request() { /* 403 */ }
```

- [ ] **Step 2: Run — expected FAIL**
- [ ] **Step 3: Implement audit logger + RequestService + RequestsController with `[Authorize]`**
- [ ] **Step 4: Tests PASS**
- [ ] **Step 5: Commit** `feat: add citizen service-request create and read path`

---

### Task 8: Staff status, notes, assignment

**Files:**
- Modify: `src/CivicFlow.Application/Requests/RequestService.cs`, `src/CivicFlow.Api/Controllers/RequestsController.cs`
- Create: `src/CivicFlow.Api/Controllers/EmployeeRequestsController.cs`
- Test: extend `RequestAuthorizationTests.cs`

**Interfaces:**
- Consumes: `WorkflowPolicy.CanTransition`
- Produces:
  - `RequestService.ChangeStatusAsync(int requestId, int actorId, RoleName role, RequestStatusName to, string? reason, string? ip)`
  - `RequestService.AddNoteAsync(int requestId, int actorId, RoleName role, string text, bool isInternal)`
  - `RequestService.AssignAsync(int requestId, int actorId, RoleName role, int assignToUserId, string? reason, string? ip)`
  - `RequestService.ListStaffQueueAsync(RoleName role, int actorId, RequestStatusName? status, Priority? priority)`
  - HTTP `GET /api/employee/requests`, `PUT /api/requests/{id}/status`, `POST /api/requests/{id}/notes`, `PUT /api/requests/{id}/assignment`

Rules:
- Status change: if `!WorkflowPolicy.CanTransition` throw `ConflictException`.
- Terminal requests: throw `ConflictException("Closed requests cannot be modified.")`.
- Assignment writes `AssignmentHistory` + audit `CASE_ASSIGNED`.
- Employee/Supervisor notes allowed; citizen cannot hit this endpoint (`[Authorize(Roles = "Employee,Supervisor,Administrator")]`).
- Internal notes stored with `IsInternal = true`.

Status body: `{ "status": "UnderReview", "reason": "optional" }` parsed to `RequestStatusName`.

- [ ] **Step 1: Write failing tests for illegal transition → 409 and employee queue 200**
- [ ] **Step 2: Run — FAIL**
- [ ] **Step 3: Implement**
- [ ] **Step 4: PASS**
- [ ] **Step 5: Commit** `feat: add staff work-queue, status, notes, and assignment`

---

### Task 9: Supervisor approve, reject, reassign, dashboard

**Files:**
- Create: `src/CivicFlow.Api/Controllers/SupervisorController.cs`
- Modify: `RequestService.cs`
- Test: extend `RequestAuthorizationTests.cs`

**Interfaces:**
- Produces:
  - `RequestService.ApproveAsync` / `RejectAsync` — Supervisor only; from `SupervisorReview`; reason required for reject
  - `RequestService.ReassignAsync` — Supervisor/Admin
  - `RequestService.GetSupervisorDashboardAsync()` → `{ OpenCount, CompletedCount, AgingOverSevenDaysCount }` where aging is `CreatedAt < UtcNow.AddDays(-7)` and not terminal
  - HTTP `POST /api/requests/{id}/approve`, `POST /api/requests/{id}/reject`, `PUT /api/requests/{id}/reassign`, `GET /api/supervisor/dashboard`

- [ ] **Step 1: Failing tests**

```csharp
[Fact]
public async Task Employee_approve_returns_403() { }

[Fact]
public async Task Supervisor_approve_happy_path_writes_history_and_audit() { }
```

- [ ] **Step 2: FAIL (employee might 404 until endpoint exists)**
- [ ] **Step 3: Implement approve/reject as wrappers around `ChangeStatusAsync` with extra role checks**
- [ ] **Step 4: PASS**
- [ ] **Step 5: Commit** `feat: add supervisor approval, rejection, and dashboard`

---

### Task 10: Admin users, departments, request types, audit viewer

**Files:**
- Create: `src/CivicFlow.Application/Admin/AdminService.cs`, `src/CivicFlow.Application/Admin/AdminDtos.cs`, `src/CivicFlow.Api/Controllers/AdminController.cs`
- Test: `tests/CivicFlow.Tests/AdminApiTests.cs`

**Interfaces:**
- Produces HTTP (all `[Authorize(Roles = "Administrator")]`):
  - `GET /api/admin/users`
  - `PUT /api/admin/users/{id}/role` body `{ "role": "Employee", "departmentId": 1 }`
  - `POST /api/admin/departments` / `PUT /api/admin/departments/{id}`
  - `POST /api/admin/request-types` / `PUT /api/admin/request-types/{id}`
  - `GET /api/admin/audit-logs?take=100`
- Role change cannot leave the system with zero Administrators.

- [ ] **Step 1: Failing test — citizen calling admin users → 403**
- [ ] **Step 2: FAIL**
- [ ] **Step 3: Implement AdminService + controller**
- [ ] **Step 4: PASS**
- [ ] **Step 5: Commit** `feat: add admin user, catalog, and audit-log APIs`

---

### Task 11: Health, Swagger, global errors

**Files:**
- Create: `src/CivicFlow.Api/Middleware/ExceptionHandlingMiddleware.cs`, `src/CivicFlow.Api/Controllers/HealthController.cs`
- Modify: `Program.cs`

**Interfaces:**
- Produces: `GET /health` → `200 { "status": "ok" }`
- `AppException` mapped to `{ status, message, traceId }`
- Unhandled exceptions → 500 with generic message in Production
- Swagger enabled in Development

- [ ] **Step 1: Test `GET /health` returns 200** (can be a one-line API test)
- [ ] **Step 2: FAIL until endpoint exists**
- [ ] **Step 3: Implement middleware + health + Swagger**
- [ ] **Step 4: PASS**
- [ ] **Step 5: Commit** `feat: add health endpoint and standard API error envelope`

---

### Task 12: React app shell, auth, routing

**Files:**
- Create: `frontend/` Vite React TypeScript app, Tailwind, React Router
- Create: `frontend/src/api/client.ts`, `frontend/src/auth/AuthContext.tsx`, `frontend/src/pages/LoginPage.tsx`, `frontend/src/App.tsx`
- Test: `npm run build`

**Interfaces:**
- Consumes: `POST /api/auth/login`, JWT in `localStorage` key `civicflow.token`
- Produces routes: `/login`, `/citizen/*`, `/staff/*`, `/admin/*` gated by role
- Vite proxy: `/api` → `http://localhost:5080`

`client.ts` attaches `Authorization: Bearer` and throws on non-OK with parsed `message`.

Role landing:
- Citizen → `/citizen`
- Employee → `/staff`
- Supervisor → `/staff`
- Administrator → `/admin`

- [ ] **Step 1: `npm create vite@latest frontend -- --template react-ts` then add `react-router-dom` and Tailwind**
- [ ] **Step 2: Implement AuthContext, login page, role redirects**
- [ ] **Step 3: `npm run build` expected succeed**
- [ ] **Step 4: Commit** `feat: add React shell with JWT login and role routing`

---

### Task 13: Citizen UI screens

**Files:**
- Create: `frontend/src/pages/citizen/CitizenLayout.tsx`, `DashboardPage.tsx`, `SubmitRequestPage.tsx`, `RequestDetailPage.tsx`
- Modify: `App.tsx`

**Interfaces:**
- Consumes: `GET /api/requests/my`, `POST /api/requests`, `GET /api/requests/{id}`, `POST /api/requests/{id}/responses`, request types from a public or authenticated `GET /api/request-types` (add a small `[Authorize] GET /api/catalog/request-types` on Api if not already present — **add this endpoint in this task** returning active types with department name)
- Produces screens 2–4 from the spec (dashboard, submit, detail + public timeline)
- Citizen layout visually distinct (light civic header, no staff queue chrome)

- [ ] **Step 1: Add `GET /api/catalog/request-types` + test it returns Residential Permit for seeded data**
- [ ] **Step 2: Build the three citizen pages; respond form only if status is `AdditionalInfoRequired`**
- [ ] **Step 3: Manual check script (document in README later): login citizen, submit permit, see `CIV-` number**
- [ ] **Step 4: Commit** `feat: add citizen portal request screens`

---

### Task 14: Staff and supervisor UI

**Files:**
- Create: `frontend/src/pages/staff/StaffLayout.tsx`, `QueuePage.tsx`, `CaseDetailPage.tsx`, `SupervisorDashboardPage.tsx`

**Interfaces:**
- Consumes staff/supervisor APIs from Tasks 8–9
- Queue filters: status, priority
- Case detail: history, notes (internal toggle), status actions allowed for the current role, assignment
- Supervisor dashboard: open / completed / aging counts
- Approve/reject buttons **only** if `role === Supervisor` and status is `SupervisorReview`

- [ ] **Step 1: Implement pages**
- [ ] **Step 2: `npm run build` succeed**
- [ ] **Step 3: Commit** `feat: add staff queue, case detail, and supervisor dashboard`

---

### Task 15: Admin UI

**Files:**
- Create: `frontend/src/pages/admin/AdminLayout.tsx`, `UsersPage.tsx`, `RequestTypesPage.tsx`, `AuditLogPage.tsx`

**Interfaces:**
- Consumes admin APIs from Task 10
- Screens: users + role change, request types (and departments as a simple form on the types page), audit log list

- [ ] **Step 1: Implement pages**
- [ ] **Step 2: `npm run build` succeed**
- [ ] **Step 3: Commit** `feat: add admin users, catalog, and audit viewer`

---

### Task 16: Docker Compose local stack

**Files:**
- Create: `docker-compose.yml`, `src/CivicFlow.Api/Dockerfile`, `frontend/Dockerfile`, `frontend/nginx.conf`
- Modify: `src/CivicFlow.Api/appsettings.json` to read `ConnectionStrings__CivicFlow`

**Interfaces:**
- Produces services: `db` (SQL Server 2022), `api` (ASP.NET, migrate+seed on start), `web` (nginx static)
- Ports: db `1433`, api `8080`, web `5173` or `80:80`
- SA password: `CivicFlow_Sql!23`
- API waits for SQL, runs `dotnet ef database update` or `Database.Migrate()` + `DbSeeder.SeedAsync` at startup

- [ ] **Step 1: Write Dockerfiles and compose file**
- [ ] **Step 2: Run `docker compose up --build` until `GET http://localhost:8080/health` is 200 and login works against `http://localhost` UI**
- [ ] **Step 3: Commit** `chore: dockerize API, SQL Server, and frontend`

---

### Task 17: GitHub Actions CI

**Files:**
- Create: `.github/workflows/ci.yml`

**Interfaces:**
- On pull_request and push to `main`: restore, build, `dotnet test`, `npm ci && npm run build` in `frontend`
- Use `actions/setup-dotnet@v4` with `9.0.x` and `actions/setup-node@v4` with Node 20
- SQL tests: use service container `mcr.microsoft.com/mssql/server:2022-latest` with the same SA password, env `ConnectionStrings__CivicFlow` for tests

- [ ] **Step 1: Write workflow YAML**
- [ ] **Step 2: Confirm YAML schema locally (`actionlint` if installed; otherwise visual review)**
- [ ] **Step 3: Commit** `ci: add GitHub Actions build and test workflow`

---

### Task 18: README (portfolio-accurate)

**Files:**
- Create: `README.md`

**Interfaces:**
- Must include: architecture diagram (text), security model (JWT + resource checks), stack, local Docker run, seed users/password, happy-path demo script from spec §13, Azure deploy pointer, Phase 2 non-goals, screenshot placeholders
- Must **not** claim Entra ID, Blob, SLA, Power BI, or RAG

Happy-path script to document:
1. Citizen submits Residential Permit
2. Employee → Under Review
3. Employee requests additional information
4. Citizen responds
5. Employee recommends approval
6. Supervisor approves
7. Case completed
8. Show status history + audit log

- [ ] **Step 1: Write README**
- [ ] **Step 2: Commit** `docs: add portfolio README with runbook and demo script`

---

### Task 19: Azure deployment (MVP definition of done)

**Files:**
- Create: `infra/main.bicep`, `.github/workflows/azure-deploy.yml` (manual `workflow_dispatch`)
- Modify: `README.md` Azure section

**Interfaces:**
- Resources: Azure SQL Database, App Service plan + Linux App Service for API, Static Web App or App Service for frontend
- App settings: `ConnectionStrings__CivicFlow`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey`, `Jwt__ExpiryMinutes`, `ASPNETCORE_ENVIRONMENT=Production`
- CORS: frontend origin only
- Document `az` login, `az deployment group create`, publish API, set frontend `VITE_API_BASE_URL`
- Verify: Azure `/health` 200, login + one status transition

Bicep parameters: `sqlAdminLogin`, `sqlAdminPassword`, `jwtSigningKey`, `location`.

- [ ] **Step 1: Write `infra/main.bicep` for SQL + App Service + frontend host**
- [ ] **Step 2: Write deploy workflow and README steps**
- [ ] **Step 3: Deploy to a resource group `rg-civicflow-mvp` when Azure credentials are available; record the public URLs in README**
- [ ] **Step 4: Commit** `feat: add Azure Bicep and deploy workflow for MVP hosting`

Do not mark the overall MVP complete until the live Azure login + one case transition has been verified.

---

## Execution notes

- Follow TDD for Tasks 2, 3, 6, 7, 8, 9, 10, 11. Scaffolding/config (1, 16–19) does not require a failing unit test first.
- Commit after every task as specified. Do not combine unrelated tasks in one commit.
- Implementation happens on a feature branch, not by dumping unreviewed work onto `master` without a branch (`feat/mvp` recommended).
- Prefer `WebApplicationFactory` + SQL Server test fixture sharing the seed so authorization tests hit real middleware.

## Self-review checklist (author)

1. Spec coverage: workflow matrix, RBAC + resource 403, history, audit, JWT, React screens, Docker, CI, Azure, README — each has a task.
2. Placeholders: none intended; catalog endpoint added in Task 13 because citizen submit needs request types.
3. Types: `RoleName` / `RequestStatusName` / DTO names reused consistently across tasks.
