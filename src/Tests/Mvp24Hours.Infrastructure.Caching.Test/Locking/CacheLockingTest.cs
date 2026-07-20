using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Extensions;
using Mvp24Hours.Infrastructure.Caching.Invalidation;
using Mvp24Hours.Infrastructure.Caching.Test.Support;

namespace Mvp24Hours.Infrastructure.Caching.Test.Locking;

/// <summary>
/// Locking coverage maps to <see cref="CacheStampedePrevention"/> (SemaphoreSlim lock
/// acquire / release / timeout). There is no DistributedCacheLock type in this assembly.
/// </summary>
[Trait("Category", "Unit")]
public class CacheStampedeLockingTest
{
    [Fact]
    public async Task ExecuteAsync_EmptyKey_ShouldThrow()
    {
        var prevention = new CacheStampedePrevention();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            prevention.ExecuteAsync(" ", _ => Task.FromResult(1)));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAcquireLockExecuteAndRelease()
    {
        var prevention = new CacheStampedePrevention();
        int calls = 0;

        int result = await prevention.ExecuteAsync(
            "lock-key",
            _ =>
            {
                Interlocked.Increment(ref calls);
                return Task.FromResult(42);
            });

        result.Should().Be(42);
        calls.Should().Be(1);

        // Lock released — subsequent call should also succeed
        int second = await prevention.ExecuteAsync("lock-key", _ => Task.FromResult(7));
        second.Should().Be(7);
    }

    [Fact]
    public async Task ExecuteAsync_Timeout_ShouldThrowTimeoutException()
    {
        var prevention = new CacheStampedePrevention();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<int> holder = prevention.ExecuteAsync(
            "timeout-key",
            async _ =>
            {
                await gate.Task;
                return 1;
            },
            timeout: TimeSpan.FromSeconds(30));

        // Wait until the first call holds the lock
        await Task.Delay(50);

        Func<Task> contender = () => prevention.ExecuteAsync(
            "timeout-key",
            _ => Task.FromResult(2),
            timeout: TimeSpan.FromMilliseconds(50));

        await contender.Should().ThrowAsync<TimeoutException>();

        gate.SetResult();
        (await holder).Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_DifferentKeys_ShouldNotBlockEachOther()
    {
        var prevention = new CacheStampedePrevention();
        var started = new CountdownEvent(2);
        var release = new ManualResetEventSlim(false);

        async Task<int> Run(string key, int value)
        {
            return await prevention.ExecuteAsync(key, async _ =>
            {
                started.Signal();
                await Task.Run(() => release.Wait(TimeSpan.FromSeconds(5)));
                return value;
            });
        }

        Task<int> t1 = Run("key-a", 1);
        Task<int> t2 = Run("key-b", 2);

        started.Wait(TimeSpan.FromSeconds(2)).Should().BeTrue();
        release.Set();

        int[] results = await Task.WhenAll(t1, t2);
        results.Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public async Task ExecuteAsync_SameKeySequential_ShouldAllowRetryAfterRelease()
    {
        var prevention = new CacheStampedePrevention();

        await prevention.ExecuteAsync("retry-key", _ => Task.FromResult("first"));
        string second = await prevention.ExecuteAsync("retry-key", _ => Task.FromResult("second"));

        second.Should().Be("second");
    }

    [Fact]
    public async Task ExecuteAsync_CustomTimeout_ShouldBeHonored()
    {
        var prevention = new CacheStampedePrevention();
        var hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _ = prevention.ExecuteAsync(
            "custom-timeout",
            async _ =>
            {
                await hold.Task;
                return true;
            },
            timeout: TimeSpan.FromSeconds(10));

        await Task.Delay(30);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        Func<Task> act = () => prevention.ExecuteAsync(
            "custom-timeout",
            _ => Task.FromResult(false),
            timeout: TimeSpan.FromMilliseconds(80));

        await act.Should().ThrowAsync<TimeoutException>();
        sw.Stop();
        sw.ElapsedMilliseconds.Should().BeLessThan(2000);

        hold.SetResult();
    }

    [Fact]
    public async Task ExecuteAsync_Cancellation_ShouldPropagate()
    {
        var prevention = new CacheStampedePrevention();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            prevention.ExecuteAsync(
                "cancel-key",
                _ => Task.FromResult(1),
                cancellationToken: cts.Token));
    }

    [Fact]
    public void AddCacheStampedePrevention_ShouldRegisterAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddMemoryCacheProvider();
        services.AddCacheStampedePrevention();
        ServiceProvider provider = services.BuildServiceProvider();

        ICacheStampedePrevention first = provider.GetRequiredService<ICacheStampedePrevention>();
        ICacheStampedePrevention second = provider.GetRequiredService<ICacheStampedePrevention>();

        first.Should().BeSameAs(second);
        first.Should().BeOfType<CacheStampedePrevention>();
    }

    [Fact]
    public async Task GetOrSetAsync_WithoutStampede_ShouldStillComputeOncePerCallOnMiss()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        int calls = 0;

        TestEntity first = await CacheInvalidationExtensions.GetOrSetAsync(
            cache,
            "gs-key",
            _ =>
            {
                calls++;
                return Task.FromResult(new TestEntity { Id = 1, Name = "A" });
            });

        TestEntity second = await CacheInvalidationExtensions.GetOrSetAsync(
            cache,
            "gs-key",
            _ =>
            {
                calls++;
                return Task.FromResult(new TestEntity { Id = 2, Name = "B" });
            });

        first.Name.Should().Be("A");
        second.Name.Should().Be("A");
        calls.Should().Be(1);
    }

    [Fact]
    public async Task GetOrSetAsync_WithStampede_ConcurrentMisses_ShouldSerializeFactory()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var stampede = new CacheStampedePrevention();
        int factoryCalls = 0;

        Task<TestEntity>[] tasks = Enumerable.Range(0, 8).Select(_ => CacheInvalidationExtensions.GetOrSetAsync(
            cache,
            "stampede-gs",
            async ct =>
            {
                Interlocked.Increment(ref factoryCalls);
                await Task.Delay(30, ct);
                return new TestEntity { Id = 1, Name = "Shared" };
            },
            stampedePrevention: stampede)).ToArray();

        TestEntity[] results = await Task.WhenAll(tasks);

        results.Should().OnlyContain(r => r.Name == "Shared");
        factoryCalls.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_FactoryReturnsNull_ShouldComplete()
    {
        var prevention = new CacheStampedePrevention();

        string? result = await prevention.ExecuteAsync<string?>(
            "null-result",
            _ => Task.FromResult<string?>(null));

        result.Should().BeNull();
    }

    [Fact]
    public void AddCacheStampedePrevention_NullServices_ShouldThrow()
    {
        Action act = () => CacheInvalidationServiceExtensions.AddCacheStampedePrevention(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
