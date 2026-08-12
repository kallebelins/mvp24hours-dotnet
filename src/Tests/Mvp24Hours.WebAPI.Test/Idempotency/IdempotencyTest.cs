using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.DistributedLocking.Contract;
using Mvp24Hours.Infrastructure.DistributedLocking.Options;
using Mvp24Hours.Infrastructure.DistributedLocking.Results;
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
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(method: "POST", path: "/api/orders");
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
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(method: "POST", path: "/api/orders", body: "{\"name\":\"order\"}");
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
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(method: "POST", path: "/api/payments", body: "{\"idempotencyKey\":\"pay-1\"}");
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

    [Fact]
    public async Task DistributedCacheIdempotencyStore_Should_ReturnInFlight_WhenAtomicLockNotAcquired()
    {
        var factory = new FakeDistributedLockFactory(new FakeDistributedLock(acquired: false));
        var options = new IdempotencyOptions
        {
            CacheKeyPrefix = "test:",
            EnableAtomicAcquisitionUsingDistributedLock = true
        };

        var store = new DistributedCacheIdempotencyStore(
            WebApiTestHelpers.CreateMemoryDistributedCache(),
            Options.Create(options),
            NullLogger<DistributedCacheIdempotencyStore>.Instance,
            distributedLockFactory: factory);

        IdempotencyLockResult result = await store.TryAcquireLockAsync("k-atomic", "/api", "POST", null, TimeSpan.FromMinutes(5));

        result.Acquired.Should().BeFalse();
        result.IsInFlight.Should().BeTrue();
    }

    [Fact]
    public async Task DistributedCacheIdempotencyStore_Should_Acquire_WhenAtomicLockAcquired()
    {
        var factory = new FakeDistributedLockFactory(new FakeDistributedLock(acquired: true));
        var options = new IdempotencyOptions
        {
            CacheKeyPrefix = "test:",
            EnableAtomicAcquisitionUsingDistributedLock = true
        };

        var store = new DistributedCacheIdempotencyStore(
            WebApiTestHelpers.CreateMemoryDistributedCache(),
            Options.Create(options),
            NullLogger<DistributedCacheIdempotencyStore>.Instance,
            distributedLockFactory: factory);

        IdempotencyLockResult result = await store.TryAcquireLockAsync("k-atomic", "/api", "POST", null, TimeSpan.FromMinutes(5));

        result.Acquired.Should().BeTrue();
    }

    private sealed class FakeDistributedLockFactory(IDistributedLock distributedLock) : IDistributedLockFactory
    {
        public IDistributedLock Create() => distributedLock;

        public IDistributedLock Create(string providerName) => distributedLock;
    }

    private sealed class FakeDistributedLock(bool acquired) : IDistributedLock
    {
        public Task<bool> IsLockedAsync(string resource, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(!acquired);
        }

        public Task<LockAcquisitionResult> TryAcquireAsync(string resource, DistributedLockOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (!acquired)
            {
                return Task.FromResult(LockAcquisitionResult.Timeout());
            }

            return Task.FromResult(LockAcquisitionResult.Acquired(new FakeLockHandle(resource)));
        }

        public Task<LockAcquisitionResult> TryAcquireWithFenceAsync(string resource, DistributedLockOptions? options = null, CancellationToken cancellationToken = default)
        {
            return TryAcquireAsync(resource, options, cancellationToken);
        }
    }

    private sealed class FakeLockHandle(string resource) : ILockHandle
    {
        public string Resource { get; } = resource;
        public long? FencedToken => null;
        public bool IsValid => true;
        public DateTimeOffset AcquiredAt { get; } = DateTimeOffset.UtcNow;
        public DateTimeOffset ExpiresAt { get; } = DateTimeOffset.UtcNow.AddMinutes(1);

        public void Dispose() { }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public Task<bool> ReleaseAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> RenewAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}
