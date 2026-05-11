using SubscriptionApp.Domain.Entities;
using SubscriptionApp.Domain.Enums;

namespace SubscriptionApp.Infrastructure.Services;

public interface ICustomerService
{
    Task<List<Customer>> GetAllAsync();
    Task<Customer> GetByIdAsync(int id);
    Task<Customer> CreateAsync(Customer customer);
    Task DeleteAsync(int id);
    Task<CustomerDashboardData> GetDashboardAsync(int customerId);
}

public class CustomerDashboardData
{
    public int ActiveSubscriptionCount { get; set; }
    public List<(int Id, string ProviderName, SubscriptionType SubscriptionType)> UnpaidThisMonth { get; set; } = [];
    public List<Payment> RecentPayments { get; set; } = [];
    public decimal TotalPaidThisYear { get; set; }
}
