using SubscriptionApp.Domain.Enums;

namespace SubscriptionApp.Api.Dtos.Subscriptions;

public class SubscriptionResponse
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerFullName { get; set; } = string.Empty;
    public SubscriptionType SubscriptionType { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string SubscriptionNumber { get; set; } = string.Empty;
    public SubscriptionStatus Status { get; set; }
    public int BillingDayOfMonth { get; set; }
    public DateTime CreatedAt { get; set; }
}
