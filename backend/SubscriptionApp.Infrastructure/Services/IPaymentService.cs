using SubscriptionApp.Domain.Entities;

namespace SubscriptionApp.Infrastructure.Services;

public interface IPaymentService
{
    Task<List<Payment>> GetAllAsync(int? subscriptionId);
    Task<Payment> GetByIdAsync(int id);
    Task<Payment> CreateAsync(int subscriptionId, decimal amount, string period);
}
