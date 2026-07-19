using MongoDB.Bson;
using MongoDB.Driver;
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
        ShardKeyField ascending = ShardKeyField.Ascending("tenantId");
        ascending.FieldName.Should().Be("tenantId");
        ascending.Order.AsInt32.Should().Be(1);

        ShardKeyField descending = ShardKeyField.Descending("createdAt");
        descending.Order.AsInt32.Should().Be(-1);

        ShardKeyField hashed = ShardKeyField.Hashed("_id");
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
}
