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

## Decimal everywhere, UTC everywhere

- `decimal` for all monetary amounts. `double`/`float` cannot represent `0.1` exactly in binary floating point; rounding errors compound across statements. `decimal(18,2)` in SQL Server maps directly to `decimal` in C# — no precision loss.
- `DateTime.UtcNow` for all timestamps. `DateTime.Now` uses the server's local timezone, which varies by deployment environment. UTC is stable, unambiguous, and correct for distributed systems.
