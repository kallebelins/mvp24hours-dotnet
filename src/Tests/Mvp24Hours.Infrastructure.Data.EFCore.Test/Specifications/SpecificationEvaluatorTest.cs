using System.Linq.Expressions;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Infrastructure.Data.EFCore.Specifications;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Specifications;

[Trait("Category", "Unit")]
public class SpecificationEvaluatorTest
{
    [Fact]
    public void GetQuery_WithActiveSpecification_FiltersEntities()
    {
        var evaluator = new SpecificationEvaluator<TestEntity>();
        var entities = EfCoreTestHelpers.CreateEntities(4);
        entities[0].Active = false;
        entities[2].Active = false;

        using var context = EfCoreTestHelpers.CreateContext();
        context.Entities.AddRange(entities);
        context.SaveChanges();

        var spec = new ActiveEntitySpecification();
        var result = evaluator.GetQuery(context.Entities, spec).ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(e => e.Active);
    }

    [Fact]
    public void GetQuery_WithEnhancedSpecification_AppliesOrderSkipAndTake()
    {
        var evaluator = new SpecificationEvaluator<TestEntity>();
        var entities = new List<TestEntity>
        {
            new() { Name = "A", Active = true, Score = 10 },
            new() { Name = "B", Active = true, Score = 50 },
            new() { Name = "C", Active = false, Score = 40 },
            new() { Name = "D", Active = true, Score = 30 },
            new() { Name = "E", Active = true, Score = 20 }
        };

        using var context = EfCoreTestHelpers.CreateContext();
        context.Entities.AddRange(entities);
        context.SaveChanges();

        var spec = new TopActiveByScoreSpecification(skip: 1, take: 2);
        var result = evaluator.GetQuery(context.Entities, spec).ToList();

        result.Should().HaveCount(2);
        result.Select(e => e.Score).Should().Equal(30, 20);
    }

    private sealed class ActiveEntitySpecification : ISpecificationQuery<TestEntity>
    {
        public Expression<Func<TestEntity, bool>> IsSatisfiedByExpression =>
            entity => entity.Active;
    }

    private sealed class TopActiveByScoreSpecification : ISpecificationQueryEnhanced<TestEntity>
    {
        public TopActiveByScoreSpecification(int skip, int take)
        {
            Skip = skip;
            Take = take;
        }

        public Expression<Func<TestEntity, bool>> IsSatisfiedByExpression =>
            entity => entity.Active;

        public IReadOnlyList<Expression<Func<TestEntity, object>>> Includes { get; } =
            Array.Empty<Expression<Func<TestEntity, object>>>();

        public IReadOnlyList<string> IncludeStrings { get; } = Array.Empty<string>();

        public IReadOnlyList<(Expression<Func<TestEntity, object>> KeySelector, bool Descending)> OrderBy { get; } =
            [(entity => entity.Score, true)];

        public int? Take { get; }

        public int? Skip { get; }

        public bool IsPagingEnabled => Skip.HasValue || Take.HasValue;
    }
}
