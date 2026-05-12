using FluentValidation;
using SubscriptionApp.Api.Dtos.Subscriptions;

namespace SubscriptionApp.Api.Validators.Subscriptions;

public class CreateMySubscriptionRequestValidator : AbstractValidator<CreateMySubscriptionRequest>
{
    public CreateMySubscriptionRequestValidator()
    {
        RuleFor(x => x.SubscriptionType).IsInEnum();
        RuleFor(x => x.ProviderName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SubscriptionNumber).NotEmpty().MaximumLength(50);
    }
}
