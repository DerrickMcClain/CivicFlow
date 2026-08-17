# CivicFlow MVP Design

**Date:** 2026-08-16  
**Status:** Approved — 2026-08-17  
**Repo location:** `C:\Users\derri\Projects\CivicFlow`

## 1. Purpose

CivicFlow is a government services case-management platform that models how a state or local agency processes citizen service requests through a controlled workflow.

The MVP exists to strengthen a portfolio for public-sector / Microsoft-enterprise roles (for example NCDIT and Triangle-area local government IT) by demonstrating:

- Workflow with legal state transitions (not arbitrary status updates)
- Role-based and resource-based authorization
- Immutable status history and audit logging
- ASP.NET Core + EF Core + SQL Server
- React + TypeScript client
- Dockerized local development and Azure cloud deployment

This is intentionally **not** a generic CRUD app.

## 2. Locked decisions

| Decision | Choice |
| --- | --- |
| Architecture | Clean layered: Api / Application / Domain / Infrastructure / Tests |
| Auth (MVP) | Local JWT + seeded role users; Entra ID deferred to Phase 2 |
| Demo surface | Full-stack thin UI: Citizen + Staff (Employee/Supervisor) + Admin |
| Admin | Basic Admin UI in MVP (users/roles, departments, request types, audit viewer) |
| Documents / Blob | Out of MVP |
| Notifications / SLA / Power BI / RAG | Out of MVP |
| Repo path | `C:\Users\derri\Projects\CivicFlow` |
| Cloud | **Azure deployment is in MVP definition of done** |

## 3. Scope

### 3.1 In MVP

1. Authentication (JWT) and roles: Citizen, Employee, Supervisor, Administrator
2. Create / view service requests
3. Assign / reassign requests
4. Status workflow with enforced transitions
5. Employee internal notes (`IsInternal`)
6. Status history timeline
7. Audit log (write path + admin viewer)
8. SQL Server schema via EF Core + seed data
9. React frontend for the screens listed below
10. Automated xUnit tests (workflow + authorization)
11. Docker Compose for local API + SQL Server + frontend
12. Swagger / OpenAPI, global error handling, `/health`
13. GitHub Actions CI (restore, build, test)
14. Azure deployment of API + frontend + Azure SQL (documented and working)
15. Portfolio README covering architecture, security, stack, runbook, screenshots placeholders

### 3.2 Explicitly out of MVP (Phase 2+)

- Microsoft Entra ID / OAuth / OIDC
- Document uploads and Azure Blob Storage
- In-app / email notifications
- SLA tracking
- Power BI reporting
- Application Insights / Key Vault (may be light-touch during Azure deploy if natural; not required features)
- RAG policy assistant
- Configurable workflow designer UI
- Redis / background job processing

## 4. Architecture

```text
React SPA (TypeScript)
        │ HTTPS / JWT / REST
        ▼
CivicFlow.Api
  Controllers, middleware, filters, Swagger, health
        ▼
CivicFlow.Application
  Services, DTOs, validators, interfaces
        ▼
CivicFlow.Domain
  Entities, enums, workflow transition rules, domain exceptions
        ▼
CivicFlow.Infrastructure
  EF Core DbContext, repositories (as needed), JWT, seed data
        ▼
SQL Server (local Docker / Azure SQL)
```

### 4.1 Solution layout

```text
CivicFlow/
├── src/
│   ├── CivicFlow.Api/
│   ├── CivicFlow.Application/
│   ├── CivicFlow.Domain/
│   └── CivicFlow.Infrastructure/
├── tests/
│   └── CivicFlow.Tests/
├── frontend/
├── docs/
│   └── superpowers/specs/
├── docker-compose.yml
└── README.md
```

### 4.2 Responsibility boundaries

- **Api:** HTTP concerns only. No workflow rules.
- **Application:** Use cases, authorization checks, orchestration, audit/status-history writes, validation.
- **Domain:** Entities and pure workflow transition rules (who may move from status A → B).
- **Infrastructure:** Persistence and identity token issuance. No business policy beyond mapping.

### 4.3 Request path

Every mutating case operation:

1. Authenticate (JWT)
2. Authorize role + resource ownership / assignment rules
3. Validate input
4. Enforce legal transition (if status change)
5. Persist entity + history + audit in **one transaction**
6. Return DTO (never leak internal notes to citizens)

## 5. Roles and authorization

### 5.1 Roles

| Role | Capabilities (MVP) |
| --- | --- |
| Citizen | Register/login (seeded or simple register), create request, view own requests, respond when Additional Info Required, view public timeline |
| Employee | Work queue, review assigned/department cases, update allowed statuses, internal notes, recommend approval path, assign within policy |
| Supervisor | Everything Employee can do + approve/reject, reassign, supervisor dashboard counts |
| Administrator | Manage users/roles, departments, request types, view audit logs |

### 5.2 Resource rules

- Citizen A cannot read or mutate Citizen B’s request (`403`)
- Employees cannot approve/reject (`403`)
- Supervisor approval only from `SupervisorReview`
- Citizens never receive `IsInternal = true` notes
- Closed requests (`Completed`, `Cancelled`) are immutable in MVP

Controller `[Authorize(Roles=...)]` is necessary but **not sufficient**. Service-layer checks are mandatory.

## 6. Data model

### 6.1 Entities

**User:** UserId, FirstName, LastName, Email, PasswordHash, RoleId, DepartmentId (nullable for citizens), CreatedAt, UpdatedAt, IsActive

**Role:** RoleId, RoleName

**Department:** DepartmentId, DepartmentName, Description

**ServiceRequestType:** ServiceRequestTypeId, DepartmentId, Name, Description, IsActive

**ServiceRequest:** RequestId, RequestNumber, CitizenId, RequestTypeId, AssignedEmployeeId (nullable), StatusId, Title, Description, Priority, CreatedAt, UpdatedAt, SubmittedAt, CompletedAt

**RequestStatus:** StatusId, StatusName, IsTerminal

**RequestStatusHistory:** HistoryId, RequestId, OldStatusId, NewStatusId, ChangedByUserId, Reason, ChangedAt

**CaseNote:** NoteId, RequestId, AuthorId, NoteText, CreatedAt, IsInternal

**AssignmentHistory:** AssignmentId, RequestId, AssignedFromUserId, AssignedToUserId, AssignedByUserId, AssignedAt, Reason

**AuditLog:** AuditLogId, UserId, Action, EntityType, EntityId, OldValues, NewValues, IpAddress, Timestamp

### 6.2 Request numbers

Format: `CIV-{yyyy}-{sequence:D6}`  
Example: `CIV-2026-000184`

Sequence generation must be concurrency-safe (SQL sequence or transactional counter).

### 6.3 Indexes (MVP)

- `ServiceRequests(StatusId)`
- `ServiceRequests(AssignedEmployeeId)`
- `ServiceRequests(CitizenId)`
- `ServiceRequests(RequestNumber)` unique
- `AuditLogs(Timestamp)`
- `RequestStatusHistories(RequestId, ChangedAt)`

## 7. Workflow

### 7.1 Statuses

- Draft
- Submitted
- UnderReview
- AdditionalInfoRequired
- EmployeeRecommendation
- SupervisorReview
- Approved
- Rejected
- Completed
- Cancelled

### 7.2 Legal transitions

| From | To | Allowed roles |
| --- | --- | --- |
| Draft | Submitted | Citizen (owner) |
| Submitted | UnderReview | Employee, Supervisor |
| UnderReview | AdditionalInfoRequired | Employee, Supervisor |
| AdditionalInfoRequired | UnderReview | Employee, Supervisor (after citizen response) or auto on citizen response |
| UnderReview | EmployeeRecommendation | Employee, Supervisor |
| EmployeeRecommendation | SupervisorReview | Employee, Supervisor |
| SupervisorReview | Approved | Supervisor |
| SupervisorReview | Rejected | Supervisor |
| Approved / Rejected | Completed | Employee, Supervisor |
| Draft or Submitted | Cancelled | Citizen (owner only) |
| Any non-terminal except Approved/Rejected | Cancelled | Supervisor, Administrator |

**Not allowed examples:**

- Employee: `UnderReview` → `Approved`
- Anyone: `Completed` → `UnderReview`
- Citizen: status changes other than submit / cancel (limited) / trigger return from info-required via response

Transition matrix lives in Domain and is unit-tested.

## 8. API surface (MVP)

### Citizen

- `POST /api/auth/login`
- `POST /api/auth/register` (citizen self-registration only, or seed-only if register deferred — prefer simple register for demo)
- `POST /api/requests`
- `GET /api/requests/my`
- `GET /api/requests/{id}`
- `POST /api/requests/{id}/responses` (Additional Info Required)

### Employee / shared staff

- `GET /api/employee/requests`
- `PUT /api/requests/{id}/status`
- `POST /api/requests/{id}/notes`
- `PUT /api/requests/{id}/assignment`

### Supervisor

- `GET /api/supervisor/dashboard`
- `POST /api/requests/{id}/approve`
- `POST /api/requests/{id}/reject`
- `PUT /api/requests/{id}/reassign`

### Admin

- `GET /api/admin/users`
- `PUT /api/admin/users/{id}/role`
- `POST /api/admin/departments` / `PUT`
- `POST /api/admin/request-types` / `PUT`
- `GET /api/admin/audit-logs`

### Platform

- `GET /health`
- Swagger UI in Development (and optionally secured in Azure)

Standard error shape:

```json
{
  "status": 404,
  "message": "Service request not found.",
  "traceId": "..."
}
```

## 9. Frontend (thin)

### Screens

1. Login
2. Citizen dashboard (my requests)
3. Submit request
4. Citizen request detail + public status timeline
5. Employee work queue (filters: status, priority)
6. Employee/Supervisor case detail (notes, actions, history)
7. Supervisor dashboard (open / completed / overdue-age counts — age-based only, no formal SLA)
8. Admin: users, request types, audit log list

Citizen and staff shells should look visually distinct (layout/nav), even if both use the same component library.

### Stack

- React + TypeScript
- React Router
- Fetch or Axios
- Tailwind or Fluent-like styling (choose one during implementation; prefer Tailwind for speed unless Fluent UI is already familiar)

## 10. Testing

### Unit

- Transition matrix allow/deny
- Role permission helpers
- Request number formatting
- Validation rules

### Integration / API

- Citizen creates request → `201` + audit `CASE_CREATED`
- Citizen A gets Citizen B’s id → `403`
- Employee attempts approve → `403`
- Supervisor approve happy path → `200` + status history + audit
- Illegal transition → `409` or `400` (pick one and use consistently: **409 Conflict**)

## 11. Local delivery

`docker-compose.yml` services:

- `db` — SQL Server
- `api` — ASP.NET Core
- `web` — React (dev server or nginx static)

Seed users (documented in README):

- `citizen@civicflow.local`
- `employee@civicflow.local`
- `supervisor@civicflow.local`
- `admin@civicflow.local`

Shared demo password documented for local only.

## 12. CI/CD and Azure (MVP required)

### GitHub Actions (PR)

- restore
- build
- test

### Azure definition of done

Deploy a working cloud environment:

- Azure SQL Database (or Azure SQL compatible)
- API on Azure App Service (or Container Apps)
- Frontend hosted (App Service static / Static Web Apps / same App Service)
- Environment configuration via App Settings (Key Vault optional, not required for MVP)
- Documented deploy steps in README
- Health endpoint reachable in Azure
- End-to-end login + one case transition verified against Azure

Entra ID is still Phase 2; Azure MVP uses the same JWT auth against the deployed API.

## 13. Seed / demo narrative

Department: Planning & Development  
Request type: Residential Permit  

Happy path script for README and interviews:

1. Citizen submits permit request
2. Employee moves to Under Review
3. Employee requests additional information
4. Citizen responds
5. Employee recommends approval
6. Supervisor approves
7. Case completed
8. Show status history + audit log

## 14. Success criteria

MVP is complete when all of the following are true:

1. Full citizen → employee → supervisor path works in UI locally and in Azure
2. Illegal transitions and cross-citizen access are blocked and covered by tests
3. Status history and audit log are accurate and visible
4. Docker Compose brings up the stack locally
5. GitHub Actions runs build/test on PR
6. README explains architecture, security model, stack, local run, Azure deploy, and resume talking points — without overstating Phase 2 features

## 15. Phase 2 preview (non-goals for this plan)

- Entra ID
- Blob documents
- Notifications
- SLA
- Application Insights / Key Vault hardening
- Power BI
- RAG policy assistant

## 16. Resume positioning (post-MVP, factual)

Only claim what is implemented. Expected truthful bullets after MVP:

- Full-stack government case-management platform with citizen, employee, supervisor, and admin workflows
- ASP.NET Core + EF Core + SQL Server with enforced workflow transitions, status history, and audit logging
- JWT RBAC plus resource-level authorization, covered by automated tests
- Dockerized local development, GitHub Actions CI, and Azure deployment (App Service + Azure SQL)
