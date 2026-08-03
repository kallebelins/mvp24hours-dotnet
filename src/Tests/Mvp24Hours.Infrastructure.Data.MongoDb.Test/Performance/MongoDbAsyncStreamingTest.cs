using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Streaming;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Performance;

[Trait("Category", "Unit")]
public class MongoDbAsyncStreamingTest
{
    [Fact]
    public void Constructor_WithNullCollection_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new MongoDbAsyncStreaming<StreamOrder>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task StreamProjectedAsync_ShouldYieldProjectedDocuments()
    {
        var projected = new List<StreamSummary> { new() { Name = "Projected" } };
        Mock<IMongoCollection<StreamOrder>> collectionMock = CreateProjectedCollectionMock(projected);
        var streamer = new MongoDbAsyncStreaming<StreamOrder>(collectionMock.Object);

        List<StreamSummary> results = [];
        await foreach (StreamSummary item in streamer.StreamProjectedAsync(
                           Builders<StreamOrder>.Filter.Empty,
                           Builders<StreamOrder>.Projection.Expression(o => new StreamSummary { Name = o.Name })))
        {
            results.Add(item);
        }

        results.Should().ContainSingle().Which.Name.Should().Be("Projected");
    }

    [Fact]
    public async Task StreamAggregationAsync_ShouldYieldAggregationResults()
    {
        var aggregated = new List<StreamSummary> { new() { Name = "Agg", Count = 3 } };
        Mock<IMongoCollection<StreamOrder>> collectionMock = CreateAggregationCollectionMock(aggregated);
        var streamer = new MongoDbAsyncStreaming<StreamOrder>(collectionMock.Object);
        var pipeline =
            PipelineDefinition<StreamOrder, StreamSummary>.Create(Array.Empty<BsonDocument>());

        List<StreamSummary> results = [];
        await foreach (StreamSummary item in streamer.StreamAggregationAsync(pipeline))
        {
            results.Add(item);
        }

        results.Should().ContainSingle().Which.Count.Should().Be(3);
    }

    [Fact]
    public async Task StreamProjectedAsync_WithSort_ShouldPassSortToFindOptions()
    {
        var projected = new List<StreamSummary> { new() { Name = "Sorted" } };
        Mock<IMongoCollection<StreamOrder>> collectionMock = CreateProjectedCollectionMock(projected);
        var streamer = new MongoDbAsyncStreaming<StreamOrder>(collectionMock.Object);

        List<StreamSummary> results = [];
        await foreach (StreamSummary item in streamer.StreamProjectedAsync(
                           Builders<StreamOrder>.Filter.Empty,
                           Builders<StreamOrder>.Projection.Expression(o => new StreamSummary { Name = o.Name }),
                           Builders<StreamOrder>.Sort.Ascending(o => o.Name)))
        {
            results.Add(item);
        }

        results.Should().ContainSingle();
        collectionMock.Verify(
            c => c.FindAsync(
                It.IsAny<FilterDefinition<StreamOrder>>(),
                It.IsAny<FindOptions<StreamOrder, StreamSummary>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Mock<IMongoCollection<StreamOrder>> CreateProjectedCollectionMock(IReadOnlyList<StreamSummary> items)
    {
        var collectionMock = new Mock<IMongoCollection<StreamOrder>>();
        collectionMock.SetupGet(c => c.DocumentSerializer)
            .Returns(MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry.GetSerializer<StreamOrder>());
        collectionMock.SetupGet(c => c.Settings).Returns(new MongoCollectionSettings());
        collectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<StreamOrder>>(),
                It.IsAny<FindOptions<StreamOrder, StreamSummary>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FakeAsyncCursor<StreamSummary>(items));
        return collectionMock;
    }

    private static Mock<IMongoCollection<StreamOrder>> CreateAggregationCollectionMock(IReadOnlyList<StreamSummary> items)
    {
        var collectionMock = new Mock<IMongoCollection<StreamOrder>>();
        collectionMock
            .Setup(c => c.AggregateAsync(
                It.IsAny<PipelineDefinition<StreamOrder, StreamSummary>>(),
                It.IsAny<AggregateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FakeAsyncCursor<StreamSummary>(items));
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

public class StreamOrder
{
    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}

public class StreamSummary
{
    public string Name { get; set; } = string.Empty;

    public int Count { get; set; }
}
