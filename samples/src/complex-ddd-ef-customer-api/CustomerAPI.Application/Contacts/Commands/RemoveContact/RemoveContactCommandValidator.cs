using FluentValidation;

namespace CustomerAPI.Application.Contacts.Commands.RemoveContact;

public sealed class RemoveContactCommandValidator : AbstractValidator<RemoveContactCommand>
{
    public RemoveContactCommandValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Customer id must be a positive integer.");
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Contact id must be a positive integer.");
    }
}
