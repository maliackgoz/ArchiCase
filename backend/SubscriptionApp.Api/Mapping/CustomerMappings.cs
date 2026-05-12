using SubscriptionApp.Api.Dtos.Customers;
using SubscriptionApp.Domain.Entities;

namespace SubscriptionApp.Api.Mapping;

public static class CustomerMappings
{
    public static CustomerResponse ToResponse(this Customer customer) => new()
    {
        Id = customer.Id,
        FullName = customer.FullName,
        Email = customer.Email,
        PhoneNumber = customer.PhoneNumber,
        CreatedAt = customer.CreatedAt,
        SubscriptionCount = customer.Subscriptions?.Count ?? 0
    };
}
