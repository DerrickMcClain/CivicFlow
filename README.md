# CivicFlow

Government services case-management platform for city, county, and state-style workflows.

Built as a portfolio project for public-sector / Microsoft-enterprise roles (applications analyst, .NET developer, systems analyst) in the Raleigh–Durham Triangle area.

**Live code:** [github.com/DerrickMcClain/CivicFlow](https://github.com/DerrickMcClain/CivicFlow)

---

## What it demonstrates

- **Controlled workflow** — legal status transitions only (not free-form CRUD status edits)
- **JWT RBAC + resource authorization** — citizens cannot read another citizen’s cases; employees cannot approve
- **Immutable status history + audit log** on mutating case actions
- **Clean layered ASP.NET Core** API with EF Core + SQL Server
- **React + TypeScript** portals for Citizen, Staff/Supervisor, and Admin
- Automated **xUnit** coverage for workflow and authorization paths

## Stack

| Layer | Technology |
|---|---|
| API | .NET 9, ASP.NET Core, JWT Bearer |
| Domain | Workflow policy, request-number formatting |
| Data | EF Core, SQL Server sequence (`CIV-YYYY-######`) |
| Tests | xUnit + `WebApplicationFactory` against SQL Server / LocalDB |
| UI | React 19, TypeScript, Vite, React Router, Tailwind CSS |
| Delivery (planned) | Docker Compose, GitHub Actions CI, Azure App Service + Azure SQL |

## Architecture

```text
React SPA (Citizen / Staff / Admin)
        │ HTTPS / JWT / REST
        ▼
 CivicFlow.Api  ──► Controllers (HTTP only)
        │
 CivicFlow.Application  ──► use cases, authz, orchestration
        │
 CivicFlow.Domain  ──► WorkflowPolicy, entities, enums
        ▲
 CivicFlow.Infrastructure  ──► EF Core, JWT, seed, sequence
        │
   SQL Server
```

## Security model

1. Authenticate with local JWT (`sub`, `email`, `role` claims).
2. Authorize by role on endpoints (`Citizen`, `Employee`, `Supervisor`, `Administrator`).
3. Enforce resource ownership in application services (cross-citizen → **403**).
4. Enforce workflow transitions in `WorkflowPolicy` (illegal → **409**).
5. Closed cases (`Completed`, `Cancelled`) are immutable.
6. Internal notes are never returned to citizens.

Microsoft Entra ID is intentionally **Phase 2** (not claimed in this MVP).

## Current status

**Done**
- Solution scaffold + domain workflow/entities
- EF Core schema, SQL sequence, demo seed
- JWT login + citizen registration
- Citizen create/list/get/respond APIs
- Staff queue, status, notes, assignment
- Supervisor approve/reject/reassign + dashboard
- Admin users, departments, request types, audit logs
- Health endpoint + standard error envelope `{ status, message, traceId }`
- React shell: login, JWT storage, role-gated routes

**Next**
- Citizen / staff / admin UI screens
- Docker Compose local stack
- GitHub Actions CI
- Azure deployment (MVP definition of done)

## Local run (dev)

### Prerequisites

- .NET 9 SDK
- Node.js 20+
- SQL Server **LocalDB** (default for local dev) — or SQL Server / Docker on `localhost,1433` later

### 1. Database connection

Development (`appsettings.Development.json`) uses LocalDB:

```text
Server=(localdb)\MSSQLLocalDB;Database=CivicFlow;Trusted_Connection=True;TrustServerCertificate=True
```

Start LocalDB if needed:

```powershell
sqllocaldb start MSSQLLocalDB
```

Docker SQL (`localhost,1433` / `sa` / `CivicFlow_Sql!23`) is for the Compose stack. Azure SQL comes in the Azure deploy task.

Tests default to LocalDB database `CivicFlow_Test` unless `ConnectionStrings__CivicFlow` is set.

### 2. API

```powershell
cd C:\Users\derri\Projects\CivicFlow
dotnet restore
dotnet test CivicFlow.sln
dotnet run --project src/CivicFlow.Api --launch-profile http
```

API listens on `http://localhost:5080`. On startup it migrates the database and seeds demo data. Swagger UI is available in Development.

### 3. Frontend

```powershell
cd frontend
npm install
npm run dev
```

Vite proxies `/api` and `/health` to `http://localhost:5080`. Open `http://localhost:5173`.

## Seed users (local / demo only)

Password for all seeded accounts: `CivicFlow!dev1`

| Email | Role |
|---|---|
| `citizen@civicflow.local` | Citizen |
| `employee@civicflow.local` | Employee |
| `supervisor@civicflow.local` | Supervisor |
| `admin@civicflow.local` | Administrator |

Seed catalog: department **Planning & Development**, type **Residential Permit**.

## Happy-path demo script

1. Citizen signs in and submits a **Residential Permit** request → gets a `CIV-` number
2. Employee moves the case to **Under Review**
3. Employee requests **Additional Info**
4. Citizen responds (returns to **Under Review**)
5. Employee moves to **Employee Recommendation** → **Supervisor Review**
6. Supervisor **approves**
7. Staff marks **Completed**
8. Show **status history** on the case and **audit log** in Admin

## Screenshots

_Placeholder — add after UI screens ship_

- Login / role landing
- Citizen request detail with timeline
- Staff queue + case actions
- Supervisor dashboard
- Admin audit log

## Azure deploy (planned)

MVP definition of done includes Azure SQL + App Service API + hosted frontend. Bicep and deploy workflow are still to come. When present, configure:

- `ConnectionStrings__CivicFlow`
- `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey`, `Jwt__ExpiryMinutes`

## Phase 2 (explicit non-goals for this MVP)

Not implemented / not claimed:

- Microsoft Entra ID
- Document upload / Blob storage
- Email or push notifications
- SLA timers
- Power BI dashboards
- RAG / policy assistant
- Application Insights / Key Vault hardening

## Resume talking points (factual)

After full MVP (UI + Docker + CI + Azure):

- Designed a government-style case workflow with enforced state transitions and auditability
- Implemented ASP.NET Core clean architecture with JWT RBAC and resource-level authorization
- Delivered React portals for citizen, staff, and admin personas against a REST API
- Packaged local Docker run and cloud deployment for interview demos

Until those delivery pieces land, claim only what is listed under **Current status → Done**.

## License

Personal portfolio project.
