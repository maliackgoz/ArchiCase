# Subscription & Auto-Payment Reminder Application

A full-stack banking case study demonstrating a 3-layer .NET 10 Web API with a React 18 frontend. The application manages customers, utility subscriptions (electricity, water, internet, GSM, natural gas), and payments with a real transactional payment flow including a mock external payment gateway.

This project was built using a 9-phase multi-agent AI workflow — each phase was handled by a specialist Claude Code agent with a bounded scope. See the [AI Usage Disclosure](#ai-usage-disclosure) section for full transparency.

---

## Tech stack

| Layer | Technology |
|---|---|
| Backend API | .NET 10 Web API (controller-based) |
| ORM | Entity Framework Core 10 + SQL Server provider |
| Database | Azure SQL Edge (Docker — ARM64 native) |
| Validation | FluentValidation 11 |
| API docs | Swashbuckle / Swagger UI |
| Tests | xUnit + EF Core InMemory + FluentAssertions |
| Frontend | React 18 + Vite (plain JavaScript) |
| Routing | React Router v6 |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Node.js 18+
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for the database container)
- Git

---

## Setup and run

### 1. Start the database

```bash
docker run \
  -e 'ACCEPT_EULA=1' \
  -e 'MSSQL_SA_PASSWORD=YourStrong!Passw0rd' \
  -p 1433:1433 \
  -d --name subscriptionapp-db \
  mcr.microsoft.com/azure-sql-edge
```

> **Apple Silicon (ARM64):** Azure SQL Edge is the ARM64-native image. The standard SQL Server 2022 image runs via emulation and is significantly slower.

### 2. Apply migrations and run the backend

```bash
export PATH="$PATH:/usr/local/share/dotnet"
cd backend

# Apply the database migration (creates SubscriptionAppDb and seeds data)
dotnet ef database update \
  --project SubscriptionApp.Infrastructure \
  --startup-project SubscriptionApp.Api

# Start the API
dotnet run --project SubscriptionApp.Api
# API: http://localhost:5072
# Swagger UI: http://localhost:5072/swagger
```

Seed data is applied automatically on first startup in Development: 3 customers and 5 subscriptions.

### 3. Run the frontend

```bash
cd frontend
npm install
npm run dev
# Frontend: http://localhost:5173
```

The Vite dev server proxies all `/api` requests to `http://localhost:5072`.

---

## Run tests

```bash
cd backend
dotnet test SubscriptionApp.slnx
# Expected: 6 passed, 0 failed
```

Tests cover:
1. Duplicate payment rejection
2. Payment on passive subscription rejection
3. Zero / negative amount rejection (theory — 3 inputs)
4. Dashboard unpaid-this-month calculation

---

## Project structure

```
ArchiCase/
├── backend/
│   ├── SubscriptionApp.slnx
│   ├── SubscriptionApp.Api/
│   │   ├── Controllers/           # Thin controllers (Customers, Subscriptions, Payments, External)
│   │   ├── Dtos/                  # Request / Response DTOs (at API boundary only)
│   │   ├── Validators/            # FluentValidation validators
│   │   ├── Mapping/               # Hand-written ToEntity / ToResponse extension methods
│   │   ├── Middleware/            # ExceptionHandlingMiddleware → uniform error shape
│   │   └── Program.cs
│   ├── SubscriptionApp.Domain/
│   │   ├── Entities/              # Customer, Subscription, Payment (pure POCOs)
│   │   ├── Enums/                 # SubscriptionType, SubscriptionStatus, PaymentStatus
│   │   └── Exceptions/            # DomainException (409), NotFoundException (404), ExternalServiceException (502)
│   ├── SubscriptionApp.Infrastructure/
│   │   ├── Persistence/           # AppDbContext, EF configurations, DbInitializer
│   │   ├── Services/              # CustomerService, SubscriptionService, PaymentService
│   │   └── ExternalServices/      # Typed HttpClients + Models
│   └── SubscriptionApp.Tests/
│       └── UnitTest1.cs           # 4 test classes, 6 test cases
├── frontend/
│   └── src/
│       ├── api/                   # fetch wrapper + resource modules
│       ├── components/            # Modal, Table, Button, LoadingSpinner, ErrorBanner
│       ├── pages/                 # CustomersPage, SubscriptionsPage, SubscriptionDetailPage, DashboardPage
│       └── styles/                # global.css
├── docs/
│   ├── ER-diagram.md
│   ├── api-endpoints.md
│   ├── flow-diagrams.md
│   └── architecture.md
├── SPEC.md
├── AGENTS.md
├── PHASE_LOG.md
└── FUTURE_IMPROVEMENTS.md
```

---

## Documentation

| Document | Contents |
|---|---|
| [docs/ER-diagram.md](./docs/ER-diagram.md) | Mermaid entity-relationship diagram + cardinality notes |
| [docs/api-endpoints.md](./docs/api-endpoints.md) | Every endpoint: request/response schemas, all status codes |
| [docs/flow-diagrams.md](./docs/flow-diagrams.md) | Sequence diagrams: happy path, duplicate, passive, gateway failure |
| [docs/architecture.md](./docs/architecture.md) | 3-layer rationale, decimal/UTC decisions, transaction design |
| [SPEC.md](./SPEC.md) | Full project specification (single source of truth) |
| [PHASE_LOG.md](./PHASE_LOG.md) | Append-only log of every agent's decisions and output |
| [FUTURE_IMPROVEMENTS.md](./FUTURE_IMPROVEMENTS.md) | Enhancements scoped out of the core deliverable |

---

## AI Usage Disclosure

This project was built with AI assistance using a multi-agent workflow with Claude Code. Development was orchestrated as 9 sequential phases, each handled by a specialist agent with a bounded scope.

### Workflow architecture

Nine agent definitions (`.claude/agents/*.md`) encode the scope, rules, and output requirements for each phase. The agents are invoked sequentially: each reads `SPEC.md` and `PHASE_LOG.md`, does its bounded work, appends a structured entry to `PHASE_LOG.md`, and stops. The human reviews the output at each phase boundary before proceeding.

This pipeline produced working, tested, documented full-stack application code across 9 phases in a single session.

### What the AI was used for

- Generating all backend C# code (entities, EF Core configurations, services, controllers, middleware, tests)
- Generating all frontend JavaScript/JSX (API client, components, pages, routing, CSS)
- Writing Mermaid diagram syntax
- FluentValidation rule patterns
- EF Core filtered index configuration
- xUnit test structure and FluentAssertions syntax

### Key decisions I made (not the AI)

These are decisions extracted from `PHASE_LOG.md` where the approach required human judgement:

- **Database choice:** Started with SQL Server LocalDB (spec), evaluated PostgreSQL for macOS compatibility, chose SQL Server on Docker for banking-domain realism, then switched from SQL Server 2022 to **Azure SQL Edge** when the standard image showed ARM64 platform warnings on Apple Silicon.
- **`.slnx` solution format:** .NET 10 generates `.slnx` by default (new XML format). Accepted this deviation from `.sln` as the forward-looking standard.
- **`NotFoundException` not inheriting `DomainException`:** 404 (not found) and 409 (business rule violation) are semantically distinct. Sharing a base class would require middleware type inspection and obscure intent.
- **`PaymentStatus.Successful = 0`:** The filtered unique index uses `[Status] = 0` as a literal integer. This coupling was documented with inline comments rather than hidden.
- **Period validated at service layer, not via DB CHECK constraint:** SQL Server LIKE-based CHECK constraints cannot enforce month range 01–12. Service-layer validation is sufficient and simpler.
- **Commit before throw on gateway failure:** Failed payment records must be persisted for audit. The transaction commits even on decline; the `committed` flag prevents double-rollback.
- **Fire-and-forget notifications after commit:** Holding a DB transaction open during a network call to a third-party notification service would starve the connection pool. The payment is committed first; notification failure cannot roll it back.

### Things I changed or corrected in AI output

- **Missing `using Microsoft.Extensions.Logging;`** in three `Infrastructure/ExternalServices/` client files — build failed, fixed immediately.
- **`TransactionIgnoredWarning` as error in xUnit** — EF Core InMemory raises this warning as an exception by default. Tests failed; fixed by adding `.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))` to the test helper.
- **Port 5072 vs 5000** — `launchSettings.json` assigned port 5072, not the spec's assumed 5000. Corrected in `appsettings.json` (`ExternalServices:BaseUrl`) and Vite proxy config.
- **`.gitignore` missing `.claude/settings.local.json`** — Claude Code session state was accidentally staged. Added the exclusion and removed from index.
- **`zsh: event not found: Passw0rd`** — The `!` in `YourStrong!Passw0rd` triggers zsh history expansion inside double quotes. Corrected all documentation to use single quotes.
- **ARM64 Docker platform warning** — Switched from `mcr.microsoft.com/mssql/server:2022-latest` (amd64 only) to `mcr.microsoft.com/azure-sql-edge` (ARM64 native). Environment variables also differ (`ACCEPT_EULA=1` and `MSSQL_SA_PASSWORD` instead of `ACCEPT_EULA=Y` and `SA_PASSWORD`).

### What I would do differently

- **Seed cleanup between test phases:** Smoke-testing each CRUD phase deleted seeded records, so later phases had to work with partial data. A per-phase seed reset strategy would have made testing cleaner.
- **Run `dotnet test` after every phase, not just Phase 7:** Catching the `TransactionIgnoredWarning` earlier would have been less surprising.
- **Version-pin the Vite scaffold template:** `npm create vite@latest` pulled `v9.0.7` which generated a slightly different `App.jsx` boilerplate than expected. Pinning the version in the agent definition would make the scaffold reproducible.
- **Define `ExternalServiceException` in Phase 2:** It was created in Phase 6. Because it belongs in the Domain layer, it should have been part of the initial exception hierarchy.

---

## Known limitations and future improvements

See [FUTURE_IMPROVEMENTS.md](./FUTURE_IMPROVEMENTS.md) for the full list. Key items:

- No authentication or authorisation (any caller can access any data)
- Notification delivery is simulated (ILogger only — no real SMS/email)
- No background scheduler for automatic monthly payment reminders
- No idempotency keys on the payment endpoint (duplicate POST on network retry)
- Frontend has no pagination on large lists
