using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Domain.Specifications;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Async;

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class ReadOnlyRepositoryAsyncIntegrationTest(MongoDbIntegrationFixture fixture)
{
    private ReadOnlyRepositoryAsync<TestEntity> CreateRepository()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        return new ReadOnlyRepositoryAsync<TestEntity>(context, MongoDbIntegrationTestHelper.CreateRepositoryOptions());
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
    public async Task ListAnyAsync_And_ListCountAsync_ShouldReflectStoredEntities()
    {
        await SeedAsync(new TestEntity { Name = "A" }, new TestEntity { Name = "B" });
        ReadOnlyRepositoryAsync<TestEntity> repository = CreateRepository();

        (await repository.ListAnyAsync()).Should().BeTrue();
        (await repository.ListCountAsync()).Should().Be(2);
    }

    [DockerFact]
    public async Task GetByAsync_ShouldFilterEntities()
    {
        await SeedAsync(new TestEntity { Name = "Match" }, new TestEntity { Name = "Other" });
        ReadOnlyRepositoryAsync<TestEntity> repository = CreateRepository();

        (await repository.GetByAnyAsync(e => e.Name == "Match")).Should().BeTrue();
        (await repository.GetByCountAsync(e => e.Name == "Match")).Should().Be(1);
        IList<TestEntity> results = await repository.GetByAsync(e => e.Name == "Match");
        results.Should().ContainSingle().Which.Name.Should().Be("Match");
    }

    [DockerFact]
    public async Task GetByIdAsync_ShouldReturnEntity()
    {
        var entity = new TestEntity { Name = "ById" };
        await SeedAsync(entity);
        ReadOnlyRepositoryAsync<TestEntity> repository = CreateRepository();

        TestEntity? found = await repository.GetByIdAsync(entity.Id);

        found.Should().NotBeNull();
        found!.Name.Should().Be("ById");
    }

    [DockerFact]
    public async Task SpecificationMethods_ShouldFilterEntities()
    {
        await SeedAsync(new TestEntity { Name = "Active" }, new TestEntity { Name = "Inactive" });
        ReadOnlyRepositoryAsync<TestEntity> repository = CreateRepository();
        var spec = new ActiveNameSpecification("Active");

        (await repository.AnyBySpecificationAsync(spec)).Should().BeTrue();
        (await repository.CountBySpecificationAsync(spec)).Should().Be(1);
        (await repository.GetBySpecificationAsync(spec)).Should().ContainSingle();
        (await repository.GetSingleBySpecificationAsync(spec))!.Name.Should().Be("Active");
        (await repository.GetFirstBySpecificationAsync(spec))!.Name.Should().Be("Active");
    }

    [DockerFact]
    public async Task GetByKeysetPaginationAsync_ShouldReturnPages()
    {
        await SeedAsync(
            new TestEntity { Name = "A" },
            new TestEntity { Name = "B" },
            new TestEntity { Name = "C" });
        ReadOnlyRepositoryAsync<TestEntity> repository = CreateRepository();

        IKeysetPageResultString<TestEntity> firstPage = await repository.GetByKeysetPaginationAsync(
            clause: null,
            keySelector: e => e.Name,
            lastKey: null,
            pageSize: 2,
            ascending: true);

        firstPage.Items.Should().HaveCount(2);
        firstPage.HasMore.Should().BeTrue();

        IKeysetPageResultString<TestEntity> secondPage = await repository.GetByKeysetPaginationAsync(
            clause: null,
            keySelector: e => e.Name,
            lastKey: firstPage.LastKey,
            pageSize: 2,
            ascending: true);

        secondPage.Items.Should().NotBeEmpty();
    }

    [DockerFact]
    public async Task GetByKeysetPaginationAsync_WithSpecification_ShouldReturnFilteredPage()
    {
        await SeedAsync(
            new TestEntity { Name = "Keep-A" },
            new TestEntity { Name = "Keep-B" },
            new TestEntity { Name = "Skip" });
        ReadOnlyRepositoryAsync<TestEntity> repository = CreateRepository();
        var spec = new NamePrefixSpecification("Keep");

        IKeysetPageResult<TestEntity, ObjectId> page = await repository.GetByKeysetPaginationAsync(
            spec,
            keySelector: e => e.Id,
            lastKey: null,
            pageSize: 10,
            ascending: true);

        page.Items.Should().HaveCount(2);
        page.Items.Should().OnlyContain(e => e.Name.StartsWith("Keep", StringComparison.Ordinal));
    }

    [DockerFact]
    public async Task LoadRelationAsync_ShouldThrowNotSupported()
    {
        await SeedAsync(new TestEntity { Name = "Rel" });
        ReadOnlyRepositoryAsync<TestEntity> repository = CreateRepository();
        TestEntity entity = (await repository.ListAsync()).Single();

        Func<Task> act = () => repository.LoadRelationAsync(entity, e => e.Name);

        await act.Should().ThrowAsync<NotSupportedException>();
    }
}

internal sealed class ActiveNameSpecification(string name) : Specification<TestEntity>
{
    protected override Expression<Func<TestEntity, bool>> Criteria => entity => entity.Name == name;
}

internal sealed class NamePrefixSpecification(string prefix) : Specification<TestEntity>
{
    protected override Expression<Func<TestEntity, bool>> Criteria => entity => entity.Name.StartsWith(prefix);
}
