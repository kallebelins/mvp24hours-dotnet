using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Moq;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Aggregation;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Performance;

[Trait("Category", "Unit")]
public class MongoDbAggregationPipelineTest
{
    [Fact]
    public void Create_WithNullCollection_ShouldThrowArgumentNullException()
    {
        Action act = () => MongoDbAggregationPipeline<PipelineOrder>.Create(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Pipeline_ShouldBuildAllStageTypes()
    {
        Mock<IMongoCollection<PipelineOrder>> collectionMock = CreateCollectionMock([]);
        MongoDbAggregationPipeline<PipelineOrder> pipeline = MongoDbAggregationPipeline<PipelineOrder>.Create(collectionMock.Object)
            .Match(o => o.Status == "Open")
            .Match(Builders<PipelineOrder>.Filter.Eq(o => o.Status, "Open"))
            .Project(Builders<PipelineOrder>.Projection.Include(o => o.Amount))
            .Project(new BsonDocument("amount", 1))
            .Group("$status", new BsonDocument("count", new BsonDocument("$sum", 1)))
            .Group(new BsonDocument("tenantId", "$tenantId"), new BsonDocument("total", new BsonDocument("$sum", "$amount")))
            .Sort(new BsonDocument("amount", -1))
            .Sort(Builders<PipelineOrder>.Sort.Descending(o => o.Amount))
            .Sort("amount", descending: true)
            .Limit(10)
            .Skip(5)
            .Lookup("customers", "customerId", "_id", "customer")
            .Lookup("orders", new BsonDocument("cid", "$customerId"), [], "related")
            .Unwind("items")
            .Unwind("$tags", preserveNullAndEmptyArrays: true)
            .Count("total")
            .Facet(new Dictionary<string, BsonArray>
            {
                ["open"] = [new BsonDocument("$match", new BsonDocument("status", "Open"))]
            })
            .AddFields(new BsonDocument("computed", 1))
            .Set(new BsonDocument("flag", true))
            .Unset("temp")
            .Unset("a", "b")
            .ReplaceRoot("$doc")
            .Sample(3)
            .AddStage(new BsonDocument("$limit", 1));

        IReadOnlyList<BsonDocument> stages = pipeline.GetStages();

        stages.Should().HaveCountGreaterThan(20);
        stages[0].Contains("$match").Should().BeTrue();
        stages.Should().Contain(s => s.Contains("$lookup"));
        stages.Should().Contain(s => s.Contains("$facet"));
        stages.Should().Contain(s => s.Contains("$unset"));
        stages.Should().Contain(s => s.Contains("$sample"));
    }

    [Fact]
    public async Task ToListAsync_ShouldExecuteAggregatePipeline()
    {
        var docs = new List<BsonDocument>
        {
            new("status", "Open"),
            new("status", "Closed")
        };
        Mock<IMongoCollection<PipelineOrder>> collectionMock = CreateCollectionMock(docs);
        MongoDbAggregationPipeline<PipelineOrder> pipeline = MongoDbAggregationPipeline<PipelineOrder>.Create(collectionMock.Object)
            .Match(o => o.Status == "Open");

        List<BsonDocument> results = await pipeline.ToListAsync();

        results.Should().HaveCount(2);
        collectionMock.Verify(
            c => c.AggregateAsync(
                It.IsAny<PipelineDefinition<PipelineOrder, BsonDocument>>(),
                It.IsAny<AggregateOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ToListAsync_Generic_ShouldReturnTypedResults()
    {
        var typedResults = new List<PipelineSummary>
        {
            new() { Status = "Open", Count = 2 }
        };
        Mock<IMongoCollection<PipelineOrder>> collectionMock = CreateTypedCollectionMock(typedResults);
        MongoDbAggregationPipeline<PipelineOrder> pipeline = MongoDbAggregationPipeline<PipelineOrder>.Create(collectionMock.Object)
            .Group("$status", new BsonDocument("count", new BsonDocument("$sum", 1)));

        List<PipelineSummary> results = await pipeline.ToListAsync<PipelineSummary>();

        results.Should().ContainSingle();
        results[0].Status.Should().Be("Open");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ShouldReturnFirstResult()
    {
        var typedResults = new List<PipelineSummary>
        {
            new() { Status = "Open", Count = 1 }
        };
        Mock<IMongoCollection<PipelineOrder>> collectionMock = CreateTypedCollectionMock(typedResults);
        var pipeline = MongoDbAggregationPipeline<PipelineOrder>.Create(collectionMock.Object);

        PipelineSummary? result = await pipeline.FirstOrDefaultAsync<PipelineSummary>();

        result.Should().NotBeNull();
        result!.Count.Should().Be(1);
    }

    [Fact]
    public async Task ToCursorAsync_ShouldReturnAggregateCursor()
    {
        var typedResults = new List<PipelineSummary> { new() { Status = "Open", Count = 5 } };
        Mock<IMongoCollection<PipelineOrder>> collectionMock = CreateTypedCollectionMock(typedResults);
        var pipeline = MongoDbAggregationPipeline<PipelineOrder>.Create(collectionMock.Object);

        IAsyncCursor<PipelineSummary> cursor = await pipeline.ToCursorAsync<PipelineSummary>();

        cursor.Should().NotBeNull();
        (await cursor.ToListAsync()).Should().HaveCount(1);
    }

    [Fact]
    public void Build_ShouldReturnPipelineDefinition()
    {
        Mock<IMongoCollection<PipelineOrder>> collectionMock = CreateCollectionMock([]);
        MongoDbAggregationPipeline<PipelineOrder> pipeline = MongoDbAggregationPipeline<PipelineOrder>.Create(collectionMock.Object)
            .Limit(5);

        PipelineDefinition<PipelineOrder, BsonDocument> definition = pipeline.Build();

        definition.Should().NotBeNull();
        pipeline.GetStages().Should().ContainSingle(s => s.Contains("$limit"));
    }

    private static Mock<IMongoCollection<PipelineOrder>> CreateCollectionMock(IReadOnlyList<BsonDocument> items)
    {
        var collectionMock = new Mock<IMongoCollection<PipelineOrder>>();
        collectionMock.SetupGet(c => c.DocumentSerializer).Returns(MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry.GetSerializer<PipelineOrder>());
        collectionMock.SetupGet(c => c.Settings).Returns(new MongoCollectionSettings());
        collectionMock
            .Setup(c => c.AggregateAsync(
                It.IsAny<PipelineDefinition<PipelineOrder, BsonDocument>>(),
                It.IsAny<AggregateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FakeAsyncCursor<BsonDocument>(items));
        return collectionMock;
    }

    private static Mock<IMongoCollection<PipelineOrder>> CreateTypedCollectionMock(IReadOnlyList<PipelineSummary> items)
    {
        var collectionMock = new Mock<IMongoCollection<PipelineOrder>>();
        collectionMock.SetupGet(c => c.DocumentSerializer).Returns(MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry.GetSerializer<PipelineOrder>());
        collectionMock.SetupGet(c => c.Settings).Returns(new MongoCollectionSettings());
        collectionMock
            .Setup(c => c.AggregateAsync(
                It.IsAny<PipelineDefinition<PipelineOrder, PipelineSummary>>(),
                It.IsAny<AggregateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FakeAsyncCursor<PipelineSummary>(items));
        return collectionMock;
    }

    private sealed class FakeAsyncCursor<T>(IReadOnlyList<T> items) : IAsyncCursor<T>
    {
        private bool _hasMoved;

        public IEnumerable<T> Current => items;

        public void Dispose()
        {
        }

        public bool MoveNext(CancellationToken cancellationToken = default)
        {
            if (!_hasMoved)
            {
                _hasMoved = true;
                return items.Count > 0;
            }

            return false;
        }

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(MoveNext(cancellationToken));
        }
    }
}

public class PipelineOrder
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string CustomerId { get; set; } = string.Empty;
}

public class PipelineSummary
{
    public string Status { get; set; } = string.Empty;

    public int Count { get; set; }
}
