using Mvp24Hours.Infrastructure.CronJob.Resiliency;

namespace Mvp24Hours.Infrastructure.CronJob.Test;

[Trait("Category", "Unit")]
public class ResilientScheduleConfigTest
{
    [Fact]
    public void ToString_WithCronExpressionAndTimeZone_ShouldIncludeResilienceInfo()
    {
        var config = new ResilientScheduleConfig<MarkerJob>
        {
            CronExpression = "0 0 * * *",
            TimeZoneInfo = TimeZoneInfo.Utc,
            Resilience = CronJobResilienceConfig<MarkerJob>.FullResilience()
        };

        string result = config.ToString();

        result.Should().Contain(nameof(MarkerJob));
        result.Should().Contain("0 0 * * *");
        result.Should().Contain("UTC");
        result.Should().Contain(config.Resilience.ToString()!);
    }

    [Fact]
    public void ToString_WithoutCronExpression_ShouldIndicateRunOnce()
    {
        var config = new ResilientScheduleConfig<MarkerJob>();

        string result = config.ToString();

        result.Should().Contain("(run once)");
        result.Should().Contain("Local");
    }

    [Fact]
    public void Resilience_DefaultsToNewInstance()
    {
        var config = new ResilientScheduleConfig<MarkerJob>();

        config.Resilience.Should().NotBeNull();
        config.Resilience.Should().BeOfType<CronJobResilienceConfig<MarkerJob>>();
    }

    private sealed class MarkerJob;
}
