using FluentValidation;
using SubscriptionApp.Api.Dtos.Auth;

namespace SubscriptionApp.Api.Validators.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^\+90[0-9]{10}$")
            .WithMessage("PhoneNumber must be +90XXXXXXXXXX");
    }
}
