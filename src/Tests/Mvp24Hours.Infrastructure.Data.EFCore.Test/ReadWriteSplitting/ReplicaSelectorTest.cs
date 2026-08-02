using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Data.EFCore.ReadWriteSplitting;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.ReadWriteSplitting;

[Trait("Category", "Unit")]
public class ReplicaSelectorTest
{
    private const string Primary = "Server=primary;Database=App;";
    private const string Replica1 = "Server=replica1;Database=App;";
    private const string Replica2 = "Server=replica2;Database=App;";

    [Fact]
    public async Task SelectReplicaAsync_RoundRobin_ShouldRotateAcrossReplicas()
    {
        ReplicaSelector selector = CreateSelector(new ReadWriteOptions
        {
            PrimaryConnectionString = Primary,
            ReplicaConnectionStrings = [Replica1, Replica2],
            LoadBalancing = ReplicaLoadBalancing.RoundRobin
        });

        string? first = await selector.SelectReplicaAsync();
        string? second = await selector.SelectReplicaAsync();
        string? third = await selector.SelectReplicaAsync();

        first.Should().BeOneOf(Replica1, Replica2);
        second.Should().BeOneOf(Replica1, Replica2);
        third.Should().Be(first);
        second.Should().NotBe(first);
    }

    [Fact]
    public async Task SelectReplicaAsync_Random_ShouldReturnHealthyReplica()
    {
        ReplicaSelector selector = CreateSelector(new ReadWriteOptions
        {
            PrimaryConnectionString = Primary,
            ReplicaConnectionStrings = [Replica1, Replica2],
            LoadBalancing = ReplicaLoadBalancing.Random
        });

        string? selected = await selector.SelectReplicaAsync();

        selected.Should().BeOneOf(Replica1, Replica2);
    }

    [Fact]
    public async Task SelectReplicaAsync_Weighted_ShouldPreferHigherWeight()
    {
        ReplicaSelector selector = CreateSelector(new ReadWriteOptions
        {
            PrimaryConnectionString = Primary,
            ReplicaConnectionStrings = [Replica1, Replica2],
            ReplicaWeights = [1, 100],
            LoadBalancing = ReplicaLoadBalancing.Weighted
        });

        var selections = new List<string?>();
        for (int i = 0; i < 20; i++)
        {
            selections.Add(await selector.SelectReplicaAsync());
        }

        selections.Count(s => s == Replica2).Should().BeGreaterThan(selections.Count(s => s == Replica1));
    }

    [Fact]
    public async Task SelectReplicaAsync_WhenAllReplicasUnhealthy_ShouldFallbackToPrimary()
    {
        ReplicaSelector selector = CreateSelector(new ReadWriteOptions
        {
            PrimaryConnectionString = Primary,
            ReplicaConnectionStrings = [Replica1],
            FailureThreshold = 1,
            FallbackToPrimaryOnReplicaFailure = true
        });

        selector.MarkReplicaFailed(Replica1);

        string? selected = await selector.SelectReplicaAsync();

        selected.Should().Be(Primary);
    }

    [Fact]
    public void GetPrimaryConnectionString_ShouldReturnConfiguredPrimary()
    {
        ReplicaSelector selector = CreateSelector(new ReadWriteOptions
        {
            PrimaryConnectionString = Primary,
            ReplicaConnectionStrings = [Replica1]
        });

        selector.GetPrimaryConnectionString().Should().Be(Primary);
    }

    [Fact]
    public void MarkReplicaRecovered_ShouldRestoreReplicaHealth()
    {
        ReplicaSelector selector = CreateSelector(new ReadWriteOptions
        {
            PrimaryConnectionString = Primary,
            ReplicaConnectionStrings = [Replica1],
            FailureThreshold = 1
        });

        selector.MarkReplicaFailed(Replica1);
        selector.GetReplicaHealthStatus()[Replica1].IsHealthy.Should().BeFalse();

        selector.MarkReplicaRecovered(Replica1);

        selector.GetReplicaHealthStatus()[Replica1].IsHealthy.Should().BeTrue();
    }

    private static ReplicaSelector CreateSelector(ReadWriteOptions options)
    {
        ILogger<ReplicaSelector> logger = new LoggerFactory().CreateLogger<ReplicaSelector>();
        return new ReplicaSelector(Options.Create(options), logger);
    }
}
