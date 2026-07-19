using Moq;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Compression;
using Mvp24Hours.Infrastructure.Caching.Serializers;
using Mvp24Hours.Infrastructure.Caching.Test.Support;

namespace Mvp24Hours.Infrastructure.Caching.Test.Serializers;

[Trait("Category", "Unit")]
public class JsonCacheSerializerTest
{
    [Fact]
    public async Task SerializeAndDeserialize_ShouldRoundTrip()
    {
        var serializer = new JsonCacheSerializer();
        var entity = new TestEntity { Id = 1, Name = "Json" };

        byte[] bytes = await serializer.SerializeAsync(entity);
        TestEntity? result = await serializer.DeserializeAsync<TestEntity>(bytes);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Json");
    }

    [Fact]
    public async Task SerializeToStringAndDeserializeFromString_ShouldRoundTrip()
    {
        var serializer = new JsonCacheSerializer();
        var entity = new TestEntity { Id = 2, Name = "String" };

        string json = await serializer.SerializeToStringAsync(entity);
        TestEntity? result = await serializer.DeserializeFromStringAsync<TestEntity>(json);

        result!.Name.Should().Be("String");
    }

    [Fact]
    public async Task DeserializeAsync_EmptyBytes_ShouldReturnNull()
    {
        var serializer = new JsonCacheSerializer();

        TestEntity? result = await serializer.DeserializeAsync<TestEntity>([]);

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeserializeAsync_InvalidJson_ShouldReturnNull()
    {
        var serializer = new JsonCacheSerializer();

        TestEntity? result = await serializer.DeserializeAsync<TestEntity>("not-json"u8.ToArray());

        result.Should().BeNull();
    }

    [Fact]
    public async Task SerializeAsync_NullValue_ShouldThrow()
    {
        var serializer = new JsonCacheSerializer();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            serializer.SerializeAsync<TestEntity>(null!));
    }
}

[Trait("Category", "Unit")]
public class MessagePackCacheSerializerTest
{
    [Fact]
    public async Task SerializeAndDeserialize_ShouldRoundTrip()
    {
        var serializer = new MessagePackCacheSerializer();
        var item = new MessagePackCacheItem { Id = 10, Name = "MessagePack" };

        byte[] bytes = await serializer.SerializeAsync(item);
        MessagePackCacheItem? result = await serializer.DeserializeAsync<MessagePackCacheItem>(bytes);

        result.Should().NotBeNull();
        result!.Id.Should().Be(10);
        result.Name.Should().Be("MessagePack");
    }

    [Fact]
    public async Task SerializeToString_ShouldUseBase64()
    {
        var serializer = new MessagePackCacheSerializer();
        var item = new MessagePackCacheItem { Id = 1, Name = "Base64" };

        string encoded = await serializer.SerializeToStringAsync(item);
        MessagePackCacheItem? result = await serializer.DeserializeFromStringAsync<MessagePackCacheItem>(encoded);

        result!.Name.Should().Be("Base64");
    }
}

[Trait("Category", "Unit")]
public class CompressedCacheSerializerTest
{
    [Fact]
    public async Task SerializeAsync_BelowThreshold_ShouldPrefixWithUncompressedMarker()
    {
        var inner = new JsonCacheSerializer();
        var compressor = new CacheCompressor();
        var serializer = new CompressedCacheSerializer(inner, compressor, compressionThresholdBytes: 10_000);
        var entity = new TestEntity { Id = 1, Name = "Small" };

        byte[] bytes = await serializer.SerializeAsync(entity);

        bytes[0].Should().Be(0);
        TestEntity? result = await serializer.DeserializeAsync<TestEntity>(bytes);
        result!.Name.Should().Be("Small");
    }

    [Fact]
    public async Task SerializeAsync_AboveThreshold_ShouldCompress()
    {
        var inner = new JsonCacheSerializer();
        var compressor = new CacheCompressor();
        var serializer = new CompressedCacheSerializer(inner, compressor, compressionThresholdBytes: 16);
        var entity = new TestEntity { Id = 1, Name = new string('x', 500) };

        byte[] bytes = await serializer.SerializeAsync(entity);

        bytes[0].Should().NotBe(0);
        TestEntity? result = await serializer.DeserializeAsync<TestEntity>(bytes);
        result!.Name.Should().HaveLength(500);
    }

    [Fact]
    public void Constructor_InvalidThreshold_ShouldThrow()
    {
        Action act = () => new CompressedCacheSerializer(new JsonCacheSerializer(), new CacheCompressor(), 0);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task SerializeToStringAsync_ShouldDelegateToInnerWithoutCompression()
    {
        var inner = new Mock<ICacheSerializer>();
        inner.Setup(x => x.SerializeToStringAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{}");
        var serializer = new CompressedCacheSerializer(inner.Object, new CacheCompressor());

        string result = await serializer.SerializeToStringAsync(new TestEntity { Id = 1, Name = "X" });

        result.Should().Be("{}");
    }
}
