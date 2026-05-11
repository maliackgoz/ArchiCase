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
