# Phase Execution Log

Each agent appends its entry here after completing its phase. **Append-only** — never edit or delete prior entries. Corrections to an earlier phase go in a new `## Errata for Phase N` sub-section right after the original phase entry.

The very first entry below (Phase 0) is the scaffolding bootstrap and was written by the project setup, not by a phase agent. Phase agents start at Phase 1.

---

## Phase 0 — bootstrap (scaffolding) — initial setup

### What was built
- Root markdown documentation: `SPEC.md`, `AGENTS.md`, `PHASE_LOG.md` (this file), `README.md`, `FUTURE_IMPROVEMENTS.md`
- 9 specialist agent definitions under `.claude/agents/`
- Empty `backend/` and `frontend/` directories with `.gitkeep` placeholders
- `.gitignore` and `.editorconfig`
- Initial git commit on `main`

### Key decisions
- Decision: bootstrap creates the initial git commit. Reason: spec lists "initialized git repo" as a deliverable. Alternative considered: leaving git init to the student — rejected to keep the first commit reproducible and to avoid a redundant README step.
- Decision: `backend/` and `frontend/` are tracked as empty (via `.gitkeep`) rather than created later by Phase 1 / Phase 8. Reason: keeps the directory tree predictable and makes the next agent's diff easier to review. Alternative considered: omitting them — rejected to avoid implicit `mkdir` work spread across agents.

### Files created
- `SPEC.md`, `AGENTS.md`, `PHASE_LOG.md`, `README.md`, `FUTURE_IMPROVEMENTS.md`
- `.gitignore`, `.editorconfig`
- `.claude/agents/{solution-architect,domain-modeler,customer-feature-builder,subscription-feature-builder,external-services-builder,payment-feature-builder,test-and-dashboard-builder,frontend-builder,documentation-finalizer}.md`
- `backend/.gitkeep`, `frontend/.gitkeep`

### Verification commands the student should run
- `ls .claude/agents/` — should list 9 `.md` files
- `git log --oneline` — should show one commit
- `git status` — should be clean

### Notes for the next agent (`solution-architect`)
- The repository is empty of code. `backend/` contains only `.gitkeep`. You will run `dotnet new sln -n SubscriptionApp` inside `backend/` and delete the `.gitkeep` once real files exist (or leave it — your call, document it).
- `SPEC.md` defines the tech stack, project layout, and naming. Do not deviate.
- `AGENTS.md` defines the rules of engagement. Read both before doing anything.

### Open questions raised
- None.

---

## Phase 1 — solution-architect — 2026-05-11

### What was built
- `.NET 10` solution `SubscriptionApp.slnx` in `/backend` (see Key Decisions for version deviation)
- 4 projects scaffolded and added to the solution:
  - `SubscriptionApp.Api` (`dotnet new webapi --use-controllers`) — target `net10.0`
  - `SubscriptionApp.Domain` (`dotnet new classlib`) — target `net10.0`, **zero NuGet dependencies**
  - `SubscriptionApp.Infrastructure` (`dotnet new classlib`) — target `net10.0`
  - `SubscriptionApp.Tests` (`dotnet new xunit`) — target `net10.0`
- Project references wired: Api→Infrastructure+Domain, Infrastructure→Domain, Tests→Api+Infrastructure+Domain
- NuGet packages installed (all resolved to their latest stable versions for .NET 10):
  - **Api:** Swashbuckle.AspNetCore 10.1.7, FluentValidation.AspNetCore 11.3.1, Microsoft.EntityFrameworkCore.Design 10.0.7
  - **Infrastructure:** Microsoft.EntityFrameworkCore 10.0.7, Microsoft.EntityFrameworkCore.SqlServer 10.0.7, Microsoft.EntityFrameworkCore.Tools 10.0.7
  - **Tests:** Microsoft.EntityFrameworkCore.InMemory 10.0.7, FluentAssertions 8.9.0, xunit 2.9.3
- `appsettings.json` — `DefaultConnection` SQL Server (Docker) connection string added
- `Program.cs` — minimal skeleton with Swashbuckle Swagger UI (dev only), controller registration, and `TODO` placeholder comments for every future registration (DbContext, FluentValidation, services, middleware, DbInitializer)
- Deleted template-generated `WeatherForecast.cs` and `Controllers/WeatherForecastController.cs`
- `backend/.gitkeep` deleted (replaced by real content)
- Build result: **0 errors, 0 warnings**

### Key decisions
- Decision: used .NET 10.0.203 (current LTS). Reason: .NET 10 is installed and matches the spec. All EF Core, Swashbuckle, and FluentValidation packages resolved to their .NET 10 stable versions.
- Decision: solution file is `.slnx` (new XML format), not `.sln`. Reason: `dotnet new sln` in .NET 10 generates `.slnx` by default. All `dotnet` CLI commands accept `.slnx`. Alternative: force `.sln` with `--format sln` — unnecessary and `.slnx` is the forward-looking standard.
- Decision: kept `Microsoft.AspNetCore.OpenApi` 10.0.7 in the Api csproj even though we use Swashbuckle. Reason: it was added by the webapi template and Swashbuckle may use it transitively. Alternative: remove it — benign either way; left it to avoid touching auto-generated project files needlessly.
- Decision: `Program.cs` uses Swashbuckle (`AddSwaggerGen` / `UseSwagger` / `UseSwaggerUI`) rather than the .NET 10 built-in `AddOpenApi()`. Reason: spec explicitly lists Swashbuckle for Swagger UI. Built-in OpenAPI generates JSON but has no UI endpoint.
- Decision: `backend/.gitkeep` was deleted after real files landed. Alternative: leave it — deleted to keep the directory clean.

### Files created/modified
- `backend/SubscriptionApp.slnx`
- `backend/SubscriptionApp.Api/SubscriptionApp.Api.csproj`
- `backend/SubscriptionApp.Api/Program.cs` (rewritten)
- `backend/SubscriptionApp.Api/appsettings.json` (connection string added)
- `backend/SubscriptionApp.Api/Controllers/` (WeatherForecastController.cs deleted)
- `backend/SubscriptionApp.Api/WeatherForecast.cs` (deleted)
- `backend/SubscriptionApp.Domain/SubscriptionApp.Domain.csproj`
- `backend/SubscriptionApp.Infrastructure/SubscriptionApp.Infrastructure.csproj`
- `backend/SubscriptionApp.Tests/SubscriptionApp.Tests.csproj`

### Verification commands the student should run
```bash
export PATH="$PATH:/usr/local/share/dotnet"   # add to your shell profile
cd backend
dotnet build SubscriptionApp.slnx             # should print: Build succeeded. 0 Warning(s) 0 Error(s)
```

### Notes for the next agent (`domain-modeler`)
- Solution compiles clean. `Domain` csproj has zero NuGet packages — keep it that way.
- `Program.cs` has placeholder `TODO` comments exactly where you need to add `AddDbContext<AppDbContext>` and the `DbInitializer` call — do not restructure the file, just fill in the TODOs.
- The connection string is at `ConnectionStrings:DefaultConnection` in `appsettings.json`.
- **Database:** SQL Server 2022 via Docker — best fit for a .NET banking case study. Start the container before running migrations: `docker run -e 'ACCEPT_EULA=1' -e 'MSSQL_SA_PASSWORD=YourStrong!Passw0rd' -p 1433:1433 -d --name subscriptionapp-db mcr.microsoft.com/azure-sql-edge`. The connection string in `appsettings.json` is pre-configured to match this container.

### Open questions raised
- None.

---

## Phase 2 — domain-modeler — 2026-05-11

### What was built

**Domain project** (zero NuGet dependencies — verified):
- `Enums/SubscriptionType.cs` — Electricity(0), Water(1), Internet(2), Gsm(3), NaturalGas(4)
- `Enums/SubscriptionStatus.cs` — Active(0), Passive(1)
- `Enums/PaymentStatus.cs` — Successful(0), Failed(1) — **Successful must remain 0** (filtered index relies on this)
- `Entities/Customer.cs`, `Subscription.cs`, `Payment.cs` — pure POCOs, no EF attributes, non-virtual nav properties
- `Exceptions/DomainException.cs` (base, maps to 409), `DuplicatePaymentException`, `InactiveSubscriptionException`, `InvalidPeriodException`, `NotFoundException` (maps to 404, NOT a DomainException subclass)

**Infrastructure project:**
- `Persistence/AppDbContext.cs` — three DbSets, applies configurations via `ApplyConfiguration`
- `Persistence/Configurations/CustomerConfiguration.cs` — unique index on Email, max lengths
- `Persistence/Configurations/SubscriptionConfiguration.cs` — compound unique index on (ProviderName, SubscriptionNumber), CASCADE FK to Customer
- `Persistence/Configurations/PaymentConfiguration.cs` — `decimal(18,2)` on Amount, Period maxLength 7, filtered unique index on (SubscriptionId, Period) WHERE [Status] = 0, CASCADE FK to Subscription
- `Persistence/DbInitializer.cs` — idempotent seed: 3 customers (Ahmet Yılmaz, Fatma Kaya, Mehmet Demir), 5 subscriptions (BEDAŞ, Türk Telekom, İSKİ, Türkcell, İGDAŞ)

**Api project:**
- `Program.cs` — wired `AddDbContext<AppDbContext>` with `UseSqlServer`, seeder called on startup in Development

**Migration:**
- `InitialCreate` generated and applied. `SubscriptionAppDb` created in the Docker SQL Edge container.

### Key decisions
- Decision: `NotFoundException` is NOT a subclass of `DomainException`. Reason: 404 (not found) is semantically different from 409 (business rule violation). Sharing the same base class would require the middleware to differentiate by type anyway. Alternative: single base Exception hierarchy — rejected to keep the middleware mapping explicit and readable.
- Decision: Period validated in service layer, not via DB CHECK constraint. Reason: SQL Server's LIKE-based CHECK constraints cannot fully validate `YYYY-MM` (they cannot enforce the month range 01–12). Service-layer validation is simpler and sufficient given the layered-defense approach. Alternative: add a partial CHECK `LIKE '[0-9][0-9][0-9][0-9]-[01][0-9]'` — rejected because it still allows months 13–19 and adds schema complexity for marginal benefit.
- Decision: Nav properties are non-virtual. Reason: lazy loading is disabled. Non-virtual nav properties enable explicit `.Include()` queries in services without the risk of N+1 surprises from accidental lazy loads. Alternative: omit nav properties entirely — rejected because explicit `.Include()` in service queries is cleaner than manual FK joins.
- Decision: `PaymentStatus.Successful = 0` is documented with an inline comment in `PaymentStatus.cs`. Reason: the filtered index `[Status] = 0` will silently break if this value ever changes. The comment makes the coupling visible to future developers.

### Files created/modified
- `backend/SubscriptionApp.Domain/Enums/SubscriptionType.cs`
- `backend/SubscriptionApp.Domain/Enums/SubscriptionStatus.cs`
- `backend/SubscriptionApp.Domain/Enums/PaymentStatus.cs`
- `backend/SubscriptionApp.Domain/Entities/Customer.cs`
- `backend/SubscriptionApp.Domain/Entities/Subscription.cs`
- `backend/SubscriptionApp.Domain/Entities/Payment.cs`
- `backend/SubscriptionApp.Domain/Exceptions/DomainException.cs`
- `backend/SubscriptionApp.Domain/Exceptions/DuplicatePaymentException.cs`
- `backend/SubscriptionApp.Domain/Exceptions/InactiveSubscriptionException.cs`
- `backend/SubscriptionApp.Domain/Exceptions/InvalidPeriodException.cs`
- `backend/SubscriptionApp.Domain/Exceptions/NotFoundException.cs`
- `backend/SubscriptionApp.Infrastructure/Persistence/AppDbContext.cs`
- `backend/SubscriptionApp.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs`
- `backend/SubscriptionApp.Infrastructure/Persistence/Configurations/SubscriptionConfiguration.cs`
- `backend/SubscriptionApp.Infrastructure/Persistence/Configurations/PaymentConfiguration.cs`
- `backend/SubscriptionApp.Infrastructure/Persistence/DbInitializer.cs`
- `backend/SubscriptionApp.Infrastructure/Migrations/` (InitialCreate)
- `backend/SubscriptionApp.Api/Program.cs` (DbContext + DbInitializer wired)

### Verification commands the student should run
```bash
export PATH="$PATH:/usr/local/share/dotnet:$HOME/.dotnet/tools"
cd backend

# Migration is applied — confirm:
dotnet ef migrations list --project SubscriptionApp.Infrastructure --startup-project SubscriptionApp.Api
# Expected: 20260511122352_InitialCreate (Applied)

# Build still clean:
dotnet build SubscriptionApp.slnx

# Start the API and open Swagger:
dotnet run --project SubscriptionApp.Api
# Navigate to http://localhost:5000/swagger
# (No endpoints yet — but the app should start without errors and seed data runs)
```

### Errata / runtime observations
- App listens on **http://localhost:5072** (set by `launchSettings.json`), not port 5000. The `ExternalServices:BaseUrl` key in `appsettings.json` (added in Phase 5) must use `http://localhost:5072`. The `external-services-builder` agent must update this value — add a note when it runs.

### Notes for the next agent (`customer-feature-builder`)
- All entities, enums, and exceptions are in place. The Domain project has zero NuGet dependencies.
- `AppDbContext` is registered as Scoped (default for `AddDbContext`). Services that use it should also be Scoped.
- Nav properties are non-virtual — always use explicit `.Include()` in queries; never rely on lazy loading.
- `NotFoundException` does NOT inherit from `DomainException`. The exception middleware must catch them separately: `NotFoundException` → 404, `DomainException` → 409.
- The `DomainException` base class exposes a `Code` string (e.g. `"DUPLICATE_PAYMENT"`) — use this as the `code` field in the error response shape.
- DbInitializer seeds on every `Development` startup (idempotent — skips if data exists). Don't add seeding logic elsewhere.
- Folder conventions to follow for services: `Infrastructure/Services/`, for controllers: `Api/Controllers/`, for DTOs: `Api/Dtos/<Resource>/`, for validators: `Api/Validators/<Resource>/`, for mappings: `Api/Mapping/`.

### Open questions raised
- None.

---

## Phase 3 — customer-feature-builder — 2026-05-11

### What was built

**Api project:**
- `Dtos/Customers/CreateCustomerRequest.cs` — `FullName`, `Email`, `PhoneNumber`
- `Dtos/Customers/CustomerResponse.cs` — mirrors entity + `SubscriptionCount` (computed from nav property)
- `Validators/Customers/CreateCustomerRequestValidator.cs` — `NotEmpty` + `MaximumLength` on all fields; `EmailAddress()` on Email; Turkish phone regex `^\+90[0-9]{10}$` with custom message on PhoneNumber
- `Mapping/CustomerMappings.cs` — static extension methods `ToEntity(this CreateCustomerRequest)` and `ToResponse(this Customer)`; `SubscriptionCount` uses `?.Count ?? 0` to handle cases where nav property is not loaded
- `Controllers/CustomersController.cs` — `GET /api/customers`, `GET /api/customers/{id}`, `POST /api/customers` (201 + Location header), `DELETE /api/customers/{id}` (204)
- `Middleware/ExceptionHandlingMiddleware.cs` — catches `NotFoundException` → 404, `DomainException` → 409, `Exception` → 500; uniform JSON shape `{ "error": { "code", "message" } }`
- `Program.cs` — updated with `AddFluentValidationAutoValidation()`, `AddValidatorsFromAssemblyContaining<CreateCustomerRequestValidator>()`, `AddScoped<ICustomerService, CustomerService>()`, `UseMiddleware<ExceptionHandlingMiddleware>()` (first middleware), custom `InvalidModelStateResponseFactory` returning consistent 400 error shape

**Infrastructure project:**
- `Services/ICustomerService.cs` — `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `DeleteAsync`
- `Services/CustomerService.cs` — EF Core scoped service; email uniqueness check throws `DomainException("DUPLICATE_EMAIL", …)`; `GetAllAsync` and `GetByIdAsync` use `.Include(c => c.Subscriptions)` for SubscriptionCount; `CreatedAt` set to `DateTime.UtcNow` in `CreateAsync`

**Build result:** `0 errors, 0 warnings`

### Key decisions
- Decision: `InvalidModelStateResponseFactory` overrides the default ASP.NET Core 400 response shape. Reason: FluentValidation auto-validation produces `ValidationProblemDetails` by default, which differs from the spec's `{ "error": { "code", "message", "details" } }` shape. Overriding the factory keeps all error shapes uniform without needing to catch `ValidationException` in the middleware. Alternative: catch `FluentValidation.ValidationException` in middleware — rejected because FluentValidation auto-validation swallows the exception before it propagates; the factory override is the correct integration point.
- Decision: `ICustomerService` and `CustomerService` live in `Infrastructure/Services/` (not a separate `Application` layer). Reason: spec uses a pragmatic 3-layer architecture (Domain, Infrastructure, Api); adding an `Application` layer is a premature abstraction for this scope. Alternative: move to `Api/Services/` — rejected because services depend on `AppDbContext` which lives in Infrastructure, so co-locating them avoids a circular reference.
- Decision: Service methods operate on domain entities (`Customer`), not DTOs. Reason: keeps the service layer reusable across controllers without depending on Api-layer types. Mapping happens at the controller boundary only. Alternative: pass DTOs all the way through — rejected per SPEC.md working principle "DTOs only at the API boundary."
- Decision: `ExceptionHandlingMiddleware` does NOT handle `FluentValidation.ValidationException`. Reason: with `AddFluentValidationAutoValidation()`, validation failures are caught by the MVC pipeline and routed to `InvalidModelStateResponseFactory` before the action executes. The exception never reaches middleware. Alternative: disable auto-validation and throw manually — rejected; more boilerplate with no benefit.
- Decision: Middleware is registered as the very first `app.Use*` call (before seeding, Swagger, routing). Reason: ensures that any unhandled exception thrown during request processing — including from controllers, services, and future middleware — is caught. Alternative: register after Swagger/HTTPS — rejected because exceptions from those middlewares would bypass the handler.

### Files created/modified
- `backend/SubscriptionApp.Api/Dtos/Customers/CreateCustomerRequest.cs`
- `backend/SubscriptionApp.Api/Dtos/Customers/CustomerResponse.cs`
- `backend/SubscriptionApp.Api/Validators/Customers/CreateCustomerRequestValidator.cs`
- `backend/SubscriptionApp.Api/Mapping/CustomerMappings.cs`
- `backend/SubscriptionApp.Api/Controllers/CustomersController.cs`
- `backend/SubscriptionApp.Api/Middleware/ExceptionHandlingMiddleware.cs`
- `backend/SubscriptionApp.Api/Program.cs` (FluentValidation, CustomerService DI, middleware registered)
- `backend/SubscriptionApp.Infrastructure/Services/ICustomerService.cs`
- `backend/SubscriptionApp.Infrastructure/Services/CustomerService.cs`

### Verification commands the student should run
```bash
export PATH="$PATH:/usr/local/share/dotnet"
cd backend

# Build must be clean
dotnet build SubscriptionApp.slnx
# Expected: Build succeeded. 0 Warning(s) 0 Error(s)

# Start the API (Docker container must be running)
dotnet run --project SubscriptionApp.Api
# Navigate to http://localhost:5072/swagger

# Smoke tests in Swagger:
# GET  /api/customers           → 200 with 3 seeded customers, each with SubscriptionCount
# GET  /api/customers/1         → 200 with Ahmet Yılmaz + SubscriptionCount = 2
# POST /api/customers { "fullName":"Test","email":"t@t.com","phoneNumber":"+905551111111" }
#                               → 201 with Location header
# POST /api/customers (same email again) → 409 { "error": { "code": "DUPLICATE_EMAIL", ... } }
# POST /api/customers { "phoneNumber":"bad" } → 400 { "error": { "code": "VALIDATION_ERROR", ... } }
# GET  /api/customers/9999      → 404 { "error": { "code": "NOT_FOUND", ... } }
# DELETE /api/customers/{id}    → 204
```

### Notes for the next agent (`subscription-feature-builder`)
- The customer vertical-slice is the template to follow: DTOs in `Api/Dtos/<Resource>/`, validators in `Api/Validators/<Resource>/`, service interface + implementation in `Infrastructure/Services/`, hand-written mapping in `Api/Mapping/`, thin controller in `Api/Controllers/`.
- `ExceptionHandlingMiddleware` is already in place. Do NOT modify it — just throw the right domain exceptions from your service and they will be caught automatically.
- `NotFoundException` → 404, `DomainException` → 409. Both constructors require `(string code, string message)`.
- For the subscription `PUT` endpoint: allow updating `Status`, `ProviderName`, `BillingDayOfMonth` — never `CustomerId`. Validate `BillingDayOfMonth` between 1 and 28 in the validator.
- Register `ISubscriptionService → SubscriptionService` (Scoped) in `Program.cs` — a `TODO` comment is already there.
- The `GET /api/subscriptions?customerId=` filter endpoint should return all subscriptions for a customer, including the customer's FullName in the response DTO.

### Open questions raised
- None.

---

## Phase 4 — subscription-feature-builder — 2026-05-11

### What was built

Mirrors the customer vertical slice exactly for Subscriptions.

**Api project:**
- `Dtos/Subscriptions/CreateSubscriptionRequest.cs` — `CustomerId`, `SubscriptionType`, `ProviderName`, `SubscriptionNumber`, `BillingDayOfMonth`
- `Dtos/Subscriptions/UpdateSubscriptionRequest.cs` — `Status`, `ProviderName`, `BillingDayOfMonth` (no `CustomerId`)
- `Dtos/Subscriptions/SubscriptionResponse.cs` — all entity fields + `CustomerFullName` (from nav property)
- `Validators/Subscriptions/CreateSubscriptionRequestValidator.cs` — `IsInEnum()` for `SubscriptionType`, `InclusiveBetween(1, 28)` for `BillingDayOfMonth`, `NotEmpty` + `MaximumLength` for strings
- `Validators/Subscriptions/UpdateSubscriptionRequestValidator.cs` — same BillingDayOfMonth rule, `IsInEnum()` for Status
- `Mapping/SubscriptionMappings.cs` — `ToEntity(this CreateSubscriptionRequest)`, `ToResponse(this Subscription)` (CustomerFullName via `?.FullName ?? ""`)
- `Controllers/SubscriptionsController.cs` — `GET /api/subscriptions?customerId=`, `GET /api/subscriptions/{id}`, `POST /api/subscriptions` (201), `PUT /api/subscriptions/{id}` (200), `DELETE /api/subscriptions/{id}` (204)
- `Program.cs` — added `using` for `Validators.Subscriptions`, registered `ISubscriptionService → SubscriptionService` (Scoped), removed TODO comment

**Infrastructure project:**
- `Services/ISubscriptionService.cs` — `GetAllAsync(int? customerId)`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`
- `Services/SubscriptionService.cs` — customer existence check on create (→ 404); compound uniqueness pre-check on create and on update when `ProviderName` changes (→ 409 `DUPLICATE_SUBSCRIPTION`); `Status` forced to `Active` on create; `CreatedAt` = `DateTime.UtcNow`; `GetByIdAsync` re-invoked after update to return the fully-joined entity

**Build result:** `0 errors, 0 warnings`

### Key decisions
- Decision: `UpdateAsync` signature is `(int id, SubscriptionStatus status, string providerName, int billingDayOfMonth)` — individual parameters rather than passing the request DTO into the service. Reason: the service lives in `Infrastructure`, which must not reference `Api` types. Passing the DTO would create a downward dependency from Infrastructure → Api. Alternative: define a shared update model in Domain or Infrastructure — rejected as premature abstraction for three scalar fields.
- Decision: `CreateAsync` re-fetches the entity via `GetByIdAsync` after insert to return the Customer nav property populated. Reason: after `SaveChangesAsync()` the newly inserted `Subscription` only has `CustomerId` set; the `Customer` nav property is `null`. The controller would produce `CustomerFullName: ""` without a re-fetch. Alternative: attach the Customer manually after save — rejected as more fragile than a clean re-fetch.
- Decision: `UpdateAsync` also re-fetches via `GetByIdAsync` after update. Same reason as above.
- Decision: Duplicate subscription pre-check on both create and update. Reason: the DB filtered unique index is the safety net, but a pre-check gives a user-readable `DUPLICATE_SUBSCRIPTION` error rather than a SQL Server `SqlException`. On update, the check is only performed when `ProviderName` changes (since `SubscriptionNumber` is immutable), avoiding a spurious query on every PUT.
- Decision: `AddValidatorsFromAssemblyContaining<CreateCustomerRequestValidator>()` already registered in Phase 3 picks up all validators in the `SubscriptionApp.Api` assembly, including the two new Subscription validators. No additional call needed. Reason: `AddValidatorsFromAssemblyContaining<T>` scans the entire assembly, not just one namespace.

### Files created/modified
- `backend/SubscriptionApp.Api/Dtos/Subscriptions/CreateSubscriptionRequest.cs`
- `backend/SubscriptionApp.Api/Dtos/Subscriptions/UpdateSubscriptionRequest.cs`
- `backend/SubscriptionApp.Api/Dtos/Subscriptions/SubscriptionResponse.cs`
- `backend/SubscriptionApp.Api/Validators/Subscriptions/CreateSubscriptionRequestValidator.cs`
- `backend/SubscriptionApp.Api/Validators/Subscriptions/UpdateSubscriptionRequestValidator.cs`
- `backend/SubscriptionApp.Api/Mapping/SubscriptionMappings.cs`
- `backend/SubscriptionApp.Api/Controllers/SubscriptionsController.cs`
- `backend/SubscriptionApp.Api/Program.cs` (ISubscriptionService DI, using added)
- `backend/SubscriptionApp.Infrastructure/Services/ISubscriptionService.cs`
- `backend/SubscriptionApp.Infrastructure/Services/SubscriptionService.cs`

### Verification commands the student should run
```bash
export PATH="$PATH:/usr/local/share/dotnet"
cd backend

dotnet build SubscriptionApp.slnx
# Expected: Build succeeded. 0 Warning(s) 0 Error(s)

dotnet run --project SubscriptionApp.Api
# Navigate to http://localhost:5072/swagger

# Smoke tests in Swagger:
# GET  /api/subscriptions                    → 200 with 5 seeded subscriptions + CustomerFullName
# GET  /api/subscriptions?customerId=1       → 200 with 2 subscriptions (Ahmet Yılmaz)
# GET  /api/subscriptions?customerId=99      → 200 with empty array
# GET  /api/subscriptions/1                  → 200 with BEDAŞ subscription
# GET  /api/subscriptions/9999               → 404
# POST /api/subscriptions { customerId:1, subscriptionType:0, providerName:"TEST", subscriptionNumber:"T-001", billingDayOfMonth:10 }
#                                            → 201 with Location header
# POST same body again                       → 409 DUPLICATE_SUBSCRIPTION
# POST with billingDayOfMonth:29             → 400 VALIDATION_ERROR
# POST with non-existent customerId:999      → 404
# PUT  /api/subscriptions/{id} { status:1, providerName:"NEW", billingDayOfMonth:15 }
#                                            → 200 with updated fields
# DELETE /api/subscriptions/{id}             → 204 (cascades to Payments)
```

### Notes for the next agent (`external-services-builder`)
- The `SubscriptionService.GetAllAsync` accepts a nullable `int?` and applies a WHERE filter only when provided. This pattern can be reused for payment filtering by `subscriptionId`.
- Cascade delete to Payments is already configured in `PaymentConfiguration.cs` (Phase 2). No additional code needed.
- `ExternalServices:BaseUrl` in `appsettings.json` must be `http://localhost:5072` (see Phase 2 errata — app listens on port 5072, not 5000).
- The three mock external controllers go in `Api/Controllers/External/` as a sub-namespace. The three typed `HttpClient` interfaces + implementations go in `Infrastructure/ExternalServices/`. Register each with `AddHttpClient<TInterface, TImpl>()` in `Program.cs` pointing to the `ExternalServices:BaseUrl` config key.

### Open questions raised
- None.

---

## Phase 5 — external-services-builder — 2026-05-11

### What was built

**Api project — mock external controllers (`Api/Controllers/External/`):**
- `DebtInquiryController.cs` — `GET /api/external/debt-inquiry/{subscriptionId}`. Looks up subscription type in DB, uses `new Random(subscriptionId)` (seeded) for deterministic amounts. Ranges: Electricity 100–400, Water 50–150, Internet 150–250, Gsm 80–200, NaturalGas 120–350. Returns `{ subscriptionId, amount, period (yyyy-MM), currency: "TRY" }`. Returns 404 if subscription not found.
- `PaymentGatewayController.cs` — `POST /api/external/payment-gateway`. Accepts `{ subscriptionId, amount }`. ~10% random failure. Success (200): `{ success: true, transactionId: "TXN-{guid}" }`. Failure (400): `{ success: false, errorCode: "INSUFFICIENT_FUNDS" | "GATEWAY_TIMEOUT" | "DECLINED" }`.
- `NotificationController.cs` — `POST /api/external/notifications`. Accepts `{ channel, recipient, message }`. Logs via `ILogger`, returns 200.

**Infrastructure project — typed HttpClients (`Infrastructure/ExternalServices/`):**
- `Models/DebtInquiryResponse.cs` — SubscriptionId, Amount, Period, Currency
- `Models/PaymentGatewayRequest.cs` — SubscriptionId, Amount
- `Models/PaymentGatewayResponse.cs` — Success, TransactionId?, ErrorCode?
- `Models/NotificationRequest.cs` — Channel, Recipient, Message
- `IDebtInquiryClient` / `DebtInquiryClient` — `GetDebtAsync(int subscriptionId)`, 5s timeout, logs method/URL/status/ms
- `IPaymentGatewayClient` / `PaymentGatewayClient` — `ProcessPaymentAsync(int subscriptionId, decimal amount)`, 10s timeout, logs; does NOT call `EnsureSuccessStatusCode` — both 200 and 400 carry a valid `PaymentGatewayResponse` body
- `INotificationClient` / `NotificationClient` — `SendAsync(string channel, string recipient, string message)`, calls `EnsureSuccessStatusCode`

**Configuration:**
- `appsettings.json` — `ExternalServices:BaseUrl` set to `http://localhost:5072`
- `Program.cs` — three `AddHttpClient<TInterface, TImpl>` registrations with BaseAddress + timeouts

**Build result:** initial build failed (6 errors: missing `using Microsoft.Extensions.Logging;` in three client files). Fixed by adding the using directive. Final build: `0 errors, 0 warnings`.

### Key decisions
- Decision: `DebtInquiryController` injects `AppDbContext` to look up subscription type. Reason: the mock must return type-appropriate amounts per spec, which requires knowing the `SubscriptionType`. Alternative: derive amount range from `subscriptionId % N` without DB lookup — rejected because it ignores the type and produces misleading test data (e.g., an Internet subscription returning a water-range amount).
- Decision: `PaymentGatewayClient.ProcessPaymentAsync` does NOT call `EnsureSuccessStatusCode`. Reason: the gateway mock returns HTTP 400 for declined payments but still sends a valid `PaymentGatewayResponse` body. `EnsureSuccessStatusCode()` would throw `HttpRequestException` before the body is read, preventing `PaymentService` from extracting the `ErrorCode`. The caller (`PaymentService` in Phase 6) checks `response.Success` to branch logic. Alternative: use a 200 response for all outcomes with a `Success` flag — rejected because 400 for failures is a realistic gateway contract.
- Decision: `INotificationClient.SendAsync` does call `EnsureSuccessStatusCode`. Reason: a failed notification is a non-critical infrastructure error (not a payment outcome), so throwing is appropriate. The caller in Phase 6 wraps notification in fire-and-forget, so a throw there just logs and drops.
- Decision: HttpClients log at `Information` level using `ILogger<T>` from `Microsoft.Extensions.Logging`. The namespace is available transitively through EF Core in the Infrastructure project without an additional NuGet package.

### HttpClient interface contracts (for `payment-feature-builder`)

```csharp
// 1. Debt Inquiry — call BEFORE initiating payment to show user the current debt
Task<DebtInquiryResponse> IDebtInquiryClient.GetDebtAsync(int subscriptionId);
// Response: { SubscriptionId, Amount (decimal), Period ("yyyy-MM"), Currency ("TRY") }

// 2. Payment Gateway — call INSIDE a DB transaction to process payment
Task<PaymentGatewayResponse> IPaymentGatewayClient.ProcessPaymentAsync(int subscriptionId, decimal amount);
// Response: { Success (bool), TransactionId? (string), ErrorCode? (string) }
// Success = true  → record PaymentStatus.Successful, set ExternalTransactionId = TransactionId
// Success = false → record PaymentStatus.Failed, throw ExternalServiceException(ErrorCode)

// 3. Notification — call AFTER committing the successful payment (fire-and-forget)
Task INotificationClient.SendAsync(string channel, string recipient, string message);
// channel: "SMS" or "EMAIL", recipient: customer phone or email, message: free text
```

### Files created/modified
- `backend/SubscriptionApp.Api/Controllers/External/DebtInquiryController.cs`
- `backend/SubscriptionApp.Api/Controllers/External/PaymentGatewayController.cs`
- `backend/SubscriptionApp.Api/Controllers/External/NotificationController.cs`
- `backend/SubscriptionApp.Infrastructure/ExternalServices/Models/DebtInquiryResponse.cs`
- `backend/SubscriptionApp.Infrastructure/ExternalServices/Models/PaymentGatewayRequest.cs`
- `backend/SubscriptionApp.Infrastructure/ExternalServices/Models/PaymentGatewayResponse.cs`
- `backend/SubscriptionApp.Infrastructure/ExternalServices/Models/NotificationRequest.cs`
- `backend/SubscriptionApp.Infrastructure/ExternalServices/IDebtInquiryClient.cs`
- `backend/SubscriptionApp.Infrastructure/ExternalServices/DebtInquiryClient.cs`
- `backend/SubscriptionApp.Infrastructure/ExternalServices/IPaymentGatewayClient.cs`
- `backend/SubscriptionApp.Infrastructure/ExternalServices/PaymentGatewayClient.cs`
- `backend/SubscriptionApp.Infrastructure/ExternalServices/INotificationClient.cs`
- `backend/SubscriptionApp.Infrastructure/ExternalServices/NotificationClient.cs`
- `backend/SubscriptionApp.Api/appsettings.json` (`ExternalServices:BaseUrl` added)
- `backend/SubscriptionApp.Api/Program.cs` (three `AddHttpClient` registrations)

### Verification commands the student should run
```bash
export PATH="$PATH:/usr/local/share/dotnet"
cd backend
dotnet build SubscriptionApp.slnx
# Expected: Build succeeded. 0 Warning(s) 0 Error(s)

dotnet run --project SubscriptionApp.Api
# Navigate to http://localhost:5072/swagger

# Smoke tests in Swagger:
# GET  /api/external/debt-inquiry/1   → 200 { subscriptionId:1, amount:<consistent>, period:"2026-05", currency:"TRY" }
# GET  /api/external/debt-inquiry/1   → same amount every time (seeded random)
# GET  /api/external/debt-inquiry/999 → 404
# POST /api/external/payment-gateway { subscriptionId:1, amount:150 }
#                                     → 200 { success:true, transactionId:"TXN-..." } (~90%)
#                                        OR 400 { success:false, errorCode:"..." } (~10%)
# POST /api/external/notifications { channel:"SMS", recipient:"+905551234567", message:"Paid" }
#                                     → 200; check console for ILogger output
```

### Notes for the next agent (`payment-feature-builder`)
- See "HttpClient interface contracts" section above for exact method signatures and branching logic.
- `PaymentGatewayClient` returns a `PaymentGatewayResponse` regardless of HTTP status — check `response.Success`, not `HttpRequestException`.
- Add `ExternalServiceException` to `Domain/Exceptions/` (maps to HTTP 502) and register it in `ExceptionHandlingMiddleware` (catch before the generic `Exception` catch).
- The `PaymentService.CreateAsync` flow: load subscription → reject Passive → pre-check duplicate (app-level) → open `IDbContextTransaction` → call gateway → on success: insert Payment (Successful) + commit + fire-and-forget notification; on failure: insert Payment (Failed) + commit + throw `ExternalServiceException`.
- Register `IPaymentService → PaymentService` (Scoped) in `Program.cs` — a TODO comment is already there.

### Open questions raised
- None.

---

## Phase 6 — payment-feature-builder — 2026-05-11

### What was built

**Domain project:**
- `Exceptions/ExternalServiceException.cs` — NOT a `DomainException` subclass (502, not 409). Carries `Code` (the gateway error code string). Maps to HTTP 502 in middleware.

**Api project:**
- `Dtos/Payments/CreatePaymentRequest.cs` — `SubscriptionId` (int), `Amount` (decimal), `Period` (string)
- `Dtos/Payments/PaymentResponse.cs` — `Id`, `SubscriptionId`, `Amount`, `Period`, `Status`, `PaymentDate`, `ExternalTransactionId?`
- `Validators/Payments/CreatePaymentRequestValidator.cs` — `SubscriptionId > 0`; `Amount > 0m` (decimal literal — no double); Period regex `^\d{4}-(0[1-9]|1[0-2])$`
- `Mapping/PaymentMappings.cs` — `ToResponse(this Payment)`
- `Controllers/PaymentsController.cs` — `GET /api/payments?subscriptionId=`, `GET /api/payments/{id}`, `POST /api/payments`
- `Middleware/ExceptionHandlingMiddleware.cs` — added `ExternalServiceException` catch → 502 (inserted between DomainException and generic Exception)
- `Program.cs` — `AddScoped<IPaymentService, PaymentService>()`

**Infrastructure project:**
- `Services/IPaymentService.cs` — `GetAllAsync(int?)`, `GetByIdAsync(int)`, `CreateAsync(int, decimal, string)`
- `Services/PaymentService.cs` — full transactional `CreateAsync` (see flow below)

**Build result:** `0 errors, 0 warnings`

### CreateAsync full flow

```
BEGIN TRANSACTION
  1. Load Subscription (.Include Customer). → NotFoundException(404) if missing.
  2. subscription.Status == Passive → InactiveSubscriptionException(409).
  3. AnyAsync Successful payment with same (SubscriptionId, Period)
        → DuplicatePaymentException(409).  [App-level pre-check for friendly error]
        [DB filtered unique index WHERE Status=0 is the safety net for concurrent races]
  4. IPaymentGatewayClient.ProcessPaymentAsync(subscriptionId, amount)
  5. Build Payment entity:
        Status = Success ? Successful : Failed
        ExternalTransactionId = Success ? transactionId : null
        Amount = decimal (never double)
        PaymentDate = DateTime.UtcNow (never DateTime.Now)
  6. Payments.Add → SaveChangesAsync → CommitAsync  (committed = true)

  If Success:
     Fire-and-forget Task.Run → INotificationClient.SendAsync(SMS, phone, msg)
     [swallow exception — notification failure must NOT affect payment outcome]
     Return payment entity → 201

  If Failure:
     Throw ExternalServiceException(errorCode) → 502
     [Failed record is already committed — kept for audit trail]

CATCH (if !committed):
     RollbackAsync()   [domain rule violations from steps 1–3 and unexpected errors]
     Throw
```

### Key decisions
- Decision: `ExternalServiceException` is NOT a subclass of `DomainException`. Reason: 502 (upstream failure) is semantically different from 409 (business rule). `DomainException` maps to 409; sharing the base class would require middleware type inspection. Alternative: use `DomainException` with a special code — rejected because it would return 409 for a gateway error, which misrepresents the HTTP semantics to API consumers.
- Decision: Failed payments are committed before throwing `ExternalServiceException`. Reason: every payment attempt (successful or declined) must be recorded for audit. The `committed` flag prevents the catch block from rolling back an already-committed transaction. Alternative: rollback on failure — rejected because the audit trail would be lost.
- Decision: Fire-and-forget notification is launched with `Task.Run` after commit, capturing `_notificationClient` by closure. Exception is swallowed in the inner try/catch. Reason: a notification failure must never affect the already-committed payment. The scope outlives the fire-and-forget task because `IHttpClientFactory` manages the HttpClient lifetime independently of the DI scope. `AppDbContext` is NOT captured in the lambda. Alternative: background `IHostedService` queue — rejected as premature abstraction for a case study.
- Decision: `PaymentService.CreateAsync` takes scalar parameters `(int subscriptionId, decimal amount, string period)` rather than a `Payment` entity. Reason: Amount and Period are validated in the controller/validator before reaching the service; the service constructs the entity itself to enforce invariants (UTC timestamp, correct Status). Alternative: accept a partial entity — rejected because it creates ambiguity about which fields are caller-set vs service-set.
- Decision: `IPaymentService.GetAllAsync` returns payments ordered by `PaymentDate DESC`. Reason: most recent payments are most useful for inspection in Swagger and the dashboard endpoint in Phase 7.

### Error scenarios and HTTP codes

| Scenario | HTTP | Code |
|---|---|---|
| Amount ≤ 0 or invalid Period | 400 | `VALIDATION_ERROR` |
| Subscription not found | 404 | `NOT_FOUND` |
| Subscription is Passive | 409 | `INACTIVE_SUBSCRIPTION` |
| Successful payment already exists for period | 409 | `DUPLICATE_PAYMENT` |
| Gateway declines | 502 | `INSUFFICIENT_FUNDS` / `GATEWAY_TIMEOUT` / `DECLINED` |
| Happy path | 201 | — |

### Files created/modified
- `backend/SubscriptionApp.Domain/Exceptions/ExternalServiceException.cs`
- `backend/SubscriptionApp.Api/Dtos/Payments/CreatePaymentRequest.cs`
- `backend/SubscriptionApp.Api/Dtos/Payments/PaymentResponse.cs`
- `backend/SubscriptionApp.Api/Validators/Payments/CreatePaymentRequestValidator.cs`
- `backend/SubscriptionApp.Api/Mapping/PaymentMappings.cs`
- `backend/SubscriptionApp.Api/Controllers/PaymentsController.cs`
- `backend/SubscriptionApp.Api/Middleware/ExceptionHandlingMiddleware.cs` (ExternalServiceException → 502)
- `backend/SubscriptionApp.Api/Program.cs` (IPaymentService DI)
- `backend/SubscriptionApp.Infrastructure/Services/IPaymentService.cs`
- `backend/SubscriptionApp.Infrastructure/Services/PaymentService.cs`

### Verification commands the student should run
```bash
export PATH="$PATH:/usr/local/share/dotnet"
cd backend
dotnet build SubscriptionApp.slnx
# Expected: Build succeeded. 0 Warning(s) 0 Error(s)

dotnet run --project SubscriptionApp.Api
# Navigate to http://localhost:5072/swagger

# Happy path (run multiple times — ~10% chance of 502 on gateway failure):
# POST /api/payments { subscriptionId:1, amount:150.00, period:"2026-05" }
#   → 201 { id, subscriptionId:1, amount:150.00, period:"2026-05", status:0 (Successful), transactionId:"TXN-..." }
#   → Console: ILogger notification line

# Duplicate payment (same subscriptionId + period again):
# POST /api/payments { subscriptionId:1, amount:150.00, period:"2026-05" }
#   → 409 { error: { code:"DUPLICATE_PAYMENT", message:"..." } }

# Passive subscription: first set subscription 1 to Passive via PUT /api/subscriptions/1
# POST /api/payments { subscriptionId:1, amount:100.00, period:"2026-06" }
#   → 409 { error: { code:"INACTIVE_SUBSCRIPTION", message:"..." } }

# Invalid period:
# POST /api/payments { subscriptionId:1, amount:100.00, period:"2026-13" }
#   → 400 { error: { code:"VALIDATION_ERROR", ... } }

# GET /api/payments?subscriptionId=1  → 200 list ordered by PaymentDate DESC
# GET /api/payments/{id}              → 200 / 404
```

### Notes for the next agent (`test-and-dashboard-builder`)
- Four rules that MUST have xUnit test coverage:
  1. `DuplicatePaymentException` when a Successful payment already exists for (subscriptionId, period)
  2. `InactiveSubscriptionException` when subscription.Status == Passive
  3. Amount validation — zero and negative values must return 400 (theory test with multiple inputs)
  4. Dashboard correctness — `GET /api/customers/{id}/dashboard` returns correct ActiveSubscriptionCount, UnpaidThisMonth, TotalPaidThisYear, RecentPayments (last 10)
- Use EF Core InMemory for tests. Helper: `CreateInMemoryContext()` with a unique DB name per test (`Guid.NewGuid().ToString()`).
- Mock `IPaymentGatewayClient` for deterministic success (return `new PaymentGatewayResponse { Success = true, TransactionId = "TXN-TEST" }`).
- Also mock `INotificationClient` — fire-and-forget means it won't block tests but the mock prevents real HTTP calls.
- The dashboard endpoint `GET /api/customers/{id}/dashboard` still needs to be built (it's in Phase 7 scope). Implement it directly in `CustomerService` or a new `DashboardService` — keep it in the same pattern as other services.
- UnpaidThisMonth definition: subscriptions that are Active and have NO Successful payment for the current month's period (`DateTime.UtcNow.ToString("yyyy-MM")`).

### Open questions raised
- None.

---

## Phase 7 — test-and-dashboard-builder — 2026-05-11

### What was built

**Part A — Dashboard**

- `Api/Dtos/Customers/CustomerDashboardResponse.cs` — `ActiveSubscriptionCount`, `UnpaidThisMonth` (list of `UnpaidSubscriptionSummary`), `RecentPayments` (list of `PaymentResponse`), `TotalPaidThisYear` (decimal)
- `UnpaidSubscriptionSummary` nested in same file — `Id`, `ProviderName`, `SubscriptionType`
- `Infrastructure/Services/ICustomerService.cs` — added `GetDashboardAsync(int customerId)` returning `CustomerDashboardData` (a plain data-carrying class defined in the same file)
- `Infrastructure/Services/CustomerService.cs` — implements `GetDashboardAsync`: loads all subscriptions for the customer, queries `paidThisMonthIds` (Successful payments for `currentPeriod`), computes unpaid list, fetches last 10 payments ordered by `PaymentDate DESC`, sums `TotalPaidThisYear` using UTC year boundaries (`yearStart` / `yearEnd`) rather than `.Year` property for reliable SQL translation
- `Api/Controllers/CustomersController.cs` — added `GET /api/customers/{id}/dashboard` action mapping `CustomerDashboardData` to `CustomerDashboardResponse`

**Part B — Service-layer guard added to PaymentService**

- `Infrastructure/Services/PaymentService.cs` — added `if (amount <= 0m) throw DomainException("INVALID_AMOUNT", ...)` before the transaction opens. Defence-in-depth: validator catches at boundary, service catches in case of direct programmatic calls.

**Part C — Tests** (`SubscriptionApp.Tests/UnitTest1.cs`)

All 6 test cases pass (`dotnet test` — 0 failures):

| Test | What it verifies |
|---|---|
| `PaymentService_RejectsDuplicateSuccessfulPaymentForSamePeriod` | Second payment for same (subscriptionId, period) throws `DuplicatePaymentException` |
| `PaymentService_RejectsPaymentOnPassiveSubscription` | Payment on Passive subscription throws `InactiveSubscriptionException` |
| `PaymentService_RejectsZeroOrNegativeAmount(0)` | Amount = 0 throws `DomainException("INVALID_AMOUNT")` |
| `PaymentService_RejectsZeroOrNegativeAmount(-1)` | Amount = -1 throws `DomainException("INVALID_AMOUNT")` |
| `PaymentService_RejectsZeroOrNegativeAmount(-999.99)` | Amount = -999.99 throws `DomainException("INVALID_AMOUNT")` |
| `CustomerDashboard_CorrectlyIdentifiesUnpaidThisMonthSubscriptions` | 3 active subs: 1 paid this month → not in Unpaid; 1 paid last month → in Unpaid; 1 never paid → in Unpaid. TotalPaidThisYear = 230m |

**Build result:** `0 errors, 0 warnings`. `dotnet test`: **6 passed, 0 failed**.

### Key decisions
- Decision: `CustomerDashboardData` is defined in `ICustomerService.cs` (Infrastructure layer), not in the Api layer. Reason: the service method must return something the controller can map to the Api DTO. A domain-neutral data carrier in Infrastructure avoids a circular reference while keeping the service interface cohesive. Alternative: return a tuple or multiple out-parameters — rejected as unreadable for a multi-field aggregate.
- Decision: `TotalPaidThisYear` uses explicit UTC year boundaries (`new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc)`) rather than `p.PaymentDate.Year == currentYear`. Reason: EF Core can translate `.Year` comparisons but year boundaries are unambiguous and guaranteed to produce an efficient `BETWEEN`-style query in SQL. The `.SumAsync((decimal?)p.Amount) ?? 0m` pattern handles the empty-set case cleanly.
- Decision: `TransactionIgnoredWarning` is suppressed in `CreateInMemoryContext()` via `.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))`. Reason: EF Core InMemory transactions are no-ops by design; the warning-as-error default would make tests for `PaymentService` impossible without restructuring the service. The suppression is scoped to the test helper only. Alternative: wrap `BeginTransactionAsync` in a try/catch in PaymentService — rejected because it would hide real transaction issues in production.
- Decision: `file` access modifier on test doubles and helper (`SuccessGatewayStub`, `NoopNotificationStub`, `TestHelpers`). Reason: C# 11 `file`-scoped types are invisible outside their source file, preventing accidental reuse and keeping the test file self-contained. Alternative: `internal` or `private` nested classes — rejected; `file` is the cleanest scope for test-only helpers.

### Files created/modified
- `backend/SubscriptionApp.Api/Dtos/Customers/CustomerDashboardResponse.cs`
- `backend/SubscriptionApp.Api/Controllers/CustomersController.cs` (dashboard action added)
- `backend/SubscriptionApp.Infrastructure/Services/ICustomerService.cs` (GetDashboardAsync + CustomerDashboardData)
- `backend/SubscriptionApp.Infrastructure/Services/CustomerService.cs` (GetDashboardAsync implemented)
- `backend/SubscriptionApp.Infrastructure/Services/PaymentService.cs` (amount > 0 guard)
- `backend/SubscriptionApp.Tests/UnitTest1.cs` (all 4 test classes, 6 test cases)

### Verification
```bash
export PATH="$PATH:/usr/local/share/dotnet"
cd backend

dotnet test SubscriptionApp.Tests --verbosity normal
# Expected: Test Run Successful. Total tests: 6 Passed: 6

dotnet run --project SubscriptionApp.Api
# GET /api/customers/1/dashboard
#   → { activeSubscriptionCount, unpaidThisMonth: [...], recentPayments: [...], totalPaidThisYear }
```

### Notes for the next agent (`frontend-builder`)
- All backend endpoints are complete. The frontend should consume:
  - `GET /api/customers` — customer list page
  - `GET /api/customers/{id}/dashboard` — per-customer dashboard
  - `GET /api/subscriptions?customerId={id}` — subscription list per customer
  - `GET /api/external/debt-inquiry/{subscriptionId}` — "Query Debt" button on subscription detail
  - `POST /api/payments` — "Pay Now" flow; handle 201, 409, 502 distinctly
  - `PUT /api/subscriptions/{id}` — toggle Status Active↔Passive
- All error responses follow `{ error: { code, message, details? } }`.
- The `status` field in responses is an integer enum (0 = Active/Successful, 1 = Passive/Failed). Display human-readable labels in the UI.
- Vite dev server proxy `/api` → `http://localhost:5072` must be configured in `vite.config.js` to avoid CORS issues.

### Open questions raised
- None.

---

## Phase 8 — frontend-builder — 2026-05-11

### What was built

Vite + React 18 (plain JavaScript) single-page application in `/frontend`.

**Scaffold & config:**
- `npm create vite@latest . -- --template react` + `npm install react-router-dom`
- `vite.config.js` — proxy `/api` → `http://localhost:5072` (actual backend port from launchSettings.json)
- `src/main.jsx` — removed default `index.css` import; global styles loaded via `App.jsx`

**API layer (`src/api/`):**
- `client.js` — `apiFetch` wrapper: parses error body, attaches `.code`/`.details`/`.status` to thrown `Error`; handles 204 No Content
- `customers.js` — `getCustomers`, `getCustomer`, `createCustomer`, `deleteCustomer`, `getDashboard`
- `subscriptions.js` — `getSubscriptions(customerId?)`, `getSubscription`, `createSubscription`, `updateSubscription`, `deleteSubscription`
- `payments.js` — `getPayments(subscriptionId?)`, `createPayment`
- `external.js` — `getDebt(subscriptionId)`

**Components (`src/components/`):**
- `Button.jsx` — `variant` prop (`primary`/`secondary`/`danger`), `small`, `disabled`
- `Modal.jsx` — overlay with Escape key handler, click-outside-to-close
- `Table.jsx` — column config with optional `render` fn; empty-state row
- `LoadingSpinner.jsx` — CSS spin animation
- `ErrorBanner.jsx` — dismissible red banner; renders nothing when `message` is null

**Pages (`src/pages/`):**
- `CustomersPage.jsx` — customer table, Add Customer modal with client-side Turkish phone validation (`^\+90[0-9]{10}$`), delete with confirmation, row click navigates to `/subscriptions?customerId={id}`
- `SubscriptionsPage.jsx` — customer dropdown filter (synced to `?customerId=` query param), add/edit/delete modals; edit allows Status/ProviderName/BillingDayOfMonth only; BillingDayOfMonth clamped to 1–28; row click navigates to `/subscriptions/{id}`
- `SubscriptionDetailPage.jsx` — detail card, "Query Debt" button → debt card with amount/period/currency, "Pay Now" button → payment modal pre-filled with debt amount; handles 409 (DUPLICATE_PAYMENT, INACTIVE_SUBSCRIPTION) and 502 (gateway errors) distinctly via error banner
- `DashboardPage.jsx` — customer dropdown; on select renders 3 stat cards (Active Subs, Unpaid Count, Total Paid This Year) + Unpaid This Month table + Recent Payments table (last 10)

**Styling (`src/styles/global.css`):**
Single CSS file with CSS custom properties (--color-primary, --color-bg, etc.). Components: nav, page, card, table, modal, form, button, badge, spinner, toast, debt-card, dashboard-grid, stat-card. Professional banking palette (navy primary, muted grays, no gradients or animations except spinner).

**Routing (`src/App.jsx`):** React Router v6, top nav with `NavLink` active state, 5 routes.

**Build result:** `vite build` — 38 modules, 0 errors, 0 warnings.

### Key decisions
- Decision: `client.js` reads the error body even on non-2xx responses instead of calling `EnsureSuccessStatusCode` equivalent. Reason: the backend returns structured `{ error: { code, message } }` on 400/404/409/502 — discarding that body would lose the user-friendly message. The error object exposes `.code` so pages can branch on specific codes if needed.
- Decision: Enum values are displayed via index arrays (`SUB_TYPES[r.subscriptionType]`) rather than a switch or object map. Reason: the backend returns integer enum values; the display labels are stable and ordered, so array access by index is the simplest correct approach.
- Decision: No CSS modules, no styled-components — one `global.css` with BEM-light class names. Reason: spec explicitly says plain CSS; for a banking case study with 4 pages, a single file is easier to review than per-component files and avoids build configuration.
- Decision: Toasts are implemented as ephemeral state (`setToast(msg)`) dismissed on click rather than via a timer. Reason: no global Context or timer management needed; click-to-dismiss is sufficient for a case study demo.

### Files created/modified
- `frontend/vite.config.js`
- `frontend/src/main.jsx`
- `frontend/src/App.jsx`
- `frontend/src/api/client.js`
- `frontend/src/api/customers.js`
- `frontend/src/api/subscriptions.js`
- `frontend/src/api/payments.js`
- `frontend/src/api/external.js`
- `frontend/src/components/Button.jsx`
- `frontend/src/components/Modal.jsx`
- `frontend/src/components/Table.jsx`
- `frontend/src/components/LoadingSpinner.jsx`
- `frontend/src/components/ErrorBanner.jsx`
- `frontend/src/styles/global.css`
- `frontend/src/pages/CustomersPage.jsx`
- `frontend/src/pages/SubscriptionsPage.jsx`
- `frontend/src/pages/SubscriptionDetailPage.jsx`
- `frontend/src/pages/DashboardPage.jsx`

### How to run
```bash
# Terminal 1 — backend (Docker container must be running)
export PATH="$PATH:/usr/local/share/dotnet"
cd backend && dotnet run --project SubscriptionApp.Api

# Terminal 2 — frontend
cd frontend && npm run dev
# Open http://localhost:5173
```

### End-to-end flow to verify
1. **Customers** → Add Customer → fill form → Create → row appears in table
2. Click customer name → Subscriptions filtered by that customer
3. **Subscriptions** → Add Subscription → fill form → Create → row appears
4. Click provider name → **Subscription Detail** → Query Debt → debt card appears → Pay Now → Confirm → success toast
5. Pay again for same period → 409 `DUPLICATE_PAYMENT` shown in error banner
6. **Dashboard** → select customer → stat cards + unpaid list + recent payments render

### Notes for the next agent (`documentation-finalizer`)
- Screenshots to capture: Customers list, Add Customer modal with validation error, Subscriptions filtered by customer, Subscription Detail with debt card, payment error banner (409), Dashboard with all 4 sections populated.
- The AI Usage section in the final README should note: all 9 phases were AI-generated with Claude Code; human review happened at each PHASE_LOG entry; the Docker/port decisions (Azure SQL Edge, port 5072) were driven by runtime errors and corrected interactively.

### Open questions raised
- None.

---

<!--
Template for future entries — copy and fill in.

## Phase N — <agent-name> — <YYYY-MM-DD>

### What was built
- ...

### Key decisions
- Decision: <what>. Reason: <why>. Alternative considered: <what else>.

### Files created/modified
- path/to/file.cs

### Verification commands the student should run
- `dotnet build`
- ...

### Notes for the next agent
- ...

### Open questions raised
- ...

---
-->
