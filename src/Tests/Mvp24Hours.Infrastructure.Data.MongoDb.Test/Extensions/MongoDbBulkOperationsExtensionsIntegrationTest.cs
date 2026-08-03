using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Extensions;

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class MongoDbBulkOperationsExtensionsIntegrationTest(MongoDbIntegrationFixture fixture)
{
    private Mvp24HoursContext CreateContext(string? databaseName = null)
        => MongoDbIntegrationTestHelper.CreateContext(fixture, databaseName);

    private async Task CleanupAsync(string? databaseName = null)
    {
        IMongoCollection<TestEntity> collection = databaseName is null
            ? fixture.GetCollection<TestEntity>()
            : fixture.Client.GetDatabase(databaseName).GetCollection<TestEntity>(typeof(TestEntity).Name);

        await collection.DeleteManyAsync(FilterDefinition<TestEntity>.Empty);
    }

    private static List<TestEntity> CreateEntities(int count, string prefix)
        => [.. Enumerable.Range(1, count).Select(i => new TestEntity { Name = $"{prefix}-{i}" })];

    [Fact]
    [Trait("Category", "Unit")]
    public void BulkInsertAsync_ShouldThrow_WhenContextIsNull()
    {
        Mvp24HoursContext? context = null;

        Func<Task> act = () => context!.BulkInsertAsync([new TestEntity { Name = "x" }]);

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [DockerFact]
    public async Task BulkInsertAsync_ShouldInsertAllEntities()
    {
        await CleanupAsync();
        Mvp24HoursContext context = CreateContext();
        List<TestEntity> entities = CreateEntities(15, "ExtInsert");

        BulkOperationResult result = await context.BulkInsertAsync(entities);

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().Be(15);
        (await context.Set<TestEntity>().CountDocumentsAsync(FilterDefinition<TestEntity>.Empty)).Should().Be(15);
    }

    [DockerFact]
    public async Task BulkInsertAsync_WithOptions_ShouldBatchAndReportProgress()
    {
        await CleanupAsync();
        Mvp24HoursContext context = CreateContext();
        List<TestEntity> entities = CreateEntities(10, "ExtProgress");
        var progress = new List<(int processed, int total)>();
        var options = new MongoDbBulkOperationOptions
        {
            BatchSize = 3,
            IsOrdered = false,
            ProgressCallback = (p, t) => progress.Add((p, t))
        };

        BulkOperationResult result = await context.BulkInsertAsync(entities, options);

        result.IsSuccess.Should().BeTrue();
        progress.Should().NotBeEmpty();
        progress.Last().total.Should().Be(10);
    }

    [DockerFact]
    public async Task BulkInsertAsync_WithEmptyList_ShouldReturnZero()
    {
        Mvp24HoursContext context = CreateContext();

        BulkOperationResult result = await context.BulkInsertAsync(new List<TestEntity>());

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().Be(0);
    }

    [DockerFact]
    public async Task BulkUpdateAsync_ShouldReplaceMatchingDocuments()
    {
        await CleanupAsync();
        Mvp24HoursContext context = CreateContext();
        await context.BulkInsertAsync(CreateEntities(6, "ExtUpdate"));
        IList<TestEntity> inserted = await context.Set<TestEntity>().Find(FilterDefinition<TestEntity>.Empty).ToListAsync();
        foreach (TestEntity entity in inserted)
        {
            entity.Name = $"Updated-{entity.Name}";
        }

        BulkOperationResult result = await context.BulkUpdateAsync([.. inserted], e => e.Id);

        result.IsSuccess.Should().BeTrue();
        (await context.Set<TestEntity>().Find(e => e.Name.StartsWith("Updated-")).CountDocumentsAsync()).Should().Be(6);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void BulkUpdateAsync_ShouldThrow_WhenKeySelectorIsNull()
    {
        Mvp24HoursContext context = CreateContext();

        Func<Task> act = () => context.BulkUpdateAsync([new TestEntity()], null!);

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [DockerFact]
    public async Task BulkDeleteAsync_ShouldRemoveMatchingDocuments()
    {
        await CleanupAsync();
        Mvp24HoursContext context = CreateContext();
        await context.BulkInsertAsync(CreateEntities(5, "ExtDelete"));
        IList<TestEntity> inserted = await context.Set<TestEntity>().Find(FilterDefinition<TestEntity>.Empty).ToListAsync();

        BulkOperationResult result = await context.BulkDeleteAsync([.. inserted], e => e.Id);

        result.IsSuccess.Should().BeTrue();
        (await context.Set<TestEntity>().CountDocumentsAsync(FilterDefinition<TestEntity>.Empty)).Should().Be(0);
    }

    [DockerFact]
    public async Task UpdateManyAsync_ShouldUpdateFilteredDocuments()
    {
        await CleanupAsync();
        Mvp24HoursContext context = CreateContext();
        await context.BulkInsertAsync(
        [
            new TestEntity { Name = "Status-Open" },
            new TestEntity { Name = "Status-Open" },
            new TestEntity { Name = "Status-Closed" }
        ]);

        long modified = await context.UpdateManyAsync(
            e => e.Name == "Status-Open",
            Builders<TestEntity>.Update.Set(e => e.Name, "Status-Archived"));

        modified.Should().Be(2);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UpdateManyAsync_ShouldThrow_WhenFilterIsNull()
    {
        Mvp24HoursContext context = CreateContext();

        Func<Task> act = () => context.UpdateManyAsync<TestEntity>(null!, Builders<TestEntity>.Update.Set(e => e.Name, "x"));

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [DockerFact]
    public async Task DeleteManyAsync_ShouldDeleteFilteredDocuments()
    {
        await CleanupAsync();
        Mvp24HoursContext context = CreateContext();
        await context.BulkInsertAsync(
        [
            new TestEntity { Name = "Keep" },
            new TestEntity { Name = "RemoveMe" }
        ]);

        long deleted = await context.DeleteManyAsync<TestEntity>(e => e.Name == "RemoveMe");

        deleted.Should().Be(1);
        (await context.Set<TestEntity>().CountDocumentsAsync(FilterDefinition<TestEntity>.Empty)).Should().Be(1);
    }

    [DockerFact]
    public async Task BulkWriteAsync_ShouldExecuteMixedOperations()
    {
        string databaseName = $"bulkext_{Guid.NewGuid():N}";
        await CleanupAsync(databaseName);
        Mvp24HoursContext context = CreateContext(databaseName);
        var existing = new TestEntity { Name = "Existing" };
        await context.Set<TestEntity>().InsertOneAsync(existing);

        var replacement = new TestEntity { Id = existing.Id, Name = "Replaced" };
        var toInsert = new TestEntity { Name = "InsertedViaBulkWrite" };
        var toDelete = new TestEntity { Name = "DeleteTarget" };
        await context.Set<TestEntity>().InsertOneAsync(toDelete);

        IEnumerable<WriteModel<TestEntity>> models =
        [
            new ReplaceOneModel<TestEntity>(Builders<TestEntity>.Filter.Eq(e => e.Id, existing.Id), replacement),
            new InsertOneModel<TestEntity>(toInsert),
            new DeleteOneModel<TestEntity>(Builders<TestEntity>.Filter.Eq(e => e.Id, toDelete.Id))
        ];

        MongoDbBulkOperationResult result = await context.BulkWriteAsync(models);

        result.IsSuccess.Should().BeTrue();
        result.InsertedCount.Should().BeGreaterThanOrEqualTo(1);
        result.DeletedCount.Should().BeGreaterThanOrEqualTo(1);
        (await context.Set<TestEntity>().Find(e => e.Id == existing.Id).FirstAsync()).Name.Should().Be("Replaced");
    }

    [DockerFact]
    public async Task BulkWriteAsync_WithEmptyRequests_ShouldReturnSuccess()
    {
        Mvp24HoursContext context = CreateContext();

        MongoDbBulkOperationResult result = await context.BulkWriteAsync(new List<WriteModel<TestEntity>>());

        result.IsSuccess.Should().BeTrue();
        result.InsertedCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void SetLogger_ShouldAllowLoggingDuringBulkOperations()
    {
        MongoDbBulkOperationsExtensions.SetLogger(NullLogger.Instance);
    }
}
