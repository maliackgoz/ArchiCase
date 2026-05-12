# API Endpoint Catalog

Base URL (development): `http://localhost:5072`

## Conventions

All error responses follow the shape:
```json
{ "error": { "code": "ERROR_CODE", "message": "Human-readable message." } }
```
Validation errors (400) additionally include a `details` object:
```json
{ "error": { "code": "VALIDATION_ERROR", "message": "...", "details": { "FieldName": ["error msg"] } } }
```

| Situation | HTTP code |
|---|---|
| Resource created | 201 + `Location` header |
| Resource deleted / accepted | 204 |
| Validation error | 400 |
| Authentication required | 401 |
| Authenticated but wrong role / ownership | 403 |
| Resource not found | 404 |
| Method not allowed (endpoint removed) | 405 |
| Business rule violation | 409 |
| External service failure | 502 |
| Unhandled / internal | 500 |

## Authentication & roles

Most endpoints require a JWT `Authorization: Bearer <token>` header issued by `POST /api/auth/login` or `/register`. The token carries three claims that matter to the API:

| Claim | Used by |
|---|---|
| `sub` | User id |
| `customerId` (omitted for admins) | Ownership checks on `/api/users/me/*` and `/api/payments` POST |
| `role` (`Admin` or `Customer`) | `[Authorize(Roles = "...")]` guards |

Role matrix (✓ = allowed, ✗ = 403/405):

| Endpoint group | Anonymous | Customer | Admin |
|---|:---:|:---:|:---:|
| `POST /api/auth/login` & `register` | ✓ | ✓ | ✓ |
| `GET /api/users/me/*` (portal) | ✗ | ✓ (own data only) | ✗ |
| `POST/PUT/DELETE /api/users/me/subscriptions/*` | ✗ | ✓ (own data only) | ✗ |
| `POST /api/payments` | ✗ | ✓ (own subscription only) | ✗ |
| `GET /api/payments[?subscriptionId=]` | ✗ | ✗ | ✓ |
| `GET /api/subscriptions` (read-only monitoring) | ✗ | ✗ | ✓ |
| `GET /api/customers*` | ✗ | ✗ | ✓ |
| `DELETE /api/customers/{id}` | ✗ | ✗ | ✓ |
| `GET /api/notifications` | ✗ | ✗ | ✓ |
| `GET /api/external/*` (mock providers) | ✓ | ✓ | ✓ |

---

## Auth

### POST /api/auth/login
**Request**
```json
{ "email": "ahmet.yilmaz@example.com", "password": "Test1234!" }
```
**Response 200**
```json
{
  "token": "eyJhbGciOiJI...",
  "user": { "id": 1, "email": "...", "customerId": 1, "fullName": "Ahmet Yılmaz", "role": "Customer" }
}
```
- Token TTL: `Jwt:ExpiryMinutes` in `appsettings.json` (default 120 minutes).
- Admin login: `customerId` field is `null`, `role` is `"Admin"`, `fullName` is `"Bank Admin"`.

| Status | Code | Condition |
|---|---|---|
| 200 | — | Login OK |
| 401 | `INVALID_CREDENTIALS` | Email not found or password mismatch |

### POST /api/auth/register
Customer self-registration. Creates `Customer + User` atomically; if a `Customer` already exists with that email (admin pre-onboarded it), the new `User` links to that existing customer instead of duplicating it.

**Request**
```json
{ "email": "...", "password": "...", "fullName": "...", "phoneNumber": "+905551234567" }
```
**Validation**
- `password`: ≥ 8 chars
- `phoneNumber`: matches `^\+90[0-9]{10}$`
- `email`: required, max 200 chars

| Status | Code | Condition |
|---|---|---|
| 201 | — | Created, returns same shape as login |
| 400 | `VALIDATION_ERROR` | Field rules |
| 409 | `DUPLICATE_EMAIL` | A `User` already exists with that email |

---

## Portal (customer)  — `/api/users/me`

All endpoints require `Role = Customer`. The current customer is resolved from the JWT's `customerId` claim.

### GET /api/users/me
Returns the customer's own profile.

### GET /api/users/me/subscriptions
List of subscriptions for the current customer.

### GET /api/users/me/subscriptions/{id}
Single subscription with ownership check. `403` if not owned.

### GET /api/users/me/subscriptions/{id}/payments
Payment history for one of the customer's subscriptions, newest first.

| Status | Condition |
|---|---|
| 200 | List returned |
| 403 | Not your subscription |
| 404 | Subscription not found |

### POST /api/users/me/subscriptions
Create a subscription for the current customer. Billing days are **resolved server-side** by calling `IProviderInfoClient` — the request body only carries the customer-driven fields.

**Request**
```json
{ "subscriptionType": 2, "providerName": "Türk Telekom", "subscriptionNumber": "TT-NET-101" }
```
The service stamps `BillingDayOfMonth` + `LastPaymentDayOfMonth` from the provider and defaults `PaymentDayOfMonth = LastPaymentDayOfMonth`, `IsAutoPay = false`.

| Status | Code | Condition |
|---|---|---|
| 201 | — | Created |
| 400 | `VALIDATION_ERROR` | Field rules |
| 409 | `DUPLICATE_SUBSCRIPTION` | `(providerName, subscriptionNumber)` already exists |
| 502 | `PROVIDER_INFO_FAILED` | Provider lookup blew up |

### PUT /api/users/me/subscriptions/{id}
Update the customer-owned fields. `BillingDayOfMonth` and `LastPaymentDayOfMonth` are immutable post-create.

**Request**
```json
{ "status": 0, "providerName": "...", "paymentDayOfMonth": 12, "isAutoPay": true }
```

| Status | Code | Condition |
|---|---|---|
| 200 | — | Updated |
| 403 | — | Not your subscription |
| 409 | `PAYMENT_DAY_OUT_OF_RANGE` | `paymentDayOfMonth ∉ [billing, lastPayment]` |
| 409 | `DUPLICATE_SUBSCRIPTION` | Renamed provider collides with existing number |

### DELETE /api/users/me/subscriptions/{id}
| Status | Condition |
|---|---|
| 204 | Deleted (cascades to payments) |
| 403 | Not your subscription |

### POST /api/users/me/subscriptions/process-auto-pay
Customer-triggered auto-pay batch (the spec doesn't require a background worker; this endpoint is what a scheduler would call). Walks every Active + `IsAutoPay` subscription for the current customer; for each, if today is on/after `PaymentDayOfMonth` and the current period is unpaid, pulls the debt amount and attempts a gateway charge.

**Response 200**
```json
{ "processed": 1, "succeeded": 1, "failed": 0, "skipped": 0 }
```

### GET /api/users/me/dashboard
**Response 200**
```json
{
  "activeSubscriptionCount": 3,
  "unpaidThisMonth": [ { "id": 5, "providerName": "BEDAŞ", "subscriptionType": 0 } ],
  "recentPayments": [
    { "id": 12, "subscriptionId": 5, "providerName": "BEDAŞ", "subscriptionType": 0,
      "amount": 215.00, "period": "2026-04", "status": 0,
      "paymentDate": "2026-04-09T14:00:00", "externalTransactionId": "TXN-..." }
  ],
  "totalPaidThisYear": 1878.75
}
```
- `unpaidThisMonth` only includes subscriptions where today's day-of-month ≥ `BillingDayOfMonth` (i.e. the bill has actually been issued for the current period).
- `recentPayments` is up to 10 rows joined with `Subscriptions` so each row carries `providerName` + `subscriptionType`.
- "Today" and "current year" use Turkey-local time via `BusinessClock`.

### GET /api/users/me/reminders
Returns active, unpaid subscriptions whose `LastPaymentDayOfMonth - today ≤ 5` days, **excluding auto-pay subscriptions** (the bank will handle those). Reading this endpoint also fires fire-and-forget SMS + Email notifications via `INotificationClient` to the customer's phone and email.

**Response item**
```json
{ "subscriptionId": 4, "providerName": "İSKİ", "subscriptionType": 1,
  "billingDayOfMonth": 8, "lastPaymentDayOfMonth": 15, "paymentDayOfMonth": 14,
  "isAutoPay": false, "daysUntilDue": 3, "period": "2026-05", "isOverdue": false }
```

---

## Payments

### POST /api/payments
`Role = Customer`. Ownership check: the subscription's `CustomerId` must match the JWT `customerId`, otherwise `403`.

**Request**
```json
{ "subscriptionId": 4, "amount": 142.50, "period": "2026-05" }
```

**Flow:** load subscription → reject Passive → reject duplicate Successful → call gateway → on success record Successful + commit + fire SMS/Email → on gateway failure record Failed + commit + throw → middleware → 502.

| Status | Code | Condition |
|---|---|---|
| 201 | — | Payment Successful, `externalTransactionId` set |
| 400 | `VALIDATION_ERROR` | Field rules |
| 403 | — | Subscription not owned by current customer |
| 404 | `NOT_FOUND` | Subscription does not exist |
| 409 | `INACTIVE_SUBSCRIPTION` | Subscription is Passive |
| 409 | `DUPLICATE_PAYMENT` | Successful payment already exists for `(subId, period)` |
| 502 | `INSUFFICIENT_FUNDS` / `GATEWAY_TIMEOUT` / `DECLINED` | Gateway declined; Failed record still committed |

### GET /api/payments (admin)
Returns all payments, optionally filtered by `?subscriptionId=`. Ordered by `PaymentDate DESC`.

### GET /api/payments/{id} (admin)
Single payment.

---

## Subscriptions (admin — read-only monitoring)

`Role = Admin`. Customer mutations live under `/api/users/me/*`.

### GET /api/subscriptions
List with optional `?customerId=` filter.

### GET /api/subscriptions/{id}
Single subscription. Response includes both provider days + customer days:
```json
{ "id": 4, "customerId": 2, "customerFullName": "Fatma Kaya",
  "subscriptionType": 1, "providerName": "İSKİ", "subscriptionNumber": "ISKI-FK-2001",
  "status": 0, "billingDayOfMonth": 8, "lastPaymentDayOfMonth": 15,
  "paymentDayOfMonth": 14, "isAutoPay": false,
  "createdAt": "2025-11-12T05:46:39Z" }
```

**Mutation endpoints removed.** `POST/PUT/DELETE /api/subscriptions` were retired when subscription CRUD moved to the customer portal. Calls now return `405 Method Not Allowed`.

---

## Customers (admin — monitor + off-board)

`Role = Admin`.

### GET /api/customers
List with `subscriptionCount`.

### GET /api/customers/{id}
Single customer.

### GET /api/customers/{id}/dashboard
Same shape as `/api/users/me/dashboard` but for any customer — used by the admin's per-customer drill-down page.

### DELETE /api/customers/{id}
Cascade-deletes the customer, their subscriptions, payments, and `User` login.

**Customer creation is intentionally not exposed here.** Customers self-register via `POST /api/auth/register`. The admin form was removed; admins can still off-board but not onboard.

---

## Notifications (admin — log viewer)

### GET /api/notifications
`Role = Admin`. Returns every persisted notification (SMS + Email) newest-first. The recipient is resolved back to a `Customer` by matching phone (`SMS`) or email (`EMAIL`) at read time, so each row carries an optional `customerId` + `customerName` for the admin search UI.

**Query params**
| Param | Default | Notes |
|---|---|---|
| `channel` | (all) | `"SMS"` or `"EMAIL"` |
| `take` | 100 | Clamped to `[1, 500]` |

**Response item**
```json
{ "id": 12, "channel": "SMS", "recipient": "+905559876543",
  "message": "Reminder: Your İSKİ bill for 2026-05 is due in 3 days. ...",
  "sentAt": "2026-05-12T08:46:51",
  "customerId": 2, "customerName": "Fatma Kaya" }
```

`customerId` / `customerName` are `null` when no customer matches (e.g., recipient deleted).

---

## External Services (Mock)

These endpoints simulate third-party REST APIs. They live in the same process and are reachable via self-loopback HttpClients in the Infrastructure layer. **Four** mock services, all anonymous.

### GET /api/external/debt-inquiry/{subscriptionId}
Returns the current debt amount + the provider's deadline for the current period.

Amount ranges (seeded by `subscriptionId` for determinism):
- Electricity 100–400 · Water 50–150 · Internet 150–250 · GSM 80–200 · Natural Gas 120–350

**Response 200**
```json
{ "subscriptionId": 4, "amount": 132, "period": "2026-05", "currency": "TRY",
  "lastPaymentDate": "2026-05-15T00:00:00" }
```

### GET /api/external/provider-info?providerName=…&subscriptionNumber=…
Returns the provider's chosen billing day (deterministic from `providerName + subscriptionNumber`, capped at 21) and `lastPaymentDayOfMonth = billing + 7`. Called server-side when a customer adds a subscription so billing days are owned by the provider, not the customer.

**Response 200**
```json
{ "providerName": "Türk Telekom", "subscriptionNumber": "TT-NET-101",
  "billingDayOfMonth": 6, "lastPaymentDayOfMonth": 13 }
```

| Status | Code | Condition |
|---|---|---|
| 200 | — | OK |
| 400 | `MISSING_PARAMETERS` | Empty `providerName` or `subscriptionNumber` |

### POST /api/external/payment-gateway
Simulates a payment gateway with ~10% random failure rate.

**Request:** `{ "subscriptionId": 4, "amount": 132.00 }`
- 200: `{ "success": true, "transactionId": "TXN-...", "errorCode": null }`
- 400: `{ "success": false, "transactionId": null, "errorCode": "INSUFFICIENT_FUNDS" }` (or `GATEWAY_TIMEOUT` / `DECLINED`)

### POST /api/external/notifications
Logs **and persists** an SMS/Email payload, then returns 200.
```json
{ "channel": "EMAIL", "recipient": "fatma.kaya@example.com", "message": "Hi Fatma Kaya, ..." }
```
Stored in the `Notifications` table with `SentAt = BusinessClock.Now()` and exposed via the admin `GET /api/notifications` endpoint.
