using Microsoft.Extensions.Time.Testing;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Resiliency;

/// <summary>
/// Behavioral tests for <see cref="InMemoryDistributedCronJobLock"/> and resilience config factories.
/// </summary>
[Trait("Category", "Unit")]
public class InMemoryDistributedCronJobLockTest
{
    [Fact]
    public async Task TryAcquireAsync_ShouldReturnHandle_ForFirstInstance()
    {
        var time = new FakeTimeProvider();
        var distributedLock = new InMemoryDistributedCronJobLock(time);

        IDistributedCronJobLockHandle? handle = await distributedLock.TryAcquireAsync(
            "JobA", "instance-1", TimeSpan.FromMinutes(5));

        handle.Should().NotBeNull();
        handle!.IsValid.Should().BeTrue();
        handle.InstanceId.Should().Be("instance-1");
        (await distributedLock.IsLockedAsync("JobA")).Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldReturnNull_WhenHeldByAnotherInstance()
    {
        var time = new FakeTimeProvider();
        var distributedLock = new InMemoryDistributedCronJobLock(time);

        await using IDistributedCronJobLockHandle? first = await distributedLock.TryAcquireAsync(
            "JobA", "instance-1", TimeSpan.FromMinutes(5));

        IDistributedCronJobLockHandle? second = await distributedLock.TryAcquireAsync(
            "JobA", "instance-2", TimeSpan.FromMinutes(5));

        first.Should().NotBeNull();
        second.Should().BeNull();
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldAllowSameInstanceToReacquire()
    {
        var time = new FakeTimeProvider();
        var distributedLock = new InMemoryDistributedCronJobLock(time);

        await using IDistributedCronJobLockHandle? first = await distributedLock.TryAcquireAsync(
            "JobA", "instance-1", TimeSpan.FromMinutes(1));

        IDistributedCronJobLockHandle? second = await distributedLock.TryAcquireAsync(
            "JobA", "instance-1", TimeSpan.FromMinutes(2));

        second.Should().NotBeNull();
        second!.IsValid.Should().BeTrue();
        await second.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldSucceed_AfterExpiry()
    {
        var time = new FakeTimeProvider();
        var distributedLock = new InMemoryDistributedCronJobLock(time);

        await using IDistributedCronJobLockHandle? first = await distributedLock.TryAcquireAsync(
            "JobA", "instance-1", TimeSpan.FromMinutes(1));

        time.Advance(TimeSpan.FromMinutes(2));

        IDistributedCronJobLockHandle? second = await distributedLock.TryAcquireAsync(
            "JobA", "instance-2", TimeSpan.FromMinutes(1));

        second.Should().NotBeNull();
        await second!.DisposeAsync();
    }

    [Fact]
    public async Task ExtendAsync_ShouldExtendExpiry()
    {
        var time = new FakeTimeProvider();
        var distributedLock = new InMemoryDistributedCronJobLock(time);

        await using IDistributedCronJobLockHandle? handle = await distributedLock.TryAcquireAsync(
            "JobA", "instance-1", TimeSpan.FromMinutes(1));

        bool extended = await handle!.ExtendAsync(TimeSpan.FromMinutes(5));

        extended.Should().BeTrue();
        handle.IsValid.Should().BeTrue();

        time.Advance(TimeSpan.FromMinutes(2));
        handle.IsValid.Should().BeTrue();

        DistributedLockInfo? info = await distributedLock.GetLockInfoAsync("JobA");
        info.Should().NotBeNull();
        info!.InstanceId.Should().Be("instance-1");
    }

    [Fact]
    public async Task ReleaseAsync_ShouldAllowOtherInstanceToAcquire()
    {
        var time = new FakeTimeProvider();
        var distributedLock = new InMemoryDistributedCronJobLock(time);

        IDistributedCronJobLockHandle? first = await distributedLock.TryAcquireAsync(
            "JobA", "instance-1", TimeSpan.FromMinutes(5));

        await first!.ReleaseAsync();

        IDistributedCronJobLockHandle? second = await distributedLock.TryAcquireAsync(
            "JobA", "instance-2", TimeSpan.FromMinutes(5));

        second.Should().NotBeNull();
        await second!.DisposeAsync();
    }

    [Fact]
    public async Task GetLockInfoAsync_ShouldReturnNull_WhenExpired()
    {
        var time = new FakeTimeProvider();
        var distributedLock = new InMemoryDistributedCronJobLock(time);

        await using IDistributedCronJobLockHandle? handle = await distributedLock.TryAcquireAsync(
            "JobA", "instance-1", TimeSpan.FromSeconds(30));

        time.Advance(TimeSpan.FromMinutes(1));

        (await distributedLock.GetLockInfoAsync("JobA")).Should().BeNull();
        (await distributedLock.IsLockedAsync("JobA")).Should().BeFalse();
        handle!.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ExtendAsync_ShouldReturnFalse_AfterRelease()
    {
        var time = new FakeTimeProvider();
        var distributedLock = new InMemoryDistributedCronJobLock(time);

        IDistributedCronJobLockHandle? handle = await distributedLock.TryAcquireAsync(
            "JobA", "instance-1", TimeSpan.FromMinutes(1));

        await handle!.ReleaseAsync();

        (await handle.ExtendAsync(TimeSpan.FromMinutes(1))).Should().BeFalse();
        handle.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Clear_ShouldRemoveAllLocks()
    {
        var distributedLock = new InMemoryDistributedCronJobLock();
        await using IDistributedCronJobLockHandle? _ = await distributedLock.TryAcquireAsync(
            "JobA", "i1", TimeSpan.FromMinutes(1));

        distributedLock.Clear();

        (await distributedLock.IsLockedAsync("JobA")).Should().BeFalse();
    }

    [Fact]
    public void CronJobResilienceConfig_Default_ShouldDisableRetryAndCircuitBreaker()
    {
        CronJobResilienceConfig<object> config = CronJobResilienceConfig<object>.Default();

        config.EnableRetry.Should().BeFalse();
        config.EnableCircuitBreaker.Should().BeFalse();
        config.PreventOverlapping.Should().BeTrue();
    }

    [Fact]
    public void CronJobResilienceConfig_ToString_ShouldIncludeTimeout_WhenSet()
    {
        var config = new CronJobResilienceConfig<object>
        {
            EnableRetry = true,
            MaxRetryAttempts = 2,
            ExecutionTimeout = TimeSpan.FromSeconds(15)
        };

        string text = config.ToString();

        text.Should().Contain("Retry(2x)");
        text.Should().Contain("Timeout(15s)");
    }

    [Fact]
    public void CronJobResilienceConfig_FullResilience_ShouldEnableAllFeatures()
    {
        CronJobResilienceConfig<object> config = CronJobResilienceConfig<object>.FullResilience();

        config.EnableRetry.Should().BeTrue();
        config.EnableCircuitBreaker.Should().BeTrue();
        config.PreventOverlapping.Should().BeTrue();
        config.PropagateCancellation.Should().BeTrue();
    }
}
