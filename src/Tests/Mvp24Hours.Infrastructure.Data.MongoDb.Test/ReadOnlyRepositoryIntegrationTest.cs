using System.Linq.Expressions;
using MongoDB.Driver;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Domain.Specifications;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test;

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class ReadOnlyRepositoryIntegrationTest(MongoDbIntegrationFixture fixture)
{
    private ReadOnlyRepository<TestEntity> CreateRepository()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        return new ReadOnlyRepository<TestEntity>(context, MongoDbIntegrationTestHelper.CreateRepositoryOptions());
    }

    private async Task SeedAsync(params TestEntity[] entities)
    {
        IMongoCollection<TestEntity> collection = fixture.GetCollection<TestEntity>();
        await collection.DeleteManyAsync(FilterDefinition<TestEntity>.Empty);
        if (entities.Length > 0)
        {
            await collection.InsertManyAsync(entities);
        }
    }

    [DockerFact]
    public void ListAny_And_ListCount_ShouldReflectStoredEntities()
    {
        SeedAsync(new TestEntity { Name = "A" }, new TestEntity { Name = "B" }).GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();

        repository.ListAny().Should().BeTrue();
        repository.ListCount().Should().Be(2);
    }

    [DockerFact]
    public void GetBy_ShouldFilterEntities()
    {
        SeedAsync(new TestEntity { Name = "Match" }, new TestEntity { Name = "Other" }).GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();

        repository.GetByAny(e => e.Name == "Match").Should().BeTrue();
        repository.GetByCount(e => e.Name == "Match").Should().Be(1);
        repository.GetBy(e => e.Name == "Match").Should().ContainSingle().Which.Name.Should().Be("Match");
    }

    [DockerFact]
    public void GetById_ShouldReturnEntity()
    {
        var entity = new TestEntity { Name = "ById" };
        SeedAsync(entity).GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();

        TestEntity? found = repository.GetById(entity.Id);

        found.Should().NotBeNull();
        found!.Name.Should().Be("ById");
    }

    [DockerFact]
    public void SpecificationMethods_ShouldFilterEntities()
    {
        SeedAsync(new TestEntity { Name = "Active" }, new TestEntity { Name = "Inactive" }).GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();
        var spec = new SyncActiveNameSpecification("Active");

        repository.AnyBySpecification(spec).Should().BeTrue();
        repository.CountBySpecification(spec).Should().Be(1);
        repository.GetBySpecification(spec).Should().ContainSingle();
        repository.GetSingleBySpecification(spec)!.Name.Should().Be("Active");
        repository.GetFirstBySpecification(spec)!.Name.Should().Be("Active");
    }

    [DockerFact]
    public void GetByKeysetPagination_ShouldReturnPages()
    {
        SeedAsync(
            new TestEntity { Name = "A" },
            new TestEntity { Name = "B" },
            new TestEntity { Name = "C" }).GetAwaiter().GetResult();
        ReadOnlyRepository<TestEntity> repository = CreateRepository();

        IKeysetPageResultString<TestEntity> firstPage = repository.GetByKeysetPagination(
            clause: null,
            keySelector: e => e.Name,
            lastKey: null,
            pageSize: 2,
            ascending: true);

        firstPage.Items.Should().HaveCount(2);
        firstPage.HasMore.Should().BeTrue();

        IKeysetPageResultString<TestEntity> secondPage = repository.GetByKeysetPagination(
            clause: null,
            keySelector: e => e.Name,
            lastKey: firstPage.LastKey,
            pageSize: 2,
            ascending: true);

        secondPage.Items.Should().NotBeEmpty();
    }

    [DockerFact]
    public void LoadRelation_ShouldThrowNotSupportedException()
    {
        ReadOnlyRepository<TestEntity> repository = CreateRepository();
        var entity = new TestEntity { Name = "Relation" };

        Action act = () => repository.LoadRelation(entity, e => e.Name);

        act.Should().Throw<NotSupportedException>();
    }
}

internal sealed class SyncActiveNameSpecification(string name) : Specification<TestEntity>
{
    protected override Expression<Func<TestEntity, bool>> Criteria => entity => entity.Name == name;
}
