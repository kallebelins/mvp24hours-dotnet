using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Data.EFCore.ReadWriteSplitting;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.ReadWriteSplitting;

[Trait("Category", "Unit")]
public class ReadWriteOptionsTest
{
    [Fact]
    public void DefaultValues_ShouldMatchExpectedDefaults()
    {
        var options = new ReadWriteOptions();

        options.PrimaryConnectionString.Should().BeEmpty();
        options.ReplicaConnectionStrings.Should().BeEmpty();
        options.FallbackToPrimaryOnReplicaFailure.Should().BeTrue();
        options.LoadBalancing.Should().Be(ReplicaLoadBalancing.RoundRobin);
        options.EnableReplicaHealthChecks.Should().BeTrue();
        options.HealthCheckInterval.Should().Be(TimeSpan.FromSeconds(30));
        options.FailureThreshold.Should().Be(3);
        options.EnableReadAfterWriteConsistency.Should().BeFalse();
        options.ReadAfterWriteWindow.Should().Be(TimeSpan.FromSeconds(5));
        options.AutoDetectOperationType.Should().BeTrue();
        options.MaxReplicaRetries.Should().Be(2);
    }

    [Fact]
    public void SimpleSetup_ShouldConfigurePrimaryAndReplica()
    {
        ReadWriteOptions options = ReadWriteOptions.SimpleSetup("primary", "replica");

        options.PrimaryConnectionString.Should().Be("primary");
        options.ReplicaConnectionStrings.Should().ContainSingle("replica");
        options.LoadBalancing.Should().Be(ReplicaLoadBalancing.RoundRobin);
    }
}
