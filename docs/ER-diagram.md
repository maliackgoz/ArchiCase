# Entity-Relationship Diagram

```mermaid
erDiagram
    Customer {
        int Id PK
        string FullName "nvarchar(100)"
        string Email "nvarchar(200), UNIQUE"
        string PhoneNumber "nvarchar(13)"
        datetime CreatedAt
    }

    Subscription {
        int Id PK
        int CustomerId FK
        int SubscriptionType "0=Electricity 1=Water 2=Internet 3=GSM 4=NaturalGas"
        string ProviderName "nvarchar(100)"
        string SubscriptionNumber "nvarchar(50)"
        int Status "0=Active 1=Passive"
        int BillingDayOfMonth "1–28"
        datetime CreatedAt
    }

    Payment {
        int Id PK
        int SubscriptionId FK
        decimal Amount "decimal(18,2)"
        datetime PaymentDate
        string Period "nvarchar(7), format YYYY-MM"
        int Status "0=Successful 1=Failed"
        string ExternalTransactionId "nvarchar(100), nullable"
    }

    Customer ||--o{ Subscription : "has"
    Subscription ||--o{ Payment : "has"
```

## Cardinality and design notes

**Customer → Subscription (one-to-many)**
A customer may have zero or many subscriptions. Subscriptions cannot exist without a customer — `CustomerId` is a non-nullable foreign key with `CASCADE DELETE`, so removing a customer automatically removes all their subscriptions.

**Subscription → Payment (one-to-many)**
A subscription accumulates one payment record per billing period. Failed attempts are also recorded (no filtered delete), creating a complete audit trail. `SubscriptionId` is non-nullable with `CASCADE DELETE`.

**Compound unique index on Payment**
`(SubscriptionId, Period) WHERE [Status] = 0` — the filtered index allows multiple Failed records for the same period (retries are permitted) while preventing two Successful payments for the same subscription and period. This is the ultimate database-level safety net; the service layer adds a pre-check for a friendlier error message.

**`PaymentStatus.Successful = 0` is load-bearing**
The filtered index is defined as `WHERE [Status] = 0`. This integer value must never change. It is documented with an inline comment in `PaymentStatus.cs`.

**No soft-delete**
Entities are hard-deleted. Cascade ensures referential integrity without orphan records.
