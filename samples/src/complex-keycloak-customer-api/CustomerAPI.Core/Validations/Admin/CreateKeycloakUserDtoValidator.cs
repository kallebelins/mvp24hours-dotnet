using CustomerAPI.Core.DTOs.Admin;
using FluentValidation;

namespace CustomerAPI.Core.Validations.Admin;

public sealed class CreateKeycloakUserDtoValidator : AbstractValidator<CreateKeycloakUserDto>
{
    public CreateKeycloakUserDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email must be a valid address.");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.");

        RuleFor(x => x.TemporaryPassword)
            .NotEmpty()
            .WithMessage("Temporary password is required.")
            .MinimumLength(8)
            .WithMessage("Temporary password must be at least 8 characters.");
    }
}
