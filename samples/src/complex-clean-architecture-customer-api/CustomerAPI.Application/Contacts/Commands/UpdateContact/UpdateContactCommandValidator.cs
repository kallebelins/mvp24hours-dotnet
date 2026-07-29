using CustomerAPI.Domain.Enums;
using FluentValidation;
using Mvp24Hours.Extensions;

namespace CustomerAPI.Application.Contacts.Commands.UpdateContact;

public sealed class UpdateContactCommandValidator : AbstractValidator<UpdateContactCommand>
{
    public UpdateContactCommandValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).NotNull();
        RuleFor(x => x.Model.Description)
            .NotEmpty()
            .WithMessage("Contact Description is required.");

        When(x => x.Model.Type == ContactType.Email, () => RuleFor(x => x.Model.Description)
                .EmailAddress()
                .WithMessage("Incorrect email."));

        When(x => x.Model.Type is ContactType.CellPhone or ContactType.HomePhone or ContactType.CommercialPhone, () => RuleFor(x => x.Model.Description)
                .Must(m => m.IsValidPhoneNumber())
                .WithMessage("Incorrect phone number."));
    }
}
