using FluentValidation;

namespace CustomerAPI.Application.Customers.Commands.UpdateCustomer;

public sealed class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Customer id must be a positive integer.");
        RuleFor(x => x.Model).NotNull();
        RuleFor(x => x.Model.Name)
            .NotEmpty()
            .WithMessage("Customer name is required.")
            .MaximumLength(50)
            .WithMessage("Customer name cannot exceed 50 characters.");
    }
}
