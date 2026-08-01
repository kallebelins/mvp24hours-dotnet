using CustomerAPI.Core.DTOs.Admin;
using CustomerAPI.Core.Validations.Admin;
using FluentValidation.Results;

namespace CustomerAPI.Test.Unit;

[Trait("Category", "Unit")]
public class ResetPasswordDtoValidatorTests
{
    private readonly ResetPasswordDtoValidator _validator = new();

    [Fact]
    public async Task Validate_WhenPasswordTooShort_IsInvalid()
    {
        var dto = new ResetPasswordDto(
            UserId: Guid.NewGuid().ToString(),
            NewPassword: "short");

        ValidationResult result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
    }
}
