using FluentValidation;
using SubscriptionApp.Api.Dtos.Subscriptions;

namespace SubscriptionApp.Api.Validators.Subscriptions;

public class UpdateSubscriptionRequestValidator : AbstractValidator<UpdateSubscriptionRequest>
{
    public UpdateSubscriptionRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum();

        RuleFor(x => x.ProviderName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.BillingDayOfMonth)
            .InclusiveBetween(1, 28)
            .WithMessage("BillingDayOfMonth must be between 1 and 28.");
    }
}
