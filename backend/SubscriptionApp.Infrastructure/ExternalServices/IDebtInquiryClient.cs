using SubscriptionApp.Infrastructure.ExternalServices.Models;

namespace SubscriptionApp.Infrastructure.ExternalServices;

public interface IDebtInquiryClient
{
    Task<DebtInquiryResponse> GetDebtAsync(int subscriptionId);
}
