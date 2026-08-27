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
}
