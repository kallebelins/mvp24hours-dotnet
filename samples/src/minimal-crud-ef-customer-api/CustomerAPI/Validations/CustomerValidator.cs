using CustomerAPI.Entities;
using FluentValidation;

namespace CustomerAPI.Validations
{
    public class CustomerValidator : AbstractValidator<Customer>
    {
        public CustomerValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Customer {PropertyName} is required.");
        }
    }
}
