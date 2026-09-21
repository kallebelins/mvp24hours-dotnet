//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test;

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class RepositoryIntegrationTest(MongoDbIntegrationFixture fixture)
{
    private Repository<TestEntity> CreateRepository()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        return new Repository<TestEntity>(context, MongoDbIntegrationTestHelper.CreateRepositoryOptions());
    }

    private async Task CleanupAsync()
    {
        IMongoCollection<TestEntity> collection = fixture.GetCollection<TestEntity>();
        await collection.DeleteManyAsync(FilterDefinition<TestEntity>.Empty);
    }

    [DockerFact]
    public async Task Add_ListWithMultipleEntities_InsertsAll()
    {
        await CleanupAsync();
        Repository<TestEntity> repository = CreateRepository();
        var entities = new List<TestEntity>
        {
            new() { Name = "One" },
            new() { Name = "Two" },
            new() { Name = "Three" }
        };

        repository.Add(entities);

        repository.ListCount().Should().Be(3);
        repository.ListAny().Should().BeTrue();
    }

    [DockerFact]
    public async Task Add_ListContainingNull_SkipsNullAndInsertsRest()
    {
        await CleanupAsync();
        Repository<TestEntity> repository = CreateRepository();
        var entities = new List<TestEntity>
        {
            new() { Name = "Kept-1" },
            null!,
            new() { Name = "Kept-2" }
        };

        repository.Add(entities);

        repository.ListCount().Should().Be(2);
    }

    [DockerFact]
    public async Task Add_EmptyList_DoesNotThrow()
    {
        await CleanupAsync();
        Repository<TestEntity> repository = CreateRepository();

        Action act = () => repository.Add([]);

        act.Should().NotThrow();
        repository.ListCount().Should().Be(0);
    }

    private Repository<TestEntityLogOfString> CreateEntityLogRepository()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        return new Repository<TestEntityLogOfString>(context, MongoDbIntegrationTestHelper.CreateRepositoryOptions());
    }

    private Repository<TestDateLogOnlyEntity> CreateDateLogOnlyRepository()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        return new Repository<TestDateLogOnlyEntity>(context, MongoDbIntegrationTestHelper.CreateRepositoryOptions());
    }

    private async Task CleanupEntityLogAsync()
    {
        IMongoCollection<TestEntityLogOfString> collection = fixture.GetCollection<TestEntityLogOfString>();
        await collection.DeleteManyAsync(FilterDefinition<TestEntityLogOfString>.Empty);
    }

    private async Task CleanupDateLogOnlyAsync()
    {
        IMongoCollection<TestDateLogOnlyEntity> collection = fixture.GetCollection<TestDateLogOnlyEntity>();
        await collection.DeleteManyAsync(FilterDefinition<TestDateLogOnlyEntity>.Empty);
    }

    [DockerFact]
    public async Task Remove_EntityWithEntityLogOfString_SetsRemovedAndDoesNotDeleteDocument()
    {
        await CleanupEntityLogAsync();
        Repository<TestEntityLogOfString> repository = CreateEntityLogRepository();
        var entity = new TestEntityLogOfString { Name = "Keep-Soft-Deleted", CreatedBy = "seed-user" };
        repository.Add(entity);

        repository.Remove(entity);

        repository.ListCount().Should().Be(1);
        TestEntityLogOfString? stored = repository.GetById(entity.Id);
        stored.Should().NotBeNull();
        stored!.Removed.Should().NotBeNull();
    }

    [DockerFact]
    public async Task Remove_EntityWithOnlyDateLog_SetsRemovedWithoutRemovedBy()
    {
        await CleanupDateLogOnlyAsync();
        Repository<TestDateLogOnlyEntity> repository = CreateDateLogOnlyRepository();
        var entity = new TestDateLogOnlyEntity { Name = "DateLogOnly" };
        repository.Add(entity);

        repository.Remove(entity);

        TestDateLogOnlyEntity? stored = repository.GetById(entity.Id);
        stored.Should().NotBeNull();
        stored!.Removed.Should().NotBeNull();
    }

    [DockerFact]
    public async Task Remove_EntityWithoutLog_DeletesDocument()
    {
        await CleanupAsync();
        Repository<TestEntity> repository = CreateRepository();
        var entity = new TestEntity { Name = "HardDeleteMe" };
        repository.Add(entity);

        repository.Remove(entity);

        repository.ListCount().Should().Be(0);
        repository.GetById(entity.Id).Should().BeNull();
    }

    [DockerFact]
    public async Task Remove_EntityWithEntityLog_WhenEntityLogByUnavailable_DoesNotThrow()
    {
        await CleanupEntityLogAsync();
        Repository<TestEntityLogOfString> repository = CreateEntityLogRepository();
        var entity = new TestEntityLogOfString { Name = "NoUserContext" };
        repository.Add(entity);

        Action act = () => repository.Remove(entity);

        act.Should().NotThrow();
        TestEntityLogOfString? stored = repository.GetById(entity.Id);
        stored!.RemovedBy.Should().BeNull();
    }

    [DockerFact]
    public async Task Modify_EntityWithEntityLogOfString_PreservesCreatedAndCreatedBy()
    {
        await CleanupEntityLogAsync();
        Repository<TestEntityLogOfString> repository = CreateEntityLogRepository();
        DateTime originalCreated = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var entity = new TestEntityLogOfString { Name = "Original", CreatedBy = "original-user", Created = originalCreated };
        repository.Add(entity);

        var updated = new TestEntityLogOfString
        {
            Id = entity.Id,
            Name = "Updated",
            CreatedBy = "someone-else",
            Created = default
        };
        repository.Modify(updated);

        TestEntityLogOfString? stored = repository.GetById(entity.Id);
        stored.Should().NotBeNull();
        stored!.Name.Should().Be("Updated");
        stored.CreatedBy.Should().Be("original-user");
        stored.Created.Should().Be(originalCreated);
    }
}
