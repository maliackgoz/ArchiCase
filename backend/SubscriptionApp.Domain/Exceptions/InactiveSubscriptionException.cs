namespace SubscriptionApp.Domain.Exceptions;

public class InactiveSubscriptionException : DomainException
{
    public int SubscriptionId { get; }

    public InactiveSubscriptionException(int subscriptionId)
        : base("INACTIVE_SUBSCRIPTION",
               $"Subscription {subscriptionId} is passive and cannot accept new payments.")
    {
        SubscriptionId = subscriptionId;
    }
}
