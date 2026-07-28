using CustomerAPI.WebAPI.Controllers;
using FluentValidation;

namespace CustomerAPI.WebAPI.Validations;

public sealed class RenameCustomerRequestValidator : AbstractValidator<RenameCustomerRequest>
{
    public RenameCustomerRequestValidator()
    {
        RuleFor(x => x.NewName)
            .NotEmpty()
            .WithMessage("New name is required.")
            .MaximumLength(100)
            .WithMessage("New name cannot exceed 100 characters.");
    }
}
