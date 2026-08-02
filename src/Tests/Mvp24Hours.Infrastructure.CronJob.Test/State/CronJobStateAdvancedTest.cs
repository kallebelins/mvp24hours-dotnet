using Mvp24Hours.Infrastructure.CronJob.State;

namespace Mvp24Hours.Infrastructure.CronJob.Test.State;

/// <summary>
/// Additional state-store coverage (RecordRetry/Skipped, properties, restart semantics).
/// Redis/SqlServer stores do not exist in this assembly — only in-memory persistence is tested.
/// </summary>
[Trait("Category", "Unit")]
public class CronJobStateAdvancedTest
{
    [Fact]
    public void CronJobState_RecordRetry_ShouldIncrementRetryCount()
    {
        var state = new CronJobState("RetryJob");

        state.RecordRetry();
        state.RecordRetry();

        state.RetryCount.Should().Be(2);
        state.LastUpdated.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CronJobState_RecordSkipped_ShouldIncrementSkippedCount()
    {
        var state = new CronJobState("SkipJob");

        state.RecordSkipped();

        state.SkippedCount.Should().Be(1);
    }

    [Fact]
    public async Task SaveStateAsync_ShouldPersistCustomProperties()
    {
        var store = new InMemoryCronJobStateStore();
        var state = new CronJobState("PropsJob")
        {
            Properties =
            {
                ["batchId"] = 42,
                ["source"] = "phase25"
            }
        };

        await store.SaveStateAsync(state);

        CronJobState? loaded = await store.GetStateAsync("PropsJob");
        loaded!.Properties["batchId"].Should().Be(42);
        loaded.Properties["source"].Should().Be("phase25");
    }

    [Fact]
    public async Task NewStoreInstance_ShouldNotRetainPreviousState()
    {
        var store1 = new InMemoryCronJobStateStore();
        await store1.SaveStateAsync(new CronJobState("Ephemeral") { ExecutionCount = 9 });

        var store2 = new InMemoryCronJobStateStore();

        (await store2.GetStateAsync("Ephemeral")).Should().BeNull();
        (await store2.GetAllStatesAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteStateAsync_ShouldMakeIsPausedReturnFalse()
    {
        var store = new InMemoryCronJobStateStore();
        await store.SetPausedAsync("PausedThenDeleted", true);

        await store.DeleteStateAsync("PausedThenDeleted");

        (await store.IsPausedAsync("PausedThenDeleted")).Should().BeFalse();
        (await store.GetStateAsync("PausedThenDeleted")).Should().BeNull();
    }

    [Fact]
    public async Task GetAllStatesAsync_ShouldKeepJobsIndependent()
    {
        var store = new InMemoryCronJobStateStore();
        var a = new CronJobState("A");
        a.RecordSuccess(TimeSpan.FromMilliseconds(10));
        var b = new CronJobState("B");
        b.RecordFailure(TimeSpan.FromMilliseconds(20), "err");

        await store.SaveStateAsync(a);
        await store.SaveStateAsync(b);

        IReadOnlyList<CronJobState> all = await store.GetAllStatesAsync();
        all.Should().HaveCount(2);
        all.Single(x => x.JobName == "A").SuccessCount.Should().Be(1);
        all.Single(x => x.JobName == "B").FailureCount.Should().Be(1);
    }

    [Fact]
    public void CronJobState_RecordSuccess_WithZeroDuration_ShouldUpdateStats()
    {
        var state = new CronJobState("Zero");

        state.RecordSuccess(TimeSpan.Zero);

        state.ExecutionCount.Should().Be(1);
        state.AverageDurationMs.Should().Be(0);
        state.MinDurationMs.Should().Be(0);
        state.MaxDurationMs.Should().Be(0);
    }

    [Fact]
    public async Task ConcurrentSaveStateAsync_ShouldKeepLastWrite()
    {
        var store = new InMemoryCronJobStateStore();

        await Task.WhenAll(
            Enumerable.Range(0, 20).Select(async i => await store.SaveStateAsync(new CronJobState("Concurrent") { ExecutionCount = i })));

        CronJobState? loaded = await store.GetStateAsync("Concurrent");
        loaded.Should().NotBeNull();
        loaded!.ExecutionCount.Should().BeInRange(0, 19);
    }

    [Fact]
    public async Task SetPausedAsync_ShouldSetPausedAt_WhenPausing()
    {
        var store = new InMemoryCronJobStateStore();

        await store.SetPausedAsync("PauseReasonJob", true);

        CronJobState? state = await store.GetStateAsync("PauseReasonJob");
        state!.IsPaused.Should().BeTrue();
        state.PausedAt.Should().NotBeNull();
    }

    [Fact]
    public void CronJobState_Constructor_ShouldThrow_WhenJobNameNull()
    {
        Action act = () => _ = new CronJobState(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("jobName");
    }
}
