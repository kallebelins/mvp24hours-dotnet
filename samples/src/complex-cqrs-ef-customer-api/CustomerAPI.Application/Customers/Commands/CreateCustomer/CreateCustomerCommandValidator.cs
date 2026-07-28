using FluentValidation;

namespace CustomerAPI.Application.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Model).NotNull();
        RuleFor(x => x.Model.Name)
            .NotEmpty()
            .WithMessage("Customer Name is required.");
    }
}
