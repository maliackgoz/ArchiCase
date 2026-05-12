using SubscriptionApp.Infrastructure.ExternalServices.Models;

namespace SubscriptionApp.Infrastructure.ExternalServices;

public interface IProviderInfoClient
{
    Task<ProviderInfoResponse> GetProviderInfoAsync(string providerName, string subscriptionNumber);
}
