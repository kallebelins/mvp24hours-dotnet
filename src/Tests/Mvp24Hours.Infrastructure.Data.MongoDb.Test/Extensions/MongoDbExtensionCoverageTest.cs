using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.ChangeStreams;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Sharding;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Aggregation;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Pagination;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Streaming;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Extensions;

[Trait("Category", "Unit")]
public class MongoDbExtensionCoverageTest
{
    [Fact]
    public void AsKeysetPagination_ShouldReturnKeysetPaginationBuilder()
    {
        Mock<IMongoCollection<ExtensionDoc>> collectionMock = new();

        MongoDbKeysetPagination<ExtensionDoc> paginator = collectionMock.Object.AsKeysetPagination();

        paginator.Should().NotBeNull();
        paginator.Should().BeOfType<MongoDbKeysetPagination<ExtensionDoc>>();
    }

    [Fact]
    public void AsAsyncStreaming_ShouldReturnStreamingProvider()
    {
        Mock<IMongoCollection<ExtensionDoc>> collectionMock = new();

        MongoDbAsyncStreaming<ExtensionDoc> streamer = collectionMock.Object.AsAsyncStreaming();

        streamer.Should().NotBeNull();
    }

    [Fact]
    public void StreamAllAsync_ShouldDelegateToStreamingProvider()
    {
        Mock<IMongoCollection<ExtensionDoc>> collectionMock = new();

        IAsyncEnumerable<ExtensionDoc> stream = collectionMock.Object.StreamAllAsync();

        stream.Should().NotBeNull();
    }

    [Fact]
    public void StreamAsync_WithExpression_ShouldDelegateToStreamingProvider()
    {
        Mock<IMongoCollection<ExtensionDoc>> collectionMock = new();

        IAsyncEnumerable<ExtensionDoc> stream = collectionMock.Object.StreamAsync(d => d.Active);

        stream.Should().NotBeNull();
    }

    [Fact]
    public void StreamBatchesAsync_ShouldDelegateToStreamingProvider()
    {
        Mock<IMongoCollection<ExtensionDoc>> collectionMock = new();

        IAsyncEnumerable<IReadOnlyList<ExtensionDoc>> stream = collectionMock.Object.StreamBatchesAsync(batchSize: 25);

        stream.Should().NotBeNull();
    }

    [Fact]
    public void AsAggregation_ShouldReturnAggregationPipelineBuilder()
    {
        Mock<IMongoCollection<ExtensionDoc>> collectionMock = new();

        MongoDbAggregationPipeline<ExtensionDoc> pipeline = collectionMock.Object.AsAggregation();

        pipeline.Should().NotBeNull();
    }

    [Fact]
    public void ToAggregation_ShouldThrowNotImplementedException()
    {
        Mock<IFindFluent<ExtensionDoc, ExtensionDoc>> findMock = new();

        Action act = () => _ = findMock.Object.ToAggregation();

        act.Should().Throw<NotImplementedException>()
            .WithMessage("*AsAggregation()*");
    }
}

[Trait("Category", "Unit")]
public class MongoDbAdvancedExtensionsTest
{
    [Fact]
    public void AddMvpMongoDbAdvanced_ShouldRegisterCoreServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IMongoClient>());
        services.AddSingleton(Mock.Of<IMongoDatabase>());

        services.AddMvpMongoDbAdvanced();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMongoDbShardingService>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvpMongoDbChangeStream_ShouldResolveChangeStreamService()
    {
        var services = new ServiceCollection();
        var databaseMock = new Mock<IMongoDatabase>();
        var collectionMock = new Mock<IMongoCollection<ExtensionDoc>>();
        databaseMock
            .Setup(d => d.GetCollection<ExtensionDoc>("docs", null))
            .Returns(collectionMock.Object);
        services.AddSingleton(databaseMock.Object);

        services.AddMvpMongoDbChangeStream<ExtensionDoc>("docs");

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMongoDbChangeStreamService<ExtensionDoc>>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvpMongoDbSharding_ShouldResolveShardingService()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IMongoClient>());

        services.AddMvpMongoDbSharding();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMongoDbShardingService>().Should().NotBeNull();
    }
}

public class ExtensionDoc
{
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; }
}
