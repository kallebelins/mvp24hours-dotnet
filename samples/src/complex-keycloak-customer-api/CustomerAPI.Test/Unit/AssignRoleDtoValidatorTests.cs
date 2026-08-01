using CustomerAPI.Core.DTOs.Admin;
using CustomerAPI.Core.Validations.Admin;
using FluentValidation.Results;

namespace CustomerAPI.Test.Unit;

[Trait("Category", "Unit")]
public class AssignRoleDtoValidatorTests
{
    private readonly AssignRoleDtoValidator _validator = new();

    [Fact]
    public async Task Validate_WhenRoleNameMissing_IsInvalid()
    {
        var dto = new AssignRoleDto(
            UserId: Guid.NewGuid().ToString(),
            RoleId: Guid.NewGuid().ToString(),
            RoleName: string.Empty);

        ValidationResult result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }
}
