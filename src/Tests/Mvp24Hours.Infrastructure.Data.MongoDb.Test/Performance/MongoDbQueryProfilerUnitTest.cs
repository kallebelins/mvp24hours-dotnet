using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Profiling;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Performance;

[Trait("Category", "Unit")]
public class MongoDbQueryProfilerUnitTest
{
    [Fact]
    public void Constructor_WithNullCollection_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new MongoDbQueryProfiler<TestEntity>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ExplainAsync_WhenRunCommandFails_ShouldRethrow()
    {
        Mock<IMongoCollection<TestEntity>> collectionMock = CreateCollectionMock(out Mock<IMongoDatabase> databaseMock);
        databaseMock
            .Setup(d => d.RunCommandAsync(
                It.IsAny<Command<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("explain failed"));

        var profiler = new MongoDbQueryProfiler<TestEntity>(collectionMock.Object, NullLogger<MongoDbQueryProfiler<TestEntity>>.Instance);

        Func<Task> act = () => profiler.ExplainAsync(_ => true);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("explain failed");
    }

    [Fact]
    public async Task ExplainAsync_WithSort_ShouldReturnParsedResult()
    {
        Mock<IMongoCollection<TestEntity>> collectionMock = CreateCollectionMock(out Mock<IMongoDatabase> databaseMock);
        BsonDocument explainOutput = CreateExplainOutput();
        databaseMock
            .Setup(d => d.RunCommandAsync(
                It.IsAny<Command<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(explainOutput);

        var profiler = new MongoDbQueryProfiler<TestEntity>(collectionMock.Object);

        QueryExplainResult result = await profiler.ExplainAsync(
            Builders<TestEntity>.Filter.Eq(e => e.Name, "Open"),
            Builders<TestEntity>.Sort.Ascending(e => e.Name),
            ExplainVerbosity.ExecutionStats);

        result.ExecutionSuccess.Should().BeTrue();
        result.IndexName.Should().Be("Name_1");
        result.DocumentsReturned.Should().Be(2);
        result.Efficiency.Should().BeApproximately(0.5, 0.01);
        result.RejectedPlansCount.Should().Be(1);
    }

    [Fact]
    public async Task ExplainAggregationAsync_ShouldReturnParsedResult()
    {
        Mock<IMongoCollection<TestEntity>> collectionMock = CreateCollectionMock(out Mock<IMongoDatabase> databaseMock);
        databaseMock
            .Setup(d => d.RunCommandAsync(
                It.IsAny<Command<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateExplainOutput());

        var profiler = new MongoDbQueryProfiler<TestEntity>(collectionMock.Object);
        var pipeline =
            PipelineDefinition<TestEntity, BsonDocument>.Create("{ $match: { Name: 'Open' } }");

        QueryExplainResult result = await profiler.ExplainAggregationAsync(pipeline);

        result.ExecutionTimeMs.Should().Be(5);
        result.UsedIndex.Should().BeTrue();
    }

    [Fact]
    public async Task ExplainAggregationAsync_WhenRunCommandFails_ShouldRethrow()
    {
        Mock<IMongoCollection<TestEntity>> collectionMock = CreateCollectionMock(out Mock<IMongoDatabase> databaseMock);
        databaseMock
            .Setup(d => d.RunCommandAsync(
                It.IsAny<Command<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("aggregation explain failed"));

        var profiler = new MongoDbQueryProfiler<TestEntity>(collectionMock.Object);

        var pipeline =
            PipelineDefinition<TestEntity, BsonDocument>.Create("{ $match: { Name: 'Open' } }");

        Func<Task> act = () => profiler.ExplainAggregationAsync(pipeline);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("aggregation explain failed");
    }

    [Fact]
    public async Task FindWithHintAsync_WithIndexName_ShouldReturnDocuments()
    {
        Mock<IMongoCollection<TestEntity>> collectionMock = CreateCollectionMock(out _);
        var items = new List<TestEntity> { new() { Name = "Hinted" } };
        collectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<TestEntity>>(),
                It.IsAny<FindOptions<TestEntity>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FakeAsyncCursor<TestEntity>(items));

        var profiler = new MongoDbQueryProfiler<TestEntity>(collectionMock.Object);

        List<TestEntity> results = await profiler.FindWithHintAsync(
            Builders<TestEntity>.Filter.Eq(e => e.Name, "Hinted"),
            "Name_1");

        results.Should().ContainSingle().Which.Name.Should().Be("Hinted");
    }

    [Fact]
    public async Task FindWithHintAsync_WithIndexKeys_ShouldReturnDocuments()
    {
        Mock<IMongoCollection<TestEntity>> collectionMock = CreateCollectionMock(out _);
        var items = new List<TestEntity> { new() { Name = "KeysHint" } };
        collectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<TestEntity>>(),
                It.IsAny<FindOptions<TestEntity>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FakeAsyncCursor<TestEntity>(items));

        var profiler = new MongoDbQueryProfiler<TestEntity>(collectionMock.Object);

        List<TestEntity> results = await profiler.FindWithHintAsync(
            Builders<TestEntity>.Filter.Empty,
            new BsonDocument("Name", 1));

        results.Should().ContainSingle();
    }

    [Fact]
    public async Task GetIndexesAsync_ShouldParseIndexMetadata()
    {
        Mock<IMongoCollection<TestEntity>> collectionMock = CreateCollectionMock(out _);
        var indexesMock = new Mock<IMongoIndexManager<TestEntity>>();
        indexesMock
            .Setup(i => i.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FakeAsyncCursor<BsonDocument>(
            [
                new BsonDocument
                {
                    { "name", "ttl_index" },
                    { "key", new BsonDocument("CreatedAt", 1) },
                    { "unique", true },
                    { "sparse", true },
                    { "background", true },
                    { "expireAfterSeconds", 3600L },
                    { "partialFilterExpression", new BsonDocument("Active", true) }
                }
            ]));
        collectionMock.SetupGet(c => c.Indexes).Returns(indexesMock.Object);

        var profiler = new MongoDbQueryProfiler<TestEntity>(collectionMock.Object);

        List<IndexInfo> indexes = await profiler.GetIndexesAsync();

        indexes.Should().ContainSingle();
        indexes[0].Name.Should().Be("ttl_index");
        indexes[0].Unique.Should().BeTrue();
        indexes[0].ExpireAfterSeconds.Should().Be(3600);
        indexes[0].PartialFilterExpression.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCollectionStatsAsync_ShouldParseStatsDocument()
    {
        Mock<IMongoCollection<TestEntity>> collectionMock = CreateCollectionMock(out Mock<IMongoDatabase> databaseMock);
        databaseMock
            .Setup(d => d.RunCommandAsync(
                It.IsAny<Command<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BsonDocument
            {
                { "ns", "db.TestEntity" },
                { "count", 10 },
                { "size", 1000 },
                { "avgObjSize", 100.5 },
                { "storageSize", 2048 },
                { "totalIndexSize", 512 },
                { "indexSizes", new BsonDocument("_id_", 256) },
                { "capped", false }
            });

        var profiler = new MongoDbQueryProfiler<TestEntity>(collectionMock.Object);

        CollectionStats stats = await profiler.GetCollectionStatsAsync();

        stats.Namespace.Should().Be("db.TestEntity");
        stats.Count.Should().Be(10);
        stats.AvgObjSize.Should().Be(100.5);
        stats.TotalIndexSize.Should().Be(512);
        stats.IndexSizes["_id_"].AsInt32.Should().Be(256);
    }

    private static Mock<IMongoCollection<TestEntity>> CreateCollectionMock(out Mock<IMongoDatabase> databaseMock)
    {
        databaseMock = new Mock<IMongoDatabase>();
        var collectionMock = new Mock<IMongoCollection<TestEntity>>();
        collectionMock.SetupGet(c => c.Database).Returns(databaseMock.Object);
        collectionMock.SetupGet(c => c.CollectionNamespace).Returns(new CollectionNamespace("db", "TestEntity"));
        collectionMock.SetupGet(c => c.DocumentSerializer)
            .Returns(MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry.GetSerializer<TestEntity>());
        collectionMock.SetupGet(c => c.Settings).Returns(new MongoCollectionSettings());
        return collectionMock;
    }

    private static BsonDocument CreateExplainOutput()
    {
        return new BsonDocument
        {
            {
                "queryPlanner", new BsonDocument
                {
                    {
                        "winningPlan", new BsonDocument
                        {
                            { "stage", "FETCH" },
                            {
                                "inputStage", new BsonDocument
                                {
                                    { "stage", "IXSCAN" },
                                    { "indexName", "Name_1" },
                                    { "indexBounds", new BsonDocument("Name", new BsonArray { "[\"Open\", \"Open\"]" }) }
                                }
                            }
                        }
                    },
                    { "rejectedPlans", new BsonArray { new BsonDocument() } }
                }
            },
            {
                "executionStats", new BsonDocument
                {
                    { "executionSuccess", true },
                    { "nReturned", 2 },
                    { "executionTimeMillis", 5 },
                    { "totalKeysExamined", 2 },
                    { "totalDocsExamined", 4 }
                }
            }
        };
    }
}

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class MongoDbQueryProfilerIntegrationTest(MongoDbIntegrationFixture fixture)
{
    [DockerFact]
    public async Task ExplainAsync_WithExpressionFilter_ShouldExecuteExplainCommand()
    {
        IMongoCollection<OrderDocument> collection = fixture.GetCollection<OrderDocument>("profiler_explain");
        await collection.DeleteManyAsync(FilterDefinition<OrderDocument>.Empty);
        await collection.InsertOneAsync(new OrderDocument { Status = "Open" });

        var profiler = new MongoDbQueryProfiler<OrderDocument>(collection);

        Func<Task> act = () => profiler.ExplainAsync(o => o.Status == "Open");

        await act.Should().ThrowAsync<MongoCommandException>();
    }
}
