using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Idempotency;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Idempotency;

[Trait("Category", "Unit")]
public class IdempotencyTest
{
    [Fact]
    public async Task DefaultIdempotencyKeyGenerator_Should_ReadHeaderKey()
    {
        var context = WebApiTestHelpers.CreateHttpContext(method: "POST", path: "/api/orders");
        context.Request.Headers["Idempotency-Key"] = "order-123";
        var sut = new DefaultIdempotencyKeyGenerator(Options.Create(new IdempotencyOptions()));

        IdempotencyKeyResult result = await sut.GenerateKeyAsync(context);

        result.HasKey.Should().BeTrue();
        result.Key.Should().Be("order-123");
        result.IsFromHeader.Should().BeTrue();
    }

    [Fact]
    public async Task DefaultIdempotencyKeyGenerator_Should_GenerateFromBody_WhenNoHeader()
    {
        var context = WebApiTestHelpers.CreateHttpContext(method: "POST", path: "/api/orders", body: "{\"name\":\"order\"}");
        var options = new IdempotencyOptions { KeySource = IdempotencyKeySource.RequestBody };
        var sut = new DefaultIdempotencyKeyGenerator(Options.Create(options));

        IdempotencyKeyResult result = await sut.GenerateKeyAsync(context);

        result.HasKey.Should().BeTrue();
        result.IsGenerated.Should().BeTrue();
        result.Key.Should().StartWith("POST:/api/orders:");
    }

    [Fact]
    public async Task CqrsIdempotencyKeyGenerator_Should_UseCommandBodyIdempotencyKey()
    {
        var context = WebApiTestHelpers.CreateHttpContext(method: "POST", path: "/api/payments", body: "{\"idempotencyKey\":\"pay-1\"}");
        var sut = new CqrsIdempotencyKeyGenerator(Options.Create(new IdempotencyOptions { IntegrateWithCqrs = true }));

        IdempotencyKeyResult result = await sut.GenerateKeyAsync(context);

        result.HasKey.Should().BeTrue();
        result.Key.Should().Contain("cqrs:pay-1");
    }

    [Fact]
    public async Task DistributedCacheIdempotencyStore_Should_AcquireAndCompleteRecord()
    {
        var store = new DistributedCacheIdempotencyStore(
            WebApiTestHelpers.CreateMemoryDistributedCache(),
            Options.Create(new IdempotencyOptions { CacheKeyPrefix = "test:" }),
            NullLogger<DistributedCacheIdempotencyStore>.Instance);

        IdempotencyLockResult lockResult = await store.TryAcquireLockAsync("k1", "/api", "POST", null, TimeSpan.FromMinutes(5));
        await store.CompleteAsync("k1", 201, Encoding.UTF8.GetBytes("{\"ok\":true}"), "application/json");
        IdempotencyRecord? record = await store.GetAsync("k1");

        lockResult.Acquired.Should().BeTrue();
        record.Should().NotBeNull();
        record!.IsCompleted.Should().BeTrue();
        record.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task DistributedCacheIdempotencyStore_Should_MarkFailedAndRemoveRecord()
    {
        var store = new DistributedCacheIdempotencyStore(
            WebApiTestHelpers.CreateMemoryDistributedCache(),
            Options.Create(new IdempotencyOptions { CacheKeyPrefix = "test:" }),
            NullLogger<DistributedCacheIdempotencyStore>.Instance);
        await store.TryAcquireLockAsync("k1", "/api", "POST", null, TimeSpan.FromMinutes(5));

        await store.FailAsync("k1", removeRecord: true);
        bool exists = await store.ExistsAsync("k1");

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DistributedCacheIdempotencyStore_Should_ReturnExistingRecord_OnSecondAcquire()
    {
        var store = new DistributedCacheIdempotencyStore(
            WebApiTestHelpers.CreateMemoryDistributedCache(),
            Options.Create(new IdempotencyOptions { CacheKeyPrefix = "test:" }),
            NullLogger<DistributedCacheIdempotencyStore>.Instance);
        await store.TryAcquireLockAsync("k1", "/api", "POST", null, TimeSpan.FromMinutes(5));

        IdempotencyLockResult second = await store.TryAcquireLockAsync("k1", "/api", "POST", null, TimeSpan.FromMinutes(5));

        second.Acquired.Should().BeFalse();
        second.ExistingRecord.Should().NotBeNull();
        second.ExistingRecord!.IsProcessing.Should().BeTrue();
    }
}
