using FluentValidation;

namespace CustomerAPI.Application.Customers.Commands.UpdateCustomer;

public sealed class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).NotNull();
        RuleFor(x => x.Model.Name)
            .NotEmpty()
            .WithMessage("Customer Name is required.");
    }
}
