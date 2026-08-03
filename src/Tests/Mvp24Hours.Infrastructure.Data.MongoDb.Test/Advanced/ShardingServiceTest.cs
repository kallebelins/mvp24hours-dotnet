using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Sharding;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Advanced;

[Trait("Category", "Unit")]
public class ShardingServiceTest
{
    [Fact]
    public void EnableShardingAsync_ShouldThrowWhenDatabaseNameEmpty()
    {
        var client = new Moq.Mock<IMongoClient>();
        var service = new MongoDbShardingService(client.Object);

        Func<Task> act = () => service.EnableShardingAsync(" ");
        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void ShardCollectionAsync_ShouldThrowWhenOptionsInvalid()
    {
        var client = new Moq.Mock<IMongoClient>();
        var service = new MongoDbShardingService(client.Object);

        Func<Task> noKey = () => service.ShardCollectionAsync("db", "orders", new MongoDbShardingOptions());
        noKey.Should().ThrowAsync<ArgumentException>();

        Func<Task> noDb = () => service.ShardCollectionAsync("", "orders", new MongoDbShardingOptions
        {
            ShardKeyFields = [ShardKeyField.Ascending("_id")]
        });
        noDb.Should().ThrowAsync<ArgumentException>();

        Func<Task> noCollection = () => service.ShardCollectionAsync("db", "", new MongoDbShardingOptions
        {
            ShardKeyFields = [ShardKeyField.Ascending("_id")]
        });
        noCollection.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void ShardKeyField_ShouldCreateAscendingDescendingAndHashed()
    {
        var ascending = ShardKeyField.Ascending("tenantId");
        ascending.FieldName.Should().Be("tenantId");
        ascending.Order.AsInt32.Should().Be(1);

        var descending = ShardKeyField.Descending("createdAt");
        descending.Order.AsInt32.Should().Be(-1);

        var hashed = ShardKeyField.Hashed("_id");
        hashed.Order.AsString.Should().Be("hashed");
    }

    [Fact]
    public void MongoDbShardingOptions_ShouldStoreShardKeyFields()
    {
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

        options.ShardKeyFields.Should().HaveCount(2);
        options.UniqueShardKey.Should().BeTrue();
        options.NumInitialChunks.Should().Be(4);
    }

    [Fact]
    public void Constructor_WithNullClient_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new MongoDbShardingService(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task GetShardDistributionAsync_ShouldMapShardStats()
    {
        var clientMock = new Moq.Mock<IMongoClient>();
        var databaseMock = new Moq.Mock<IMongoDatabase>();
        var stats = new BsonDocument
        {
            { "count", 100 },
            { "size", 2048 },
            {
                "shards", new BsonDocument
                {
                    {
                        "shard01", new BsonDocument
                        {
                            { "count", 60 },
                            { "size", 1200 }
                        }
                    },
                    {
                        "shard02", new BsonDocument
                        {
                            { "count", 40 },
                            { "size", 848 }
                        }
                    }
                }
            }
        };

        clientMock.Setup(c => c.GetDatabase("orders", null)).Returns(databaseMock.Object);
        databaseMock
            .Setup(d => d.RunCommandAsync(
                It.IsAny<BsonDocumentCommand<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var service = new MongoDbShardingService(clientMock.Object);

        ShardDistribution distribution = await service.GetShardDistributionAsync("orders", "items");

        distribution.TotalDocuments.Should().Be(100);
        distribution.TotalDataSize.Should().Be(2048);
        distribution.ShardStats.Should().HaveCount(2);
        distribution.ShardStats[0].PercentageOfTotal.Should().Be(60);
    }
}
