using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Infrastructure.Caching.HybridCache;
using StackExchange.Redis;

namespace Mvp24Hours.Infrastructure.Caching.Test.Tags;

[Trait("Category", "Unit")]
public class RedisHybridCacheTagManagerTest
{
    [Fact]
    public void Constructor_WithNullRedis_ShouldThrow()
    {
        Action act = () => _ = new RedisHybridCacheTagManager(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task TrackKeyWithTagsAsync_EmptyKey_ShouldThrow()
    {
        RedisHybridCacheTagManager manager = CreateManager(out _);

        await Assert.ThrowsAsync<ArgumentException>(() => manager.TrackKeyWithTagsAsync(" ", ["tag"]));
    }

    [Fact]
    public async Task TrackKeyWithTagsAsync_EmptyTags_ShouldNoOp()
    {
        RedisHybridCacheTagManager manager = CreateManager(out Mock<IDatabase> database);

        await manager.TrackKeyWithTagsAsync("key", []);

        database.Verify(d => d.CreateTransaction(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task TrackKeyWithTagsAsync_ShouldExecuteTransaction()
    {
        RedisHybridCacheTagManager manager = CreateManager(out Mock<IDatabase> database, out Mock<ITransaction> transaction, out _);
        transaction.Setup(t => t.ExecuteAsync(It.IsAny<CommandFlags>())).ReturnsAsync(true);

        await manager.TrackKeyWithTagsAsync("product:1", ["products", "catalog"]);

        transaction.Verify(t => t.SetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()), Times.AtLeastOnce);
        transaction.Verify(t => t.ExecuteAsync(It.IsAny<CommandFlags>()), Times.Once);
        database.Verify(d => d.CreateTransaction(It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task RemoveKeyFromTagsAsync_EmptyKey_ShouldNoOp()
    {
        RedisHybridCacheTagManager manager = CreateManager(out Mock<IDatabase> database);

        await manager.RemoveKeyFromTagsAsync(" ");

        database.Verify(d => d.SetMembersAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task GetKeysByTagAsync_EmptyTag_ShouldReturnEmpty()
    {
        RedisHybridCacheTagManager manager = CreateManager(out _);

        IEnumerable<string> keys = await manager.GetKeysByTagAsync(" ");

        keys.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTagsByKeyAsync_EmptyKey_ShouldReturnEmpty()
    {
        RedisHybridCacheTagManager manager = CreateManager(out _);

        IEnumerable<string> tags = await manager.GetTagsByKeyAsync(" ");

        tags.Should().BeEmpty();
    }

    [Fact]
    public async Task InvalidateTagAsync_EmptyTag_ShouldNoOp()
    {
        RedisHybridCacheTagManager manager = CreateManager(out Mock<IDatabase> database);

        await manager.InvalidateTagAsync(" ");

        database.Verify(d => d.SetMembersAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public void GetStatistics_ShouldReturnCountersFromRedis()
    {
        RedisHybridCacheTagManager manager = CreateManager(
            out Mock<IDatabase> database,
            out _,
            out Mock<IServer> server);

        database.Setup(d => d.StringGet(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns(new RedisValue("3"));
        server.Setup(s => s.Keys(It.IsAny<int>(), It.IsAny<RedisValue>(), It.IsAny<int>(), It.IsAny<long>()))
            .Returns([]);
        database.Setup(d => d.SetLength(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns(0);

        HybridCacheTagStatistics stats = manager.GetStatistics();

        stats.TagInvalidations.Should().Be(3);
    }

    [Fact]
    public async Task ClearAsync_ShouldDeleteTagKeys()
    {
        RedisHybridCacheTagManager manager = CreateManager(
            out Mock<IDatabase> database,
            out _,
            out Mock<IServer> server);

        server.Setup(s => s.Keys(It.IsAny<int>(), It.IsAny<RedisValue>(), It.IsAny<int>(), It.IsAny<long>()))
            .Returns([new RedisKey("tag:products"), new RedisKey("key:item:tags")]);
        database.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(2);

        await manager.ClearAsync();

        database.Verify(d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()), Times.AtLeastOnce);
    }

    private static RedisHybridCacheTagManager CreateManager(
        out Mock<IDatabase> database,
        out Mock<ITransaction> transaction,
        out Mock<IServer> server)
    {
        transaction = new Mock<ITransaction>();
        database = new Mock<IDatabase>();
        server = new Mock<IServer>();
        var multiplexer = new Mock<IConnectionMultiplexer>();

        database.Setup(d => d.CreateTransaction(It.IsAny<object>())).Returns(transaction.Object);
        database.Setup(d => d.SetMembersAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync([]);
        database.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        database.Setup(d => d.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);
        database.Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(0));

        multiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(database.Object);
        multiplexer.Setup(m => m.GetEndPoints(It.IsAny<bool>())).Returns([new System.Net.DnsEndPoint("localhost", 6379)]);
        multiplexer.Setup(m => m.GetServer(It.IsAny<System.Net.EndPoint>(), It.IsAny<object>())).Returns(server.Object);

        return new RedisHybridCacheTagManager(
            multiplexer.Object,
            Options.Create(new RedisHybridCacheTagManagerOptions()),
            Mock.Of<ILogger<RedisHybridCacheTagManager>>());
    }

    private static RedisHybridCacheTagManager CreateManager(out Mock<IDatabase> database)
    {
        return CreateManager(out database, out _, out _);
    }
}
