# Future Improvements (Optional — Add If Time Permits)

These were originally scoped out of the bootstrap. Items that have **since been implemented** during Phase 10 (the post-bootstrap iteration) are kept here struck through so the original scope decisions remain visible — search for them in `PHASE_LOG.md` Phase 10 for full context.

## Backend

### ~~Authentication & Authorization~~ ✅ implemented (Phase 10)
- JWT bearer auth, customer/admin roles, ownership checks on every portal endpoint
- `AuthController`, `JwtService`, `Microsoft.AspNetCore.Authentication.JwtBearer` + PBKDF2-SHA256 password hashing
- Customers self-register at `POST /api/auth/register`

### Background job for reminders & auto-pay
A `BackgroundService` (or Hangfire) that calls `GET /api/users/me/reminders` and `POST /api/users/me/subscriptions/process-auto-pay` on a nightly schedule for every active customer. The endpoints themselves already exist — this just removes the manual trigger. ~30 lines.

### Idempotency keys on payments
- `Idempotency-Key` header support on `POST /api/payments`
- Returns the original response if the same key is replayed within 24h
- Protects against double-charge when a network retry succeeds after the first request timed out
- Particularly relevant once the background auto-pay job exists

### Audit log beyond notifications
The `Notifications` table is one audit channel. A general append-only `AuditEvents` table covering subscription state changes, customer deletes, and admin actions would round out the banking-compliance story.

### Soft delete
Replace cascade hard-delete with `IsDeleted` flag + EF Core global query filter. Trade-off vs current behavior documented in `/docs/architecture.md`.

### Real provider integration
Swap the four mock controllers in `Api/Controllers/External/` for actual third-party clients (e.g. real SMS gateway, real bank payment provider). The typed `HttpClient` shape is already production-ready — only the base URLs + auth headers would change.

### Retry auto-pay on transient gateway failures
Currently `ProcessAutoPayAsync` counts a gateway decline as `failed` and moves on. A Polly-based retry policy on `IPaymentGatewayClient` (e.g. 3 attempts with exponential backoff for `GATEWAY_TIMEOUT` only) would handle transient failures gracefully.

## Frontend

### Better validation UX
- Real-time field validation with debouncing
- Show validation rules on focus

### i18n
- Turkish + English toggle via `react-i18next`
- Currently every UI string + email/SMS body is English

### Optimistic updates on payment
- Show "Processing…" immediately
- Roll back the UI on backend failure

### Pagination
- Long lists (admin Analysis, Notifications, large customer histories) load everything client-side today. Switch to server-side `take` + cursor or offset pagination once the dataset grows.

## Testing

### Integration tests
- `WebApplicationFactory<Program>` based
- Hit real endpoints, real DB (Testcontainers with a throwaway SQL Server container)
- Test the full payment flow end-to-end, including JWT issuance + ownership rejection paths

### Frontend tests
- Vitest + React Testing Library
- One test per critical user flow (sign in, add subscription, pay, view reminders)

## DevOps

### Docker Compose
- SQL Server container, backend container, frontend container
- One `docker-compose up`

### CI pipeline
- GitHub Actions: build, test, lint on every PR
- Apply migrations against a Testcontainer DB to verify they roll forward cleanly

## Domain

### Multiple currencies
- `Subscription` gains a `Currency` field
- Payment amounts stored with currency
- Mock conversion service

### Partial payments
- Allow `Amount < debt`
- Track outstanding balance per `(Subscription, Period)`
- Non-trivial — touches the unique-filtered-index invariant
