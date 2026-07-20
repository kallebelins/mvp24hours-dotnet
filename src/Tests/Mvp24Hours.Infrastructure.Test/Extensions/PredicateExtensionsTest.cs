//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Linq.Expressions;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Infrastructure.Test.Extensions;

[Trait("Category", "Unit")]
public class PredicateExtensionsTest
{
    [Fact]
    public void True_ShouldEvaluateToTrue()
    {
        Expression<Func<TestEntity, bool>> predicate = PredicateExtensions.True<TestEntity>();
        Func<TestEntity, bool> compiled = predicate.Compile();

        compiled(new TestEntity { Active = false }).Should().BeTrue();
    }

    [Fact]
    public void False_ShouldEvaluateToFalse()
    {
        Expression<Func<TestEntity, bool>> predicate = PredicateExtensions.False<TestEntity>();
        Func<TestEntity, bool> compiled = predicate.Compile();

        compiled(new TestEntity { Active = true }).Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldReturnSamePredicate()
    {
        Expression<Func<TestEntity, bool>> source = entity => entity.Score > 10;

        Expression<Func<TestEntity, bool>> created = PredicateExtensions.Create(source);

        created.Compile()(new TestEntity { Score = 20 }).Should().BeTrue();
        created.Compile()(new TestEntity { Score = 5 }).Should().BeFalse();
    }

    [Fact]
    public void And_WithExpression_ShouldCombinePredicates()
    {
        Expression<Func<TestEntity, bool>> first = entity => entity.Active;
        Expression<Func<TestEntity, bool>> second = entity => entity.Score > 10;
        Func<TestEntity, bool> combined = first.And(second).Compile();

        combined(new TestEntity { Active = true, Score = 20 }).Should().BeTrue();
        combined(new TestEntity { Active = true, Score = 5 }).Should().BeFalse();
        combined(new TestEntity { Active = false, Score = 20 }).Should().BeFalse();
    }

    [Fact]
    public void And_WithSpecification_ShouldCombinePredicates()
    {
        Expression<Func<TestEntity, bool>> first = entity => entity.Score > 0;
        Func<TestEntity, bool> combined = first.And(new ActiveEntitySpecification()).Compile();

        combined(new TestEntity { Active = true, Score = 1 }).Should().BeTrue();
        combined(new TestEntity { Active = false, Score = 1 }).Should().BeFalse();
    }

    [Fact]
    public void And_WithSpecificationType_ShouldCombinePredicates()
    {
        Expression<Func<TestEntity, bool>> first = entity => entity.Score > 0;
        Func<TestEntity, bool> combined = first.And<TestEntity, ActiveEntitySpecification>().Compile();

        combined(new TestEntity { Active = true, Score = 1 }).Should().BeTrue();
        combined(new TestEntity { Active = false, Score = 1 }).Should().BeFalse();
    }

    [Fact]
    public void Or_WithExpression_ShouldCombinePredicates()
    {
        Expression<Func<TestEntity, bool>> first = entity => entity.Active;
        Expression<Func<TestEntity, bool>> second = entity => entity.Score > 10;
        Func<TestEntity, bool> combined = first.Or(second).Compile();

        combined(new TestEntity { Active = false, Score = 20 }).Should().BeTrue();
        combined(new TestEntity { Active = true, Score = 5 }).Should().BeTrue();
        combined(new TestEntity { Active = false, Score = 5 }).Should().BeFalse();
    }

    [Fact]
    public void Or_WithSpecification_ShouldCombinePredicates()
    {
        Expression<Func<TestEntity, bool>> first = entity => entity.Score > 100;
        Func<TestEntity, bool> combined = first.Or(new ActiveEntitySpecification()).Compile();

        combined(new TestEntity { Active = true, Score = 1 }).Should().BeTrue();
        combined(new TestEntity { Active = false, Score = 200 }).Should().BeTrue();
        combined(new TestEntity { Active = false, Score = 1 }).Should().BeFalse();
    }

    [Fact]
    public void Or_WithSpecificationType_ShouldCombinePredicates()
    {
        Expression<Func<TestEntity, bool>> first = entity => entity.Score > 100;
        Func<TestEntity, bool> combined = first.Or<TestEntity, ActiveEntitySpecification>().Compile();

        combined(new TestEntity { Active = true, Score = 1 }).Should().BeTrue();
        combined(new TestEntity { Active = false, Score = 200 }).Should().BeTrue();
    }

    [Fact]
    public void Not_ShouldNegatePredicate()
    {
        Expression<Func<TestEntity, bool>> source = entity => entity.Active;
        Func<TestEntity, bool> negated = source.Not().Compile();

        negated(new TestEntity { Active = true }).Should().BeFalse();
        negated(new TestEntity { Active = false }).Should().BeTrue();
    }

    private sealed class TestEntity
    {
        public bool Active { get; init; }

        public int Score { get; init; }
    }

    private sealed class ActiveEntitySpecification : ISpecificationQuery<TestEntity>
    {
        public Expression<Func<TestEntity, bool>> IsSatisfiedByExpression => entity => entity.Active;
    }
}
