using MongoDB.Bson;
using MongoDB.Driver;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.SchemaValidation;
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

    [DockerFact]
    public async Task BulkInsertAsync_Unordered_WithDuplicateKey_ShouldReturnPartialSuccess()
    {
        string databaseName = $"bulk_partial_{Guid.NewGuid():N}";
        await CleanupAsync(databaseName);
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository(databaseName);
        IMongoCollection<TestEntity> collection = fixture.Client.GetDatabase(databaseName).GetCollection<TestEntity>(typeof(TestEntity).Name);

        var existingId = ObjectId.GenerateNewId();
        await collection.InsertOneAsync(new TestEntity { Id = existingId, Name = "Existing" });
        long initialCount = await collection.CountDocumentsAsync(FilterDefinition<TestEntity>.Empty);

        List<TestEntity> entities =
        [
            new TestEntity { Name = "Partial-First" },
            new TestEntity { Id = existingId, Name = "Partial-Duplicate" },
            new TestEntity { Name = "Partial-Second" }
        ];

        BulkOperationResult result = await repository.BulkInsertAsync(
            entities,
            new MongoDbBulkOperationOptions { IsOrdered = false, UseTransaction = false, BatchSize = 10 });

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().BeGreaterThan(0);

        await WaitUntilAsync(
            async () => await collection.CountDocumentsAsync(FilterDefinition<TestEntity>.Empty) >= initialCount + 2,
            TimeSpan.FromSeconds(10));

        long finalCount = await collection.CountDocumentsAsync(FilterDefinition<TestEntity>.Empty);
        finalCount.Should().Be(initialCount + 2);
        (await repository.GetByCountAsync(e => e.Name == "Partial-First")).Should().Be(1);
        (await repository.GetByCountAsync(e => e.Name == "Partial-Second")).Should().Be(1);
    }

    [DockerFact]
    public async Task BulkInsertAsync_Ordered_WithDuplicateKey_ShouldReturnFailure()
    {
        string databaseName = $"bulk_ordered_{Guid.NewGuid():N}";
        await CleanupAsync(databaseName);
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository(databaseName);
        IMongoCollection<TestEntity> collection = fixture.Client.GetDatabase(databaseName).GetCollection<TestEntity>(typeof(TestEntity).Name);

        var existingId = ObjectId.GenerateNewId();
        await collection.InsertOneAsync(new TestEntity { Id = existingId, Name = "Existing" });
        long initialCount = await collection.CountDocumentsAsync(FilterDefinition<TestEntity>.Empty);

        List<TestEntity> entities =
        [
            new TestEntity { Name = "Ordered-First" },
            new TestEntity { Id = existingId, Name = "Ordered-Duplicate" },
            new TestEntity { Name = "Ordered-Second" }
        ];

        BulkOperationResult result = await repository.BulkInsertAsync(
            entities,
            new MongoDbBulkOperationOptions { IsOrdered = true, UseTransaction = false, BatchSize = 10 });

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("partially failed");

        long finalCount = await collection.CountDocumentsAsync(FilterDefinition<TestEntity>.Empty);
        finalCount.Should().BeLessThan(initialCount + 3);
        (await repository.GetByCountAsync(e => e.Name == "Ordered-Second")).Should().Be(0);
    }

    [DockerFact]
    public async Task BulkWriteAsync_Unordered_WithDuplicateKey_ShouldReturnPartialSuccess()
    {
        string databaseName = $"bulkwrite_partial_{Guid.NewGuid():N}";
        await CleanupAsync(databaseName);
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository(databaseName);
        IMongoCollection<TestEntity> collection = fixture.Client.GetDatabase(databaseName).GetCollection<TestEntity>(typeof(TestEntity).Name);

        var existingId = ObjectId.GenerateNewId();
        await collection.InsertOneAsync(new TestEntity { Id = existingId, Name = "Existing" });
        long initialCount = await collection.CountDocumentsAsync(FilterDefinition<TestEntity>.Empty);

        IEnumerable<WriteModel<TestEntity>> writeModels =
        [
            new InsertOneModel<TestEntity>(new TestEntity { Name = "Write-First" }),
            new InsertOneModel<TestEntity>(new TestEntity { Id = existingId, Name = "Write-Duplicate" }),
            new InsertOneModel<TestEntity>(new TestEntity { Name = "Write-Second" })
        ];

        MongoDbBulkOperationResult result = await repository.BulkWriteAsync(
            writeModels,
            new MongoDbBulkOperationOptions { IsOrdered = false, UseTransaction = false });

        result.IsSuccess.Should().BeTrue();
        result.InsertedCount.Should().BeGreaterThan(0);

        await WaitUntilAsync(
            async () => await collection.CountDocumentsAsync(FilterDefinition<TestEntity>.Empty) >= initialCount + 2,
            TimeSpan.FromSeconds(10));

        (await repository.GetByCountAsync(e => e.Name == "Write-First")).Should().Be(1);
        (await repository.GetByCountAsync(e => e.Name == "Write-Second")).Should().Be(1);
    }

    [DockerFact]
    public async Task BulkWriteAsync_Ordered_WithDuplicateKey_ShouldReturnFailure()
    {
        string databaseName = $"bulkwrite_ordered_{Guid.NewGuid():N}";
        await CleanupAsync(databaseName);
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository(databaseName);
        IMongoCollection<TestEntity> collection = fixture.Client.GetDatabase(databaseName).GetCollection<TestEntity>(typeof(TestEntity).Name);

        var existingId = ObjectId.GenerateNewId();
        await collection.InsertOneAsync(new TestEntity { Id = existingId, Name = "Existing" });

        IEnumerable<WriteModel<TestEntity>> writeModels =
        [
            new InsertOneModel<TestEntity>(new TestEntity { Name = "WriteOrdered-First" }),
            new InsertOneModel<TestEntity>(new TestEntity { Id = existingId, Name = "WriteOrdered-Duplicate" }),
            new InsertOneModel<TestEntity>(new TestEntity { Name = "WriteOrdered-Second" })
        ];

        MongoDbBulkOperationResult result = await repository.BulkWriteAsync(
            writeModels,
            new MongoDbBulkOperationOptions { IsOrdered = true, UseTransaction = false });

        result.IsSuccess.Should().BeFalse();
        result.WriteErrorCount.Should().BeGreaterThan(0);
        (await repository.GetByCountAsync(e => e.Name == "WriteOrdered-Second")).Should().Be(0);
    }

    [DockerFact]
    public async Task BulkInsertAsync_WithoutBypassDocumentValidation_ShouldFailOnInvalidDocuments()
    {
        string databaseName = $"bulk_validation_{Guid.NewGuid():N}";
        await SetupValidatedTestEntityCollectionAsync(databaseName);
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository(databaseName);

        BulkOperationResult result = await repository.BulkInsertAsync(
            [new TestEntity { Name = string.Empty }],
            new MongoDbBulkOperationOptions { BypassDocumentValidation = false, UseTransaction = false });

        result.IsSuccess.Should().BeFalse();
        (await repository.ListCountAsync()).Should().Be(0);
    }

    [DockerFact]
    public async Task BulkInsertAsync_WithBypassDocumentValidation_ShouldInsertInvalidDocuments()
    {
        string databaseName = $"bulk_bypass_{Guid.NewGuid():N}";
        await SetupValidatedTestEntityCollectionAsync(databaseName);
        BulkOperationsRepositoryAsync<TestEntity> repository = CreateRepository(databaseName);

        BulkOperationResult result = await repository.BulkInsertAsync(
            [new TestEntity { Name = string.Empty }],
            new MongoDbBulkOperationOptions { BypassDocumentValidation = true, UseTransaction = false });

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().Be(1);

        await WaitUntilAsync(async () => await repository.ListCountAsync() == 1, TimeSpan.FromSeconds(10));
        (await repository.ListAsync()).Should().ContainSingle().Which.Name.Should().BeEmpty();
    }

    private async Task SetupValidatedTestEntityCollectionAsync(string databaseName)
    {
        IMongoDatabase database = fixture.Client.GetDatabase(databaseName);
        await database.DropCollectionAsync(typeof(TestEntity).Name);

        BsonDocument schema = new JsonSchemaBuilder()
            .WithBsonType("object")
            .WithRequired("Name")
            .WithProperty("Name", p => p.WithBsonType("string").WithMinLength(1))
            .Build();

        var validationService = new MongoDbSchemaValidationService(database);
        await validationService.CreateCollectionWithValidationAsync(typeof(TestEntity).Name, schema);
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(50);
        }

        if (!await condition())
        {
            throw new TimeoutException($"Condition was not met within {timeout.TotalSeconds} seconds.");
        }
    }
}
