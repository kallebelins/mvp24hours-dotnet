using CustomerAPI.Application.DTOs.Contacts;
using CustomerAPI.Core.Enums;
using FluentValidation;
using Mvp24Hours.Extensions;

namespace CustomerAPI.Application.Validations.Contacts;

public sealed class ContactUpdateValidator : AbstractValidator<ContactUpdate>
{
    public ContactUpdateValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Contact description is required.")
            .MaximumLength(255)
            .WithMessage("Contact description cannot exceed 255 characters.");

        When(x => x.Type == ContactType.Email, () =>
        {
            RuleFor(x => x.Description)
                .EmailAddress()
                .WithMessage("Incorrect email.");
        });

        When(x => x.Type == ContactType.CellPhone, () =>
        {
            RuleFor(x => x.Description)
                .Must(m => m.IsValidPhoneNumber())
                .WithMessage("Incorrect phone number.");
        });
    }
}
