using Mvp24Hours.Application.Logic.Validation;

namespace Mvp24Hours.Application.Test.Logic.Validation;

[Trait("Category", "Unit")]
public class DataAnnotationValidationStepTest
{
    [Fact]
    public void Execute_ValidModel_ShouldReturnSuccess()
    {
        var step = new DataAnnotationValidationStep<AnnotatedModel>();
        var context = new ValidationStepContext(ValidationOptions.Default, null);

        ValidationServiceResult result = step.Execute(new AnnotatedModel { Name = "Valid" }, context);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Execute_InvalidModel_ShouldReturnErrors()
    {
        var step = new DataAnnotationValidationStep<AnnotatedModel>();
        var context = new ValidationStepContext(ValidationOptions.Default, null);

        ValidationServiceResult result = step.Execute(new AnnotatedModel { Name = "" }, context);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Execute_StopOnFirstError_ShouldReturnAfterFirstError()
    {
        var step = new DataAnnotationValidationStep<MultiFieldModel>();
        var context = new ValidationStepContext(new ValidationOptions { StopOnFirstError = true }, null);

        ValidationServiceResult result = step.Execute(new MultiFieldModel(), context);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDelegateToSyncExecute()
    {
        var step = new DataAnnotationValidationStep<AnnotatedModel>();
        var context = new ValidationStepContext(ValidationOptions.Default, null);

        ValidationServiceResult result = await step.ExecuteAsync(new AnnotatedModel { Name = "OK" }, context);

        result.IsValid.Should().BeTrue();
    }

    private sealed class AnnotatedModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class MultiFieldModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Name { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Range(1, 10)]
        public int Value { get; set; }
    }
}
