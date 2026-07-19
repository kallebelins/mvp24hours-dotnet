using Mvp24Hours.Application.Contract.Validation;
using Mvp24Hours.Application.Logic.Validation;
using Mvp24Hours.Core.Exceptions;

namespace Mvp24Hours.Application.Test.Logic.Validation;

[Trait("Category", "Unit")]
public class ValidationPipelineTest
{
    [Fact]
    public void Execute_WithNullInstance_ShouldReturnFailure()
    {
        var pipeline = new ValidationPipeline<TestModel>();

        ValidationServiceResult result = pipeline.Execute(null!);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Key == "instance");
    }

    [Fact]
    public void Execute_WithSuccessfulSteps_ShouldReturnSuccess()
    {
        var pipeline = new ValidationPipeline<TestModel>([
            new PredicateValidationStep<TestModel>(
                "AlwaysValid",
                (_, _) => ValidationServiceResult.Success())
        ]);

        ValidationServiceResult result = pipeline.Execute(new TestModel { Name = "ok" });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Execute_StopOnFirstError_ShouldStopAfterFirstFailure()
    {
        int secondStepCalls = 0;
        var pipeline = new ValidationPipeline<TestModel>([
            new PredicateValidationStep<TestModel>(
                "FirstFail",
                (_, _) => ValidationServiceResult.Failure("Name", "Required"),
                order: 1),
            new PredicateValidationStep<TestModel>(
                "Second",
                (_, _) =>
                {
                    secondStepCalls++;
                    return ValidationServiceResult.Success();
                },
                order: 2)
        ]);

        ValidationServiceResult result = pipeline.Execute(
            new TestModel(),
            new ValidationOptions { StopOnFirstError = true });

        result.IsValid.Should().BeFalse();
        secondStepCalls.Should().Be(0);
    }

    [Fact]
    public void Execute_ThrowOnValidationFailure_ShouldThrowValidationException()
    {
        var pipeline = new ValidationPipeline<TestModel>([
            new PredicateValidationStep<TestModel>(
                "Fail",
                (_, _) => ValidationServiceResult.Failure("Name", "Invalid"))
        ]);

        Action act = () => pipeline.Execute(new TestModel(), new ValidationOptions { ThrowOnValidationFailure = true });

        act.Should().Throw<Mvp24Hours.Core.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRunAsyncSteps()
    {
        var pipeline = new ValidationPipeline<TestModel>([
            new PredicateValidationStep<TestModel>(
                "AsyncStep",
                (_, _) => ValidationServiceResult.Success(),
                (_, ctx) => Task.FromResult(ValidationServiceResult.Success()),
                order: 1)
        ]);

        ValidationServiceResult result = await pipeline.ExecuteAsync(new TestModel { Name = "async" });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Builder_UseDataAnnotations_ShouldBuildPipelineWithStep()
    {
        IValidationPipeline<TestModel> pipeline = new ValidationPipelineBuilder<TestModel>()
            .UseDataAnnotations()
            .Build();

        pipeline.Steps.Should().ContainSingle(s => s.Name == "DataAnnotations");
    }

    [Fact]
    public void AddAndRemoveStep_ShouldManageSteps()
    {
        var pipeline = new ValidationPipeline<TestModel>();
        var step = new PredicateValidationStep<TestModel>("S", (_, _) => ValidationServiceResult.Success());

        pipeline.AddStep(step);
        pipeline.Steps.Should().HaveCount(1);
        pipeline.RemoveStep(step).Should().BeTrue();
        pipeline.Steps.Should().BeEmpty();
    }

    private sealed class TestModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Name { get; set; } = string.Empty;
    }
}
