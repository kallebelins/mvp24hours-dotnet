using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Sharding;
using Mvp24Hours.Infrastructure.Testing.Logging;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Advanced;

[Trait("Category", "Unit")]
public class ShardingServiceBehaviorTest
{
    [Fact]
    public async Task EnableShardingAsync_ShouldRunAdminCommand()
    {
        BsonDocument? capturedCommand = null;
        Mock<IMongoClient> clientMock = CreateAdminClientMock(cmd => capturedCommand = cmd);

        var service = new MongoDbShardingService(clientMock.Object);

        await service.EnableShardingAsync("ordersDb");

        capturedCommand.Should().NotBeNull();
        capturedCommand!["enableSharding"].AsString.Should().Be("ordersDb");
    }

    [Fact]
    public async Task ShardCollectionAsync_ShouldBuildShardKeyAndOptions()
    {
        BsonDocument? capturedCommand = null;
        Mock<IMongoClient> clientMock = CreateAdminClientMock(cmd => capturedCommand = cmd);

        var service = new MongoDbShardingService(clientMock.Object);
        var options = new MongoDbShardingOptions
        {
            UniqueShardKey = true,
            NumInitialChunks = 4,
            ShardKeyFields =
            [
                ShardKeyField.Ascending("tenantId"),
                ShardKeyField.Hashed("_id")
            ]
        };

        await service.ShardCollectionAsync("ordersDb", "orders", options);

        capturedCommand.Should().NotBeNull();
        capturedCommand!["shardCollection"].AsString.Should().Be("ordersDb.orders");
        capturedCommand["unique"].AsBoolean.Should().BeTrue();
        capturedCommand["numInitialChunks"].AsInt32.Should().Be(4);
        capturedCommand["key"].AsBsonDocument["tenantId"].AsInt32.Should().Be(1);
        capturedCommand["key"]["_id"].AsString.Should().Be("hashed");
    }

    [Fact]
    public async Task GetShardDistributionAsync_WithoutShards_ShouldReturnTotalsOnly()
    {
        var stats = new BsonDocument
        {
            { "count", 50 },
            { "size", 1024 }
        };

        Mock<IMongoClient> clientMock = CreateDatabaseClientMock("ordersDb", stats);

        var service = new MongoDbShardingService(clientMock.Object);

        ShardDistribution distribution = await service.GetShardDistributionAsync("ordersDb", "items");

        distribution.TotalDocuments.Should().Be(50);
        distribution.TotalDataSize.Should().Be(1024);
        distribution.ShardStats.Should().BeEmpty();
    }

    [Fact]
    public async Task MoveChunkAsync_ShouldRunAdminMoveChunkCommand()
    {
        BsonDocument? capturedCommand = null;
        Mock<IMongoClient> clientMock = CreateAdminClientMock(cmd => capturedCommand = cmd);
        var service = new MongoDbShardingService(clientMock.Object);
        var splitPoint = new BsonDocument("tenantId", "A");

        await service.MoveChunkAsync("ordersDb", "orders", splitPoint, "shard02");

        capturedCommand!["moveChunk"].AsString.Should().Be("ordersDb.orders");
        capturedCommand["to"].AsString.Should().Be("shard02");
    }

    [Fact]
    public async Task SplitChunkAsync_ShouldRunAdminSplitCommand()
    {
        BsonDocument? capturedCommand = null;
        Mock<IMongoClient> clientMock = CreateAdminClientMock(cmd => capturedCommand = cmd);
        var service = new MongoDbShardingService(clientMock.Object);
        var middle = new BsonDocument("tenantId", "B");

        await service.SplitChunkAsync("ordersDb", "orders", middle);

        capturedCommand!["split"].AsString.Should().Be("ordersDb.orders");
        capturedCommand["middle"].AsBsonDocument["tenantId"].AsString.Should().Be("B");
    }

    [Fact]
    public async Task GetClusterStatsAsync_ShouldReturnServerStatus()
    {
        Mock<IMongoClient> clientMock = CreateAdminClientMock(_ => { });
        var logger = new FakeLogger<MongoDbShardingService>();
        var service = new MongoDbShardingService(clientMock.Object, logger);

        BsonDocument result = await service.GetClusterStatsAsync();

        result["ok"].AsInt32.Should().Be(1);
    }

    [Fact]
    public async Task EnableShardingAsync_WithLogger_ShouldLogInformation()
    {
        Mock<IMongoClient> clientMock = CreateAdminClientMock(_ => { });
        var logger = new FakeLogger<MongoDbShardingService>();
        var service = new MongoDbShardingService(clientMock.Object, logger);

        await service.EnableShardingAsync("ordersDb");

        logger.ContainsLog(LogLevel.Information, "Sharding enabled").Should().BeTrue();
    }

    private static Mock<IMongoClient> CreateAdminClientMock(Action<BsonDocument> onCommand)
    {
        var clientMock = new Mock<IMongoClient>();
        var adminDbMock = new Mock<IMongoDatabase>();

        clientMock.Setup(c => c.GetDatabase("admin", null)).Returns(adminDbMock.Object);
        adminDbMock
            .Setup(d => d.RunCommandAsync(
                It.IsAny<BsonDocumentCommand<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((BsonDocumentCommand<BsonDocument> command, ReadPreference _, CancellationToken __) =>
            {
                onCommand(command.Document);
                return new BsonDocument("ok", 1);
            });

        return clientMock;
    }

    private static Mock<IMongoClient> CreateDatabaseClientMock(string databaseName, BsonDocument stats)
    {
        var clientMock = new Mock<IMongoClient>();
        var databaseMock = new Mock<IMongoDatabase>();

        clientMock.Setup(c => c.GetDatabase(databaseName, null)).Returns(databaseMock.Object);
        databaseMock
            .Setup(d => d.RunCommandAsync(
                It.IsAny<BsonDocumentCommand<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        return clientMock;
    }
}
