using MongoDB.Driver;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Async;

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class BulkOperationsRepositoryAsyncIntegrationTest(MongoDbIntegrationFixture fixture)
{
    private BulkOperationsRepositoryAsync<TestEntity> CreateRepository(string? databaseName = null)
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture, databaseName);
        return new BulkOperationsRepositoryAsync<TestEntity>(context, MongoDbIntegrationTestHelper.CreateRepositoryOptions());
    }

    private async Task CleanupAsync(string? databaseName = null)
    {
        IMongoCollection<TestEntity> collection = databaseName is null
            ? fixture.GetCollection<TestEntity>()
            : fixture.Client.GetDatabase(databaseName).GetCollection<TestEntity>(typeof(TestEntity).Name);

        await collection.DeleteManyAsync(FilterDefinition<TestEntity>.Empty);
    }

    private static List<TestEntity> CreateEntities(int count, string prefix)
    {
        return [.. Enumerable.Range(1, count).Select(i => new TestEntity { Name = $"{prefix}-{i}" })];
    }

    [DockerFact]
    public async Task BulkInsertAsync_ShouldInsertAllEntities()
    {
        await CleanupAsync();
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository();
        List<TestEntity> entities = CreateEntities(20, "BulkInsert");

        BulkOperationResult result = await repository.BulkInsertAsync(entities);

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().Be(20);
        (await repository.ListCountAsync()).Should().Be(20);
    }

    [DockerFact]
    public async Task BulkInsertAsync_WithMongoOptions_ShouldReportProgress()
    {
        await CleanupAsync();
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository();
        List<TestEntity> entities = CreateEntities(12, "BulkProgress");
        var progressReported = new List<(int processed, int total)>();
        var options = new MongoDbBulkOperationOptions
        {
            BatchSize = 4,
            IsOrdered = false,
            ProgressCallback = (processed, total) => progressReported.Add((processed, total))
        };

        BulkOperationResult result = await repository.BulkInsertAsync(entities, options);

        result.IsSuccess.Should().BeTrue();
        progressReported.Should().NotBeEmpty();
        progressReported.Last().total.Should().Be(12);
    }

    [DockerFact]
    public async Task BulkUpdateAsync_ShouldUpdateAllEntities()
    {
        await CleanupAsync();
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository();
        await repository.BulkInsertAsync(CreateEntities(8, "BulkUpdate"));
        IList<TestEntity> inserted = await repository.ListAsync();
        foreach (TestEntity entity in inserted)
        {
            entity.Name = $"Updated-{entity.Name}";
        }

        BulkOperationResult result = await repository.BulkUpdateAsync([.. inserted]);

        result.IsSuccess.Should().BeTrue();
        (await repository.ListAsync()).Should().OnlyContain(e => e.Name.StartsWith("Updated-", StringComparison.Ordinal));
    }

    [DockerFact]
    public async Task BulkDeleteAsync_ShouldDeleteAllEntities()
    {
        await CleanupAsync();
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository();
        await repository.BulkInsertAsync(CreateEntities(7, "BulkDelete"));
        IList<TestEntity> inserted = await repository.ListAsync();

        BulkOperationResult result = await repository.BulkDeleteAsync([.. inserted]);

        result.IsSuccess.Should().BeTrue();
        (await repository.ListCountAsync()).Should().Be(0);
    }

    [DockerFact]
    public async Task ExecuteUpdateAsync_ShouldUpdateMatchingEntities()
    {
        await CleanupAsync();
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository();
        await repository.BulkInsertAsync(
        [
            new TestEntity { Name = "Keep" },
            new TestEntity { Name = "UpdateMe" },
            new TestEntity { Name = "UpdateMe" }
        ]);

        int rowsAffected = await repository.ExecuteUpdateAsync(
            e => e.Name == "UpdateMe",
            e => e.Name,
            "Updated");

        rowsAffected.Should().Be(2);
        (await repository.GetByCountAsync(e => e.Name == "Updated")).Should().Be(2);
    }

    [DockerFact]
    public async Task ExecuteDeleteAsync_ShouldDeleteMatchingEntities()
    {
        await CleanupAsync();
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository();
        await repository.BulkInsertAsync(
        [
            new TestEntity { Name = "Keep" },
            new TestEntity { Name = "DeleteMe" }
        ]);

        int rowsAffected = await repository.ExecuteDeleteAsync(e => e.Name == "DeleteMe");

        rowsAffected.Should().Be(1);
        (await repository.ListCountAsync()).Should().Be(1);
    }

    [DockerFact]
    public async Task UpdateManyAsync_And_DeleteManyAsync_ShouldApplyFilterOperations()
    {
        await CleanupAsync();
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository();
        await repository.BulkInsertAsync(
        [
            new TestEntity { Name = "Status-Open" },
            new TestEntity { Name = "Status-Open" },
            new TestEntity { Name = "Status-Closed" }
        ]);

        long modified = await repository.UpdateManyAsync(
            e => e.Name == "Status-Open",
            Builders<TestEntity>.Update.Set(e => e.Name, "Status-Archived"));

        modified.Should().Be(2);

        long deleted = await repository.DeleteManyAsync(e => e.Name == "Status-Closed");
        deleted.Should().Be(1);
        (await repository.ListCountAsync()).Should().Be(2);
    }

    [DockerFact]
    public async Task BulkWriteAsync_ShouldExecuteMixedWriteModels()
    {
        string databaseName = $"bulkwrite_{Guid.NewGuid():N}";
        await CleanupAsync(databaseName);
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository(databaseName);
        var existing = new TestEntity { Name = "Existing" };
        await repository.AddAsync(existing);

        var replacement = new TestEntity { Id = existing.Id, Name = "Replaced" };
        var toInsert = new TestEntity { Name = "Inserted" };
        var toDelete = new TestEntity { Name = "DeleteTarget" };
        await repository.AddAsync(toDelete);

        IEnumerable<WriteModel<TestEntity>> writeModels =
        [
            new ReplaceOneModel<TestEntity>(
                Builders<TestEntity>.Filter.Eq(e => e.Id, existing.Id),
                replacement),
            new InsertOneModel<TestEntity>(toInsert),
            new DeleteOneModel<TestEntity>(
                Builders<TestEntity>.Filter.Eq(e => e.Id, toDelete.Id))
        ];

        MongoDbBulkOperationResult result = await repository.BulkWriteAsync(
            writeModels,
            MongoDbBulkOperationOptions.Default);

        result.IsSuccess.Should().BeTrue();
        result.InsertedCount.Should().BeGreaterThanOrEqualTo(1);
        result.DeletedCount.Should().BeGreaterThanOrEqualTo(1);
        (await repository.GetByIdAsync(existing.Id))!.Name.Should().Be("Replaced");
        (await repository.GetByCountAsync(e => e.Name == "DeleteTarget")).Should().Be(0);
    }

    [DockerFact]
    public async Task BulkInsertAsync_WithEmptyList_ShouldReturnZeroRows()
    {
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository();

        BulkOperationResult result = await repository.BulkInsertAsync([]);

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().Be(0);
    }

    [DockerFact]
    public async Task BulkInsertAsync_WithBulkOperationOptionsOverload_ShouldInsert()
    {
        await CleanupAsync();
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository();
        List<TestEntity> entities = CreateEntities(5, "OptionsOverload");
        var options = new BulkOperationOptions { BatchSize = 2 };

        BulkOperationResult result = await repository.BulkInsertAsync(entities, options);

        result.IsSuccess.Should().BeTrue();
        (await repository.ListCountAsync()).Should().Be(5);
    }

    [DockerFact]
    public async Task BulkUpdateAsync_WithBulkOperationOptionsOverload_ShouldUpdateAll()
    {
        await CleanupAsync();
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository();
        await repository.BulkInsertAsync(CreateEntities(6, "BulkUpdateOptions"));
        IList<TestEntity> inserted = await repository.ListAsync();
        foreach (TestEntity entity in inserted)
        {
            entity.Name = $"Updated-{entity.Name}";
        }

        BulkOperationResult result = await repository.BulkUpdateAsync([.. inserted], new BulkOperationOptions { BatchSize = 3 });

        result.IsSuccess.Should().BeTrue();
        (await repository.ListAsync()).Should().OnlyContain(e => e.Name.StartsWith("Updated-", StringComparison.Ordinal));
    }

    [DockerFact]
    public async Task BulkWriteAsync_WithEmptyRequests_ShouldReturnSuccessWithZeroCounts()
    {
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository();

        MongoDbBulkOperationResult result = await repository.BulkWriteAsync(
            [],
            MongoDbBulkOperationOptions.Default);

        result.IsSuccess.Should().BeTrue();
        result.InsertedCount.Should().Be(0);
        result.DeletedCount.Should().Be(0);
    }

    [DockerFact]
    public async Task BulkDeleteAsync_WithBulkOperationOptionsOverload_ShouldDeleteAll()
    {
        await CleanupAsync();
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository();
        await repository.BulkInsertAsync(CreateEntities(4, "BulkDeleteOptions"));
        IList<TestEntity> inserted = await repository.ListAsync();

        BulkOperationResult result = await repository.BulkDeleteAsync([.. inserted], new BulkOperationOptions { BatchSize = 2 });

        result.IsSuccess.Should().BeTrue();
        (await repository.ListCountAsync()).Should().Be(0);
    }

    [DockerFact]
    public async Task DeleteManyAsync_WithFilterDefinition_ShouldDeleteMatchingDocuments()
    {
        await CleanupAsync();
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository();
        await repository.BulkInsertAsync(
        [
            new TestEntity { Name = "FilterDelete-Keep" },
            new TestEntity { Name = "FilterDelete-Remove" }
        ]);

        long deleted = await repository.DeleteManyAsync(
            Builders<TestEntity>.Filter.Eq(e => e.Name, "FilterDelete-Remove"));

        deleted.Should().Be(1);
        (await repository.ListCountAsync()).Should().Be(1);
    }

    [DockerFact]
    public async Task ExecuteUpdateAsync_WithPropertyExpression_ShouldUpdateMatchingEntities()
    {
        await CleanupAsync();
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository();
        await repository.BulkInsertAsync(
        [
            new TestEntity { Name = "Prop-Keep" },
            new TestEntity { Name = "Prop-Update" }
        ]);

        int rowsAffected = await repository.ExecuteUpdateAsync(
            e => e.Name == "Prop-Update",
            e => e.Name,
            "Prop-Updated");

        rowsAffected.Should().Be(1);
        (await repository.GetByCountAsync(e => e.Name == "Prop-Updated")).Should().Be(1);
    }

    [DockerFact]
    public async Task ExecuteUpdateAsync_WithMultiPropertyExpression_ShouldUpdateFields()
    {
        await CleanupAsync();
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository();
        await repository.BulkInsertAsync([new TestEntity { Name = "Multi-Update" }]);

        int rowsAffected = await repository.ExecuteUpdateAsync(
            e => e.Name == "Multi-Update",
            s => s.SetProperty(e => e.Name, "Multi-Updated"));

        rowsAffected.Should().Be(1);
        (await repository.GetByCountAsync(e => e.Name == "Multi-Updated")).Should().Be(1);
    }

    [DockerFact]
    public async Task BulkInsertAsync_WithNullMongoOptions_ShouldThrow()
    {
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository();
        List<TestEntity> entities = CreateEntities(2, "NullOptions");

        Func<Task> act = () => repository.BulkInsertAsync(entities, (MongoDbBulkOperationOptions)null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [DockerFact]
    public async Task UpdateManyAsync_WithUpdateDefinition_ShouldModifyDocuments()
    {
        await CleanupAsync();
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository();
        await repository.BulkInsertAsync(
        [
            new TestEntity { Name = "Def-Open" },
            new TestEntity { Name = "Def-Closed" }
        ]);

        long modified = await repository.UpdateManyAsync(
            Builders<TestEntity>.Filter.Eq(e => e.Name, "Def-Closed"),
            Builders<TestEntity>.Update.Set(e => e.Name, "Def-Archived"));

        modified.Should().Be(1);
    }
}
