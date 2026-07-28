using CustomerAPI.Application.DTOs.Customers;
using FluentValidation;

namespace CustomerAPI.Application.Validations.Customers;

public sealed class CustomerCreateValidator : AbstractValidator<CustomerCreate>
{
    public CustomerCreateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Customer name is required.")
            .MaximumLength(50)
            .WithMessage("Customer name cannot exceed 50 characters.");

        RuleFor(x => x.Note)
            .MaximumLength(2000)
            .WithMessage("Customer note cannot exceed 2000 characters.");
    }
}
