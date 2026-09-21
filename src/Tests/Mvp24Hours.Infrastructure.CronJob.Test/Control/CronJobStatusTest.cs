using Mvp24Hours.Infrastructure.CronJob.Control;
using Mvp24Hours.Infrastructure.CronJob.State;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Control;

[Trait("Category", "Unit")]
public class CronJobStatusTest
{
    [Fact]
    public void FromState_WhenNotPaused_ShouldMapToIdleState()
    {
        var state = new CronJobState("JobA")
        {
            ExecutionCount = 5,
            SuccessCount = 4,
            FailureCount = 1,
            LastErrorMessage = "boom",
            AverageDurationMs = 12.5,
            LastExecutionTime = DateTimeOffset.UtcNow
        };
        DateTimeOffset next = DateTimeOffset.UtcNow.AddMinutes(5);

        CronJobStatus status = CronJobStatus.FromState(state, "* * * * *", next);

        status.JobName.Should().Be("JobA");
        status.CronExpression.Should().Be("* * * * *");
        status.State.Should().Be(CronJobExecutionState.Idle);
        status.IsPaused.Should().BeFalse();
        status.PauseReason.Should().BeNull();
        status.PausedAt.Should().BeNull();
        status.LastExecutionTime.Should().Be(state.LastExecutionTime);
        status.NextExecutionTime.Should().Be(next);
        status.ExecutionCount.Should().Be(5);
        status.SuccessCount.Should().Be(4);
        status.FailureCount.Should().Be(1);
        status.LastError.Should().Be("boom");
        status.AverageDurationMs.Should().Be(12.5);
    }

    [Fact]
    public void FromState_WhenPaused_ShouldMapToPausedStateAndIncludePauseInfo()
    {
        DateTimeOffset pausedAt = DateTimeOffset.UtcNow;
        var state = new CronJobState("JobB")
        {
            IsPaused = true,
            PauseReason = "maintenance",
            PausedAt = pausedAt
        };

        CronJobStatus status = CronJobStatus.FromState(state);

        status.State.Should().Be(CronJobExecutionState.Paused);
        status.IsPaused.Should().BeTrue();
        status.PauseReason.Should().Be("maintenance");
        status.PausedAt.Should().Be(pausedAt);
        status.CronExpression.Should().BeNull();
        status.NextExecutionTime.Should().BeNull();
    }
}
