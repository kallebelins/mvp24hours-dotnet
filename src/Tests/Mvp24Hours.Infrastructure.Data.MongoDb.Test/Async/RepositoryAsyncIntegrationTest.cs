using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Async;

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class RepositoryAsyncIntegrationTest(MongoDbIntegrationFixture fixture)
{
    private RepositoryAsync<TestEntity> CreateRepository()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        return new RepositoryAsync<TestEntity>(context, MongoDbIntegrationTestHelper.CreateRepositoryOptions());
    }

    private async Task CleanupAsync()
    {
        IMongoCollection<TestEntity> collection = fixture.GetCollection<TestEntity>();
        await collection.DeleteManyAsync(FilterDefinition<TestEntity>.Empty);
    }

    [DockerFact]
    public async Task AddAsync_ShouldInsertSingleEntity()
    {
        await CleanupAsync();
        RepositoryAsync<TestEntity> repository = CreateRepository();
        var entity = new TestEntity { Name = "Alpha" };

        await repository.AddAsync(entity);

        (await repository.ListCountAsync()).Should().Be(1);
        TestEntity? found = await repository.GetByIdAsync(entity.Id);
        found.Should().NotBeNull();
        found!.Name.Should().Be("Alpha");
    }

    [DockerFact]
    public async Task AddAsync_ShouldInsertMultipleEntities()
    {
        await CleanupAsync();
        RepositoryAsync<TestEntity> repository = CreateRepository();
        var entities = new List<TestEntity>
        {
            new() { Name = "One" },
            new() { Name = "Two" },
            new() { Name = "Three" }
        };

        await repository.AddAsync(entities);

        (await repository.ListCountAsync()).Should().Be(3);
        (await repository.ListAnyAsync()).Should().BeTrue();
    }

    [DockerFact]
    public async Task ListAsync_ShouldReturnAllEntities()
    {
        await CleanupAsync();
        RepositoryAsync<TestEntity> repository = CreateRepository();
        await repository.AddAsync(new TestEntity { Name = "Listed" });

        IList<TestEntity> results = await repository.ListAsync();

        results.Should().ContainSingle();
        results[0].Name.Should().Be("Listed");
    }

    [DockerFact]
    public async Task GetByAsync_ShouldFilterByClause()
    {
        await CleanupAsync();
        RepositoryAsync<TestEntity> repository = CreateRepository();
        await repository.AddAsync(new TestEntity { Name = "Match" });
        await repository.AddAsync(new TestEntity { Name = "Other" });

        (await repository.GetByAnyAsync(e => e.Name == "Match")).Should().BeTrue();
        (await repository.GetByCountAsync(e => e.Name == "Match")).Should().Be(1);

        IList<TestEntity> filtered = await repository.GetByAsync(e => e.Name == "Match");
        filtered.Should().ContainSingle();
        filtered[0].Name.Should().Be("Match");
    }

    [DockerFact]
    public async Task ModifyAsync_ShouldReplaceExistingEntity()
    {
        await CleanupAsync();
        RepositoryAsync<TestEntity> repository = CreateRepository();
        var entity = new TestEntity { Name = "Before" };
        await repository.AddAsync(entity);

        entity.Name = "After";
        await repository.ModifyAsync(entity);

        TestEntity? updated = await repository.GetByIdAsync(entity.Id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("After");
    }

    [DockerFact]
    public async Task RemoveAsync_ShouldDeleteEntity()
    {
        await CleanupAsync();
        RepositoryAsync<TestEntity> repository = CreateRepository();
        var entity = new TestEntity { Name = "ToRemove" };
        await repository.AddAsync(entity);

        await repository.RemoveAsync(entity);

        (await repository.ListCountAsync()).Should().Be(0);
        (await repository.GetByIdAsync(entity.Id)).Should().BeNull();
    }

    [DockerFact]
    public async Task RemoveByIdAsync_ShouldDeleteEntityByKey()
    {
        await CleanupAsync();
        RepositoryAsync<TestEntity> repository = CreateRepository();
        var entity = new TestEntity { Name = "ById" };
        await repository.AddAsync(entity);

        await repository.RemoveByIdAsync(entity.Id);

        (await repository.ListCountAsync()).Should().Be(0);
    }

    [DockerFact]
    public async Task ListAsync_WithPagingCriteria_ShouldReturnSubset()
    {
        await CleanupAsync();
        RepositoryAsync<TestEntity> repository = CreateRepository();
        await repository.AddAsync(
        [
            new TestEntity { Name = "Page-1" },
            new TestEntity { Name = "Page-2" },
            new TestEntity { Name = "Page-3" }
        ]);

        var paging = new Mvp24Hours.Core.ValueObjects.Logic.PagingCriteria(limit: 2, offset: 0);
        IList<TestEntity> page = await repository.ListAsync(paging);

        page.Should().HaveCount(2);
        (await repository.ListCountAsync()).Should().Be(3);
    }

    [DockerFact]
    public async Task GetByAsync_WithPagingCriteria_ShouldReturnFilteredSubset()
    {
        await CleanupAsync();
        RepositoryAsync<TestEntity> repository = CreateRepository();
        await repository.AddAsync(
        [
            new TestEntity { Name = "Filter-A" },
            new TestEntity { Name = "Filter-A" },
            new TestEntity { Name = "Filter-B" }
        ]);

        var paging = new Mvp24Hours.Core.ValueObjects.Logic.PagingCriteria(limit: 1, offset: 0);
        IList<TestEntity> results = await repository.GetByAsync(e => e.Name == "Filter-A", paging);

        results.Should().ContainSingle();
        results[0].Name.Should().Be("Filter-A");
    }

    [DockerFact]
    public async Task ModifyAsync_ShouldUpdateMultipleEntities()
    {
        await CleanupAsync();
        RepositoryAsync<TestEntity> repository = CreateRepository();
        var entities = new List<TestEntity>
        {
            new() { Name = "Multi-1" },
            new() { Name = "Multi-2" }
        };
        await repository.AddAsync(entities);

        foreach (TestEntity entity in entities)
        {
            entity.Name = $"Updated-{entity.Name}";
        }

        await repository.ModifyAsync(entities);

        (await repository.ListAsync()).Should().OnlyContain(e => e.Name.StartsWith("Updated-", StringComparison.Ordinal));
    }

    [DockerFact]
    public async Task RemoveAsync_ShouldRemoveMultipleEntities()
    {
        await CleanupAsync();
        RepositoryAsync<TestEntity> repository = CreateRepository();
        var entities = new List<TestEntity>
        {
            new() { Name = "Remove-1" },
            new() { Name = "Remove-2" }
        };
        await repository.AddAsync(entities);

        await repository.RemoveAsync(entities);

        (await repository.ListCountAsync()).Should().Be(0);
    }

    [DockerFact]
    public async Task RemoveByIdAsync_WithUnknownId_ShouldNotThrow()
    {
        await CleanupAsync();
        RepositoryAsync<TestEntity> repository = CreateRepository();

        Func<Task> act = () => repository.RemoveByIdAsync(MongoDB.Bson.ObjectId.GenerateNewId());

        await act.Should().NotThrowAsync();
        (await repository.ListCountAsync()).Should().Be(0);
    }

    [DockerFact]
    public async Task RemoveByIdAsync_WithMultipleIds_ShouldRemoveAll()
    {
        await CleanupAsync();
        RepositoryAsync<TestEntity> repository = CreateRepository();
        var first = new TestEntity { Name = "BulkRemove-1" };
        var second = new TestEntity { Name = "BulkRemove-2" };
        await repository.AddAsync([first, second]);

        await repository.RemoveByIdAsync([first.Id, second.Id]);

        (await repository.ListCountAsync()).Should().Be(0);
    }

    [DockerFact]
    public async Task AddAsync_WithNullEntity_ShouldNotInsert()
    {
        await CleanupAsync();
        RepositoryAsync<TestEntity> repository = CreateRepository();

        await repository.AddAsync((TestEntity)null!);

        (await repository.ListCountAsync()).Should().Be(0);
    }

    [DockerFact]
    public async Task LoadRelationAsync_ShouldThrowNotSupportedException()
    {
        RepositoryAsync<TestEntity> repository = CreateRepository();
        var entity = new TestEntity { Name = "Relation" };

        Func<Task> act = () => repository.LoadRelationAsync(entity, e => e.Name);

        await act.Should().ThrowAsync<NotSupportedException>();
    }
}
