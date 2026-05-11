using SubscriptionApp.Domain.Enums;

namespace SubscriptionApp.Api.Dtos.Subscriptions;

public class CreateSubscriptionRequest
{
    public int CustomerId { get; set; }
    public SubscriptionType SubscriptionType { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string SubscriptionNumber { get; set; } = string.Empty;
    public int BillingDayOfMonth { get; set; }
}
