using SubscriptionApp.Api.Dtos.Subscriptions;
using SubscriptionApp.Domain.Entities;

namespace SubscriptionApp.Api.Mapping;

public static class SubscriptionMappings
{
    public static Subscription ToEntity(this CreateSubscriptionRequest request) => new()
    {
        CustomerId = request.CustomerId,
        SubscriptionType = request.SubscriptionType,
        ProviderName = request.ProviderName,
        SubscriptionNumber = request.SubscriptionNumber,
        BillingDayOfMonth = request.BillingDayOfMonth
    };

    public static SubscriptionResponse ToResponse(this Subscription subscription) => new()
    {
        Id = subscription.Id,
        CustomerId = subscription.CustomerId,
        CustomerFullName = subscription.Customer?.FullName ?? string.Empty,
        SubscriptionType = subscription.SubscriptionType,
        ProviderName = subscription.ProviderName,
        SubscriptionNumber = subscription.SubscriptionNumber,
        Status = subscription.Status,
        BillingDayOfMonth = subscription.BillingDayOfMonth,
        CreatedAt = subscription.CreatedAt
    };
}
