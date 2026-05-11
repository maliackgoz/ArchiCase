using SubscriptionApp.Domain.Enums;

namespace SubscriptionApp.Domain.Entities;

public class Subscription
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public SubscriptionType SubscriptionType { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string SubscriptionNumber { get; set; } = string.Empty;
    public SubscriptionStatus Status { get; set; }
    public int BillingDayOfMonth { get; set; }
    public DateTime CreatedAt { get; set; }

    // Non-virtual: lazy loading is disabled; use explicit .Include() in queries.
    public Customer Customer { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
