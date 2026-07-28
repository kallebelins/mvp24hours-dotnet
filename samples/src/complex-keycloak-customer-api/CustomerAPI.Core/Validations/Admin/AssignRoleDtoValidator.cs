using CustomerAPI.Core.DTOs.Admin;
using FluentValidation;

namespace CustomerAPI.Core.Validations.Admin;

public sealed class AssignRoleDtoValidator : AbstractValidator<AssignRoleDto>
{
    public AssignRoleDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User id is required.");

        RuleFor(x => x.RoleId)
            .NotEmpty()
            .WithMessage("Role id is required.");

        RuleFor(x => x.RoleName)
            .NotEmpty()
            .WithMessage("Role name is required.");
    }
}
