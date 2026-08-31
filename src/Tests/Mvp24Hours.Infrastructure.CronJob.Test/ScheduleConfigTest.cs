namespace Mvp24Hours.Infrastructure.CronJob.Test;

[Trait("Category", "Unit")]
public class ScheduleConfigTest
{
    [Fact]
    public void ToString_WithCronExpressionAndTimeZone_ShouldIncludeBoth()
    {
        var config = new ScheduleConfig<MarkerJob>
        {
            CronExpression = "0 0 * * *",
            TimeZoneInfo = TimeZoneInfo.Utc
        };

        string result = config.ToString();

        result.Should().Contain(nameof(MarkerJob));
        result.Should().Contain("0 0 * * *");
        result.Should().Contain("UTC");
    }

    [Fact]
    public void ToString_WithoutCronExpression_ShouldIndicateRunOnce()
    {
        var config = new ScheduleConfig<MarkerJob>();

        string result = config.ToString();

        result.Should().Contain("(run once)");
        result.Should().Contain("Local");
    }

    private sealed class MarkerJob;
}
