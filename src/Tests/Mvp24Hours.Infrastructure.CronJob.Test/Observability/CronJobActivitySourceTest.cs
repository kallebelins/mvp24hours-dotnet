using System.Diagnostics;
using Mvp24Hours.Infrastructure.CronJob.Observability;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Observability;

/// <summary>
/// <see cref="CronJobActivitySource"/>'s Start*Activity methods return <c>null</c> unless an
/// <see cref="ActivityListener"/> is registered and samples the source (this mirrors how
/// OpenTelemetry works in production). This fixture registers a listener that samples
/// everything for the lifetime of each test, then disposes it, matching the pattern already
/// established elsewhere in this repo for testing ActivitySource-based code.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CronJobActivitySourceTest : IDisposable
{
    private readonly ActivityListener _listener;

    public CronJobActivitySourceTest()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == CronJobActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    [Fact]
    public void StartExecuteActivity_WithCronExpressionAndTimeZone_ShouldSetAllTags()
    {
        using Activity? activity = CronJobActivitySource.StartExecuteActivity("JobA", "* * * * *", "UTC");

        activity.Should().NotBeNull();
        activity!.GetTagItem(CronJobActivitySource.Tags.JobName).Should().Be("JobA");
        activity.GetTagItem(CronJobActivitySource.Tags.CronExpression).Should().Be("* * * * *");
        activity.GetTagItem(CronJobActivitySource.Tags.TimeZone).Should().Be("UTC");
    }

    [Fact]
    public void StartExecuteActivity_WithoutOptionalArguments_ShouldOnlySetJobNameTag()
    {
        using Activity? activity = CronJobActivitySource.StartExecuteActivity("JobA");

        activity.Should().NotBeNull();
        activity!.GetTagItem(CronJobActivitySource.Tags.JobName).Should().Be("JobA");
        activity.GetTagItem(CronJobActivitySource.Tags.CronExpression).Should().BeNull();
        activity.GetTagItem(CronJobActivitySource.Tags.TimeZone).Should().BeNull();
    }

    [Fact]
    public void StartScheduleActivity_WithNextExecution_ShouldSetAllTags()
    {
        DateTimeOffset next = DateTimeOffset.UtcNow.AddMinutes(1);

        using Activity? activity = CronJobActivitySource.StartScheduleActivity("JobB", "0 0 * * *", next);

        activity.Should().NotBeNull();
        activity!.GetTagItem(CronJobActivitySource.Tags.JobName).Should().Be("JobB");
        activity.GetTagItem(CronJobActivitySource.Tags.CronExpression).Should().Be("0 0 * * *");
        activity.GetTagItem(CronJobActivitySource.Tags.NextExecution).Should().Be(next.ToString("O"));
    }

    [Fact]
    public void StartScheduleActivity_WithoutNextExecution_ShouldNotSetNextExecutionTag()
    {
        using Activity? activity = CronJobActivitySource.StartScheduleActivity("JobB", "0 0 * * *");

        activity.Should().NotBeNull();
        activity!.GetTagItem(CronJobActivitySource.Tags.NextExecution).Should().BeNull();
    }

    [Fact]
    public void StartStartActivity_WithCronExpression_ShouldSetTags()
    {
        using Activity? activity = CronJobActivitySource.StartStartActivity("JobC", "* * * * *");

        activity.Should().NotBeNull();
        activity!.GetTagItem(CronJobActivitySource.Tags.JobName).Should().Be("JobC");
        activity.GetTagItem(CronJobActivitySource.Tags.CronExpression).Should().Be("* * * * *");
    }

    [Fact]
    public void StartStartActivity_WithoutCronExpression_ShouldNotSetCronExpressionTag()
    {
        using Activity? activity = CronJobActivitySource.StartStartActivity("JobC");

        activity.Should().NotBeNull();
        activity!.GetTagItem(CronJobActivitySource.Tags.CronExpression).Should().BeNull();
    }

    [Fact]
    public void StartStopActivity_ShouldSetJobNameTag()
    {
        using Activity? activity = CronJobActivitySource.StartStopActivity("JobD");

        activity.Should().NotBeNull();
        activity!.GetTagItem(CronJobActivitySource.Tags.JobName).Should().Be("JobD");
    }

    [Fact]
    public void StartRetryActivity_ShouldSetRetryTags()
    {
        using Activity? activity = CronJobActivitySource.StartRetryActivity("JobE", attemptNumber: 2, maxAttempts: 5, delayMs: 250);

        activity.Should().NotBeNull();
        activity!.GetTagItem(CronJobActivitySource.Tags.JobName).Should().Be("JobE");
        activity.GetTagItem(CronJobActivitySource.Tags.RetryAttempt).Should().Be(2);
        activity.GetTagItem(CronJobActivitySource.Tags.MaxRetryAttempts).Should().Be(5);
        activity.GetTagItem("cronjob.resilience.retry_delay_ms").Should().Be(250d);
    }

    [Fact]
    public void StartCircuitBreakerStateChangeActivity_ShouldSetStateTags()
    {
        using Activity? activity = CronJobActivitySource.StartCircuitBreakerStateChangeActivity("JobF", "Closed", "Open");

        activity.Should().NotBeNull();
        activity!.GetTagItem(CronJobActivitySource.Tags.JobName).Should().Be("JobF");
        activity.GetTagItem("cronjob.resilience.circuit_breaker_previous_state").Should().Be("Closed");
        activity.GetTagItem(CronJobActivitySource.Tags.CircuitBreakerState).Should().Be("Open");
    }

    [Fact]
    public void StartSkippedExecutionActivity_ShouldSetSkipTags()
    {
        using Activity? activity = CronJobActivitySource.StartSkippedExecutionActivity("JobG", "overlapping");

        activity.Should().NotBeNull();
        activity!.GetTagItem(CronJobActivitySource.Tags.JobName).Should().Be("JobG");
        activity.GetTagItem(CronJobActivitySource.Tags.ExecutionSkipped).Should().Be(true);
        activity.GetTagItem(CronJobActivitySource.Tags.SkipReason).Should().Be("overlapping");
    }

    [Fact]
    public void SetExecutionResult_WithSuccess_ShouldSetOkStatusAndTags()
    {
        using Activity? activity = CronJobActivitySource.StartExecuteActivity("JobH");

        activity.SetExecutionResult(success: true, durationMs: 42);

        activity!.GetTagItem(CronJobActivitySource.Tags.Success).Should().Be(true);
        activity.GetTagItem(CronJobActivitySource.Tags.DurationMs).Should().Be(42d);
        activity.Status.Should().Be(ActivityStatusCode.Ok);
    }

    [Fact]
    public void SetExecutionResult_WithFailureAndErrorMessage_ShouldSetErrorStatusAndErrorTag()
    {
        using Activity? activity = CronJobActivitySource.StartExecuteActivity("JobH");

        activity.SetExecutionResult(success: false, durationMs: 10, errorMessage: "boom");

        activity!.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("boom");
        activity.GetTagItem(CronJobActivitySource.Tags.ErrorMessage).Should().Be("boom");
    }

    [Fact]
    public void SetExecutionResult_WithFailureAndNoErrorMessage_ShouldUseDefaultMessageAndSkipErrorTag()
    {
        using Activity? activity = CronJobActivitySource.StartExecuteActivity("JobH");

        activity.SetExecutionResult(success: false, durationMs: 10);

        activity!.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("Job execution failed");
        activity.GetTagItem(CronJobActivitySource.Tags.ErrorMessage).Should().BeNull();
    }

    [Fact]
    public void SetExecutionResult_WithNullActivity_ShouldNotThrow()
    {
        Activity? activity = null;

        Action act = () => activity.SetExecutionResult(true, 1);

        act.Should().NotThrow();
    }

    [Fact]
    public void SetResilienceInfo_ShouldSetAllThreeFlags()
    {
        using Activity? activity = CronJobActivitySource.StartExecuteActivity("JobI");

        activity.SetResilienceInfo(retryEnabled: true, circuitBreakerEnabled: false, preventOverlapping: true);

        activity!.GetTagItem(CronJobActivitySource.Tags.RetryEnabled).Should().Be(true);
        activity.GetTagItem(CronJobActivitySource.Tags.CircuitBreakerEnabled).Should().Be(false);
        activity.GetTagItem(CronJobActivitySource.Tags.PreventOverlapping).Should().Be(true);
    }

    [Fact]
    public void SetResilienceInfo_WithNullActivity_ShouldNotThrow()
    {
        Activity? activity = null;

        Action act = () => activity.SetResilienceInfo(true, true, true);

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordRetryAttempt_ShouldAddRetryEvent()
    {
        using Activity? activity = CronJobActivitySource.StartExecuteActivity("JobJ");
        var exception = new InvalidOperationException("transient failure");

        activity.RecordRetryAttempt(attemptNumber: 3, exception, delayMs: 500);

        activity!.Events.Should().ContainSingle(e => e.Name == "retry");
    }

    [Fact]
    public void RecordRetryAttempt_WithNullActivity_ShouldNotThrow()
    {
        Activity? activity = null;

        Action act = () => activity.RecordRetryAttempt(1, new InvalidOperationException(), 1);

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordError_ShouldSetErrorStatusTagsAndExceptionEvent()
    {
        using Activity? activity = CronJobActivitySource.StartExecuteActivity("JobK");
        var exception = new InvalidOperationException("fatal");

        activity.RecordError(exception);

        activity!.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("fatal");
        activity.GetTagItem(CronJobActivitySource.Tags.Success).Should().Be(false);
        activity.GetTagItem(CronJobActivitySource.Tags.ErrorMessage).Should().Be("fatal");
        activity.Events.Should().ContainSingle(e => e.Name == "exception");
    }

    [Fact]
    public void RecordError_WithNullActivity_ShouldNotThrow()
    {
        Activity? activity = null;

        Action act = () => activity.RecordError(new InvalidOperationException());

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordError_WithNullException_ShouldNotThrow()
    {
        using Activity? activity = CronJobActivitySource.StartExecuteActivity("JobK");

        Action act = () => activity.RecordError(null!);

        act.Should().NotThrow();
    }
}
