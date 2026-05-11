namespace SubscriptionApp.Infrastructure.ExternalServices.Models;

public class PaymentGatewayResponse
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? ErrorCode { get; set; }
}
