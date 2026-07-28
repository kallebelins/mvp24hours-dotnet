using FluentValidation;

namespace CustomerAPI.Application.Contacts.Commands.AddContact;

public sealed class AddContactCommandValidator : AbstractValidator<AddContactCommand>
{
    public AddContactCommandValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Customer id must be a positive integer.");
        RuleFor(x => x.Model).NotNull();
        RuleFor(x => x.Model.Description)
            .NotEmpty()
            .WithMessage("Contact description is required.")
            .MaximumLength(255)
            .WithMessage("Contact description cannot exceed 255 characters.");
    }
}
