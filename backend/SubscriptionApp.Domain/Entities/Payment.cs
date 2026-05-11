using SubscriptionApp.Domain.Enums;

namespace SubscriptionApp.Domain.Entities;

public class Payment
{
    public int Id { get; set; }
    public int SubscriptionId { get; set; }
    // decimal, not double/float — money must never lose precision to floating-point rounding.
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Period { get; set; } = string.Empty;   // Format: YYYY-MM
    public PaymentStatus Status { get; set; }
    public string? ExternalTransactionId { get; set; }

    // Non-virtual: lazy loading is disabled; use explicit .Include() in queries.
    public Subscription Subscription { get; set; } = null!;
}
