//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentAssertions;
using Mvp24Hours.Application.Specifications;
using Mvp24Hours.Core.Domain.Specifications;
using System.Linq.Expressions;
using Xunit;

namespace Mvp24Hours.Application.Test.Specifications;

/// <summary>
/// Unit tests for SpecificationCombinators static class functionality.
/// </summary>
public class SpecificationCombinatorsTest
{
    #region [ And Tests ]

    [Fact]
    public void And_BothSpecificationsSatisfied_ShouldReturnTrue()
    {
        // Arrange
        var spec1 = Specification<TestEntity>.Create(e => e.Value > 5);
        var spec2 = Specification<TestEntity>.Create(e => e.Name.StartsWith("T"));
        var entity = new TestEntity { Value = 10, Name = "Test" };

        // Act
        var combinedSpec = SpecificationCombinators.And(spec1, spec2);
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void And_FirstSpecificationNotSatisfied_ShouldReturnFalse()
    {
        // Arrange
        var spec1 = Specification<TestEntity>.Create(e => e.Value > 100);
        var spec2 = Specification<TestEntity>.Create(e => e.Name.StartsWith("T"));
        var entity = new TestEntity { Value = 10, Name = "Test" };

        // Act
        var combinedSpec = SpecificationCombinators.And(spec1, spec2);
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void And_SecondSpecificationNotSatisfied_ShouldReturnFalse()
    {
        // Arrange
        var spec1 = Specification<TestEntity>.Create(e => e.Value > 5);
        var spec2 = Specification<TestEntity>.Create(e => e.Name.StartsWith("X"));
        var entity = new TestEntity { Value = 10, Name = "Test" };

        // Act
        var combinedSpec = SpecificationCombinators.And(spec1, spec2);
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void And_NeitherSpecificationSatisfied_ShouldReturnFalse()
    {
        // Arrange
        var spec1 = Specification<TestEntity>.Create(e => e.Value > 100);
        var spec2 = Specification<TestEntity>.Create(e => e.Name.StartsWith("X"));
        var entity = new TestEntity { Value = 10, Name = "Test" };

        // Act
        var combinedSpec = SpecificationCombinators.And(spec1, spec2);
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void And_WithExpression_ShouldCombineCorrectly()
    {
        // Arrange
        var spec = Specification<TestEntity>.Create(e => e.Value > 5);
        Expression<Func<TestEntity, bool>> expression = e => e.Name.StartsWith("T");
        var entity = new TestEntity { Value = 10, Name = "Test" };

        // Act
        var combinedSpec = SpecificationCombinators.And(spec, expression);
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AndAll_AllSpecificationsSatisfied_ShouldReturnTrue()
    {
        // Arrange
        var specs = new[]
        {
            Specification<TestEntity>.Create(e => e.Value > 5),
            Specification<TestEntity>.Create(e => e.Name.Length > 2),
            Specification<TestEntity>.Create(e => e.IsActive)
        };
        var entity = new TestEntity { Value = 10, Name = "Test", IsActive = true };

        // Act
        var combinedSpec = SpecificationCombinators.AndAll(specs);
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AndAll_OneSpecificationNotSatisfied_ShouldReturnFalse()
    {
        // Arrange
        var specs = new[]
        {
            Specification<TestEntity>.Create(e => e.Value > 5),
            Specification<TestEntity>.Create(e => e.Name.Length > 10), // Not satisfied
            Specification<TestEntity>.Create(e => e.IsActive)
        };
        var entity = new TestEntity { Value = 10, Name = "Test", IsActive = true };

        // Act
        var combinedSpec = SpecificationCombinators.AndAll(specs);
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AndAll_EmptyCollection_ShouldReturnAll()
    {
        // Arrange
        var specs = Array.Empty<Specification<TestEntity>>();

        // Act
        var combinedSpec = SpecificationCombinators.AndAll(specs);
        var result = combinedSpec.IsSatisfiedBy(new TestEntity());

        // Assert
        result.Should().BeTrue(); // All() matches everything
    }

    #endregion

    #region [ Or Tests ]

    [Fact]
    public void Or_BothSpecificationsSatisfied_ShouldReturnTrue()
    {
        // Arrange
        var spec1 = Specification<TestEntity>.Create(e => e.Value > 5);
        var spec2 = Specification<TestEntity>.Create(e => e.Name.StartsWith("T"));
        var entity = new TestEntity { Value = 10, Name = "Test" };

        // Act
        var combinedSpec = SpecificationCombinators.Or(spec1, spec2);
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Or_OnlyFirstSpecificationSatisfied_ShouldReturnTrue()
    {
        // Arrange
        var spec1 = Specification<TestEntity>.Create(e => e.Value > 5);
        var spec2 = Specification<TestEntity>.Create(e => e.Name.StartsWith("X"));
        var entity = new TestEntity { Value = 10, Name = "Test" };

        // Act
        var combinedSpec = SpecificationCombinators.Or(spec1, spec2);
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Or_OnlySecondSpecificationSatisfied_ShouldReturnTrue()
    {
        // Arrange
        var spec1 = Specification<TestEntity>.Create(e => e.Value > 100);
        var spec2 = Specification<TestEntity>.Create(e => e.Name.StartsWith("T"));
        var entity = new TestEntity { Value = 10, Name = "Test" };

        // Act
        var combinedSpec = SpecificationCombinators.Or(spec1, spec2);
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Or_NeitherSpecificationSatisfied_ShouldReturnFalse()
    {
        // Arrange
        var spec1 = Specification<TestEntity>.Create(e => e.Value > 100);
        var spec2 = Specification<TestEntity>.Create(e => e.Name.StartsWith("X"));
        var entity = new TestEntity { Value = 10, Name = "Test" };

        // Act
        var combinedSpec = SpecificationCombinators.Or(spec1, spec2);
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Or_WithExpression_ShouldCombineCorrectly()
    {
        // Arrange
        var spec = Specification<TestEntity>.Create(e => e.Value > 100);
        Expression<Func<TestEntity, bool>> expression = e => e.Name.StartsWith("T");
        var entity = new TestEntity { Value = 10, Name = "Test" };

        // Act
        var combinedSpec = SpecificationCombinators.Or(spec, expression);
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void OrAll_AnySpecificationSatisfied_ShouldReturnTrue()
    {
        // Arrange
        var specs = new[]
        {
            Specification<TestEntity>.Create(e => e.Value > 100), // Not satisfied
            Specification<TestEntity>.Create(e => e.Name == "WrongName"), // Not satisfied
            Specification<TestEntity>.Create(e => e.IsActive) // Satisfied
        };
        var entity = new TestEntity { Value = 10, Name = "Test", IsActive = true };

        // Act
        var combinedSpec = SpecificationCombinators.OrAll(specs);
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void OrAll_NoSpecificationSatisfied_ShouldReturnFalse()
    {
        // Arrange
        var specs = new[]
        {
            Specification<TestEntity>.Create(e => e.Value > 100),
            Specification<TestEntity>.Create(e => e.Name == "WrongName"),
            Specification<TestEntity>.Create(e => !e.IsActive)
        };
        var entity = new TestEntity { Value = 10, Name = "Test", IsActive = true };

        // Act
        var combinedSpec = SpecificationCombinators.OrAll(specs);
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void OrAll_EmptyCollection_ShouldReturnNone()
    {
        // Arrange
        var specs = Array.Empty<Specification<TestEntity>>();

        // Act
        var combinedSpec = SpecificationCombinators.OrAll(specs);
        var result = combinedSpec.IsSatisfiedBy(new TestEntity());

        // Assert
        result.Should().BeFalse(); // None() matches nothing
    }

    #endregion

    #region [ Not Tests ]

    [Fact]
    public void Not_SpecificationSatisfied_ShouldReturnFalse()
    {
        // Arrange
        var spec = Specification<TestEntity>.Create(e => e.Value > 5);
        var entity = new TestEntity { Value = 10, Name = "Test" };

        // Act
        var notSpec = SpecificationCombinators.Not(spec);
        var result = notSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Not_SpecificationNotSatisfied_ShouldReturnTrue()
    {
        // Arrange
        var spec = Specification<TestEntity>.Create(e => e.Value > 100);
        var entity = new TestEntity { Value = 10, Name = "Test" };

        // Act
        var notSpec = SpecificationCombinators.Not(spec);
        var result = notSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Not_WithExpression_ShouldNegateCorrectly()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.Value > 100;
        var entity = new TestEntity { Value = 10, Name = "Test" };

        // Act
        var notSpec = SpecificationCombinators.Not(expression);
        var result = notSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region [ Complex Combinations Tests ]

    [Fact]
    public void ComplexCombination_AndOrNot_ShouldWorkCorrectly()
    {
        // Arrange: (Value > 5 AND Name starts with "T") OR NOT(IsActive)
        var greaterThan5 = Specification<TestEntity>.Create(e => e.Value > 5);
        var startsWithT = Specification<TestEntity>.Create(e => e.Name.StartsWith("T"));
        var isActive = Specification<TestEntity>.Create(e => e.IsActive);

        var entity1 = new TestEntity { Value = 10, Name = "Test", IsActive = true };  // First part true
        var entity2 = new TestEntity { Value = 3, Name = "Other", IsActive = false }; // Second part true (NOT active)
        var entity3 = new TestEntity { Value = 3, Name = "Other", IsActive = true };  // Neither part true

        // Act
        var combinedSpec = SpecificationCombinators.Or(
            SpecificationCombinators.And(greaterThan5, startsWithT),
            SpecificationCombinators.Not(isActive)
        );

        // Assert
        combinedSpec.IsSatisfiedBy(entity1).Should().BeTrue();
        combinedSpec.IsSatisfiedBy(entity2).Should().BeTrue();
        combinedSpec.IsSatisfiedBy(entity3).Should().BeFalse();
    }

    [Fact]
    public void ChainedAnd_AllSpecificationsSatisfied_ShouldReturnTrue()
    {
        // Arrange
        var spec1 = Specification<TestEntity>.Create(e => e.Value > 5);
        var spec2 = Specification<TestEntity>.Create(e => e.Name.Length > 2);
        var spec3 = Specification<TestEntity>.Create(e => e.IsActive);
        var entity = new TestEntity { Value = 10, Name = "Test", IsActive = true };

        // Act
        var combinedSpec = SpecificationCombinators.And(
            SpecificationCombinators.And(spec1, spec2),
            spec3
        );
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ChainedOr_AnySpecificationSatisfied_ShouldReturnTrue()
    {
        // Arrange
        var spec1 = Specification<TestEntity>.Create(e => e.Value > 100);
        var spec2 = Specification<TestEntity>.Create(e => e.Name == "WrongName");
        var spec3 = Specification<TestEntity>.Create(e => e.IsActive);
        var entity = new TestEntity { Value = 10, Name = "Test", IsActive = true };

        // Act
        var combinedSpec = SpecificationCombinators.Or(
            SpecificationCombinators.Or(spec1, spec2),
            spec3
        );
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region [ Expression Tests ]

    [Fact]
    public void IsSatisfiedByExpression_CombinedSpecification_ShouldReturnValidExpression()
    {
        // Arrange
        var spec1 = Specification<TestEntity>.Create(e => e.Value > 5);
        var spec2 = Specification<TestEntity>.Create(e => e.IsActive);
        var combinedSpec = SpecificationCombinators.And(spec1, spec2);

        // Act
        var expression = combinedSpec.IsSatisfiedByExpression;
        var compiled = expression.Compile();

        var entity1 = new TestEntity { Value = 10, IsActive = true };
        var entity2 = new TestEntity { Value = 10, IsActive = false };

        // Assert
        compiled(entity1).Should().BeTrue();
        compiled(entity2).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedByExpression_CanBeUsedInLinq_ShouldFilterCorrectly()
    {
        // Arrange
        var spec = Specification<TestEntity>.Create(e => e.Value > 5);
        var entities = new List<TestEntity>
        {
            new() { Value = 3, Name = "A" },
            new() { Value = 7, Name = "B" },
            new() { Value = 10, Name = "C" }
        };

        // Act
        var filtered = entities.AsQueryable().Where(spec.IsSatisfiedByExpression).ToList();

        // Assert
        filtered.Should().HaveCount(2);
        filtered.Select(e => e.Name).Should().Contain(new[] { "B", "C" });
    }

    #endregion

    #region [ Factory Methods Tests ]

    [Fact]
    public void FromExpression_WithExpression_ShouldCreateSpecification()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.Value > 5;

        // Act
        var spec = SpecificationCombinators.FromExpression(expression);
        var entity = new TestEntity { Value = 10 };

        // Assert
        spec.IsSatisfiedBy(entity).Should().BeTrue();
    }

    [Fact]
    public void All_ShouldCreateSpecificationThatMatchesEverything()
    {
        // Act
        var allSpec = SpecificationCombinators.All<TestEntity>();

        // Assert
        allSpec.IsSatisfiedBy(new TestEntity { Value = 0 }).Should().BeTrue();
        allSpec.IsSatisfiedBy(new TestEntity { Value = 100 }).Should().BeTrue();
        allSpec.IsSatisfiedBy(new TestEntity { Value = -50 }).Should().BeTrue();
    }

    [Fact]
    public void None_ShouldCreateSpecificationThatMatchesNothing()
    {
        // Act
        var noneSpec = SpecificationCombinators.None<TestEntity>();

        // Assert
        noneSpec.IsSatisfiedBy(new TestEntity { Value = 0 }).Should().BeFalse();
        noneSpec.IsSatisfiedBy(new TestEntity { Value = 100 }).Should().BeFalse();
        noneSpec.IsSatisfiedBy(new TestEntity { Value = -50 }).Should().BeFalse();
    }

    [Fact]
    public void All_AndWithOther_ShouldReturnOther()
    {
        // Arrange
        var allSpec = SpecificationCombinators.All<TestEntity>();
        var otherSpec = Specification<TestEntity>.Create(e => e.Value > 5);
        var entity = new TestEntity { Value = 3 };

        // Act
        var combinedSpec = SpecificationCombinators.And(allSpec, otherSpec);
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse(); // Because otherSpec is not satisfied
    }

    [Fact]
    public void None_OrWithOther_ShouldReturnOther()
    {
        // Arrange
        var noneSpec = SpecificationCombinators.None<TestEntity>();
        var otherSpec = Specification<TestEntity>.Create(e => e.Value > 5);
        var entity = new TestEntity { Value = 10 };

        // Act
        var combinedSpec = SpecificationCombinators.Or(noneSpec, otherSpec);
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue(); // Because otherSpec is satisfied
    }

    #endregion

    #region [ Conditional Combinators Tests ]

    [Fact]
    public void If_ConditionTrue_ShouldApplySpecification()
    {
        // Arrange
        var spec = Specification<TestEntity>.Create(e => e.Value > 10);
        var entity = new TestEntity { Value = 5 };

        // Act
        var result = SpecificationCombinators.If(true, spec);

        // Assert
        result.IsSatisfiedBy(entity).Should().BeFalse();
    }

    [Fact]
    public void If_ConditionFalse_ShouldReturnAll()
    {
        // Arrange
        var spec = Specification<TestEntity>.Create(e => e.Value > 10);
        var entity = new TestEntity { Value = 5 };

        // Act
        var result = SpecificationCombinators.If(false, spec);

        // Assert
        result.IsSatisfiedBy(entity).Should().BeTrue(); // All() always returns true
    }

    [Fact]
    public void If_WithExpressionAndConditionTrue_ShouldApplyExpression()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.Value > 10;
        var entity = new TestEntity { Value = 5 };

        // Act
        var result = SpecificationCombinators.If(true, expression);

        // Assert
        result.IsSatisfiedBy(entity).Should().BeFalse();
    }

    [Fact]
    public void If_WithExpressionAndConditionFalse_ShouldReturnAll()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.Value > 10;
        var entity = new TestEntity { Value = 5 };

        // Act
        var result = SpecificationCombinators.If(false, expression);

        // Assert
        result.IsSatisfiedBy(entity).Should().BeTrue();
    }

    [Fact]
    public void IfElse_ConditionTrue_ShouldReturnIfTrueSpec()
    {
        // Arrange
        var ifTrueSpec = Specification<TestEntity>.Create(e => e.Value > 5);
        var ifFalseSpec = Specification<TestEntity>.Create(e => e.Value < 5);
        var entity = new TestEntity { Value = 10 };

        // Act
        var result = SpecificationCombinators.IfElse(true, ifTrueSpec, ifFalseSpec);

        // Assert
        result.IsSatisfiedBy(entity).Should().BeTrue();
    }

    [Fact]
    public void IfElse_ConditionFalse_ShouldReturnIfFalseSpec()
    {
        // Arrange
        var ifTrueSpec = Specification<TestEntity>.Create(e => e.Value > 100);
        var ifFalseSpec = Specification<TestEntity>.Create(e => e.Value > 5);
        var entity = new TestEntity { Value = 10 };

        // Act
        var result = SpecificationCombinators.IfElse(false, ifTrueSpec, ifFalseSpec);

        // Assert
        result.IsSatisfiedBy(entity).Should().BeTrue();
    }

    #endregion

    #region [ Argument Validation Tests ]

    [Fact]
    public void And_WithNullLeft_ShouldThrowArgumentNullException()
    {
        // Arrange
        var spec = Specification<TestEntity>.Create(e => e.Value > 5);

        // Act & Assert
        var act = () => SpecificationCombinators.And<TestEntity>(null!, spec);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void And_WithNullRight_ShouldThrowArgumentNullException()
    {
        // Arrange
        var spec = Specification<TestEntity>.Create(e => e.Value > 5);

        // Act & Assert
        var act = () => SpecificationCombinators.And(spec, (Specification<TestEntity>)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Or_WithNullLeft_ShouldThrowArgumentNullException()
    {
        // Arrange
        var spec = Specification<TestEntity>.Create(e => e.Value > 5);

        // Act & Assert
        var act = () => SpecificationCombinators.Or<TestEntity>(null!, spec);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Not_WithNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => SpecificationCombinators.Not<TestEntity>((Specification<TestEntity>)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromExpression_WithNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => SpecificationCombinators.FromExpression<TestEntity>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region [ Test Support Classes ]

    private class TestEntity
    {
        public int Value { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsActive { get; init; }
    }

    #endregion
}
