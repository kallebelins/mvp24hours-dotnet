using System.Linq.Expressions;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Domain.Specifications;
using Mvp24Hours.Infrastructure.Data.MongoDb.Specifications;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Extensions;

[Trait("Category", "Unit")]
public sealed class MongoDbSpecificationEvaluatorTest
{
    [Fact]
    public void GetQuery_ShouldApplyCriteriaOrderingAndPaging()
    {
        List<SampleEntity> entities =
        [
            new() { Id = 1, Name = "alpha", Active = true },
            new() { Id = 2, Name = "beta", Active = false },
            new() { Id = 3, Name = "gamma", Active = true }
        ];
        ActiveNameSpecification specification = new();
        MongoDbSpecificationEvaluator<SampleEntity> evaluator = new();

        var results = evaluator
            .GetQuery(entities.AsQueryable(), specification)
            .ToList();

        results.Should().ContainSingle(entity => entity.Name == "gamma");
    }

    [Fact]
    public void GetQuery_WithNullSpecification_ShouldThrow()
    {
        MongoDbSpecificationEvaluator<SampleEntity> evaluator = new();

        Action act = () => evaluator.GetQuery(
            Array.Empty<SampleEntity>().AsQueryable(),
            (ISpecificationQuery<SampleEntity>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class SampleEntity : IEntityBase
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool Active { get; set; }

        public object EntityKey => Id;
    }

    private sealed class ActiveNameSpecification : Specification<SampleEntity>
    {
        protected override Expression<Func<SampleEntity, bool>> Criteria =>
            entity => entity.Active;

        public ActiveNameSpecification()
        {
            AddOrderByDescending(entity => entity.Name);
            ApplyPaging(0, 1);
        }
    }
}
