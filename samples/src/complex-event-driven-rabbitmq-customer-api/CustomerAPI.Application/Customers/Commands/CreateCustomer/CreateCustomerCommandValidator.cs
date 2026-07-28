using FluentValidation;

namespace CustomerAPI.Application.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Model).NotNull();
        RuleFor(x => x.Model.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Model.Email).EmailAddress().MaximumLength(250).When(x => !string.IsNullOrEmpty(x.Model.Email));
    }
}
