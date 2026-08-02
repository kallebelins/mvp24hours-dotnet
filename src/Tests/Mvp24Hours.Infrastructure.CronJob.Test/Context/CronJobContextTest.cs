using Mvp24Hours.Infrastructure.CronJob.Context;
using Mvp24Hours.Infrastructure.CronJob.Test.Support;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Context;

[Trait("Category", "Unit")]
public class CronJobContextTest
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        using var cts = new CancellationTokenSource();
        DateTimeOffset scheduled = DateTimeOffset.UtcNow.AddMinutes(5);
        var parentId = Guid.NewGuid();

        CronJobContext context = CronJobTestHelpers.CreateContext(
            jobName: "MyJob",
            cronExpression: "0 * * * *",
            maxAttempts: 3,
            cancellationToken: cts.Token);

        context.JobName.Should().Be("MyJob");
        context.CronExpression.Should().Be("0 * * * *");
        context.TimeZone.Should().Be(TimeZoneInfo.Utc);
        context.CancellationToken.Should().Be(cts.Token);
        context.ExecutionCount.Should().Be(1);
        context.MaxAttempts.Should().Be(3);
        context.CurrentAttempt.Should().Be(1);
        context.IsRetry.Should().BeFalse();
        context.JobId.Should().NotBeEmpty();
        context.CorrelationId.Should().NotBeNullOrWhiteSpace();
        context.StartTime.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        context.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenJobNameIsNull()
    {
        Action act = () => _ = new CronJobContext(null!, "* * * * *", null, CancellationToken.None, 1);

        act.Should().Throw<ArgumentNullException>().WithParameterName("jobName");
    }

    [Fact]
    public void SetProperty_ShouldStoreAndRetrieveTypedValue()
    {
        CronJobContext context = CronJobTestHelpers.CreateContext();

        context.SetProperty("count", 42);
        context.SetProperty("name", "test");

        context.GetProperty<int>("count").Should().Be(42);
        context.GetProperty<string>("name").Should().Be("test");
        context.GetProperty<string>("missing", "default").Should().Be("default");
        context.Properties.Should().ContainKey("count").WhoseValue.Should().Be(42);
    }

    [Fact]
    public void GetProperty_ShouldReturnDefault_WhenTypeMismatch()
    {
        CronJobContext context = CronJobTestHelpers.CreateContext();
        context.SetProperty("value", "not-a-number");

        context.GetProperty<int>("value", 7).Should().Be(7);
    }

    [Fact]
    public void IsTimedOut_ShouldBeFalse_WithoutTimeout()
    {
        CronJobContext context = CronJobTestHelpers.CreateContext(timeout: null);

        context.IsTimedOut.Should().BeFalse();
    }

    [Fact]
    public void IsTimedOut_ShouldBeTrue_WhenElapsedExceedsTimeout()
    {
        var context = new CronJobContext(
            "TimedJob",
            "* * * * *",
            TimeZoneInfo.Utc,
            CancellationToken.None,
            executionCount: 1,
            maxAttempts: 1,
            timeout: TimeSpan.Zero);

        Thread.Sleep(5);

        context.IsTimedOut.Should().BeTrue();
    }

    [Fact]
    public void MaxAttempts_ShouldBeAtLeastOne()
    {
        var context = new CronJobContext("Job", null, null, CancellationToken.None, 1, maxAttempts: 0);

        context.MaxAttempts.Should().Be(1);
    }
}
