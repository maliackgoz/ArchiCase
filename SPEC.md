# SPEC — Subscription & Auto-Payment Reminder Application

This document is the **single source of truth** for every agent and every phase. If anything in `PHASE_LOG.md`, agent definitions, or code contradicts this file, this file wins. If something here is ambiguous, stop and ask — do not guess.

---

## Goal

Build a Subscription & Auto-Payment Reminder application: customers register utility subscriptions (electricity, water, internet, GSM, natural gas), query debt from mock third-party services, pay bills, and view their payment history. This is a banking domain case study where **financial correctness and code clarity matter more than feature breadth**.

---

## Tech Stack (fixed, do not deviate)

- **Backend:** .NET 8 Web API, C#
- **Frontend:** React 18 (Vite), plain JavaScript, plain CSS — no TypeScript, no Tailwind, no UI libraries, no Axios
- **Database:** Microsoft SQL Server LocalDB via Entity Framework Core 8 (code-first + migrations)
- **Validation:** FluentValidation
- **Testing:** xUnit + EF Core InMemory provider (critical business rules only)
- **API docs:** Swashbuckle (Swagger UI in development)

---

## Architecture — 3-Layer (Pragmatic Clean Architecture)

```
/backend
  /SubscriptionApp.Api            → Controllers, DTOs, Validators, Middleware, Program.cs
  /SubscriptionApp.Domain         → Entities, Enums, Domain Exceptions (ZERO external dependencies)
  /SubscriptionApp.Infrastructure → DbContext, EF Configurations, Services, HttpClient calls
  /SubscriptionApp.Tests          → xUnit tests
/frontend
  /src/pages, /src/components, /src/api
/docs
  ER-diagram.md, api-endpoints.md, flow-diagrams.md, architecture.md
```

**The Domain project must have ZERO dependencies on EF Core, ASP.NET, or any framework.** This is the single most important architectural rule. Agents must enforce this. Verify by reading the `.csproj` for `SubscriptionApp.Domain` — it must reference no NuGet packages.

---

## Domain Model

### Customer
- `Id` (int, PK)
- `FullName` (string, required, 2–100 chars)
- `Email` (string, required, unique, valid email)
- `PhoneNumber` (string, required, Turkish format `^\+90[0-9]{10}$`)
- `CreatedAt` (DateTime, UTC)

### Subscription
- `Id` (int, PK)
- `CustomerId` (int, FK → Customer)
- `SubscriptionType` (enum: Electricity / Water / Internet / Gsm / NaturalGas)
- `ProviderName` (string, required, max 100)
- `SubscriptionNumber` (string, required, max 50; unique per `ProviderName`)
- `Status` (enum: Active / Passive; defaults to Active on creation)
- `BillingDayOfMonth` (int, 1–28 inclusive)
- `CreatedAt` (DateTime, UTC)

### Payment
- `Id` (int, PK)
- `SubscriptionId` (int, FK → Subscription)
- `Amount` (decimal(18,2), > 0)
- `PaymentDate` (DateTime, UTC)
- `Period` (string, length 7, matches `^\d{4}-(0[1-9]|1[0-2])$`, e.g. `"2026-05"`)
- `Status` (enum: Successful / Failed)
- `ExternalTransactionId` (string, nullable, max 100)

### Relationships

- Customer 1 → N Subscription
- Subscription 1 → N Payment
- **Hard delete cascade** is the chosen behavior because the spec explicitly allows Customer deletion. Configure `OnDelete(DeleteBehavior.Cascade)` on both relationships. (This decision is intentional; document the trade-off in `/docs/architecture.md` during Phase 9.)

---

## Critical Business Rules (enforced in Domain + DB)

1. **No duplicate successful payments per period.** No two `Successful` payments may exist for the same `(SubscriptionId, Period)`. Enforce both:
   - In `PaymentService` via a pre-check before insert.
   - In SQL via a **unique filtered index** on `Payment(SubscriptionId, Period) WHERE Status = 0` (where `0` is the int value of `PaymentStatus.Successful`).
   The pre-check gives nice error messages; the index is the ultimate safety net against races.
2. **Payment amount must be > 0.** Validated by FluentValidation at the boundary AND defended in the service.
3. **Passive subscriptions cannot accept new payments.** Enforced in `PaymentService`, returns 409.
4. **Period format.** Must match regex `^\d{4}-(0[1-9]|1[0-2])$`. Validated at the boundary; defended in service.
5. **All money is `decimal`.** Never `double`, never `float`, never `var` that resolves to one.
6. **All timestamps are UTC.** Use `DateTime.UtcNow`. Never `DateTime.Now`. Convert at presentation layer only.

---

## API Endpoints

### Customers

- `POST   /api/customers`             → Create customer
- `GET    /api/customers`             → List all
- `GET    /api/customers/{id}`        → Get by id
- `DELETE /api/customers/{id}`        → Delete (cascades)

### Subscriptions

- `POST   /api/subscriptions`                  → Create
- `GET    /api/subscriptions?customerId={id}`  → List (optional filter)
- `GET    /api/subscriptions/{id}`             → Get by id
- `PUT    /api/subscriptions/{id}`             → Update (Status / ProviderName / BillingDayOfMonth only — never CustomerId)
- `DELETE /api/subscriptions/{id}`             → Delete (cascades to payments)

### Payments

- `POST   /api/payments`                       → Create payment (the high-stakes flow)
- `GET    /api/payments?subscriptionId={id}`   → List for subscription
- `GET    /api/payments/{id}`                  → Get by id

### External (mock services — real HTTP endpoints, exposed on the same app)

- `GET    /api/external/debt-inquiry/{subscriptionId}`  → Returns a mock debt amount. Deterministic per subscriptionId (use it as RNG seed). Amount ranges by type: Electricity 100–400, Water 50–150, Internet 150–250, Gsm 80–200, NaturalGas 120–350. Period = current UTC year-month.
- `POST   /api/external/payment-gateway`                → Body `{ subscriptionId, amount }`. ~10% random failure rate. Success returns `{ success: true, transactionId: "TXN-<guid>" }`. Failure returns 400 with `{ success: false, errorCode: "INSUFFICIENT_FUNDS" | "GATEWAY_TIMEOUT" | "DECLINED" }`.
- `POST   /api/external/notifications`                  → Body `{ channel, recipient, message }`. Logs via `ILogger`, returns 200.

### Dashboard

- `GET    /api/customers/{id}/dashboard`  → Returns:
  - `ActiveSubscriptionCount`
  - `UnpaidThisMonth`: list of subscription summaries with no `Successful` payment for current UTC period
  - `RecentPayments`: last 10 payments across all subscriptions of the customer
  - `TotalPaidThisYear`: decimal sum of Successful payments in the current UTC year

---

## Response & Error Conventions

| Situation | HTTP code |
|---|---|
| Resource created | 201 + `Location` header |
| Resource deleted | 204 |
| Validation error (field-level) | 400 |
| Resource not found | 404 |
| Business rule violation (duplicate payment, passive sub) | 409 |
| External service failure | 502 |
| Unhandled / internal | 500 |

### Consistent error shape

```json
{
  "error": {
    "code": "DUPLICATE_PAYMENT",
    "message": "A successful payment already exists for this subscription and period.",
    "details": { "subscriptionId": 7, "period": "2026-05" }
  }
}
```

The `code` field uses SCREAMING_SNAKE_CASE constants per error type. `details` is optional and free-shape.

---

## Working Principles (every agent must respect)

- **Boring, idiomatic code over clever code.** The student must be able to defend every line in an interview.
- **No premature abstraction.** No generic repositories, no MediatR, no AutoMapper. Hand-written mapping extensions in `Api/Mapping/`.
- **Inline comments for non-obvious decisions.** Why `decimal`, why `Scoped` vs `Singleton`, why this exception type, why this index filter — explain in a one-liner next to the code.
- **DTOs only at API boundary.** Never expose entities outside the service layer.
- **Controllers stay thin.** Logic lives in services. Controllers translate HTTP ↔ service input/output and choose status codes.
- **UTC everywhere internally.** Convert at presentation layer only.
- **Validate at the boundary, defend in depth.** FluentValidation handles user-facing validation; service methods re-check critical invariants against the DB.

---

## Out of Scope (for the core deliverable)

The following are deliberately excluded from Phases 1–9. They live in `FUTURE_IMPROVEMENTS.md` and may be added only after Phase 9 is complete and reviewed.

- Authentication / authorization
- Background jobs / scheduled reminders
- Idempotency-Key support
- Audit log tables
- Soft delete
- Internationalization
- Multi-currency
- Docker / CI pipelines
- Integration tests against a real DB

---

## Glossary

- **Period** — A string `YYYY-MM` identifying a billing month. UTC-based.
- **Successful payment** — A payment that completed at the gateway and was recorded with `Status = Successful` and a non-null `ExternalTransactionId`.
- **Failed payment** — A payment attempt where the gateway rejected; recorded with `Status = Failed` and null `ExternalTransactionId` so we keep the audit trail.
- **Layered defense** — The combination of FluentValidation + service-level checks + DB constraints that protects every critical rule.
