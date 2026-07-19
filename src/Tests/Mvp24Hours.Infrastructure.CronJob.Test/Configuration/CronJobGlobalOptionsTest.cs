using Mvp24Hours.Infrastructure.CronJob.Configuration;
using Mvp24Hours.Infrastructure.CronJob.Test.Support.CronJobs;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Configuration;

[Trait("Category", "Unit")]
public class CronJobGlobalOptionsTest
{
    [Fact]
    public void DefaultTimeZoneInfo_ShouldReturnNull_WhenTimeZoneNotSet()
    {
        var options = new CronJobGlobalOptions();

        options.DefaultTimeZoneInfo.Should().BeNull();
    }

    [Fact]
    public void DefaultTimeZoneInfo_ShouldResolveTimeZone_WhenConfigured()
    {
        var options = new CronJobGlobalOptions { DefaultTimeZone = "UTC" };

        options.DefaultTimeZoneInfo!.Id.Should().Be("UTC");
    }

    [Fact]
    public void ApplyDefaultsTo_ShouldSetTimeZone_WhenJobTimeZoneMissing()
    {
        var global = new CronJobGlobalOptions { DefaultTimeZone = "UTC" };
        var job = new CronJobOptions<CustomerCronJob>();

        global.ApplyDefaultsTo(job);

        job.TimeZone.Should().Be("UTC");
    }

    [Fact]
    public void ApplyDefaultsTo_ShouldNotOverrideExistingJobTimeZone()
    {
        var global = new CronJobGlobalOptions { DefaultTimeZone = "UTC" };
        var job = new CronJobOptions<CustomerCronJob> { TimeZone = "Pacific Standard Time" };

        global.ApplyDefaultsTo(job);

        job.TimeZone.Should().Be("Pacific Standard Time");
    }

    [Fact]
    public void CreateWithDefaults_ShouldCopyGlobalDefaults()
    {
        var global = new CronJobGlobalOptions
        {
            DefaultTimeZone = "UTC",
            JobsEnabledByDefault = false,
            EnableRetryByDefault = true,
            DefaultMaxRetryAttempts = 5,
            EnableObservability = false,
            EnableHealthChecks = false
        };

        CronJobOptions<CustomerCronJob> job = global.CreateWithDefaults<CustomerCronJob>();

        job.TimeZone.Should().Be("UTC");
        job.Enabled.Should().BeFalse();
        job.EnableRetry.Should().BeTrue();
        job.MaxRetryAttempts.Should().Be(5);
        job.EnableObservability.Should().BeFalse();
        job.EnableHealthCheck.Should().BeFalse();
        job.PreventOverlapping.Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldIncludeKeySettings()
    {
        var options = new CronJobGlobalOptions
        {
            DefaultTimeZone = "UTC",
            EnableObservability = true,
            ValidateCronExpressionsOnStartup = true
        };

        options.ToString().Should().Contain("UTC");
        options.ToString().Should().Contain("Observability=True");
    }
}
