using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.CronJob.Control;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using Mvp24Hours.Infrastructure.CronJob.State;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Control;

[Trait("Category", "Unit")]
public class CronJobControllerTest
{
    private readonly InMemoryCronJobStateStore _stateStore = new();
    private readonly CronJobCircuitBreaker _circuitBreaker = new();
    private readonly CronJobController _controller;

    public CronJobControllerTest()
    {
        _controller = new CronJobController(_stateStore, _circuitBreaker, NullLogger<CronJobController>.Instance);
    }

    [Fact]
    public async Task PauseAsync_ShouldPersistPauseStateAndReason()
    {
        await _stateStore.SaveStateAsync(new CronJobState("JobA"));

        bool result = await _controller.PauseAsync("JobA", "maintenance");

        result.Should().BeTrue();
        (await _stateStore.IsPausedAsync("JobA")).Should().BeTrue();
        CronJobState? state = await _stateStore.GetStateAsync("JobA");
        state!.PauseReason.Should().Be("maintenance");
    }

    [Fact]
    public async Task ResumeAsync_ShouldClearPauseState()
    {
        await _controller.PauseAsync("JobB", "test");

        bool result = await _controller.ResumeAsync("JobB");

        result.Should().BeTrue();
        (await _stateStore.IsPausedAsync("JobB")).Should().BeFalse();
    }

    [Fact]
    public async Task IsPausedAsync_ShouldDelegateToStateStore()
    {
        await _controller.PauseAsync("JobC");

        (await _controller.IsPausedAsync("JobC")).Should().BeTrue();
        (await _controller.IsPausedAsync("Missing")).Should().BeFalse();
    }

    [Fact]
    public async Task GetStatusAsync_ShouldReturnNull_WhenStateMissing()
    {
        CronJobStatus? status = await _controller.GetStatusAsync("Missing");

        status.Should().BeNull();
    }

    [Fact]
    public async Task GetStatusAsync_ShouldIncludeCircuitBreakerAndRegistrationData()
    {
        _controller.Register("JobD", "* * * * *");
        await _stateStore.SaveStateAsync(new CronJobState("JobD")
        {
            ExecutionCount = 10,
            SuccessCount = 8,
            FailureCount = 2,
            LastErrorMessage = "err"
        });
        await _controller.PauseAsync("JobD", "hold");

        CronJobStatus? status = await _controller.GetStatusAsync("JobD");

        status.Should().NotBeNull();
        status!.JobName.Should().Be("JobD");
        status.CronExpression.Should().Be("* * * * *");
        status.IsPaused.Should().BeTrue();
        status.State.Should().Be(CronJobExecutionState.Paused);
        status.ExecutionCount.Should().Be(10);
        status.SuccessCount.Should().Be(8);
        status.FailureCount.Should().Be(2);
        status.LastError.Should().Be("err");
        status.CircuitBreakerState.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task GetAllStatusesAsync_ShouldIncludeRegisteredJobsWithoutState()
    {
        _controller.Register("RegisteredOnly", "0 0 * * *");
        await _stateStore.SaveStateAsync(new CronJobState("WithState"));

        IReadOnlyList<CronJobStatus> statuses = await _controller.GetAllStatusesAsync();

        statuses.Should().HaveCount(2);
        statuses.Should().Contain(s => s.JobName == "RegisteredOnly" && s.State == CronJobExecutionState.Idle);
        statuses.Should().Contain(s => s.JobName == "WithState");
    }

    [Fact]
    public async Task TriggerAsync_ShouldReturnFalse_WhenPaused()
    {
        _controller.Register("PausedJob", triggerCallback: _ => Task.CompletedTask);
        await _controller.PauseAsync("PausedJob");

        bool result = await _controller.TriggerAsync("PausedJob");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TriggerAsync_ShouldInvokeCallback_WhenRegistered()
    {
        var triggered = false;
        _controller.Register("TriggerJob", triggerCallback: _ =>
        {
            triggered = true;
            return Task.CompletedTask;
        });

        bool result = await _controller.TriggerAsync("TriggerJob");

        result.Should().BeTrue();
        triggered.Should().BeTrue();
    }

    [Fact]
    public async Task TriggerAsync_ShouldReturnFalse_WhenCallbackMissing()
    {
        bool result = await _controller.TriggerAsync("NoCallback");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TriggerAsync_ShouldReturnFalse_WhenCallbackThrows()
    {
        _controller.Register("FailingTrigger", triggerCallback: _ => throw new InvalidOperationException("fail"));

        bool result = await _controller.TriggerAsync("FailingTrigger");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task PauseAllAsync_AndResumeAllAsync_ShouldAffectRegisteredJobs()
    {
        _controller.Register("Job1");
        _controller.Register("Job2");

        await _controller.PauseAllAsync("global");
        (await _controller.IsPausedAsync("Job1")).Should().BeTrue();
        (await _controller.IsPausedAsync("Job2")).Should().BeTrue();

        await _controller.ResumeAllAsync();
        (await _controller.IsPausedAsync("Job1")).Should().BeFalse();
        (await _controller.IsPausedAsync("Job2")).Should().BeFalse();
    }

    [Fact]
    public async Task PauseAsyncT_ShouldUseTypeName()
    {
        await _controller.PauseAsync<MarkerJob>("typed");

        (await _controller.IsPausedAsync(nameof(MarkerJob))).Should().BeTrue();
    }

    private sealed class MarkerJob;
}
