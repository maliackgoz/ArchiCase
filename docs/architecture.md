# Architecture

## Layer overview

```
┌─────────────────────────────────────────────────────┐
│  SubscriptionApp.Api                                 │
│  Controllers · DTOs · Validators · Mapping           │
│  Middleware · Program.cs                             │
└────────────────────────┬────────────────────────────┘
                         │ depends on
┌────────────────────────▼────────────────────────────┐
│  SubscriptionApp.Infrastructure                      │
│  AppDbContext · EF Configurations · Services         │
│  ExternalService clients · DbInitializer             │
└────────────────────────┬────────────────────────────┘
                         │ depends on
┌────────────────────────▼────────────────────────────┐
│  SubscriptionApp.Domain                              │
│  Entities · Enums · Exceptions                       │
│  (zero NuGet dependencies)                           │
└─────────────────────────────────────────────────────┘
```

Dependency arrows point inward only. Domain knows nothing about Infrastructure or Api. Infrastructure knows nothing about Api. This means business rules are testable without starting a web server or hitting a database.

---

## Why 3 layers instead of full Clean Architecture

Full Clean Architecture adds an Application layer (use-cases / command handlers) and a separate Presentation layer, with interfaces abstracting every repository. For a 3-entity banking case study with well-understood requirements and no team of >2 developers, that overhead is premature abstraction.

The 3-layer version has the same isolation guarantees for the things that matter:
- Domain is framework-free and purely about business rules.
- Infrastructure owns all I/O (DB, HTTP).
- Api owns only HTTP concerns (routing, serialization, validation).

The concession is that services depend directly on `AppDbContext` rather than a repository interface. For this scope, that tradeoff is correct: one fewer layer of indirection, zero abstraction tax, and `AppDbContext` itself is already an abstraction over the database.

---

## Why Domain has zero external dependencies

`SubscriptionApp.Domain` has no NuGet packages. Entities are plain C# classes (POCOs). Exceptions inherit from `System.Exception`.

Consequences:
- The Domain project compiles and tests in milliseconds.
- Any .NET project can reference it without pulling in EF Core, ASP.NET, or anything else.
- Business rules (e.g. `PaymentStatus.Successful = 0` coupling to the DB index) can be documented and enforced without framework interference.

The moment a domain entity gains an EF Core attribute, it starts importing Microsoft.EntityFrameworkCore. The 3-layer split keeps that from happening.

---

## Why hand-written mapping instead of AutoMapper

Mapping in this project is a few static extension methods:
```csharp
public static CustomerResponse ToResponse(this Customer c) => new() { ... };
public static Customer ToEntity(this CreateCustomerRequest r) => new() { ... };
```

AutoMapper provides less:
- Compile-time checking requires explicit profile configuration anyway.
- Runtime `ProjectTo<T>` magic makes LINQ queries harder to reason about.
- Stack traces through AutoMapper are significantly harder to read.
- A new team member has to understand both the mapping config and the AutoMapper API.

Three lines of obvious C# beat one line of magic. This is especially true for a code review setting (interview context) where the reviewer needs to understand the code quickly.

---

## Why FluentValidation instead of DataAnnotations

DataAnnotations couple validation rules to the DTO class itself with attributes. This creates problems:
- Rules that span multiple fields require `IValidatableObject` — not composable.
- Custom error messages are awkward (attribute constructor strings).
- Unit-testing a validator requires instantiating the DTO and invoking the framework manually.

FluentValidation separates the validation rule from the object being validated:
```csharp
RuleFor(x => x.PhoneNumber)
    .Matches(@"^\+90[0-9]{10}$")
    .WithMessage("PhoneNumber must be +90XXXXXXXXXX");
```
The validator class is independently testable, the rules are readable prose, and adding a cross-field rule is a single additional `RuleFor` line.

---

## How transactions guarantee consistency in the payment flow

The `PaymentService.CreateAsync` method wraps steps 2–6 in an `IDbContextTransaction`:

```
BEGIN TRANSACTION
  1. Guard: amount > 0 (before transaction — no DB work needed)
  2. Load subscription + customer
  3. Reject if Passive → ROLLBACK
  4. Pre-check duplicate → ROLLBACK
  5. Call payment gateway (outside DB transaction — network I/O)
  6. INSERT payment (Successful or Failed)
  7. COMMIT
  8. Fire-and-forget notification (after commit — not in transaction)
```

**Why the gateway call is inside the transaction scope but outside a DB lock:**
The transaction is held open during the gateway call. This is a deliberate tradeoff: the transaction window is slightly longer, but it ensures the duplicate pre-check (step 4) and the payment insert (step 6) are atomic. Without this, a second concurrent request could pass the pre-check, both reach the gateway, both succeed, and then collide on the DB unique index. The unique index is the ultimate safety net; the transaction reduces the window.

**Why Failed payments are committed:**
Recording a failed attempt is important for audit. The `committed` boolean flag prevents the catch block from rolling back after commit — calling `RollbackAsync` on an already-committed transaction would throw.

**Why notifications are outside the transaction:**
A notification HTTP call can take seconds. Holding a DB transaction open across a network call to a third party would starve the connection pool. Notifications are fire-and-forget after commit — a failure there does not roll back the payment.

---

## Decimal everywhere, UTC at the storage layer

- `decimal` for all monetary amounts. `double`/`float` cannot represent `0.1` exactly in binary floating point; rounding errors compound across statements. `decimal(18,2)` in SQL Server maps directly to `decimal` in C# — no precision loss.
- All timestamps are persisted in UTC. `DateTime.Now` is never called from a service.

### Business "today" — `BusinessClock`

Reminders, dashboards and auto-pay all need a notion of "today's day-of-month" to fire correctly (e.g. `daysUntilDue = lastPaymentDay - today.Day`). Using `DateTime.UtcNow.Day` produced wrong results around midnight in Turkey (UTC+3): just past midnight local, UTC still showed the previous day so a `2d overdue` notice appeared when the customer's wall clock said it should be `3d`.

`Infrastructure/Utilities/BusinessClock.cs` is the single place that resolves "today":
```csharp
public static DateTime Now()
    => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ResolveTurkeyTimeZone());
public static DateTime Today() { /* same, truncated to date */ }
public static string CurrentPeriod() => Now().ToString("yyyy-MM");
```
- Tries the Windows id (`Turkey Standard Time`) and the IANA id (`Europe/Istanbul`); falls back to a fixed `UTC+3` zone with no DST if neither is installed.
- Used by `UserService.GetRemindersAsync`, `UserService.ProcessAutoPayAsync`, `CustomerService.GetDashboardAsync`, `DebtInquiryController` (to stamp `lastPaymentDate` for the current period), `NotificationController` (timestamps), and `DbInitializer` (so seed payment days line up with what the customer sees).
- Persisted `DateTime` values remain UTC for storage and indexing — the conversion is only at the "calendar math" step.

---

## Authentication & authorisation

Originally out-of-scope and listed in `FUTURE_IMPROVEMENTS.md`. Once the portal split was introduced (customer-driven onboarding + admin monitoring), JWT bearer auth became a hard requirement and moved into the core deliverable.

### Identity model

- `User` entity lives in Domain, joined to `Customer` via a **nullable** `CustomerId` so the bank-side `Admin` account exists without a customer row.
- `Role` is stored as a plain string (`"Admin"` or `"Customer"`). No separate `Roles` table — `[Authorize(Roles = "Admin")]` is enough for two roles and avoids a join-on-every-request.
- Passwords are hashed with **PBKDF2-SHA256, 16-byte salt, 32-byte hash, 350 000 iterations** via `Rfc2898DeriveBytes.Pbkdf2` (BCL only, no extra NuGet). Hash format: `HEX-HASH-HEX-SALT` — both segments are uppercase hex so `Split('-')` is unambiguous.
- Verification uses `CryptographicOperations.FixedTimeEquals` for constant-time comparison.

### JWT issuance

`JwtService.GenerateToken(User user)` builds three claims that the API relies on:

| Claim | Used by |
|---|---|
| `sub` | User id |
| `customerId` (omitted for admins) | Ownership checks in `UsersController.CurrentCustomerId` and `PaymentsController.Create` |
| `ClaimTypes.Role` | `[Authorize(Roles = "...")]` guards on controllers |

The HMAC signing key, issuer, audience and TTL live in `appsettings.json:Jwt`. Default TTL is 120 minutes.

### Consistent 401 shape

`JwtBearer` middleware's default response on a missing/invalid token is an empty body with a `WWW-Authenticate` header. The rest of the API returns the project's standard `{ error: { code, message } }` envelope, so a custom `OnChallenge` event handler in `Program.cs` rewrites the 401 to that same shape (`code: "UNAUTHORIZED"`). The frontend's `apiFetch` reads `body.error.message`, so this keeps every error path symmetric.

### Frontend session handling

- `AuthContext` stores `{ token, user }` in `localStorage` and re-hydrates on tab load.
- The `apiFetch` wrapper attaches `Authorization: Bearer <token>` to every request and, on any `401`, clears the session and redirects to `/login` (handles expired tokens without manual user effort).
- `ProtectedRoute` is role-aware: an admin navigating to `/portal/*` (or vice-versa) is redirected to their own landing page rather than getting a 403.

---

## Mock provider integration via typed HttpClients

Four mock external services live in `Api/Controllers/External/` and are reachable by the rest of the app through typed clients in `Infrastructure/ExternalServices/`. Each is registered with `AddHttpClient<TInterface, TImpl>` against the self-loopback `ExternalServices:BaseUrl` from config:

| Interface | Endpoint | Timeout | Used by |
|---|---|---|---|
| `IDebtInquiryClient` | `GET /api/external/debt-inquiry/{id}` | 5s | `UserService.ProcessAutoPayAsync`, debt query button |
| `IPaymentGatewayClient` | `POST /api/external/payment-gateway` | 10s | `PaymentService.CreateAsync` |
| `INotificationClient` | `POST /api/external/notifications` | (default) | `UserService.GetRemindersAsync`, payment success notifications |
| `IProviderInfoClient` | `GET /api/external/provider-info?...` | 5s | `UsersController.CreateMySubscription` |

The "real third-party" shape was preserved deliberately: no shared `DbContext`, errors bubble up as `ExternalServiceException` (mapped to 502 in middleware), and each call is logged with status + elapsed milliseconds.

---

## Subscription day model

Three independent day-of-month fields capture the "billing window" relationship.

| Field | Owner | Mutable post-create | Source |
|---|---|---|---|
| `BillingDayOfMonth` | Provider | No | `IProviderInfoClient` (deterministic, capped at 21) |
| `LastPaymentDayOfMonth` | Provider | No | `BillingDayOfMonth + 7` (returned by the same mock) |
| `PaymentDayOfMonth` | Customer | Yes — must satisfy `Billing ≤ Payment ≤ LastPayment` | `PUT /api/users/me/subscriptions/{id}` |

`SubscriptionService.CreateAsync` defaults `PaymentDayOfMonth` to `LastPaymentDayOfMonth` (latest acceptable). `UpdateAsync` validates the range with a `DomainException("PAYMENT_DAY_OUT_OF_RANGE", ...)` returning 409 with the exact valid window in the message.

The reminder threshold uses **`LastPaymentDayOfMonth`** (the real deadline), not the customer's chosen payment day, so a customer who hasn't paid yet still sees the warning even if they intended to pay on day 5 of a `[3, 10]` window.

---

## Notifications as audit data

Notifications were originally fire-and-forget log lines (just `ILogger.LogInformation`). When the admin needed to demo "where do reminders end up?" the mock `NotificationController` was extended to also persist each call to a `Notifications` table.

- No FK to `Customer` — the recipient (phone for SMS, email for EMAIL) is resolved to a `CustomerId` + `CustomerName` at **read time** in `NotificationsController`. Deleting a customer therefore preserves their notification history (good for audit) without orphan rows.
- Indexed on `SentAt DESC` so the admin Notifications page can paginate newest-first quickly.
- Reminder fetch fans out on both `SMS` and `EMAIL` channels via two independent `Task.Run` calls; either can fail silently without breaking the other or the reminder query itself.
