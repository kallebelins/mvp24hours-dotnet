//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Options;

namespace Mvp24Hours.Infrastructure.Test.BackgroundJobs.Options;

[Trait("Category", "Unit")]
public class QuartzJobOptionsTest
{
    [Fact]
    public void Default_ShouldUseExpectedValues()
    {
        var options = new QuartzJobOptions();

        options.ConnectionString.Should().BeNull();
        options.StorageProvider.Should().Be(QuartzStorageProvider.SqlServer);
        options.TablePrefix.Should().Be("QRTZ_");
        options.InstanceId.Should().BeNull();
        options.InstanceName.Should().Be("Mvp24HoursScheduler");
        options.EnableClustering.Should().BeFalse();
        options.ClusterCheckinInterval.Should().Be(TimeSpan.FromSeconds(20));
        options.MaxConcurrency.Should().Be(10);
        options.MisfireThreshold.Should().Be(TimeSpan.FromSeconds(60));
        options.ThreadPriority.Should().Be(ThreadPriority.Normal);
        options.UseUtcTimezone.Should().BeTrue();
        options.SerializationType.Should().Be(QuartzSerializationType.Json);
    }
}
