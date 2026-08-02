using Mvp24Hours.Infrastructure.Data.EFCore.Observability;
using Mvp24Hours.Infrastructure.Testing.Observability;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Observability;

[Trait("Category", "Unit")]
public class EFCoreMetricsTest
{
    [Fact]
    public void MeterName_ShouldBeExpected()
    {
        EFCoreMetrics.MeterName.Should().Be("Mvp24Hours.EFCore");
    }

    [Fact]
    public void RecordQuery_ShouldEmitCounterAndHistogram()
    {
        using var listener = new FakeMeterListener(EFCoreMetrics.MeterName);
        using var metrics = new EFCoreMetrics();

        metrics.RecordQuery(12.5, "SELECT", "AppDb");

        listener.GetSum("db.client.operations").Should().Be(1);
        listener.GetMeasurements("db.client.operation.duration").Should().ContainSingle()
            .Which.Value.Should().Be(12.5);
    }

    [Fact]
    public void RecordSlowQuery_ShouldEmitSlowQueryCounter()
    {
        using var listener = new FakeMeterListener(EFCoreMetrics.MeterName);
        using var metrics = new EFCoreMetrics();

        metrics.RecordSlowQuery(1500, 1000, "AppDb");

        listener.GetSum("db.client.slow_queries").Should().Be(1);
    }

    [Fact]
    public void RecordQueryError_ShouldEmitErrorCounter()
    {
        using var listener = new FakeMeterListener(EFCoreMetrics.MeterName);
        using var metrics = new EFCoreMetrics();

        metrics.RecordQueryError("TimeoutException", "AppDb");

        listener.GetSum("db.client.operation.errors").Should().Be(1);
    }

    [Fact]
    public void RecordSaveChanges_ShouldEmitEntityCounters()
    {
        using var listener = new FakeMeterListener(EFCoreMetrics.MeterName);
        using var metrics = new EFCoreMetrics();

        metrics.RecordSaveChanges(25, inserted: 2, updated: 3, deleted: 1, dbName: "AppDb");

        listener.GetSum("db.client.savechanges").Should().Be(1);
        listener.GetSum("db.client.entities.inserted").Should().Be(2);
        listener.GetSum("db.client.entities.updated").Should().Be(3);
        listener.GetSum("db.client.entities.deleted").Should().Be(1);
        listener.GetMeasurements("db.client.savechanges.duration").Should().ContainSingle()
            .Which.Value.Should().Be(25);
    }

    [Fact]
    public void UpdatePoolState_AndPoolHitMiss_ShouldRecord()
    {
        using var listener = new FakeMeterListener(EFCoreMetrics.MeterName);
        using var metrics = new EFCoreMetrics();

        metrics.UpdatePoolState(activeConnections: 4, idleConnections: 6);
        metrics.RecordPoolHit(3.2, "default");
        metrics.RecordPoolMiss("default");

        listener.GetSum("db.client.connections.pool.hits").Should().Be(1);
        listener.GetSum("db.client.connections.pool.misses").Should().Be(1);
        listener.GetMeasurements("db.client.connections.acquisition.duration").Should().ContainSingle()
            .Which.Value.Should().Be(3.2);
    }

    [Fact]
    public void RecordTransaction_ShouldEmitCommitAndRollback()
    {
        using var listener = new FakeMeterListener(EFCoreMetrics.MeterName);
        using var metrics = new EFCoreMetrics();

        metrics.RecordTransactionStart("AppDb");
        metrics.RecordTransactionCommit(10, "AppDb");
        metrics.RecordTransactionRollback(5, "conflict", "AppDb");

        listener.GetSum("db.client.transactions").Should().Be(1);
        listener.GetSum("db.client.transactions.commits").Should().Be(1);
        listener.GetSum("db.client.transactions.rollbacks").Should().Be(1);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        var metrics = new EFCoreMetrics();
        Action act = () => metrics.Dispose();
        act.Should().NotThrow();
    }
}
