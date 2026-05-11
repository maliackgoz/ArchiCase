# API Endpoint Catalog

Base URL (development): `http://localhost:5072`

All error responses follow the shape:
```json
{ "error": { "code": "ERROR_CODE", "message": "Human-readable message." } }
```
Validation errors (400) additionally include a `details` object:
```json
{ "error": { "code": "VALIDATION_ERROR", "message": "...", "details": { "FieldName": ["error msg"] } } }
```

---

## Customers

### GET /api/customers
Returns all customers with their subscription count.

**Response 200**
```json
[
  { "id": 1, "fullName": "Ahmet Yılmaz", "email": "ahmet.yilmaz@example.com",
    "phoneNumber": "+905551234567", "createdAt": "2026-05-11T12:00:00Z", "subscriptionCount": 2 }
]
```

---

### GET /api/customers/{id}
Returns a single customer.

| Status | Condition |
|--------|-----------|
| 200 | Found |
| 404 | `NOT_FOUND` — customer does not exist |

---

### POST /api/customers
Creates a new customer.

**Request body**
```json
{ "fullName": "Ali Veli", "email": "ali@example.com", "phoneNumber": "+905559998877" }
```

**Validation rules**
- `fullName`: required, max 100 chars
- `email`: required, valid email format, max 200 chars
- `phoneNumber`: required, must match `^\+90[0-9]{10}$`

| Status | Condition |
|--------|-----------|
| 201 | Created — Location header set to `/api/customers/{id}` |
| 400 | `VALIDATION_ERROR` — field validation failed |
| 409 | `DUPLICATE_EMAIL` — email already exists |

---

### DELETE /api/customers/{id}
Deletes a customer and all their subscriptions and payments (cascade).

| Status | Condition |
|--------|-----------|
| 204 | Deleted |
| 404 | `NOT_FOUND` |

---

### GET /api/customers/{id}/dashboard
Returns aggregated summary data for a customer.

**Response 200**
```json
{
  "activeSubscriptionCount": 2,
  "unpaidThisMonth": [
    { "id": 3, "providerName": "İSKİ", "subscriptionType": 1 }
  ],
  "recentPayments": [
    { "id": 5, "subscriptionId": 2, "amount": 150.00, "period": "2026-05",
      "status": 0, "paymentDate": "2026-05-11T15:40:32Z", "externalTransactionId": "TXN-..." }
  ],
  "totalPaidThisYear": 230.00
}
```

- `unpaidThisMonth`: Active subscriptions with no Successful payment for the current UTC month (`yyyy-MM`)
- `recentPayments`: Last 10 payments across all customer subscriptions, ordered by `PaymentDate DESC`
- `totalPaidThisYear`: Sum of Successful payments within the current UTC calendar year

| Status | Condition |
|--------|-----------|
| 200 | Found |
| 404 | `NOT_FOUND` |

---

## Subscriptions

### GET /api/subscriptions
Returns all subscriptions. Optionally filter by customer.

**Query params:** `customerId` (optional, int)

**Response 200**
```json
[
  { "id": 2, "customerId": 1, "customerFullName": "Ahmet Yılmaz",
    "subscriptionType": 2, "providerName": "Türk Telekom", "subscriptionNumber": "TT-NET-002",
    "status": 0, "billingDayOfMonth": 15, "createdAt": "2026-05-11T12:00:00Z" }
]
```

---

### GET /api/subscriptions/{id}

| Status | Condition |
|--------|-----------|
| 200 | Found |
| 404 | `NOT_FOUND` |

---

### POST /api/subscriptions
Creates a new subscription.

**Request body**
```json
{ "customerId": 1, "subscriptionType": 2, "providerName": "Türk Telekom",
  "subscriptionNumber": "TT-NET-002", "billingDayOfMonth": 15 }
```

**Validation rules**
- `customerId`: > 0
- `subscriptionType`: valid enum value (0–4)
- `providerName`: required, max 100 chars
- `subscriptionNumber`: required, max 50 chars
- `billingDayOfMonth`: 1–28

**`status` is always forced to `Active` on creation regardless of input.**

| Status | Condition |
|--------|-----------|
| 201 | Created |
| 400 | `VALIDATION_ERROR` |
| 404 | `NOT_FOUND` — `customerId` does not exist |
| 409 | `DUPLICATE_SUBSCRIPTION` — `(providerName, subscriptionNumber)` already exists |

---

### PUT /api/subscriptions/{id}
Updates a subscription. Only `status`, `providerName`, and `billingDayOfMonth` may be changed. `customerId` and `subscriptionNumber` are immutable.

**Request body**
```json
{ "status": 1, "providerName": "Türk Telekom", "billingDayOfMonth": 20 }
```

| Status | Condition |
|--------|-----------|
| 200 | Updated — returns full subscription response |
| 400 | `VALIDATION_ERROR` |
| 404 | `NOT_FOUND` |
| 409 | `DUPLICATE_SUBSCRIPTION` — new providerName + existing subscriptionNumber already taken |

---

### DELETE /api/subscriptions/{id}
Deletes a subscription and all its payment records (cascade).

| Status | Condition |
|--------|-----------|
| 204 | Deleted |
| 404 | `NOT_FOUND` |

---

## Payments

### GET /api/payments
Returns payments ordered by `PaymentDate DESC`. Optionally filter by subscription.

**Query params:** `subscriptionId` (optional, int)

**Response 200**
```json
[
  { "id": 1, "subscriptionId": 2, "amount": 150.00, "period": "2026-05",
    "status": 0, "paymentDate": "2026-05-11T15:40:32Z", "externalTransactionId": "TXN-abc123" }
]
```

---

### GET /api/payments/{id}

| Status | Condition |
|--------|-----------|
| 200 | Found |
| 404 | `NOT_FOUND` |

---

### POST /api/payments
Processes a payment. Calls the payment gateway and records the result.

**Request body**
```json
{ "subscriptionId": 2, "amount": 150.00, "period": "2026-05" }
```

**Validation rules**
- `subscriptionId`: > 0
- `amount`: > 0 (decimal — never float/double)
- `period`: matches `^\d{4}-(0[1-9]|1[0-2])$` (e.g. `2026-05`)

**Flow:** load subscription → check Active → check no duplicate Successful → call gateway → record payment → commit → (fire-and-forget SMS notification on success)

| Status | Condition |
|--------|-----------|
| 201 | Payment successful — `status: 0`, `externalTransactionId` set |
| 400 | `VALIDATION_ERROR` |
| 404 | `NOT_FOUND` — subscription does not exist |
| 409 | `INACTIVE_SUBSCRIPTION` — subscription is Passive |
| 409 | `DUPLICATE_PAYMENT` — Successful payment already exists for this (subscriptionId, period) |
| 502 | `INSUFFICIENT_FUNDS` / `GATEWAY_TIMEOUT` / `DECLINED` — gateway rejected; failed payment record committed |

---

## External Services (Mock)

These endpoints simulate third-party services. They exist in the same process for testing purposes.

### GET /api/external/debt-inquiry/{subscriptionId}
Returns the current debt amount for a subscription.

Amount ranges (seeded by `subscriptionId` for determinism):
- Electricity: 100–400 TRY
- Water: 50–150 TRY
- Internet: 150–250 TRY
- GSM: 80–200 TRY
- Natural Gas: 120–350 TRY

**Response 200**
```json
{ "subscriptionId": 2, "amount": 187, "period": "2026-05", "currency": "TRY" }
```

| Status | Condition |
|--------|-----------|
| 200 | Debt info returned |
| 404 | Subscription not found |

---

### POST /api/external/payment-gateway
Simulates a payment gateway with ~10% random failure rate.

**Request body**
```json
{ "subscriptionId": 2, "amount": 187.00 }
```

**Response 200 (success)**
```json
{ "success": true, "transactionId": "TXN-a1b2c3d4-...", "errorCode": null }
```

**Response 400 (decline)**
```json
{ "success": false, "transactionId": null, "errorCode": "INSUFFICIENT_FUNDS" }
```
Error codes: `INSUFFICIENT_FUNDS`, `GATEWAY_TIMEOUT`, `DECLINED`

---

### POST /api/external/notifications
Logs a notification. Always succeeds.

**Request body**
```json
{ "channel": "SMS", "recipient": "+905551234567", "message": "Payment accepted." }
```

**Response 200** — empty body
