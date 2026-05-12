using SubscriptionApp.Domain.Enums;

namespace SubscriptionApp.Api.Dtos.Subscriptions;

public class CreateMySubscriptionRequest
{
    public SubscriptionType SubscriptionType { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string SubscriptionNumber { get; set; } = string.Empty;
}
