using SubscriptionApp.Domain.Entities;

namespace SubscriptionApp.Infrastructure.Services;

public interface ICustomerService
{
    Task<List<Customer>> GetAllAsync();
    Task<Customer> GetByIdAsync(int id);
    Task<Customer> CreateAsync(Customer customer);
    Task DeleteAsync(int id);
}
