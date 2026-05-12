using SubscriptionApp.Domain.Enums;

namespace SubscriptionApp.Api.Dtos.Subscriptions;

public class UpdateSubscriptionRequest
{
    public SubscriptionStatus Status { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public int PaymentDayOfMonth { get; set; }
    public bool IsAutoPay { get; set; }
}
