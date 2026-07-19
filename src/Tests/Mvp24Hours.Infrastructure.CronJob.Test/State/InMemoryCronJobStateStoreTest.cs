using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.CronJob.Control;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using Mvp24Hours.Infrastructure.CronJob.State;

namespace Mvp24Hours.Infrastructure.CronJob.Test.State;

[Trait("Category", "Unit")]
public class InMemoryCronJobStateStoreTest
{
    private readonly InMemoryCronJobStateStore _store = new();

    [Fact]
    public async Task GetStateAsync_ShouldReturnNull_WhenNotExists()
    {
        CronJobState? state = await _store.GetStateAsync("MissingJob");

        state.Should().BeNull();
    }

    [Fact]
    public async Task SaveStateAsync_ShouldPersistState()
    {
        var state = new CronJobState("JobA") { ExecutionCount = 5 };

        await _store.SaveStateAsync(state);

        CronJobState? loaded = await _store.GetStateAsync("JobA");
        loaded.Should().NotBeNull();
        loaded!.ExecutionCount.Should().Be(5);
    }

    [Fact]
    public async Task DeleteStateAsync_ShouldRemoveState()
    {
        await _store.SaveStateAsync(new CronJobState("JobB"));

        await _store.DeleteStateAsync("JobB");

        (await _store.GetStateAsync("JobB")).Should().BeNull();
    }

    [Fact]
    public async Task GetAllStatesAsync_ShouldReturnAllEntries()
    {
        await _store.SaveStateAsync(new CronJobState("Job1"));
        await _store.SaveStateAsync(new CronJobState("Job2"));

        IReadOnlyList<CronJobState> states = await _store.GetAllStatesAsync();

        states.Should().HaveCount(2);
        states.Select(s => s.JobName).Should().BeEquivalentTo(["Job1", "Job2"]);
    }

    [Fact]
    public async Task IsPausedAsync_ShouldReturnFalse_WhenNotExists()
    {
        (await _store.IsPausedAsync("Unknown")).Should().BeFalse();
    }

    [Fact]
    public async Task SetPausedAsync_ShouldCreateStateAndMarkPaused()
    {
        await _store.SetPausedAsync("PausedJob", true);

        (await _store.IsPausedAsync("PausedJob")).Should().BeTrue();
        CronJobState? state = await _store.GetStateAsync("PausedJob");
        state!.IsPaused.Should().BeTrue();
        state.PausedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SetPausedAsync_ShouldClearPauseReason_WhenResumed()
    {
        var state = new CronJobState("ResumeJob") { IsPaused = true, PauseReason = "maintenance" };
        await _store.SaveStateAsync(state);

        await _store.SetPausedAsync("ResumeJob", false);

        CronJobState? updated = await _store.GetStateAsync("ResumeJob");
        updated!.IsPaused.Should().BeFalse();
        updated.PausedAt.Should().BeNull();
        updated.PauseReason.Should().BeNull();
    }

    [Fact]
    public void GetOrCreate_ShouldReturnExistingOrNewState()
    {
        CronJobState first = _store.GetOrCreate("CreateJob");
        CronJobState second = _store.GetOrCreate("CreateJob");

        first.Should().BeSameAs(second);
        first.JobName.Should().Be("CreateJob");
    }

    [Fact]
    public async Task Clear_ShouldRemoveAllStates()
    {
        await _store.SaveStateAsync(new CronJobState("X"));
        _store.Clear();

        (await _store.GetAllStatesAsync()).Should().BeEmpty();
    }

    [Fact]
    public void CronJobState_RecordSuccessAndFailure_ShouldUpdateCounters()
    {
        var state = new CronJobState("StatsJob");

        state.RecordSuccess(TimeSpan.FromMilliseconds(100));
        state.RecordSuccess(TimeSpan.FromMilliseconds(300));
        state.RecordFailure(TimeSpan.FromMilliseconds(200), "error");

        state.ExecutionCount.Should().Be(3);
        state.SuccessCount.Should().Be(2);
        state.FailureCount.Should().Be(1);
        state.LastErrorMessage.Should().Be("error");
        state.AverageDurationMs.Should().BeApproximately(200, 0.1);
        state.MinDurationMs.Should().Be(100);
        state.MaxDurationMs.Should().Be(300);
    }
}
