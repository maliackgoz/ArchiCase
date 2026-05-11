using FluentValidation;
using SubscriptionApp.Api.Dtos.Subscriptions;
using SubscriptionApp.Domain.Enums;

namespace SubscriptionApp.Api.Validators.Subscriptions;

public class CreateSubscriptionRequestValidator : AbstractValidator<CreateSubscriptionRequest>
{
    public CreateSubscriptionRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0);

        RuleFor(x => x.SubscriptionType)
            .IsInEnum();

        RuleFor(x => x.ProviderName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.SubscriptionNumber)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.BillingDayOfMonth)
            .InclusiveBetween(1, 28)
            .WithMessage("BillingDayOfMonth must be between 1 and 28.");
    }
}
