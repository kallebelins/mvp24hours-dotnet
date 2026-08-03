using Mvp24Hours.Application.Logic.Validation;
using Mvp24Hours.Application.Test.Support;

namespace Mvp24Hours.Application.Test.Logic.Validation;

[Trait("Category", "Unit")]
public class FluentValidationStepTest
{
    [Fact]
    public void Execute_NoValidators_ShouldReturnSuccess()
    {
        var step = new FluentValidationStep<AppTestEntity>([]);
        var context = new ValidationStepContext(ValidationOptions.Default, null);

        ValidationServiceResult result = step.Execute(new AppTestEntity { Name = "" }, context);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Execute_ExplicitValidatorInvalid_ShouldReturnErrors()
    {
        var step = new FluentValidationStep<AppTestEntity>([new AppTestEntityValidator()]);
        var context = new ValidationStepContext(ValidationOptions.Default, null);

        ValidationServiceResult result = step.Execute(new AppTestEntity { Name = "" }, context);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Execute_ExplicitValidatorValid_ShouldReturnSuccess()
    {
        var step = new FluentValidationStep<AppTestEntity>([new AppTestEntityValidator()]);
        var context = new ValidationStepContext(ValidationOptions.Default, null);

        ValidationServiceResult result = step.Execute(new AppTestEntity { Name = "Valid" }, context);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Execute_StopOnFirstError_ShouldReturnAfterFirstError()
    {
        var step = new FluentValidationStep<DualFieldModel>([new DualFieldModelValidator()]);
        var context = new ValidationStepContext(new ValidationOptions { StopOnFirstError = true }, null);

        ValidationServiceResult result = step.Execute(new DualFieldModel(), context);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldValidateUsingFluentValidation()
    {
        var step = new FluentValidationStep<AppTestEntity>([new AppTestEntityValidator()]);
        var context = new ValidationStepContext(ValidationOptions.Default, null);

        ValidationServiceResult result = await step.ExecuteAsync(new AppTestEntity { Name = "OK" }, context);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ShouldExecute_WithNoValidators_ShouldBeFalse()
    {
        var step = new FluentValidationStep<AppTestEntity>([]);
        var context = new ValidationStepContext(ValidationOptions.Default, null);

        step.ShouldExecute(new AppTestEntity(), context).Should().BeFalse();
    }

    [Fact]
    public void ShouldExecute_WithValidators_ShouldBeTrue()
    {
        var step = new FluentValidationStep<AppTestEntity>([new AppTestEntityValidator()]);
        var context = new ValidationStepContext(ValidationOptions.Default, null);

        step.ShouldExecute(new AppTestEntity(), context).Should().BeTrue();
    }

    [Fact]
    public void Execute_ServiceProvider_ShouldResolveValidator()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<AppTestEntity>, AppTestEntityValidator>();
        ServiceProvider provider = services.BuildServiceProvider();

        var step = new FluentValidationStep<AppTestEntity>(provider);
        var context = new ValidationStepContext(ValidationOptions.Default, provider);

        ValidationServiceResult result = step.Execute(new AppTestEntity { Name = "" }, context);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    private sealed class DualFieldModel
    {
        public string First { get; set; } = string.Empty;
        public string Second { get; set; } = string.Empty;
    }

    private sealed class DualFieldModelValidator : AbstractValidator<DualFieldModel>
    {
        public DualFieldModelValidator()
        {
            RuleFor(x => x.First).NotEmpty();
            RuleFor(x => x.Second).NotEmpty();
        }
    }
}
