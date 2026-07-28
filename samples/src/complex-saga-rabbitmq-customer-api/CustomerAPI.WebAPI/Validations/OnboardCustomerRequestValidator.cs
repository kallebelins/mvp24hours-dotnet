using CustomerAPI.WebAPI.Controllers;
using FluentValidation;

namespace CustomerAPI.WebAPI.Validations;

public sealed class OnboardCustomerRequestValidator : AbstractValidator<OnboardCustomerRequest>
{
    public OnboardCustomerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Customer name is required.")
            .MaximumLength(100)
            .WithMessage("Customer name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email must be a valid address.");
    }
}
