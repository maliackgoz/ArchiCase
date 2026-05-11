namespace SubscriptionApp.Infrastructure.ExternalServices.Models;

public class DebtInquiryResponse
{
    public int SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string Period { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
}
