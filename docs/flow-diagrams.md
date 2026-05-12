# Flow Diagrams

## 1. Happy Path — Debt Inquiry → Payment → Notification

```mermaid
sequenceDiagram
    actor User as Customer
    participant FE as Frontend (portal)
    participant API as .NET API
    participant PS as PaymentService
    participant DB as SQL Server
    participant GW as PaymentGateway (mock)
    participant NT as Notifications (mock)

    User->>FE: Click "Query Debt"
    FE->>API: GET /api/external/debt-inquiry/4
    API-->>FE: 200 { amount: 132, period: "2026-05", lastPaymentDate: "2026-05-15" }
    FE-->>User: Debt card shown with deadline

    User->>FE: Click "Pay Now" → Confirm
    FE->>API: POST /api/payments  (JWT, Role=Customer)
    API->>API: Authorize: Role=Customer + ownership check
    API->>PS: CreateAsync(4, 132, "2026-05")
    PS->>DB: BEGIN TRANSACTION
    PS->>DB: Load Subscription (Include Customer)
    DB-->>PS: { Status: Active, Customer: {...} }
    PS->>DB: Any Successful payment for (4, "2026-05")?
    DB-->>PS: false
    PS->>GW: POST /api/external/payment-gateway
    GW-->>PS: 200 { success: true, transactionId: "TXN-abc" }
    PS->>DB: INSERT Payment (Status=Successful)
    PS->>DB: COMMIT
    PS-->>API: Payment entity
    API-->>FE: 201 { status: 0, externalTransactionId: "TXN-abc" }
    FE-->>User: Toast + Payment History refreshes

    Note over PS,NT: Fire-and-forget on both channels — does not block response
    PS-)NT: POST /notifications { channel: "SMS", recipient: phone, ... }
    PS-)NT: POST /notifications { channel: "EMAIL", recipient: email, ... }
    NT->>DB: INSERT Notification row (×2)
```

---

## 2. Duplicate Payment Rejection

```mermaid
sequenceDiagram
    actor User as Customer
    participant API as .NET API
    participant PS as PaymentService
    participant DB as SQL Server

    User->>API: POST /api/payments (same subscriptionId + period as before)
    API->>PS: CreateAsync(4, 132, "2026-05")
    PS->>DB: BEGIN TRANSACTION
    PS->>DB: Load Subscription → Active ✓
    PS->>DB: Any Successful payment for (4, "2026-05")?
    DB-->>PS: true
    PS->>DB: ROLLBACK
    PS-->>API: throw DuplicatePaymentException
    API-->>User: 409 { error: { code: "DUPLICATE_PAYMENT" } }
```

---

## 3. Passive Subscription Rejection

```mermaid
sequenceDiagram
    actor User as Customer
    participant API as .NET API
    participant SS as SubscriptionService
    participant PS as PaymentService

    User->>API: PUT /api/users/me/subscriptions/4 { status: 1 }
    API->>SS: UpdateAsync(4, Passive, ...)
    SS-->>API: 200 (status: Passive)

    User->>API: POST /api/payments { subscriptionId: 4, ... }
    API->>PS: CreateAsync(4, ...)
    PS->>PS: Load → Status: Passive
    PS-->>API: throw InactiveSubscriptionException
    API-->>User: 409 { error: { code: "INACTIVE_SUBSCRIPTION" } }
```

---

## 4. Payment Gateway Failure

```mermaid
sequenceDiagram
    actor User as Customer
    participant API as .NET API
    participant PS as PaymentService
    participant DB as SQL Server
    participant GW as PaymentGateway (mock)

    User->>API: POST /api/payments
    API->>PS: CreateAsync(4, 132, "2026-07")
    PS->>DB: BEGIN TRANSACTION
    PS->>DB: Load → Active ✓ ; no duplicate
    PS->>GW: POST /api/external/payment-gateway
    GW-->>PS: 400 { success: false, errorCode: "INSUFFICIENT_FUNDS" }
    PS->>DB: INSERT Payment (Status=Failed)
    PS->>DB: COMMIT  (audit record preserved)
    PS-->>API: throw ExternalServiceException("INSUFFICIENT_FUNDS")
    API-->>User: 502 { error: { code: "INSUFFICIENT_FUNDS" } }
    Note over DB: Failed payment row stays — visible in Payment History
```

---

## 5. Sign-up Linking to an Admin-Pre-Created Customer

```mermaid
sequenceDiagram
    actor Visitor
    participant FE as Landing Page
    participant AC as AuthController
    participant AS as AuthService
    participant DB as SQL Server

    Visitor->>FE: Fill sign-up form
    FE->>AC: POST /api/auth/register { email, password, fullName, phone }
    AC->>AS: RegisterAsync(...)
    AS->>DB: Any User with that email?
    DB-->>AS: false
    AS->>DB: BEGIN TRANSACTION
    AS->>DB: Customer with that email?
    alt Pre-existing Customer (admin-created)
        DB-->>AS: existing Customer
        AS->>AS: Reuse CustomerId, do not insert duplicate
    else No matching Customer
        AS->>DB: INSERT Customer
    end
    AS->>DB: INSERT User (Role="Customer", CustomerId=resolved)
    AS->>DB: COMMIT
    AS->>AC: User + Customer
    AC->>FE: 201 + JWT
    FE-->>Visitor: Logged in → /portal/dashboard
```

---

## 6. Reminder Fetch — SMS + Email Fan-Out

```mermaid
sequenceDiagram
    actor User as Customer
    participant FE as Reminders page
    participant US as UserService
    participant DB as SQL Server
    participant NT as Notifications (mock)

    User->>FE: Open /portal/reminders
    FE->>US: GET /api/users/me/reminders
    US->>DB: SELECT Active subs WHERE CustomerId = me
    loop For each subscription
        US->>US: Skip if IsAutoPay = true
        US->>US: daysUntilDue = lastPaymentDay - today (Turkey local)
        US->>US: Skip if daysUntilDue > 5
        US->>DB: Any Successful payment for current period?
        US->>US: Skip if alreadyPaid
        US->>US: Add to reminders list
        Note over US,NT: Fire-and-forget on both channels
        US-)NT: POST /notifications { channel: "SMS", recipient: phone }
        US-)NT: POST /notifications { channel: "EMAIL", recipient: email }
        NT->>DB: INSERT Notification row (×2 per due reminder)
    end
    US-->>FE: ReminderResponse[]
    FE-->>User: Table with badges + "Pay Now" buttons
```

---

## 7. Auto-Pay Batch — `process-auto-pay`

```mermaid
sequenceDiagram
    actor User as Customer
    participant FE as Subscriptions page
    participant API as .NET API
    participant US as UserService
    participant DI as DebtInquiry (mock)
    participant PS as PaymentService
    participant GW as PaymentGateway (mock)
    participant DB as SQL Server

    User->>FE: Click "Run auto-pay (N)"
    FE->>API: POST /api/users/me/subscriptions/process-auto-pay
    API->>US: ProcessAutoPayAsync(currentCustomerId)
    US->>DB: SELECT Active + IsAutoPay = true subs
    loop Each candidate
        alt today.Day < paymentDay
            US->>US: skip (not yet due)
        else Already Successful this period
            US->>US: skip (already paid)
        else
            US->>DI: GET /debt-inquiry/{id}
            DI-->>US: { amount, lastPaymentDate }
            US->>PS: CreateAsync(id, amount, period)
            PS->>GW: POST /payment-gateway
            alt Gateway success
                PS->>DB: INSERT Payment (Successful) + COMMIT
                US->>US: succeeded++
            else Gateway decline
                PS->>DB: INSERT Payment (Failed) + COMMIT
                PS-->>US: throw ExternalServiceException (caught)
                US->>US: failed++
            end
        end
    end
    US-->>API: { processed, succeeded, failed, skipped }
    API-->>FE: 200 { ... }
    FE-->>User: Toast "Auto-pay complete: X paid, Y failed"
```
