using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Patterns;
using Mvp24Hours.Infrastructure.Caching.Test.Support;

namespace Mvp24Hours.Infrastructure.Caching.Test.Patterns;

[Trait("Category", "Unit")]
public class ReadThroughCacheTest
{
    [Fact]
    public async Task GetAsync_CacheHit_ShouldNotCallSource()
    {
        var cache = new Mock<ICacheProvider>();
        var entity = new TestEntity { Id = 1, Name = "Cached" };
        cache.Setup(x => x.GetAsync<TestEntity>("key", It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        int sourceCalls = 0;
        var readThrough = new ReadThroughCache<TestEntity>(
            cache.Object,
            (_, _) =>
            {
                sourceCalls++;
                return Task.FromResult<TestEntity?>(new TestEntity { Id = 2, Name = "Source" });
            });

        TestEntity? result = await readThrough.GetAsync("key");

        result.Should().BeSameAs(entity);
        sourceCalls.Should().Be(0);
    }

    [Fact]
    public async Task GetAsync_CacheMiss_ShouldLoadAndCache()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        int sourceCalls = 0;
        var readThrough = new ReadThroughCache<TestEntity>(
            cache,
            (_, _) =>
            {
                sourceCalls++;
                return Task.FromResult<TestEntity?>(new TestEntity { Id = 3, Name = "Loaded" });
            });

        TestEntity? result = await readThrough.GetAsync("miss-key");

        result!.Name.Should().Be("Loaded");
        sourceCalls.Should().Be(1);
        (await cache.ExistsAsync("miss-key")).Should().BeTrue();
    }

    [Fact]
    public async Task GetAsync_SourceReturnsNull_ShouldNotCache()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var readThrough = new ReadThroughCache<TestEntity>(
            cache,
            (_, _) => Task.FromResult<TestEntity?>(null));

        TestEntity? result = await readThrough.GetAsync("null-key");

        result.Should().BeNull();
        (await cache.ExistsAsync("null-key")).Should().BeFalse();
    }

    [Fact]
    public async Task GetAsync_EmptyKey_ShouldThrow()
    {
        var readThrough = new ReadThroughCache<TestEntity>(
            CacheTestHelpers.CreateMemoryProvider(),
            (_, _) => Task.FromResult<TestEntity?>(null));

        await Assert.ThrowsAsync<ArgumentException>(() => readThrough.GetAsync(" "));
    }
}

[Trait("Category", "Unit")]
public class WriteThroughCacheTest
{
    [Fact]
    public async Task SetAsync_ShouldSaveToSourceBeforeCache()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var callOrder = new List<string>();
        var writeThrough = new WriteThroughCache<TestEntity>(
            cache,
            (_, _, _) =>
            {
                callOrder.Add("source");
                return Task.CompletedTask;
            });

        await writeThrough.SetAsync("wt-key", new TestEntity { Id = 1, Name = "WriteThrough" });
        callOrder.Add("after-set");

        callOrder.Should().ContainInOrder("source", "after-set");
        (await cache.ExistsAsync("wt-key")).Should().BeTrue();
    }

    [Fact]
    public async Task SetAsync_SourceFailure_ShouldNotUpdateCache()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var writeThrough = new WriteThroughCache<TestEntity>(
            cache,
            (_, _, _) => throw new InvalidOperationException("source failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            writeThrough.SetAsync("fail-key", new TestEntity { Id = 1, Name = "Fail" }));

        (await cache.ExistsAsync("fail-key")).Should().BeFalse();
    }
}

[Trait("Category", "Unit")]
public class WriteBehindCacheTest
{
    [Fact]
    public async Task SetAsync_ShouldCacheImmediatelyAndQueueWrite()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        int sourceCalls = 0;
        var writeBehind = new WriteBehindCache<TestEntity>(
            cache,
            (_, _, _) =>
            {
                sourceCalls++;
                return Task.CompletedTask;
            });

        await writeBehind.SetAsync("wb-key", new TestEntity { Id = 1, Name = "Behind" });

        writeBehind.PendingWritesCount.Should().Be(1);
        sourceCalls.Should().Be(0);
        (await cache.GetAsync<TestEntity>("wb-key")).Should().NotBeNull();
    }

    [Fact]
    public async Task FlushAsync_ShouldProcessQueuedWrites()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        int sourceCalls = 0;
        var writeBehind = new WriteBehindCache<TestEntity>(
            cache,
            (_, _, _) =>
            {
                sourceCalls++;
                return Task.CompletedTask;
            });
        await writeBehind.SetAsync("flush-key", new TestEntity { Id = 1, Name = "Flush" });

        await writeBehind.FlushAsync();

        sourceCalls.Should().Be(1);
        writeBehind.PendingWritesCount.Should().Be(0);
    }

    [Fact]
    public async Task ProcessPendingWritesAsync_OnFailure_ShouldRequeue()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        int attempts = 0;
        var writeBehind = new WriteBehindCache<TestEntity>(
            cache,
            (_, _, _) =>
            {
                attempts++;
                if (attempts == 1)
                {
                    throw new InvalidOperationException("transient");
                }

                return Task.CompletedTask;
            });
        await writeBehind.SetAsync("retry-key", new TestEntity { Id = 1, Name = "Retry" });

        await writeBehind.ProcessPendingWritesAsync();
        writeBehind.PendingWritesCount.Should().Be(1);

        await writeBehind.ProcessPendingWritesAsync();
        writeBehind.PendingWritesCount.Should().Be(0);
    }
}

[Trait("Category", "Unit")]
public class RefreshAheadCacheTest
{
    [Fact]
    public async Task GetAsync_Miss_ShouldLoadAndCache()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var refreshAhead = new RefreshAheadCache<TestEntity>(
            cache,
            (_, _) => Task.FromResult<TestEntity?>(new TestEntity { Id = 1, Name = "Refresh" }),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(1));

        TestEntity? result = await refreshAhead.GetAsync("ra-key");

        result!.Name.Should().Be("Refresh");
    }

    [Fact]
    public async Task RefreshAsync_ShouldInvokeBackgroundRefreshWithoutThrowing()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        int loads = 0;
        var refreshAhead = new RefreshAheadCache<TestEntity>(
            cache,
            (_, _) =>
            {
                loads++;
                return Task.FromResult<TestEntity?>(new TestEntity { Id = loads, Name = $"Load-{loads}" });
            },
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(1));
        await refreshAhead.GetAsync("bg-key");

        Func<Task> act = async () => await refreshAhead.RefreshAsync("bg-key");

        await act.Should().NotThrowAsync();
    }
}

[Trait("Category", "Unit")]
public class CacheAsideExtensionsTest
{
    [Fact]
    public async Task GetOrSetAsync_Hit_ShouldNotInvokeFactory()
    {
        var cache = new Mock<ICacheProvider>();
        var entity = new TestEntity { Id = 1, Name = "Hit" };
        cache.Setup(x => x.GetAsync<TestEntity>("key", It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        int factoryCalls = 0;

        TestEntity? result = await cache.Object.GetOrSetAsync(
            "key",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult(entity);
            },
            TimeSpan.FromMinutes(1));

        result.Should().BeSameAs(entity);
        factoryCalls.Should().Be(0);
    }

    [Fact]
    public async Task GetOrSetAsync_Miss_ShouldInvokeFactoryAndSet()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        int factoryCalls = 0;

        TestEntity? result = await cache.GetOrSetAsync(
            "aside-key",
            () =>
            {
                factoryCalls++;
                return Task.FromResult(new TestEntity { Id = 2, Name = "Aside" });
            },
            TimeSpan.FromMinutes(1));

        result!.Name.Should().Be("Aside");
        factoryCalls.Should().Be(1);
        (await cache.ExistsAsync("aside-key")).Should().BeTrue();
    }

    [Fact]
    public async Task GetOrSetAsync_NullFactoryResult_ShouldNotCache()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();

        TestEntity? result = await cache.GetOrSetAsync(
            "null-aside",
            () => Task.FromResult<TestEntity>(null!),
            TimeSpan.FromMinutes(1));

        result.Should().BeNull();
        (await cache.ExistsAsync("null-aside")).Should().BeFalse();
    }

    [Fact]
    public async Task GetOrSetAsync_NullCache_ShouldThrow()
    {
        ICacheProvider? cache = null;

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            cache!.GetOrSetAsync("key", () => Task.FromResult(new TestEntity()), TimeSpan.FromMinutes(1)));
    }
}

[Trait("Category", "Unit")]
public class CachePatternExtensionsTest
{
    [Fact]
    public void AddReadThroughCache_ShouldRegisterService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICacheProvider>(CacheTestHelpers.CreateMemoryProvider());

        services.AddReadThroughCache<TestEntity>(
            (_, _) => Task.FromResult<TestEntity?>(new TestEntity { Id = 1, Name = "DI" }));

        ServiceProvider provider = services.BuildServiceProvider();
        IReadThroughCache<TestEntity> readThrough = provider.GetRequiredService<IReadThroughCache<TestEntity>>();

        readThrough.Should().NotBeNull();
    }

    [Fact]
    public void AddWriteThroughCache_NullFactory_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddWriteThroughCache<TestEntity>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddWriteBehindBackgroundService_ShouldRegisterHostedService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICacheWarmer>(new Mock<ICacheWarmer>().Object);

        services.AddWriteBehindBackgroundService(options =>
        {
            options.FlushInterval = TimeSpan.FromSeconds(10);
            options.BatchSize = 50;
        });

        ServiceDescriptor? hosted = services.FirstOrDefault(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService));

        hosted.Should().NotBeNull();
    }
}
