using Mvp24Hours.Application.Logic.Validation;

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

    [Fact]
    public void Validate_StopOnFirstError_ShouldReturnEarly()
    {
        var service = new ValidationService<TestDto>([new TestDtoValidator(), new SecondFieldValidator()]);
        var options = new ValidationOptions { StopOnFirstError = true };

        ValidationServiceResult result = service.Validate(new TestDto { Name = "", Description = "" }, options);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
    }

    [Fact]
    public void Validate_WithFluentValidationDisabled_ShouldSkipFluent()
    {
        var service = new ValidationService<TestDto>(
            [new TestDtoValidator()],
            options: new ValidationServiceOptions { UseFluentValidation = false, UseDataAnnotations = true });

        ValidationServiceResult result = service.Validate(new TestDto { Name = "" });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithNullInstance_ShouldReturnFailure()
    {
        var service = new ValidationService<TestDto>();

        ValidationServiceResult result = await service.ValidateAsync(null!);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateManyAsync_ShouldPrefixIndexInErrors()
    {
        var service = new ValidationService<TestDto>([new TestDtoValidator()]);
        TestDto[] items = [new() { Name = "" }, new() { Name = "OK" }];

        ValidationServiceResult result = await service.ValidateManyAsync(items);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Key!.StartsWith("[0]."));
    }

    [Fact]
    public void ValidateMany_WithNullCollection_ShouldReturnFailure()
    {
        var service = new ValidationService<TestDto>();

        ValidationServiceResult result = service.ValidateMany(null!);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAndThrowAsync_Invalid_ShouldThrowValidationException()
    {
        var service = new ValidationService<TestDto>([new TestDtoValidator()]);

        Func<Task> act = () => service.ValidateAndThrowAsync(new TestDto { Name = "" });

        await act.Should().ThrowAsync<Mvp24Hours.Core.Exceptions.ValidationException>();
    }

    [Fact]
    public void ValidateWithNested_ShouldValidateChildObjects()
    {
        var service = new ValidationService<ParentDto>(options: new ValidationServiceOptions
        {
            UseFluentValidation = false,
            UseDataAnnotations = true,
            UseCascadeValidation = true
        });

        ValidationServiceResult result = service.ValidateWithNested(new ParentDto
        {
            Name = "parent",
            Child = new ChildDto { Name = "" }
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Key!.Contains("Child", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateWithNestedAsync_ShouldValidateNestedCollection()
    {
        var service = new ValidationService<ParentWithListDto>(options: new ValidationServiceOptions
        {
            UseFluentValidation = false,
            UseDataAnnotations = true,
            UseCascadeValidation = true,
            ValidateAllNestedObjects = true
        });

        ValidationServiceResult result = await service.ValidateWithNestedAsync(new ParentWithListDto
        {
            Items = [new ChildDto { Name = "" }]
        });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateNested_WithDiResolvedValidator_ShouldReturnFluentErrors()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<ChildDto>, ChildDtoValidator>();
        IServiceProvider provider = services.BuildServiceProvider();
        var service = new ValidationService<ParentDto>(
            serviceProvider: provider,
            logger: NullLogger<ValidationService<ParentDto>>.Instance,
            options: new ValidationServiceOptions
            {
                UseFluentValidation = true,
                UseDataAnnotations = false,
                UseCascadeValidation = true
            });

        ValidationServiceResult result = service.ValidateWithNested(new ParentDto
        {
            Name = "parent",
            Child = new ChildDto { Name = "" }
        });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_NestedWithDiValidator_ShouldResolveAsyncValidator()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<ChildDto>, ChildDtoValidator>();
        IServiceProvider provider = services.BuildServiceProvider();
        var service = new ValidationService<ParentDto>(
            serviceProvider: provider,
            options: new ValidationServiceOptions
            {
                UseFluentValidation = true,
                UseDataAnnotations = false,
                UseCascadeValidation = true
            });

        ValidationServiceResult result = await service.ValidateWithNestedAsync(new ParentDto
        {
            Name = "parent",
            Child = new ChildDto { Name = "" }
        });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateNested_MaxDepthReached_ShouldSkipDeepValidation()
    {
        var service = new ValidationService<ParentDto>(options: new ValidationServiceOptions
        {
            UseFluentValidation = false,
            UseDataAnnotations = true,
            UseCascadeValidation = true
        });
        var options = new ValidationOptions { MaxValidationDepth = 0, ValidateNestedObjects = true };

        ValidationServiceResult result = service.Validate(new ParentDto
        {
            Name = "parent",
            Child = new ChildDto { Name = "" }
        }, options);

        result.IsValid.Should().BeTrue();
    }

    private sealed class TestDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }

    private sealed class ParentDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Name { get; set; } = string.Empty;

        [ValidateNested]
        public ChildDto? Child { get; set; }
    }

    private sealed class ParentWithListDto
    {
        public List<ChildDto> Items { get; set; } = [];
    }

    private sealed class ChildDto : IHasNestedValidation
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestDtoValidator : AbstractValidator<TestDto>
    {
        public TestDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    private sealed class SecondFieldValidator : AbstractValidator<TestDto>
    {
        public SecondFieldValidator()
        {
            RuleFor(x => x.Description).NotEmpty();
        }
    }

    private sealed class ChildDtoValidator : AbstractValidator<ChildDto>
    {
        public ChildDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
