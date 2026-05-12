using SubscriptionApp.Domain.Enums;

namespace SubscriptionApp.Infrastructure.Services;

public interface IUserService
{
    Task<List<ReminderItem>> GetRemindersAsync(int customerId);
    Task<AutoPayResult> ProcessAutoPayAsync(int customerId);
}

public class AutoPayResult
{
    public int Processed { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
}

public class ReminderItem
{
    public int SubscriptionId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public SubscriptionType SubscriptionType { get; set; }
    public int BillingDayOfMonth { get; set; }
    public int LastPaymentDayOfMonth { get; set; }
    public int PaymentDayOfMonth { get; set; }
    public bool IsAutoPay { get; set; }
    public int DaysUntilDue { get; set; }
    public string Period { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
}
