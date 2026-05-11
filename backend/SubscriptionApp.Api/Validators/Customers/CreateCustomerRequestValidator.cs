using FluentValidation;
using SubscriptionApp.Api.Dtos.Customers;

namespace SubscriptionApp.Api.Validators.Customers;

public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(200)
            .EmailAddress();

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^\+90[0-9]{10}$")
            .WithMessage("PhoneNumber must be in format +90XXXXXXXXXX (Turkish mobile).");
    }
}
