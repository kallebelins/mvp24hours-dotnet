using System.Reflection;
using MongoDB.Driver;
using Moq;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.ChangeStreams;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Advanced;

[Trait("Category", "Unit")]
public class MongoDbChangeStreamServiceUnitTest
{
    [Fact]
    public void Constructor_WithNullCollection_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new MongoDbChangeStreamService<ChangeStreamItem>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task WatchCollectionAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        Mock<IMongoCollection<ChangeStreamItem>> collectionMock = new();
        var service = new MongoDbChangeStreamService<ChangeStreamItem>(collectionMock.Object);

        Func<Task> act = () => service.WatchCollectionAsync((Func<ChangeStreamDocument<ChangeStreamItem>, Task>)null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task WatchCollectionAsync_WithEmptyOperationTypes_ShouldThrowArgumentException()
    {
        Mock<IMongoCollection<ChangeStreamItem>> collectionMock = new();
        var service = new MongoDbChangeStreamService<ChangeStreamItem>(collectionMock.Object);

        Func<Task> act = () => service.WatchCollectionAsync(_ => Task.CompletedTask, []);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task WatchInsertsAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        Mock<IMongoCollection<ChangeStreamItem>> collectionMock = new();
        var service = new MongoDbChangeStreamService<ChangeStreamItem>(collectionMock.Object);

        Func<Task> act = () => service.WatchInsertsAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task WatchUpdatesAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        Mock<IMongoCollection<ChangeStreamItem>> collectionMock = new();
        var service = new MongoDbChangeStreamService<ChangeStreamItem>(collectionMock.Object);

        Func<Task> act = () => service.WatchUpdatesAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task WatchDeletesAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        Mock<IMongoCollection<ChangeStreamItem>> collectionMock = new();
        var service = new MongoDbChangeStreamService<ChangeStreamItem>(collectionMock.Object);

        Func<Task> act = () => service.WatchDeletesAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ResumeWatchingAsync_WithNullToken_ShouldThrowArgumentNullException()
    {
        Mock<IMongoCollection<ChangeStreamItem>> collectionMock = new();
        var service = new MongoDbChangeStreamService<ChangeStreamItem>(collectionMock.Object);

        Func<Task> act = () => service.ResumeWatchingAsync(null!, _ => Task.CompletedTask);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task WatchCollectionAsync_WithNullPipeline_ShouldThrowArgumentNullException()
    {
        Mock<IMongoCollection<ChangeStreamItem>> collectionMock = new();
        var service = new MongoDbChangeStreamService<ChangeStreamItem>(collectionMock.Object);

        Func<Task> act = () => service.WatchCollectionAsync(
            _ => Task.CompletedTask,
            (PipelineDefinition<ChangeStreamDocument<ChangeStreamItem>, ChangeStreamDocument<ChangeStreamItem>>)null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task WatchCollectionAsync_WithNullOperationTypes_ShouldThrowArgumentNullException()
    {
        Mock<IMongoCollection<ChangeStreamItem>> collectionMock = new();
        var service = new MongoDbChangeStreamService<ChangeStreamItem>(collectionMock.Object);

        Func<Task> act = () => service.WatchCollectionAsync(
            _ => Task.CompletedTask,
            (IEnumerable<ChangeStreamOperationType>)null!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(ChangeStreamOperationType.Insert, "insert")]
    [InlineData(ChangeStreamOperationType.Update, "update")]
    [InlineData(ChangeStreamOperationType.Replace, "replace")]
    [InlineData(ChangeStreamOperationType.Delete, "delete")]
    [InlineData(ChangeStreamOperationType.Invalidate, "invalidate")]
    [InlineData(ChangeStreamOperationType.Rename, "rename")]
    [InlineData(ChangeStreamOperationType.Drop, "drop")]
    [InlineData(ChangeStreamOperationType.DropDatabase, "dropDatabase")]
    public void GetOperationTypeString_ShouldMapKnownOperations(ChangeStreamOperationType operationType, string expected)
    {
        MethodInfo? method = typeof(MongoDbChangeStreamService<ChangeStreamItem>)
            .GetMethod("GetOperationTypeString", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        string result = (string)method!.Invoke(null, [operationType])!;
        result.Should().Be(expected);
    }
}

public class ChangeStreamItem
{
    public string Name { get; set; } = string.Empty;
}
