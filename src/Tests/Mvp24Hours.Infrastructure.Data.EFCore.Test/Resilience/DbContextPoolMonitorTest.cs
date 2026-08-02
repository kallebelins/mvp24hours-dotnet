using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using Mvp24Hours.Infrastructure.Data.EFCore.Resilience;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Resilience;

[Trait("Category", "Unit")]
public class DbContextPoolMonitorTest
{
    private static DbContextPoolMonitor CreateMonitor()
    {
        return new(Options.Create(new EFCoreResilienceOptions()), NullLogger<DbContextPoolMonitor>.Instance);
    }

    [Fact]
    public void RecordPoolHit_UpdatesStatistics()
    {
        DbContextPoolMonitor monitor = CreateMonitor();

        monitor.RecordPoolHit(TimeSpan.FromMilliseconds(10));
        monitor.RecordPoolHit(TimeSpan.FromMilliseconds(30));

        PoolStatisticsSnapshot snapshot = monitor.Statistics.GetSnapshot();
        snapshot.PoolHits.Should().Be(2);
        snapshot.PoolMisses.Should().Be(0);
        snapshot.TotalRequests.Should().Be(2);
        snapshot.ActiveContexts.Should().Be(2);
        snapshot.AverageCheckoutTimeMs.Should().Be(20);
    }

    [Fact]
    public void RecordPoolMiss_UpdatesStatistics()
    {
        DbContextPoolMonitor monitor = CreateMonitor();

        monitor.RecordPoolMiss();

        PoolStatisticsSnapshot snapshot = monitor.Statistics.GetSnapshot();
        snapshot.PoolMisses.Should().Be(1);
        snapshot.PoolHits.Should().Be(0);
        snapshot.ActiveContexts.Should().Be(1);
    }

    [Fact]
    public void RecordReturn_DecrementsActiveContexts()
    {
        DbContextPoolMonitor monitor = CreateMonitor();

        monitor.RecordPoolHit(TimeSpan.FromMilliseconds(5));
        monitor.RecordReturn();

        monitor.Statistics.GetSnapshot().ActiveContexts.Should().Be(0);
    }
}
