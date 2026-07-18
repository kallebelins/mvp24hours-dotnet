//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Linq.Expressions;
using Mvp24Hours.Application.Specifications;
using Mvp24Hours.Core.Domain.Specifications;

namespace Mvp24Hours.Application.Test.Specifications;

/// <summary>
/// Unit tests for SpecificationCombinators static class functionality.
/// </summary>
[Trait("Category", "Unit")]
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
        Specification<TestEntity> combinedSpec = SpecificationCombinators.And(spec1, spec2);
        bool result = combinedSpec.IsSatisfiedBy(entity);

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
        Specification<TestEntity> combinedSpec = SpecificationCombinators.And(spec1, spec2);
        bool result = combinedSpec.IsSatisfiedBy(entity);

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
        Specification<TestEntity> combinedSpec = SpecificationCombinators.And(spec1, spec2);
        bool result = combinedSpec.IsSatisfiedBy(entity);

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
        Specification<TestEntity> combinedSpec = SpecificationCombinators.And(spec1, spec2);
        bool result = combinedSpec.IsSatisfiedBy(entity);

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
        Specification<TestEntity> combinedSpec = SpecificationCombinators.And(spec, expression);
        bool result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AndAll_AllSpecificationsSatisfied_ShouldReturnTrue()
    {
        // Arrange
        Specification<TestEntity>[] specs =
        [
            Specification<TestEntity>.Create(e => e.Value > 5),
            Specification<TestEntity>.Create(e => e.Name.Length > 2),
            Specification<TestEntity>.Create(e => e.IsActive)
        ];
        var entity = new TestEntity { Value = 10, Name = "Test", IsActive = true };

        // Act
        Specification<TestEntity> combinedSpec = SpecificationCombinators.AndAll(specs);
        bool result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AndAll_OneSpecificationNotSatisfied_ShouldReturnFalse()
    {
        // Arrange
        Specification<TestEntity>[] specs =
        [
            Specification<TestEntity>.Create(e => e.Value > 5),
            Specification<TestEntity>.Create(e => e.Name.Length > 10), // Not satisfied
            Specification<TestEntity>.Create(e => e.IsActive)
        ];
        var entity = new TestEntity { Value = 10, Name = "Test", IsActive = true };

        // Act
        Specification<TestEntity> combinedSpec = SpecificationCombinators.AndAll(specs);
        bool result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AndAll_EmptyCollection_ShouldReturnAll()
    {
        // Arrange
        Specification<TestEntity>[] specs = [];

        // Act
        Specification<TestEntity> combinedSpec = SpecificationCombinators.AndAll(specs);
        bool result = combinedSpec.IsSatisfiedBy(new TestEntity());

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
        Specification<TestEntity> combinedSpec = SpecificationCombinators.Or(spec1, spec2);
        bool result = combinedSpec.IsSatisfiedBy(entity);

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
        Specification<TestEntity> combinedSpec = SpecificationCombinators.Or(spec1, spec2);
        bool result = combinedSpec.IsSatisfiedBy(entity);

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
        Specification<TestEntity> combinedSpec = SpecificationCombinators.Or(spec1, spec2);
        bool result = combinedSpec.IsSatisfiedBy(entity);

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
        Specification<TestEntity> combinedSpec = SpecificationCombinators.Or(spec1, spec2);
        bool result = combinedSpec.IsSatisfiedBy(entity);

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
        Specification<TestEntity> combinedSpec = SpecificationCombinators.Or(spec, expression);
        bool result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void OrAll_AnySpecificationSatisfied_ShouldReturnTrue()
    {
        // Arrange
        Specification<TestEntity>[] specs =
        [
            Specification<TestEntity>.Create(e => e.Value > 100), // Not satisfied
            Specification<TestEntity>.Create(e => e.Name == "WrongName"), // Not satisfied
            Specification<TestEntity>.Create(e => e.IsActive) // Satisfied
        ];
        var entity = new TestEntity { Value = 10, Name = "Test", IsActive = true };

        // Act
        Specification<TestEntity> combinedSpec = SpecificationCombinators.OrAll(specs);
        bool result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void OrAll_NoSpecificationSatisfied_ShouldReturnFalse()
    {
        // Arrange
        Specification<TestEntity>[] specs =
        [
            Specification<TestEntity>.Create(e => e.Value > 100),
            Specification<TestEntity>.Create(e => e.Name == "WrongName"),
            Specification<TestEntity>.Create(e => !e.IsActive)
        ];
        var entity = new TestEntity { Value = 10, Name = "Test", IsActive = true };

        // Act
        Specification<TestEntity> combinedSpec = SpecificationCombinators.OrAll(specs);
        bool result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void OrAll_EmptyCollection_ShouldReturnNone()
    {
        // Arrange
        Specification<TestEntity>[] specs = [];

        // Act
        Specification<TestEntity> combinedSpec = SpecificationCombinators.OrAll(specs);
        bool result = combinedSpec.IsSatisfiedBy(new TestEntity());

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
        Specification<TestEntity> notSpec = SpecificationCombinators.Not(spec);
        bool result = notSpec.IsSatisfiedBy(entity);

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
        Specification<TestEntity> notSpec = SpecificationCombinators.Not(spec);
        bool result = notSpec.IsSatisfiedBy(entity);

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
        Specification<TestEntity> notSpec = SpecificationCombinators.Not(expression);
        bool result = notSpec.IsSatisfiedBy(entity);

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
        Specification<TestEntity> combinedSpec = SpecificationCombinators.Or(
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
        Specification<TestEntity> combinedSpec = SpecificationCombinators.And(
            SpecificationCombinators.And(spec1, spec2),
            spec3
        );
        bool result = combinedSpec.IsSatisfiedBy(entity);

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
        Specification<TestEntity> combinedSpec = SpecificationCombinators.Or(
            SpecificationCombinators.Or(spec1, spec2),
            spec3
        );
        bool result = combinedSpec.IsSatisfiedBy(entity);

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
        Specification<TestEntity> combinedSpec = SpecificationCombinators.And(spec1, spec2);

        // Act
        Expression<Func<TestEntity, bool>> expression = combinedSpec.IsSatisfiedByExpression;
        Func<TestEntity, bool> compiled = expression.Compile();

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
        filtered.Select(e => e.Name).Should().Contain(["B", "C"]);
    }

    #endregion

    #region [ Factory Methods Tests ]

    [Fact]
    public void FromExpression_WithExpression_ShouldCreateSpecification()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.Value > 5;

        // Act
        Specification<TestEntity> spec = SpecificationCombinators.FromExpression(expression);
        var entity = new TestEntity { Value = 10 };

        // Assert
        spec.IsSatisfiedBy(entity).Should().BeTrue();
    }

    [Fact]
    public void All_ShouldCreateSpecificationThatMatchesEverything()
    {
        // Act
        Specification<TestEntity> allSpec = SpecificationCombinators.All<TestEntity>();

        // Assert
        allSpec.IsSatisfiedBy(new TestEntity { Value = 0 }).Should().BeTrue();
        allSpec.IsSatisfiedBy(new TestEntity { Value = 100 }).Should().BeTrue();
        allSpec.IsSatisfiedBy(new TestEntity { Value = -50 }).Should().BeTrue();
    }

    [Fact]
    public void None_ShouldCreateSpecificationThatMatchesNothing()
    {
        // Act
        Specification<TestEntity> noneSpec = SpecificationCombinators.None<TestEntity>();

        // Assert
        noneSpec.IsSatisfiedBy(new TestEntity { Value = 0 }).Should().BeFalse();
        noneSpec.IsSatisfiedBy(new TestEntity { Value = 100 }).Should().BeFalse();
        noneSpec.IsSatisfiedBy(new TestEntity { Value = -50 }).Should().BeFalse();
    }

    [Fact]
    public void All_AndWithOther_ShouldReturnOther()
    {
        // Arrange
        Specification<TestEntity> allSpec = SpecificationCombinators.All<TestEntity>();
        var otherSpec = Specification<TestEntity>.Create(e => e.Value > 5);
        var entity = new TestEntity { Value = 3 };

        // Act
        Specification<TestEntity> combinedSpec = SpecificationCombinators.And(allSpec, otherSpec);
        bool result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse(); // Because otherSpec is not satisfied
    }

    [Fact]
    public void None_OrWithOther_ShouldReturnOther()
    {
        // Arrange
        Specification<TestEntity> noneSpec = SpecificationCombinators.None<TestEntity>();
        var otherSpec = Specification<TestEntity>.Create(e => e.Value > 5);
        var entity = new TestEntity { Value = 10 };

        // Act
        Specification<TestEntity> combinedSpec = SpecificationCombinators.Or(noneSpec, otherSpec);
        bool result = combinedSpec.IsSatisfiedBy(entity);

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
        Specification<TestEntity> result = SpecificationCombinators.If(true, spec);

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
        Specification<TestEntity> result = SpecificationCombinators.If(false, spec);

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
        Specification<TestEntity> result = SpecificationCombinators.If(true, expression);

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
        Specification<TestEntity> result = SpecificationCombinators.If(false, expression);

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
        Specification<TestEntity> result = SpecificationCombinators.IfElse(true, ifTrueSpec, ifFalseSpec);

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
        Specification<TestEntity> result = SpecificationCombinators.IfElse(false, ifTrueSpec, ifFalseSpec);

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
        Func<Specification<TestEntity>> act = () => SpecificationCombinators.And<TestEntity>(null!, spec);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void And_WithNullRight_ShouldThrowArgumentNullException()
    {
        // Arrange
        var spec = Specification<TestEntity>.Create(e => e.Value > 5);

        // Act & Assert
        Func<Specification<TestEntity>> act = () => SpecificationCombinators.And(spec, (Specification<TestEntity>)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Or_WithNullLeft_ShouldThrowArgumentNullException()
    {
        // Arrange
        var spec = Specification<TestEntity>.Create(e => e.Value > 5);

        // Act & Assert
        Func<Specification<TestEntity>> act = () => SpecificationCombinators.Or<TestEntity>(null!, spec);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Not_WithNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Func<Specification<TestEntity>> act = () => SpecificationCombinators.Not<TestEntity>((Specification<TestEntity>)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromExpression_WithNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Func<Specification<TestEntity>> act = () => SpecificationCombinators.FromExpression<TestEntity>(null!);
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
