using FluentValidation;
using SubscriptionApp.Api.Dtos.Payments;

namespace SubscriptionApp.Api.Validators.Payments;

public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.SubscriptionId)
            .GreaterThan(0);

        // decimal — never double/float for money
        RuleFor(x => x.Amount)
            .GreaterThan(0m)
            .WithMessage("Amount must be greater than zero.");

        // YYYY-MM: four digits, dash, month 01-12
        RuleFor(x => x.Period)
            .NotEmpty()
            .Matches(@"^\d{4}-(0[1-9]|1[0-2])$")
            .WithMessage("Period must be in format YYYY-MM with a valid month (e.g. 2026-05).");
    }
}
