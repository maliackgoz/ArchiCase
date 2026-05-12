using SubscriptionApp.Domain.Entities;

namespace SubscriptionApp.Infrastructure.Services;

public interface IAuthService
{
    Task<User?> ValidateAsync(string email, string password);
    Task<User> RegisterAsync(string email, string password, string fullName, string phoneNumber);
}
