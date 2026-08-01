using CustomerAPI.Core.DTOs.Admin;
using CustomerAPI.Core.Validations.Admin;
using FluentValidation.Results;

namespace CustomerAPI.Test.Unit;

[Trait("Category", "Unit")]
public class CreateKeycloakUserDtoValidatorTests
{
    private readonly CreateKeycloakUserDtoValidator _validator = new();

    [Fact]
    public async Task Validate_WhenUsernameMissing_IsInvalid()
    {
        var dto = new CreateKeycloakUserDto(
            Username: string.Empty,
            Email: "user@example.com",
            FirstName: "Ada",
            LastName: "Lovelace",
            TemporaryPassword: "ChangeMe1!");

        ValidationResult result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateKeycloakUserDto.Username));
    }

    [Fact]
    public async Task Validate_WhenRequiredFieldsProvided_IsValid()
    {
        var dto = new CreateKeycloakUserDto(
            Username: "ada",
            Email: "ada@example.com",
            FirstName: "Ada",
            LastName: "Lovelace",
            TemporaryPassword: "ChangeMe1!");

        ValidationResult result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }
}
