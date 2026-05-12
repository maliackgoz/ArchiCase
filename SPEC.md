# SPEC — Subscription & Auto-Payment Reminder Application

This document is the **single source of truth** for every agent and every phase. If anything in `PHASE_LOG.md`, agent definitions, or code contradicts this file, this file wins. If something here is ambiguous, stop and ask — do not guess.

---

## Goal

Build a Subscription & Auto-Payment Reminder application: customers register utility subscriptions (electricity, water, internet, GSM, natural gas), query debt from mock third-party services, pay bills, and view their payment history. This is a banking domain case study where **financial correctness and code clarity matter more than feature breadth**.

---

## Tech Stack (fixed, do not deviate)

- **Backend:** .NET 10 Web API, C#
- **Frontend:** React 18 (Vite), plain JavaScript, plain CSS — no TypeScript, no Tailwind, no UI libraries, no Axios
- **Database:** Azure SQL Edge (ARM64-native Docker image, SQL Server-compatible) via Entity Framework Core 10 (code-first + migrations)
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
- **Provider-owned, immutable post-create:**
  - `BillingDayOfMonth` (int, 1–21; supplied by `IProviderInfoClient` at create time)
  - `LastPaymentDayOfMonth` (int = `BillingDayOfMonth + 7`, so 8–28)
- **Customer-owned, editable:**
  - `PaymentDayOfMonth` (int, must satisfy `BillingDayOfMonth ≤ PaymentDayOfMonth ≤ LastPaymentDayOfMonth`)
  - `IsAutoPay` (bool, default false)
- `CreatedAt` (DateTime, UTC)

### Payment
- `Id` (int, PK)
- `SubscriptionId` (int, FK → Subscription)
- `Amount` (decimal(18,2), > 0)
- `PaymentDate` (DateTime, UTC)
- `Period` (string, length 7, matches `^\d{4}-(0[1-9]|1[0-2])$`, e.g. `"2026-05"`)
- `Status` (enum: Successful / Failed)
- `ExternalTransactionId` (string, nullable, max 100)

### User (authentication identity)
- `Id` (int, PK)
- `Email` (string, required, unique, max 200)
- `PasswordHash` (string, PBKDF2-SHA256 + 16-byte salt + 350 000 iterations, format `HEX-HASH-HEX-SALT`)
- `Role` (string, `"Admin"` or `"Customer"`, max 20)
- `CustomerId` (int?, **nullable** FK → Customer — null for admin accounts)
- `CreatedAt` (DateTime, UTC)

### Notification (audit log for the mock SMS/Email channel)
- `Id` (int, PK)
- `Channel` (string, max 20 — `"SMS"` or `"EMAIL"`)
- `Recipient` (string, max 200 — phone number or email)
- `Message` (string, required)
- `SentAt` (DateTime, indexed for newest-first queries)

### Relationships

- Customer 1 → N Subscription (cascade delete)
- Subscription 1 → N Payment (cascade delete)
- Customer 1 → 0..1 User (cascade delete; `User.CustomerId` is nullable so Admin exists without a customer row)
- `Notification` has **no** foreign keys — recipient is resolved back to a `Customer` at read time in `NotificationsController`, so deleting a customer preserves their notification history.
- **Hard delete cascade** is the chosen behavior because the spec explicitly allows Customer deletion. Configure `OnDelete(DeleteBehavior.Cascade)` on Subscription, Payment, and User. (Trade-off discussion in `/docs/architecture.md`.)

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
6. **All persisted timestamps are UTC.** "Business today" (used by reminders, dashboards, auto-pay, seed dates) goes through `Infrastructure/Utilities/BusinessClock.cs` which converts to Turkey local time (Europe/Istanbul, fallback fixed UTC+3). Storage stays UTC; calendar math uses Turkey-local "today" so the wall clock matches what the user sees.
7. **Payment day stays inside the provider window.** `PaymentDayOfMonth ∈ [BillingDayOfMonth, LastPaymentDayOfMonth]`. Enforced in `SubscriptionService.UpdateAsync` with `409 PAYMENT_DAY_OUT_OF_RANGE` — the response message includes the exact valid bounds.
8. **Customers act on their own data only.** Every `/api/users/me/*` endpoint resolves the current customer from the JWT `customerId` claim. `POST /api/payments` additionally verifies the target subscription's `CustomerId` matches before charging — cross-customer attempts return `403`.
9. **Reminders are not sent for auto-pay subscriptions.** `UserService.GetRemindersAsync` skips any `IsAutoPay = true` subscription — the bank is responsible for charging those, so a reminder would be noise.
10. **"Unpaid this month" gates on the billing day.** A subscription only appears in the dashboard's `UnpaidThisMonth` list once today's day-of-month has reached `BillingDayOfMonth` for the current period. Before the bill is issued there is nothing to owe.

---

## API Endpoints

Detailed endpoint catalog (request/response shapes + status codes) lives in `/docs/api-endpoints.md`. This section is the bird's-eye view.

### Auth (anonymous)

- `POST   /api/auth/login`     → JWT bearer login (`Admin` or `Customer`)
- `POST   /api/auth/register`  → Customer self-registration; reuses an existing `Customer` row if an admin pre-onboarded one with the same email

### Portal — Customer role only (`/api/users/me/*`)

- `GET    /api/users/me`                                          → Own profile
- `GET    /api/users/me/dashboard`                                → Active count / Unpaid this month / Recent payments (with provider+type) / Total paid this year
- `GET    /api/users/me/reminders`                                → Active, unpaid, non-auto-pay subs within 5 days of `LastPaymentDayOfMonth`. Fires fire-and-forget SMS + Email
- `GET    /api/users/me/subscriptions`                            → List of own subscriptions
- `GET    /api/users/me/subscriptions/{id}`                       → Single subscription (ownership-checked)
- `GET    /api/users/me/subscriptions/{id}/payments`              → Payment history for one subscription
- `POST   /api/users/me/subscriptions`                            → Create (server calls `IProviderInfoClient` to fetch billing days)
- `PUT    /api/users/me/subscriptions/{id}`                       → Update `Status` / `ProviderName` / `PaymentDayOfMonth` / `IsAutoPay`
- `DELETE /api/users/me/subscriptions/{id}`                       → Delete
- `POST   /api/users/me/subscriptions/process-auto-pay`           → Manual trigger for the auto-pay batch

### Payments

- `POST   /api/payments`                       → Customer-only; ownership-checked; the high-stakes transactional flow
- `GET    /api/payments?subscriptionId={id}`   → Admin-only monitoring
- `GET    /api/payments/{id}`                  → Admin-only monitoring

### Admin — monitoring & off-boarding (`Role = Admin`)

- `GET    /api/customers`                      → List (with `subscriptionCount`)
- `GET    /api/customers/{id}`                 → Single customer
- `GET    /api/customers/{id}/dashboard`       → Same shape as `/api/users/me/dashboard`
- `DELETE /api/customers/{id}`                 → Cascade delete (subscriptions, payments, user login)
- `GET    /api/subscriptions[?customerId=]`    → Read-only list / filter
- `GET    /api/subscriptions/{id}`             → Read-only detail
- `GET    /api/notifications[?channel=&take=]` → Notifications log with recipient → customer resolution

**Customer creation is NOT exposed to admins.** Customers self-register via `POST /api/auth/register`. Admins can off-board (DELETE) but not onboard.

**Subscription mutations are NOT exposed to admins.** All CRUD lives under the customer portal. `POST/PUT/DELETE /api/subscriptions` return `405 Method Not Allowed`.

### External (mock third-party REST services — same process, anonymous)

- `GET    /api/external/debt-inquiry/{subscriptionId}`                      → Deterministic mock debt amount; ranges by type (Electricity 100–400, Water 50–150, Internet 150–250, Gsm 80–200, NaturalGas 120–350). Also returns `lastPaymentDate` for the current period.
- `GET    /api/external/provider-info?providerName=…&subscriptionNumber=…`  → Deterministic `BillingDayOfMonth` (1–21) + `LastPaymentDayOfMonth` (= billing + 7). Called by the create-subscription flow.
- `POST   /api/external/payment-gateway`                                    → ~10% random failure. Success → `{ success: true, transactionId }`. Failure → 400 with `errorCode: "INSUFFICIENT_FUNDS" | "GATEWAY_TIMEOUT" | "DECLINED"`.
- `POST   /api/external/notifications`                                      → Logs via `ILogger` + persists into the `Notifications` table for the admin log viewer.

### Dashboard payload shape

`UnpaidThisMonth` only includes subs where today (Turkey local) ≥ `BillingDayOfMonth`. `RecentPayments` returns up to 10 rows joined with `Subscriptions` so each carries `ProviderName` + `SubscriptionType` + `ExternalTransactionId`. `TotalPaidThisYear` is the decimal sum of Successful payments in the current Turkey-local year.

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

The following are deliberately excluded. They live in `FUTURE_IMPROVEMENTS.md`.

- Background workers / scheduled jobs (auto-pay is manually triggered via `POST /api/users/me/subscriptions/process-auto-pay`; in a real deployment a `BackgroundService` would hit the same endpoint nightly)
- Idempotency-Key support on `POST /api/payments`
- Audit log tables beyond the `Notifications` channel log
- Soft delete (hard cascade everywhere)
- Internationalization (UI is English; SMS/Email copy is English)
- Multi-currency (every amount is TRY)
- Docker Compose / CI pipelines
- Integration tests against a real DB (xUnit + EF InMemory only — 6 tests on the critical rules)

## In Scope (added after the original bootstrap)

These were originally listed as future improvements but became part of the core scope when the design shifted to a mobile-bank-style portal:

- JWT-based authentication (`Admin` / `Customer` roles)
- Customer self-registration
- Per-customer ownership checks on every portal endpoint
- Notifications log persisted + browsable by admin
- SMS **and** Email mock channels (originally SMS only)
- Auto-pay flag + manual processor endpoint
- Provider-supplied billing day + last payment day
- Customer-supplied payment day with range validation

---

## Glossary

- **Period** — A string `YYYY-MM` identifying a billing month. UTC-based.
- **Successful payment** — A payment that completed at the gateway and was recorded with `Status = Successful` and a non-null `ExternalTransactionId`.
- **Failed payment** — A payment attempt where the gateway rejected; recorded with `Status = Failed` and null `ExternalTransactionId` so we keep the audit trail.
- **Layered defense** — The combination of FluentValidation + service-level checks + DB constraints that protects every critical rule.
