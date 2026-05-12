using FluentValidation;
using SubscriptionApp.Api.Dtos.Subscriptions;

namespace SubscriptionApp.Api.Validators.Subscriptions;

public class UpdateSubscriptionRequestValidator : AbstractValidator<UpdateSubscriptionRequest>
{
    public UpdateSubscriptionRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();

        RuleFor(x => x.ProviderName)
            .NotEmpty()
            .MaximumLength(100);

        // Loose 1–28 sanity check; the service applies the provider's [billing, lastPayment] window.
        RuleFor(x => x.PaymentDayOfMonth)
            .InclusiveBetween(1, 28)
            .WithMessage("PaymentDayOfMonth must be between 1 and 28.");
    }
}
