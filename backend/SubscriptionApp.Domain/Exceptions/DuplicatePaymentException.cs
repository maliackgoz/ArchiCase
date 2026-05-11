namespace SubscriptionApp.Domain.Exceptions;

public class DuplicatePaymentException : DomainException
{
    public int SubscriptionId { get; }
    public string Period { get; }

    public DuplicatePaymentException(int subscriptionId, string period)
        : base("DUPLICATE_PAYMENT",
               $"A successful payment already exists for subscription {subscriptionId} in period {period}.")
    {
        SubscriptionId = subscriptionId;
        Period = period;
    }
}
