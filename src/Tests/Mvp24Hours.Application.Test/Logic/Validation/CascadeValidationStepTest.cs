using System.ComponentModel.DataAnnotations;
using Mvp24Hours.Application.Contract.Validation;
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
}
