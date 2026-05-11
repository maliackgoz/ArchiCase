using SubscriptionApp.Infrastructure.ExternalServices.Models;

namespace SubscriptionApp.Infrastructure.ExternalServices;

public interface IPaymentGatewayClient
{
    Task<PaymentGatewayResponse> ProcessPaymentAsync(int subscriptionId, decimal amount);
}
