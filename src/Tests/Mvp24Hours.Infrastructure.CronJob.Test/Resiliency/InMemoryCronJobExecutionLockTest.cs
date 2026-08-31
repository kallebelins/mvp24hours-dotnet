using Mvp24Hours.Infrastructure.CronJob.Resiliency;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Resiliency;

[Trait("Category", "Unit")]
public class InMemoryCronJobExecutionLockTest
{
    [Fact]
    public void IsLocked_ForUnknownJob_ShouldReturnFalse()
    {
        var executionLock = new InMemoryCronJobExecutionLock();

        executionLock.IsLocked("NeverLocked").Should().BeFalse();
    }

    [Fact]
    public async Task IsLocked_AfterAcquire_ShouldReturnTrue()
    {
        var executionLock = new InMemoryCronJobExecutionLock();

        ICronJobLockHandle? handle = await executionLock.TryAcquireAsync("JobA", TimeSpan.Zero);

        handle.Should().NotBeNull();
        executionLock.IsLocked("JobA").Should().BeTrue();
    }

    [Fact]
    public async Task IsLocked_AfterRelease_ShouldReturnFalse()
    {
        var executionLock = new InMemoryCronJobExecutionLock();
        ICronJobLockHandle? handle = await executionLock.TryAcquireAsync("JobB", TimeSpan.Zero);

        await handle!.ReleaseAsync();

        executionLock.IsLocked("JobB").Should().BeFalse();
    }

    [Fact]
    public async Task TryAcquireAsync_WithZeroTimeout_WhenAlreadyLocked_ShouldReturnNull()
    {
        var executionLock = new InMemoryCronJobExecutionLock();
        await executionLock.TryAcquireAsync("JobC", TimeSpan.Zero);

        ICronJobLockHandle? second = await executionLock.TryAcquireAsync("JobC", TimeSpan.Zero);

        second.Should().BeNull();
    }

    [Fact]
    public async Task TryAcquireAsync_WithTimeout_WhenReleasedBeforeTimeoutElapses_ShouldSucceed()
    {
        var executionLock = new InMemoryCronJobExecutionLock();
        ICronJobLockHandle? first = await executionLock.TryAcquireAsync("JobD", TimeSpan.Zero);

        Task<ICronJobLockHandle?> secondTask = executionLock.TryAcquireAsync("JobD", TimeSpan.FromSeconds(5));
        await first!.ReleaseAsync();
        ICronJobLockHandle? second = await secondTask;

        second.Should().NotBeNull();
    }

    [Fact]
    public async Task TryAcquireAsync_WithNullOrWhitespaceJobName_ShouldThrow()
    {
        var executionLock = new InMemoryCronJobExecutionLock();

        Func<Task> act = () => executionLock.TryAcquireAsync(" ", TimeSpan.Zero);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetLockAcquiredTime_ForUnknownJob_ShouldReturnNull()
    {
        var executionLock = new InMemoryCronJobExecutionLock();

        executionLock.GetLockAcquiredTime("Unknown").Should().BeNull();
    }

    [Fact]
    public async Task GetLockAcquiredTime_WhileLocked_ShouldReturnAcquiredTimeFromTimeProvider()
    {
        var fakeTime = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var executionLock = new InMemoryCronJobExecutionLock(fakeTime);

        await executionLock.TryAcquireAsync("JobE", TimeSpan.Zero);

        executionLock.GetLockAcquiredTime("JobE").Should().Be(fakeTime.GetUtcNow());
    }

    [Fact]
    public async Task GetLockAcquiredTime_AfterRelease_ShouldReturnNull()
    {
        var executionLock = new InMemoryCronJobExecutionLock();
        ICronJobLockHandle? handle = await executionLock.TryAcquireAsync("JobF", TimeSpan.Zero);
        await handle!.ReleaseAsync();

        executionLock.GetLockAcquiredTime("JobF").Should().BeNull();
    }

    [Fact]
    public async Task Handle_IsValid_ShouldBeTrueBeforeReleaseAndFalseAfter()
    {
        var executionLock = new InMemoryCronJobExecutionLock();
        ICronJobLockHandle handle = (await executionLock.TryAcquireAsync("JobG", TimeSpan.Zero))!;

        handle.IsValid.Should().BeTrue();

        await handle.ReleaseAsync();

        handle.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ReleaseAsync_CalledTwice_ShouldOnlyReleaseOnce()
    {
        var executionLock = new InMemoryCronJobExecutionLock();
        ICronJobLockHandle handle = (await executionLock.TryAcquireAsync("JobH", TimeSpan.Zero))!;

        await handle.ReleaseAsync();
        await handle.ReleaseAsync();

        // A double-release must not throw or over-release the semaphore (which would allow
        // more than one concurrent lock holder); re-acquiring exactly once must still succeed.
        ICronJobLockHandle? reacquired = await executionLock.TryAcquireAsync("JobH", TimeSpan.Zero);
        reacquired.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_DisposeAsync_ShouldReleaseLock()
    {
        var executionLock = new InMemoryCronJobExecutionLock();
        ICronJobLockHandle handle = (await executionLock.TryAcquireAsync("JobI", TimeSpan.Zero))!;

        await handle.DisposeAsync();

        executionLock.IsLocked("JobI").Should().BeFalse();
    }

    [Fact]
    public async Task Handle_JobNameAndAcquiredAt_ShouldMatchAcquisition()
    {
        var fakeTime = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(DateTimeOffset.Parse("2026-06-15T12:00:00Z"));
        var executionLock = new InMemoryCronJobExecutionLock(fakeTime);

        ICronJobLockHandle handle = (await executionLock.TryAcquireAsync("JobJ", TimeSpan.Zero))!;

        handle.JobName.Should().Be("JobJ");
        handle.AcquiredAt.Should().Be(fakeTime.GetUtcNow());
    }
}
