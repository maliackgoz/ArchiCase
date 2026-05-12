namespace SubscriptionApp.Infrastructure.ExternalServices.Models;

public class ProviderInfoResponse
{
    public string ProviderName { get; set; } = string.Empty;
    public string SubscriptionNumber { get; set; } = string.Empty;
    public int BillingDayOfMonth { get; set; }
    public int LastPaymentDayOfMonth { get; set; }
}
