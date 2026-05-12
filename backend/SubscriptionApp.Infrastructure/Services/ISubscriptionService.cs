using SubscriptionApp.Domain.Entities;
using SubscriptionApp.Domain.Enums;

namespace SubscriptionApp.Infrastructure.Services;

public interface ISubscriptionService
{
    Task<List<Subscription>> GetAllAsync(int? customerId);
    Task<Subscription> GetByIdAsync(int id);
    Task<Subscription> CreateAsync(Subscription subscription);
    Task<Subscription> UpdateAsync(int id, SubscriptionStatus status, string providerName, int paymentDayOfMonth, bool isAutoPay);
    Task DeleteAsync(int id);
}
