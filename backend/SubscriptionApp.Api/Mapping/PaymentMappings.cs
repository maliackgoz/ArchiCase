using SubscriptionApp.Api.Dtos.Payments;
using SubscriptionApp.Domain.Entities;

namespace SubscriptionApp.Api.Mapping;

public static class PaymentMappings
{
    public static PaymentResponse ToResponse(this Payment payment) => new()
    {
        Id = payment.Id,
        SubscriptionId = payment.SubscriptionId,
        Amount = payment.Amount,
        Period = payment.Period,
        Status = payment.Status,
        PaymentDate = payment.PaymentDate,
        ExternalTransactionId = payment.ExternalTransactionId
    };
}
