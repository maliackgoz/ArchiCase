# Subscription & Auto-Payment Reminder Application

A banking case study built with a multi-agent AI workflow.

## You are here

You're about to build this project phase-by-phase. The architecture, specification, and agent definitions are already set up. You drive the build by invoking agents one at a time.

## Prerequisites

- .NET 8 SDK
- Node.js 18+
- SQL Server LocalDB (comes with Visual Studio or as a standalone installer)
- Git
- Claude Code CLI installed and authenticated

## Project layout right now

```
/
├── AGENTS.md            ← Multi-agent coordination rules (READ THIS)
├── SPEC.md              ← The full specification (single source of truth)
├── PHASE_LOG.md         ← Bootstrap entry already there; each agent appends as it finishes
├── FUTURE_IMPROVEMENTS.md ← Optional enhancements for after core is done
├── README.md            ← This file
├── .claude/agents/      ← 9 specialist agent definitions
├── backend/             ← (empty — Phase 1 will populate)
└── frontend/            ← (empty — Phase 8 will populate)
```

Git is already initialized with the initial scaffolding commit on the `main` branch.

## How to build the project

### Step 1: Read the foundational docs
1. Read [AGENTS.md](./AGENTS.md) (10 minutes)
2. Skim [SPEC.md](./SPEC.md) (15 minutes — you'll come back to it)
3. Look at [.claude/agents/solution-architect.md](./.claude/agents/solution-architect.md) to understand the first phase

### Step 2: Run Phase 1

Open Claude Code in this directory and run:

```
/agents solution-architect
```

or invoke directly:

```
Use the solution-architect agent to execute Phase 1.
```

The agent will:
- Create the .NET solution and 4 projects under `/backend`
- Set up NuGet packages and project references
- Configure `Program.cs` skeleton
- Run `dotnet build` to verify

### Step 3: Review Phase 1 output
- Read the new entry in [PHASE_LOG.md](./PHASE_LOG.md)
- Run the verification commands the agent listed
- Open the solution in your IDE and look at what was created
- Make sure you understand each file

### Step 4: Commit and proceed

```bash
git add .
git commit -m "Phase 1: Solution skeleton"
```

Then invoke Phase 2's agent (`domain-modeler`). Repeat the review-commit cycle for each phase.

## Phase pipeline

| Phase | Agent | What gets built |
|------:|-------|-----------------|
| 1 | `solution-architect` | Solution + projects + NuGet |
| 2 | `domain-modeler` | Entities, DbContext, migrations, seed |
| 3 | `customer-feature-builder` | Customers CRUD (template phase) |
| 4 | `subscription-feature-builder` | Subscriptions CRUD |
| 5 | `external-services-builder` | Mock services + HttpClients |
| 6 | `payment-feature-builder` | Payment flow (highest stakes) |
| 7 | `test-and-dashboard-builder` | Dashboard + xUnit tests |
| 8 | `frontend-builder` | React app (4 pages) |
| 9 | `documentation-finalizer` | All `/docs/*` + final README |

## Rules of engagement

- **Never skip a phase.** Each one assumes the previous is complete.
- **Always review PHASE_LOG.md before invoking the next agent.** If something looks wrong, fix it or re-run the phase.
- **Agents stay in their scope.** If an agent tries to do work outside its assigned phase, stop it.
- **You are the human in the loop.** The agents are smart but they make mistakes. Your review at each phase boundary is what makes this work for an interview deliverable.

## When you're stuck

- If an agent's output doesn't compile or doesn't match the spec, re-invoke it with specific feedback. Don't try to fix it manually and pretend it worked — the AI Usage section requires honest disclosure.
- If you don't understand a piece of code an agent wrote, ask Claude Code to explain it before moving on. You need to be able to defend everything.

## After Phase 9

- Run the submission checklist the `documentation-finalizer` agent produces
- Look at [FUTURE_IMPROVEMENTS.md](./FUTURE_IMPROVEMENTS.md) — pick anything you have time for
- Practice explaining: 3-layer architecture, why `decimal`, why filtered unique index, the payment transaction flow, why hand-written mapping

Good luck.
