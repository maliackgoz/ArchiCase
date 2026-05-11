namespace SubscriptionApp.Infrastructure.ExternalServices.Models;

public class PaymentGatewayRequest
{
    public int SubscriptionId { get; set; }
    public decimal Amount { get; set; }
}
