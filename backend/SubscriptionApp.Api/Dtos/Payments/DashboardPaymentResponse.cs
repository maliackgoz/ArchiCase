using SubscriptionApp.Domain.Enums;

namespace SubscriptionApp.Api.Dtos.Payments;

public class DashboardPaymentResponse
{
    public int Id { get; set; }
    public int SubscriptionId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public SubscriptionType SubscriptionType { get; set; }
    public decimal Amount { get; set; }
    public string Period { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? ExternalTransactionId { get; set; }
}
