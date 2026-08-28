//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson;
using MongoDB.Driver;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test;

/// <summary>
/// Behavior parity between the strongly-typed identifier members of
/// <see cref="IRepository{T, TId}"/> / <see cref="IRepositoryAsync{T, TId}"/> and the
/// <see cref="object"/>-based members they delegate to.
/// </summary>
[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class RepositoryTypedIdIntegrationTest(MongoDbIntegrationFixture fixture)
{
    private Repository<TestTypedEntity, ObjectId> CreateRepository()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        return new Repository<TestTypedEntity, ObjectId>(context, MongoDbIntegrationTestHelper.CreateRepositoryOptions());
    }

    private RepositoryAsync<TestTypedEntity, ObjectId> CreateRepositoryAsync()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        return new RepositoryAsync<TestTypedEntity, ObjectId>(context, MongoDbIntegrationTestHelper.CreateRepositoryOptions());
    }

    private async Task CleanupAsync()
    {
        IMongoCollection<TestTypedEntity> collection = fixture.GetCollection<TestTypedEntity>();
        await collection.DeleteManyAsync(FilterDefinition<TestTypedEntity>.Empty);
    }

    [DockerFact]
    public async Task GetById_WithTypedId_ReturnsSameEntityAsObjectOverload()
    {
        await CleanupAsync();
        Repository<TestTypedEntity, ObjectId> repository = CreateRepository();
        var entity = new TestTypedEntity { Name = "Typed-ById" };
        repository.Add(entity);

        TestTypedEntity? typed = repository.GetById(entity.Id);
        TestTypedEntity? untyped = ((IRepository<TestTypedEntity>)repository).GetById((object)entity.Id);

        typed.Should().NotBeNull();
        typed!.Id.Should().Be(entity.Id);
        typed.Name.Should().Be(untyped!.Name);
    }

    [DockerFact]
    public async Task RemoveById_WithTypedId_BehavesLikeObjectOverload()
    {
        await CleanupAsync();
        Repository<TestTypedEntity, ObjectId> repository = CreateRepository();
        var kept = new TestTypedEntity { Name = "Kept" };
        var removed = new TestTypedEntity { Name = "Removed" };
        repository.Add([kept, removed]);

        repository.RemoveById(removed.Id);

        repository.ListCount().Should().Be(1);
        repository.GetById(removed.Id).Should().BeNull();
        repository.GetById(kept.Id).Should().NotBeNull();
    }

    [DockerFact]
    public async Task RemoveById_WithTypedIdList_RemovesAll()
    {
        await CleanupAsync();
        Repository<TestTypedEntity, ObjectId> repository = CreateRepository();
        var first = new TestTypedEntity { Name = "One" };
        var second = new TestTypedEntity { Name = "Two" };
        repository.Add([first, second]);

        repository.RemoveById(new List<ObjectId> { first.Id, second.Id });

        repository.ListAny().Should().BeFalse();
    }

    [DockerFact]
    public async Task GetByIdAsync_WithTypedId_ReturnsSameEntityAsObjectOverload()
    {
        await CleanupAsync();
        RepositoryAsync<TestTypedEntity, ObjectId> repository = CreateRepositoryAsync();
        var entity = new TestTypedEntity { Name = "Typed-ByIdAsync" };
        await repository.AddAsync(entity);

        TestTypedEntity? typed = await repository.GetByIdAsync(entity.Id);
        TestTypedEntity? untyped = await ((IRepositoryAsync<TestTypedEntity>)repository).GetByIdAsync((object)entity.Id);

        typed.Should().NotBeNull();
        typed!.Id.Should().Be(entity.Id);
        typed.Name.Should().Be(untyped!.Name);
    }

    [DockerFact]
    public async Task RemoveByIdAsync_WithTypedId_BehavesLikeObjectOverload()
    {
        await CleanupAsync();
        RepositoryAsync<TestTypedEntity, ObjectId> repository = CreateRepositoryAsync();
        var kept = new TestTypedEntity { Name = "KeptAsync" };
        var removed = new TestTypedEntity { Name = "RemovedAsync" };
        await repository.AddAsync([kept, removed]);

        await repository.RemoveByIdAsync(removed.Id);

        (await repository.ListCountAsync()).Should().Be(1);
        (await repository.GetByIdAsync(removed.Id)).Should().BeNull();
        (await repository.GetByIdAsync(kept.Id)).Should().NotBeNull();
    }

    [DockerFact]
    public async Task RemoveByIdAsync_WithTypedIdList_RemovesAll()
    {
        await CleanupAsync();
        RepositoryAsync<TestTypedEntity, ObjectId> repository = CreateRepositoryAsync();
        var first = new TestTypedEntity { Name = "OneAsync" };
        var second = new TestTypedEntity { Name = "TwoAsync" };
        await repository.AddAsync([first, second]);

        await repository.RemoveByIdAsync(new List<ObjectId> { first.Id, second.Id });

        (await repository.ListAnyAsync()).Should().BeFalse();
    }
}
