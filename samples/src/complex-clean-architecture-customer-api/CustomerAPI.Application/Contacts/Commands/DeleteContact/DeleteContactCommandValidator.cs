using FluentValidation;

namespace CustomerAPI.Application.Contacts.Commands.DeleteContact;

public sealed class DeleteContactCommandValidator : AbstractValidator<DeleteContactCommand>
{
    public DeleteContactCommandValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
