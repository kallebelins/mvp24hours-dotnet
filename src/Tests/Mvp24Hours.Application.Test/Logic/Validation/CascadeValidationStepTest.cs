using System.ComponentModel.DataAnnotations;
using Mvp24Hours.Application.Logic.Validation;

namespace Mvp24Hours.Application.Test.Logic.Validation;

[Trait("Category", "Unit")]
public class CascadeValidationStepTest
{
    [Fact]
    public void Execute_ValidateNestedObjectsFalse_ShouldReturnSuccess()
    {
        var step = new CascadeValidationStep<ParentWithInvalidChild>();
        var context = new ValidationStepContext(
            new ValidationOptions { ValidateNestedObjects = false },
            null);

        ValidationServiceResult result = step.Execute(
            new ParentWithInvalidChild { Child = new NestedChild { Name = "" } },
            context);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Execute_NestedWithValidateNestedAndDataAnnotations_ShouldFail()
    {
        var step = new CascadeValidationStep<ParentWithInvalidChild>();
        var context = new ValidationStepContext(ValidationOptions.Default, null);

        ValidationServiceResult result = step.Execute(
            new ParentWithInvalidChild { Child = new NestedChild { Name = "" } },
            context);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Key!.Contains("Child", StringComparison.Ordinal));
    }

    [Fact]
    public void ShouldExecute_WhenCurrentDepthAtMaxValidationDepth_ShouldBeFalse()
    {
        var step = new CascadeValidationStep<ParentWithInvalidChild>();
        var context = new ValidationStepContext(
            new ValidationOptions { ValidateNestedObjects = true, MaxValidationDepth = 2 },
            null)
        {
            CurrentDepth = 2
        };

        step.ShouldExecute(new ParentWithInvalidChild(), context).Should().BeFalse();
    }

    [Fact]
    public void ShouldExecute_WhenBelowMaxValidationDepth_ShouldBeTrue()
    {
        var step = new CascadeValidationStep<ParentWithInvalidChild>();
        var context = new ValidationStepContext(
            new ValidationOptions { ValidateNestedObjects = true, MaxValidationDepth = 5 },
            null)
        {
            CurrentDepth = 1
        };

        step.ShouldExecute(new ParentWithInvalidChild(), context).Should().BeTrue();
    }

    [Fact]
    public void Execute_CircularReferences_ShouldNotLoopIndefinitely()
    {
        var step = new CascadeValidationStep<CircularNodeA>();
        var context = new ValidationStepContext(ValidationOptions.Default, null);

        var nodeA = new CircularNodeA();
        var nodeB = new CircularNodeB();
        nodeA.Next = nodeB;
        nodeB.Next = nodeA;

        ValidationServiceResult result = step.Execute(nodeA, context);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ValidNestedObject_ShouldReturnSuccess()
    {
        var step = new CascadeValidationStep<ParentWithInvalidChild>();
        var context = new ValidationStepContext(ValidationOptions.Default, null);

        ValidationServiceResult result = await step.ExecuteAsync(
            new ParentWithInvalidChild { Child = new NestedChild { Name = "Valid" } },
            context);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Execute_NullNestedProperty_ShouldReturnSuccess()
    {
        var step = new CascadeValidationStep<ParentWithInvalidChild>();
        var context = new ValidationStepContext(ValidationOptions.Default, null);

        ValidationServiceResult result = step.Execute(new ParentWithInvalidChild { Child = null }, context);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Execute_NestedCollection_ShouldValidateEachItem()
    {
        var step = new CascadeValidationStep<ParentWithNestedCollection>();
        var context = new ValidationStepContext(ValidationOptions.Default, null);

        ValidationServiceResult result = step.Execute(
            new ParentWithNestedCollection
            {
                Children = [new NestedChild { Name = "" }, new NestedChild { Name = "Valid" }]
            },
            context);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Key!.Contains("Children[0]", StringComparison.Ordinal));
    }

    [Fact]
    public void Execute_WithFluentValidationValidator_ShouldReturnErrors()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<NestedChild>, NestedChildFluentValidator>();
        var step = new CascadeValidationStep<ParentWithRegisteredValidator>(services.BuildServiceProvider());
        var context = new ValidationStepContext(ValidationOptions.Default, services.BuildServiceProvider());

        ValidationServiceResult result = step.Execute(
            new ParentWithRegisteredValidator { Child = new NestedChild { Name = "" } },
            context);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Key!.Contains("Child", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WithFluentValidationValidator_ShouldReturnErrors()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<NestedChild>, NestedChildFluentValidator>();
        var step = new CascadeValidationStep<ParentWithRegisteredValidator>(services.BuildServiceProvider());
        var context = new ValidationStepContext(ValidationOptions.Default, services.BuildServiceProvider());

        ValidationServiceResult result = await step.ExecuteAsync(
            new ParentWithRegisteredValidator { Child = new NestedChild { Name = "" } },
            context);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Execute_StopOnFirstError_ShouldReturnAfterFirstFailure()
    {
        var step = new CascadeValidationStep<ParentWithNestedCollection>();
        var context = new ValidationStepContext(
            new ValidationOptions { ValidateNestedObjects = true, StopOnFirstError = true },
            null);

        ValidationServiceResult result = step.Execute(
            new ParentWithNestedCollection
            {
                Children =
                [
                    new NestedChild { Name = "" },
                    new NestedChild { Name = "" }
                ]
            },
            context);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
    }

    [Fact]
    public void Execute_IncludePropertyPath_ShouldPrefixNestedPath()
    {
        var step = new CascadeValidationStep<ParentWithInvalidChild>();
        var context = new ValidationStepContext(
            new ValidationOptions { ValidateNestedObjects = true, IncludePropertyPath = true },
            null);

        ValidationServiceResult result = step.Execute(
            new ParentWithInvalidChild { Child = new NestedChild { Name = "" } },
            context);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Key!.StartsWith("Child.", StringComparison.Ordinal));
    }

    [Fact]
    public void Execute_IHasNestedValidation_ShouldValidateNestedType()
    {
        var step = new CascadeValidationStep<ParentWithNestedInterface>();
        var context = new ValidationStepContext(ValidationOptions.Default, null);

        ValidationServiceResult result = step.Execute(
            new ParentWithNestedInterface { Address = new NestedAddress { Line = "" } },
            context);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Key!.Contains("Address", StringComparison.Ordinal));
    }

    private sealed class ParentWithInvalidChild
    {
        [ValidateNested]
        public NestedChild? Child { get; set; }
    }

    private sealed class NestedChild
    {
        [Required]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class CircularNodeA
    {
        [ValidateNested]
        public CircularNodeB? Next { get; set; }
    }

    private sealed class CircularNodeB
    {
        [ValidateNested]
        public CircularNodeA? Next { get; set; }
    }

    private sealed class ParentWithNestedInterface
    {
        public NestedAddress? Address { get; set; }
    }

    private sealed class NestedAddress : IHasNestedValidation
    {
        [Required]
        public string Line { get; set; } = string.Empty;
    }

    private sealed class ParentWithNestedCollection
    {
        [ValidateNested]
        public List<NestedChild> Children { get; set; } = [];
    }

    private sealed class ParentWithRegisteredValidator
    {
        public NestedChild? Child { get; set; }
    }

    private sealed class NestedChildFluentValidator : AbstractValidator<NestedChild>
    {
        public NestedChildFluentValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
