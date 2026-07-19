//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.DistributedLocking.Metrics;

namespace Mvp24Hours.Infrastructure.Test.DistributedLocking;

[Trait("Category", "Unit")]
public class DistributedLockMetricsTest
{
    [Fact]
    public void GetMetrics_WhenNoData_ShouldReturnNull()
    {
        var metrics = new DistributedLockMetrics();

        metrics.GetMetrics("missing").Should().BeNull();
    }

    [Fact]
    public void RecordAcquisition_Success_ShouldUpdateSuccessRate()
    {
        var metrics = new DistributedLockMetrics();

        metrics.RecordAcquisition("res", true, TimeSpan.FromMilliseconds(10));
        metrics.RecordAcquisition("res", true, TimeSpan.FromMilliseconds(20));

        LockResourceMetrics? snapshot = metrics.GetMetrics("res");
        snapshot.Should().NotBeNull();
        snapshot!.TotalAttempts.Should().Be(2);
        snapshot.SuccessfulAttempts.Should().Be(2);
        snapshot.FailedAttempts.Should().Be(0);
        snapshot.SuccessRate.Should().Be(1.0);
        snapshot.ContentionRate.Should().Be(0.0);
        snapshot.AverageWaitTime.Should().Be(TimeSpan.FromMilliseconds(15));
        snapshot.MaxWaitTime.Should().Be(TimeSpan.FromMilliseconds(20));
    }

    [Fact]
    public void RecordAcquisition_Failure_ShouldIncrementFailedAndTimeouts()
    {
        var metrics = new DistributedLockMetrics();

        metrics.RecordAcquisition("res", false, TimeSpan.FromMilliseconds(50));

        LockResourceMetrics? snapshot = metrics.GetMetrics("res");
        snapshot!.FailedAttempts.Should().Be(1);
        snapshot.Timeouts.Should().Be(1);
        snapshot.SuccessRate.Should().Be(0.0);
        snapshot.ContentionRate.Should().Be(1.0);
    }

    [Fact]
    public void RecordRelease_WhenMetricsExist_ShouldIncrementReleases()
    {
        var metrics = new DistributedLockMetrics();
        metrics.RecordAcquisition("res", true, TimeSpan.Zero);

        metrics.RecordRelease("res");
        metrics.RecordRelease("res");

        metrics.GetMetrics("res")!.Releases.Should().Be(2);
    }

    [Fact]
    public void RecordRelease_WhenNoMetrics_ShouldNotThrow()
    {
        var metrics = new DistributedLockMetrics();

        Action act = () => metrics.RecordRelease("missing");

        act.Should().NotThrow();
    }

    [Fact]
    public void GetAllMetrics_ShouldReturnAllResources()
    {
        var metrics = new DistributedLockMetrics();
        metrics.RecordAcquisition("a", true, TimeSpan.Zero);
        metrics.RecordAcquisition("b", false, TimeSpan.FromMilliseconds(1));

        Dictionary<string, LockResourceMetrics> all = metrics.GetAllMetrics();

        all.Should().ContainKeys("a", "b");
        all.Should().HaveCount(2);
    }

    [Fact]
    public void ResetMetrics_ShouldRemoveResource()
    {
        var metrics = new DistributedLockMetrics();
        metrics.RecordAcquisition("res", true, TimeSpan.Zero);

        metrics.ResetMetrics("res");

        metrics.GetMetrics("res").Should().BeNull();
    }

    [Fact]
    public void ResetAllMetrics_ShouldClearEverything()
    {
        var metrics = new DistributedLockMetrics();
        metrics.RecordAcquisition("a", true, TimeSpan.Zero);
        metrics.RecordAcquisition("b", true, TimeSpan.Zero);

        metrics.ResetAllMetrics();

        metrics.GetAllMetrics().Should().BeEmpty();
    }
}
