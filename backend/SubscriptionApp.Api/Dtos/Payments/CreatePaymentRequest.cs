namespace SubscriptionApp.Api.Dtos.Payments;

public class CreatePaymentRequest
{
    public int SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string Period { get; set; } = string.Empty;
}
