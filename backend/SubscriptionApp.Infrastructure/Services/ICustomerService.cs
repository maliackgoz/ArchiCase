using SubscriptionApp.Domain.Entities;
using SubscriptionApp.Domain.Enums;

namespace SubscriptionApp.Infrastructure.Services;

public interface ICustomerService
{
    Task<List<Customer>> GetAllAsync();
    Task<Customer> GetByIdAsync(int id);
    Task DeleteAsync(int id);
    Task<CustomerDashboardData> GetDashboardAsync(int customerId);
}

public class CustomerDashboardData
{
    public int ActiveSubscriptionCount { get; set; }
    public List<(int Id, string ProviderName, SubscriptionType SubscriptionType)> UnpaidThisMonth { get; set; } = [];
    public List<RecentPaymentRow> RecentPayments { get; set; } = [];
    public decimal TotalPaidThisYear { get; set; }
}

public class RecentPaymentRow
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
