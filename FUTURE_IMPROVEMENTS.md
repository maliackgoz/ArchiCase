# Future Improvements (Optional — Add If Time Permits)

These are deliberately scoped OUT of the core deliverable. Add only after Phase 9 is complete and reviewed. Each item references where it would fit.

## Backend

### Authentication & Authorization
- JWT bearer auth on all endpoints except `/api/external/*` (still mocks)
- Customer can only see their own data
- Adds an `AuthController` and updates middleware

### Background job for reminders
- Hangfire or built-in `BackgroundService`
- Runs daily, checks for subscriptions with billing day approaching and no payment for current period
- Calls notification client
- Spec explicitly says this is optional; document why we skipped it

### Idempotency keys on payments
- `Idempotency-Key` header support
- Returns the original response if same key is replayed within 24h
- Prevents double-charge on client retry

### Audit log
- Append-only `AuditEvents` table
- Every payment attempt, every subscription state change
- Banking compliance pattern

### Soft delete
- Replace cascade hard-delete with `IsDeleted` flag + global query filter
- Document trade-off vs current behavior

## Frontend

### Better validation UX
- Real-time field validation with debouncing
- Show validation rules on focus

### i18n
- Turkish + English toggle
- `react-i18next`

### Optimistic updates on payment
- Show "Processing..." immediately
- Roll back UI on backend failure

## Testing

### Integration tests
- `WebApplicationFactory<Program>` based
- Hit real endpoints, real DB (Testcontainers with a throwaway SQL Server container)
- Test the full payment flow end-to-end

### Frontend tests
- Vitest + React Testing Library
- One test per critical user flow

## DevOps

### Docker Compose setup
- SQL Server container, backend container, frontend container
- One `docker-compose up`

### CI pipeline
- GitHub Actions: build, test, lint on every PR

## Domain

### Multiple currencies
- Subscription has a Currency field
- Payment amounts stored with currency
- Conversion service (mock)

### Partial payments
- Allow Amount < debt amount
- Track outstanding balance per period
- Requires data model change — non-trivial
