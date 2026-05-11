using SubscriptionApp.Api.Dtos.Payments;
using SubscriptionApp.Domain.Enums;

namespace SubscriptionApp.Api.Dtos.Customers;

public class CustomerDashboardResponse
{
    public int ActiveSubscriptionCount { get; set; }
    public List<UnpaidSubscriptionSummary> UnpaidThisMonth { get; set; } = [];
    public List<PaymentResponse> RecentPayments { get; set; } = [];
    public decimal TotalPaidThisYear { get; set; }
}

public class UnpaidSubscriptionSummary
{
    public int Id { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public SubscriptionType SubscriptionType { get; set; }
}
