using FluentValidation;
using Mvp24Hours.Application.Logic.Validation;
using Mvp24Hours.Core.Exceptions;

namespace Mvp24Hours.Application.Test.Logic.Validation;

[Trait("Category", "Unit")]
public class ValidationServiceTest
{
    [Fact]
    public void Validate_WithFluentValidation_ShouldReturnErrors()
    {
        var service = new ValidationService<TestDto>([new TestDtoValidator()]);
        var dto = new TestDto { Name = "" };

        ValidationServiceResult result = service.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_WithDataAnnotations_ShouldReturnErrors()
    {
        var service = new ValidationService<TestDto>(options: new ValidationServiceOptions
        {
            UseFluentValidation = false,
            UseDataAnnotations = true
        });

        ValidationServiceResult result = service.Validate(new TestDto { Name = "" });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ValidInstance_ShouldSucceed()
    {
        var service = new ValidationService<TestDto>([new TestDtoValidator()]);

        ValidationServiceResult result = service.Validate(new TestDto { Name = "Valid" });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateMany_ShouldPrefixIndexInErrors()
    {
        var service = new ValidationService<TestDto>([new TestDtoValidator()]);
        TestDto[] items = [new() { Name = "" }, new() { Name = "OK" }];

        ValidationServiceResult result = service.ValidateMany(items);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Key!.StartsWith("[0]."));
    }

    [Fact]
    public void ValidateAndThrow_Invalid_ShouldThrowValidationException()
    {
        var service = new ValidationService<TestDto>([new TestDtoValidator()]);

        Action act = () => service.ValidateAndThrow(new TestDto { Name = "" });

        act.Should().Throw<Mvp24Hours.Core.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task ValidateAsync_ShouldValidateWithFluentValidation()
    {
        var service = new ValidationService<TestDto>([new TestDtoValidator()]);

        ValidationServiceResult result = await service.ValidateAsync(new TestDto { Name = "" });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithNullInstance_ShouldReturnFailure()
    {
        var service = new ValidationService<TestDto>();

        ValidationServiceResult result = service.Validate(null!);

        result.IsValid.Should().BeFalse();
    }

    private sealed class TestDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestDtoValidator : AbstractValidator<TestDto>
    {
        public TestDtoValidator() => RuleFor(x => x.Name).NotEmpty();
    }
}
