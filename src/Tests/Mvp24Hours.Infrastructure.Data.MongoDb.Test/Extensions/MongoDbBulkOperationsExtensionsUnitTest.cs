using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Extensions;

[Trait("Category", "Unit")]
public class MongoDbBulkOperationsExtensionsUnitTest
{
    [Fact]
    public async Task BulkInsertAsync_WithNullEntities_ShouldReturnZeroRows()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();

        BulkOperationResult result = await context.BulkInsertAsync((IList<TestEntity>)null!);

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().Be(0);
    }

    [Fact]
    public async Task BulkInsertAsync_WithEmptyEntities_ShouldReturnZeroRows()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();

        BulkOperationResult result = await context.BulkInsertAsync(new List<TestEntity>());

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().Be(0);
    }

    [Fact]
    public void BulkUpdateAsync_WithNullContext_ShouldThrowArgumentNullException()
    {
        Mvp24HoursContext? context = null;

        Func<Task> act = () => context!.BulkUpdateAsync([new TestEntity()], e => e.Id);

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void BulkUpdateAsync_WithNullKeySelector_ShouldThrowArgumentNullException()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();

        Func<Task> act = () => context.BulkUpdateAsync([new TestEntity()], null!);

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task BulkUpdateAsync_WithEmptyEntities_ShouldReturnZeroRows()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();

        BulkOperationResult result = await context.BulkUpdateAsync(new List<TestEntity>(), e => e.Id);

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().Be(0);
    }

    [Fact]
    public void BulkDeleteAsync_WithNullContext_ShouldThrowArgumentNullException()
    {
        Mvp24HoursContext? context = null;

        Func<Task> act = () => context!.BulkDeleteAsync([new TestEntity()], e => e.Id);

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void BulkDeleteAsync_WithNullKeySelector_ShouldThrowArgumentNullException()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();

        Func<Task> act = () => context.BulkDeleteAsync([new TestEntity()], null!);

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task BulkDeleteAsync_WithNullEntities_ShouldReturnZeroRows()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();

        BulkOperationResult result = await context.BulkDeleteAsync((IList<TestEntity>)null!, e => e.Id);

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().Be(0);
    }

    [Fact]
    public void UpdateManyAsync_WithNullUpdate_ShouldThrowArgumentNullException()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();

        Func<Task> act = () => context.UpdateManyAsync<TestEntity>(_ => true, null!);

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void DeleteManyAsync_WithNullContext_ShouldThrowArgumentNullException()
    {
        Mvp24HoursContext? context = null;

        Func<Task> act = () => context!.DeleteManyAsync<TestEntity>(_ => true);

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void BulkWriteAsync_WithNullRequests_ShouldThrowArgumentNullException()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();

        Func<Task> act = () => context.BulkWriteAsync<TestEntity>(null!);

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void BulkInsertAsync_WithNullContext_ShouldThrowArgumentNullException()
    {
        Mvp24HoursContext? context = null;

        Func<Task> act = () => context!.BulkInsertAsync([new TestEntity { Name = "x" }]);

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void UpdateManyAsync_WithNullFilter_ShouldThrowArgumentNullException()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();

        Func<Task> act = () => context.UpdateManyAsync<TestEntity>(
            null!,
            Builders<TestEntity>.Update.Set(e => e.Name, "x"));

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void SetLogger_ShouldAllowLoggingDuringBulkOperations()
    {
        MongoDbBulkOperationsExtensions.SetLogger(NullLogger.Instance);
    }

    [Fact]
    public async Task BulkWriteAsync_WithEmptyRequests_ShouldReturnSuccess()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();

        MongoDbBulkOperationResult result = await context.BulkWriteAsync<TestEntity>([]);

        result.IsSuccess.Should().BeTrue();
        result.InsertedCount.Should().Be(0);
    }
}

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class MongoDbBulkOperationsExtensionsErrorIntegrationTest(MongoDbIntegrationFixture fixture)
{
    private Mvp24HoursContext CreateContext(string? databaseName = null)
    {
        return MongoDbIntegrationTestHelper.CreateContext(fixture, databaseName);
    }

    [DockerFact]
    public async Task BulkInsertAsync_WithNullEntities_ShouldReturnZeroRows()
    {
        Mvp24HoursContext context = CreateContext();

        BulkOperationResult result = await context.BulkInsertAsync((IList<TestEntity>)null!);

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().Be(0);
    }

    [DockerFact]
    public async Task BulkUpdateAsync_Ordered_WithMissingEntity_ShouldReturnSuccessWithZeroModified()
    {
        await CleanupAsync();
        Mvp24HoursContext context = CreateContext();
        var missing = new TestEntity { Id = MongoDB.Bson.ObjectId.GenerateNewId(), Name = "Missing" };

        BulkOperationResult result = await context.BulkUpdateAsync(
            [missing],
            e => e.Id,
            new MongoDbBulkOperationOptions { IsOrdered = true });

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().Be(0);
    }

    [DockerFact]
    public async Task BulkDeleteAsync_Unordered_WithMissingEntities_ShouldReturnPartialSuccess()
    {
        await CleanupAsync();
        Mvp24HoursContext context = CreateContext();
        var missingOne = new TestEntity { Id = MongoDB.Bson.ObjectId.GenerateNewId(), Name = "Missing-1" };
        var missingTwo = new TestEntity { Id = MongoDB.Bson.ObjectId.GenerateNewId(), Name = "Missing-2" };

        BulkOperationResult result = await context.BulkDeleteAsync(
            [missingOne, missingTwo],
            e => e.Id,
            new MongoDbBulkOperationOptions { IsOrdered = false });

        result.IsSuccess.Should().BeTrue();
    }

    [DockerFact]
    public async Task BulkWriteAsync_Ordered_WithInvalidOperation_ShouldReturnFailure()
    {
        string databaseName = $"ext_bulkwrite_{Guid.NewGuid():N}";
        Mvp24HoursContext context = CreateContext(databaseName);
        var existing = new TestEntity { Name = "Existing" };
        await context.Set<TestEntity>().InsertOneAsync(existing);
        var duplicate = new TestEntity { Id = existing.Id, Name = "Duplicate" };

        MongoDbBulkOperationResult result = await context.BulkWriteAsync<TestEntity>(
            [
                new InsertOneModel<TestEntity>(new TestEntity { Name = "First" }),
                new InsertOneModel<TestEntity>(duplicate),
                new InsertOneModel<TestEntity>(new TestEntity { Name = "Second" })
            ],
            new MongoDbBulkOperationOptions { IsOrdered = true });

        result.IsSuccess.Should().BeFalse();
        result.WriteErrorCount.Should().BeGreaterThan(0);
    }

    [DockerFact]
    public async Task UpdateManyAsync_WhenDatabaseUnavailable_ShouldThrow()
    {
        using var context = new Mvp24HoursContext($"missing_{Guid.NewGuid():N}", "mongodb://127.0.0.1:1");

        Func<Task> act = () => context.UpdateManyAsync<TestEntity>(
            _ => true,
            Builders<TestEntity>.Update.Set(e => e.Name, "x"));

        await act.Should().ThrowAsync<Exception>();
    }

    private async Task CleanupAsync()
    {
        IMongoCollection<TestEntity> collection = fixture.GetCollection<TestEntity>();
        await collection.DeleteManyAsync(FilterDefinition<TestEntity>.Empty);
    }
}
