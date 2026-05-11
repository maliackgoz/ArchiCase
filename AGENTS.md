# AGENTS.md — Multi-Agent Coordination Contract

Every specialist agent reads this file before doing anything else. It defines the rules of engagement for the 9-phase pipeline.

---

## Project Overview

This repository is a banking-domain case study: a Subscription & Auto-Payment Reminder application built as a .NET 8 Web API + React (Vite, plain JS) front-end on top of EF Core 8 + SQL Server LocalDB. The full specification lives in [SPEC.md](./SPEC.md). The build is divided into 9 sequential phases, each owned by a single specialist agent with a tight, non-overlapping scope. A human (the student) reviews the output of each phase and manually invokes the next.

---

## The 9-Phase Pipeline

| Phase | Agent | Owns |
|------:|-------|------|
| 1 | `solution-architect` | Solution + 4 projects + NuGet + Program.cs skeleton |
| 2 | `domain-modeler` | Entities, enums, exceptions, DbContext, EF configs, initial migration, seed data |
| 3 | `customer-feature-builder` | Customers vertical slice — establishes the pattern |
| 4 | `subscription-feature-builder` | Subscriptions vertical slice |
| 5 | `external-services-builder` | Mock debt-inquiry / payment-gateway / notification endpoints + HttpClients |
| 6 | `payment-feature-builder` | Payment flow + business rule enforcement + transaction handling |
| 7 | `test-and-dashboard-builder` | Dashboard endpoint + xUnit tests for critical rules |
| 8 | `frontend-builder` | React app, 4 pages, plain CSS, fetch-based API client |
| 9 | `documentation-finalizer` | ER + flow diagrams, endpoint catalog, architecture doc, AI usage section |

### Current status

Status is tracked in [PHASE_LOG.md](./PHASE_LOG.md). After completing its scope an agent **appends** a new section to that file — never edits or deletes a previous entry.

---

## Universal Rules (every agent follows these)

1. **SPEC.md wins.** If an instruction in an agent file or this file appears to contradict `SPEC.md`, treat it as a bug and stop. Ask the human before continuing.
2. **Boring, idiomatic code over clever code.** The student must defend every line in an interview.
3. **No premature abstraction.** No generic repositories. No MediatR. No AutoMapper. Hand-written mapping extensions only.
4. **No unrequested features.** If you finish your scope, stop — do not add "nice-to-haves".
5. **DTOs only at the API boundary.** Entities never leave the service layer.
6. **Controllers stay thin.** Logic lives in services.
7. **`decimal` for money. `DateTime.UtcNow` for timestamps.** Never `double`/`float` for money; never `DateTime.Now`.
8. **Inline comments for non-obvious decisions.** Single-line, focused on _why_.
9. **Validate at the boundary, defend in the service, enforce in the DB.** Layered defense for every critical rule.
10. **The Domain project has zero external dependencies.** No EF, no ASP.NET, no Newtonsoft. Verify the `.csproj` after editing the Domain project.

---

## Handoff Protocol

Every agent follows this exact sequence:

1. **Read** `AGENTS.md` (this file), `SPEC.md`, and `PHASE_LOG.md` in full. Skim earlier phase entries to understand the established patterns and conventions.
2. **Confirm scope.** Match your agent name to its phase in the table above. If `PHASE_LOG.md` shows the previous phase incomplete, stop and report — do not proceed.
3. **Do only your scope.** Each agent file has explicit "Your scope" and "You do NOT" sections. Respect both.
4. **Comment as you write.** When you make a non-obvious decision (e.g. choosing `Scoped` over `Singleton`, using `HasFilter` for a partial unique index, ordering middleware), leave a single-line comment in the code AND a note in your PHASE_LOG entry.
5. **Run verification.** Build, migrate, swagger smoke test — whichever your scope requires. Don't claim completion if the build is red.
6. **Append a PHASE_LOG entry** following the template at the top of that file: what was built, key decisions, files touched, verification commands, notes for the next agent, open questions.
7. **Stop.** Do not start the next phase. Do not modify files outside your scope. End your run with a one-paragraph summary in chat referencing your PHASE_LOG entry.

---

## Forbidden Actions

Every agent is forbidden from:

- **Modifying files outside its phase scope.** If a previous phase produced something wrong, stop and ask — do not silently patch it.
- **Skipping ahead.** If you're the Phase 3 agent and you spot a Phase 6 problem, note it in PHASE_LOG "Notes for next agent" and stop.
- **Adding features not in `SPEC.md`.** If you think something is missing, write it as an open question — do not just add it.
- **Deleting or rewriting prior PHASE_LOG entries.** They are append-only. Corrections go in a new "Errata for Phase N" sub-section.
- **Touching git.** Commits are the student's responsibility. (Exception: the bootstrap commit, which was already created.)
- **Adding NuGet packages, npm packages, or top-level config files** unless your phase explicitly lists them. If you think you need one, stop and ask.
- **Modifying `SPEC.md` or `AGENTS.md`.** They are read-only inputs. If they're wrong, the human edits them.

---

## Escalation Protocol

If at any point you encounter ambiguity, conflicting instructions, or unexpected state:

1. Stop work immediately.
2. Add an "Open questions raised" entry to your PHASE_LOG section (even though your phase is incomplete — clearly mark it `## Phase N — INCOMPLETE`).
3. Summarize the issue in chat with: what you tried, what you observed, what the spec says, what you think the right answer is.
4. **Wait for human direction.** Do not guess. Do not work around the problem by improvising.

---

## Patterns Established by Earlier Phases

Some patterns are not in `SPEC.md` because they only emerge once code is written. The first phase that establishes a pattern documents it in PHASE_LOG. Subsequent phases must follow it.

Examples of patterns that propagate forward:
- Folder layout under `Api/Dtos/<Resource>/`, `Api/Validators/<Resource>/`, `Api/Mapping/`, `Infrastructure/Services/`
- Mapping extension method naming (`ToResponse`, `ToEntity`)
- Service method naming (`CreateAsync`, `GetByIdAsync`, `GetAllAsync`, `DeleteAsync`)
- Controller shape and the exact set of status codes per verb
- Error response shape (matches `SPEC.md` but the C# implementation is shared)

When the `customer-feature-builder` (Phase 3) finishes, the patterns it established become **mandatory** for Phases 4, 6, and 7. Document deviations explicitly.

---

## Source of Truth

[SPEC.md](./SPEC.md) is the single source of truth for what to build.
[PHASE_LOG.md](./PHASE_LOG.md) is the single source of truth for what has been built.
This file (`AGENTS.md`) is the single source of truth for how agents collaborate.

If you ever find yourself wanting to update behavior that isn't in your scope, instead update PHASE_LOG with your concern and stop.
