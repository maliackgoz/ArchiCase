namespace SubscriptionApp.Api.Dtos.Users;

public class ReminderResponse
{
    public int SubscriptionId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public int SubscriptionType { get; set; }
    public int BillingDayOfMonth { get; set; }
    public int LastPaymentDayOfMonth { get; set; }
    public int PaymentDayOfMonth { get; set; }
    public bool IsAutoPay { get; set; }
    public int DaysUntilDue { get; set; }
    public string Period { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
}
