# Subscription & Auto-Payment Reminder Application

A full-stack banking case study: customers register their utility subscriptions (electricity, water, internet, GSM, natural gas), see live debt from a mock provider, pay through a mock gateway with a real transactional flow, and receive SMS + Email reminders before each provider-set deadline. A separate admin role monitors everything across the bank.

The codebase started as the output of a 9-phase multi-agent AI workflow and was extended afterwards into a portal-style experience with JWT auth, role-based access, auto-pay, provider-driven billing windows, and a notifications log. See the [AI Usage Disclosure](#ai-usage-disclosure) for the full development story.

---

## Highlights

- **Mobile-bank-style portal.** Customer-driven onboarding (sign-up at the landing page), self-service subscription CRUD, debt query + one-click pay, payment history per subscription, reminders, and an auto-pay toggle.
- **Provider-owned vs customer-owned days.** Provider supplies `BillingDayOfMonth` + `LastPaymentDayOfMonth` (= billing + 7); customer picks any `PaymentDayOfMonth` inside that window. Backend rejects out-of-range updates with 409 + the exact valid bounds.
- **Two-channel reminders.** Reminders fan out on both SMS and Email, are persisted to a `Notifications` table, and are skipped entirely for auto-pay subscriptions (no noise when the bank is already handling charges).
- **Admin = monitor + off-board.** Read-only customer / subscription analysis, per-customer dashboard drill-down, full notifications log with customer-name resolution. No mutation paths beyond customer delete.
- **Four mock third-party REST services.** Debt inquiry, provider info, payment gateway (~10% random failure), notifications — each reached via a typed HttpClient with timeouts and structured logs.
- **Transactional payment flow.** Pre-check + DB-level unique filtered index + commit-before-throw on gateway failure so failed attempts stay in the audit trail.

---

## Tech stack

| Layer | Technology |
|---|---|
| Backend API | .NET 10 Web API (controller-based) |
| Auth | `Microsoft.AspNetCore.Authentication.JwtBearer` + PBKDF2-SHA256 password hashing (BCL only, no extra NuGet) |
| ORM | Entity Framework Core 10 |
| Database | Azure SQL Edge (Docker, ARM64 native) |
| Validation | FluentValidation 11 |
| API docs | Swashbuckle / Swagger UI |
| Tests | xUnit + EF Core InMemory + FluentAssertions |
| Frontend | React 18 + Vite (plain JavaScript) |
| Routing | React Router v6 |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Node.js 18+
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- `dotnet ef` global tool (`dotnet tool install --global dotnet-ef`)

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
export PATH="$PATH:$HOME/.dotnet/tools"
cd backend

# Apply all migrations (creates SubscriptionAppDb and runs the demo seed)
dotnet ef database update \
  --project SubscriptionApp.Infrastructure \
  --startup-project SubscriptionApp.Api

# Start the API
dotnet run --project SubscriptionApp.Api
# API: http://localhost:5072
# Swagger UI: http://localhost:5072/swagger
```

Seed data is applied on first startup in Development. It generates 3 customers + 1 admin + 9 subscriptions + several months of payment history, with payment days pinned relative to "today" (Turkey local) so every reminder/dashboard state is exercised. To reseed from scratch:

```bash
dotnet ef database drop --force \
  --project SubscriptionApp.Infrastructure \
  --startup-project SubscriptionApp.Api
dotnet ef database update \
  --project SubscriptionApp.Infrastructure \
  --startup-project SubscriptionApp.Api
```

### 3. Run the frontend

```bash
cd frontend
npm install
npm run dev
# Frontend: http://localhost:5173
```

The Vite dev server proxies `/api/*` to `http://localhost:5072`.

---

## Demo accounts

The landing page lists all four as click-to-fill chips. Or hit `POST /api/auth/login` directly.

| Role | Email | Password | Scenario it demonstrates |
|---|---|---|---|
| Admin | `admin@bank.com` | `Admin1234!` | Customers list, subscription analysis, per-customer dashboard, notifications log |
| Customer | `ahmet.yilmaz@example.com` | `Test1234!` | Caught up — every sub on auto-pay, no reminders, healthy dashboard |
| Customer | `fatma.kaya@example.com` | `Test1234!` | Manual payer with mixed history — 1 overdue + 1 due-soon reminder, includes a failed-then-retried payment |
| Customer | `mehmet.demir@example.com` | `Test1234!` | One "due today" reminder, one auto-pay sub, one Passive sub with a historical failure |

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
3. Zero / negative amount rejection (xUnit `[Theory]`, 3 inputs)
4. Dashboard unpaid-this-month calculation

---

## Project structure

```
ArchiCase/
├── backend/
│   ├── SubscriptionApp.slnx
│   ├── SubscriptionApp.Api/
│   │   ├── Controllers/                    # Thin controllers
│   │   │   ├── AuthController.cs           # login, register
│   │   │   ├── UsersController.cs          # /api/users/me/* (portal)
│   │   │   ├── CustomersController.cs      # admin read-only + DELETE
│   │   │   ├── SubscriptionsController.cs  # admin read-only GETs
│   │   │   ├── PaymentsController.cs       # POST = customer-only, GETs = admin-only
│   │   │   ├── NotificationsController.cs  # admin log viewer
│   │   │   └── External/                   # 4 mock 3rd-party services
│   │   ├── Dtos/  Validators/  Mapping/  Middleware/
│   │   ├── Services/                       # JwtService
│   │   └── Program.cs                      # JWT setup, FluentValidation, HttpClients, seeding
│   ├── SubscriptionApp.Domain/
│   │   ├── Entities/                       # Customer, Subscription, Payment, User, Notification (POCOs only)
│   │   ├── Enums/                          # SubscriptionType, SubscriptionStatus, PaymentStatus
│   │   └── Exceptions/                     # DomainException (409), NotFoundException (404), ExternalServiceException (502)
│   ├── SubscriptionApp.Infrastructure/
│   │   ├── Persistence/                    # AppDbContext, EF configurations, DbInitializer (demo seed)
│   │   ├── Services/                       # CustomerService, SubscriptionService, PaymentService, AuthService, UserService
│   │   ├── ExternalServices/               # 4 typed HttpClients + their models
│   │   ├── Utilities/                      # PasswordHasher (PBKDF2), BusinessClock (Turkey time)
│   │   └── Migrations/                     # 6 EF migrations
│   └── SubscriptionApp.Tests/              # 6 xUnit tests
├── frontend/
│   └── src/
│       ├── api/                            # fetch wrapper + per-resource modules (incl. JWT injection + 401 handler)
│       ├── context/AuthContext.jsx         # Token storage + role-aware redirect helpers
│       ├── components/                     # ProtectedRoute (role-gated), Modal, Table, Button, ...
│       ├── pages/
│       │   ├── LandingPage.jsx             # Sign-in + Sign-up tabs, demo-account chips
│       │   ├── CustomersPage.jsx           # Admin: search by name/id + per-row Dashboard/Subs/Notifs/Delete
│       │   ├── SubscriptionsPage.jsx       # Admin: multi-filter analysis view + summary stats
│       │   ├── DashboardPage.jsx           # Admin: /dashboard/:customerId drill-down
│       │   ├── NotificationsPage.jsx       # Admin: SMS/Email log with customer resolution
│       │   ├── SubscriptionDetailPage.jsx  # Role-aware: debt query, payment, history
│       │   └── portal/                     # PortalDashboardPage, PortalSubscriptionsPage, RemindersPage
│       └── styles/global.css
├── docs/
│   ├── ER-diagram.md
│   ├── api-endpoints.md
│   ├── flow-diagrams.md
│   └── architecture.md
├── SPEC.md  AGENTS.md  PHASE_LOG.md  FUTURE_IMPROVEMENTS.md
```

---

## Documentation

| Document | Contents |
|---|---|
| [docs/ER-diagram.md](./docs/ER-diagram.md) | Mermaid ER diagram including User + Notification + the three-day subscription model |
| [docs/api-endpoints.md](./docs/api-endpoints.md) | Every endpoint: auth, portal, admin, external mocks — request/response schemas + status codes + role matrix |
| [docs/flow-diagrams.md](./docs/flow-diagrams.md) | Mermaid sequence diagrams: happy path, duplicate, passive, gateway failure, sign-up reuse, reminder fan-out, auto-pay batch |
| [docs/architecture.md](./docs/architecture.md) | 3-layer rationale + auth, BusinessClock, role-based access, mock provider integration, day model, notifications-as-audit |
| [SPEC.md](./SPEC.md) | Single source of truth — domain model, business rules, endpoint catalog, in/out of scope |
| [PHASE_LOG.md](./PHASE_LOG.md) | Append-only log of every agent's decisions and the post-bootstrap iteration |
| [FUTURE_IMPROVEMENTS.md](./FUTURE_IMPROVEMENTS.md) | Enhancements still scoped out |

---

## AI Usage Disclosure

This project was built with AI assistance using two phases:

1. **Multi-agent bootstrap (Phases 1–9).** Nine specialist agents defined in `.claude/agents/*.md`, invoked sequentially. Each read `SPEC.md` + `PHASE_LOG.md`, did bounded work, appended an entry, stopped. The human reviewed at each boundary.
2. **Post-bootstrap iteration (Phase 10+).** A single Claude Code conversation where the design moved from "admin runs everything" to "mobile-bank-style portal + admin monitoring". JWT auth, role splits, customer self-service CRUD, auto-pay, two-channel reminders, the notifications log, the BusinessClock fix for the overdue calculation, and the admin restructure all landed during this iteration.

### What the AI was used for

- Generating C# code: entities, EF configurations, services, controllers, middleware, JWT setup, mock external services, migrations
- Generating React code: API client (with JWT injection + 401 redirect), Auth context, role-aware routing, portal pages, admin analysis view, notifications log UI
- Mermaid diagrams for ER + sequence flows
- FluentValidation rule patterns
- xUnit test structure and FluentAssertions syntax

---

## Known limitations and future improvements

See [FUTURE_IMPROVEMENTS.md](./FUTURE_IMPROVEMENTS.md) for the full list. Highlights:

- No real scheduler — `process-auto-pay` and reminders both require a triggering request. A `BackgroundService` calling the same endpoints nightly is a 30-line drop-in.
- No idempotency keys on `POST /api/payments`.
- Notifications are mock-only — the `INotificationClient` posts to a self-loopback controller that logs and persists rather than calling a real SMS/Email gateway.
- No pagination on long lists in the frontend.
- All copy and amounts are English / TRY only.
