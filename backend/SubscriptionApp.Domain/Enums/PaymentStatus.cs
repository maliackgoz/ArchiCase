namespace SubscriptionApp.Domain.Enums;

public enum PaymentStatus
{
    // IMPORTANT: Successful must stay 0. The filtered unique index on Payment
    // uses [Status] = 0 to prevent duplicate successful payments per period.
    Successful = 0,
    Failed = 1
}
