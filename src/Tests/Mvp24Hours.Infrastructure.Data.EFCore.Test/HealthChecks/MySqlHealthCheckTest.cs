using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.Data.EFCore.HealthChecks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.HealthChecks;

[Trait("Category", "Unit")]
public class MySqlHealthCheckTest
{
    private static HealthCheckContext CreateContext() =>
        new()
        {
            Registration = new HealthCheckRegistration(
                "mysql",
                _ => throw new NotSupportedException(),
                HealthStatus.Unhealthy,
                null)
        };

    [Fact]
    public void Options_Defaults_ShouldMatchExpected()
    {
        var options = new MySqlHealthCheckOptions();

        options.HealthQuery.Should().Be("SELECT 1");
        options.QueryTimeoutSeconds.Should().Be(5);
        options.DegradedThresholdMs.Should().Be(500);
        options.FailureThresholdMs.Should().Be(2000);
        options.CheckConnectionUsage.Should().BeTrue();
        options.ConnectionUsageThreshold.Should().Be(0.8);
        options.CheckSlowQueries.Should().BeFalse();
        options.CheckTableLocks.Should().BeFalse();
        options.CheckBufferPool.Should().BeFalse();
        options.CheckReplication.Should().BeFalse();
        options.ReplicationLagThresholdSeconds.Should().Be(30);
        options.Tags.Should().BeEquivalentTo(["db", "database", "mysql", "ready"]);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionOpenFails_ReturnsUnhealthy()
    {
        var check = new MySqlHealthCheck(
            "Server=invalid;Database=x;",
            new MySqlHealthCheckOptions { QueryTimeoutSeconds = 1 },
            NullLogger<MySqlHealthCheck>.Instance,
            _ => new FailingDbConnection());

        HealthCheckResult result = await check.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("failed");
        result.Data.Should().ContainKey("error");
    }

    [Fact]
    public void Constructor_WithNullConnectionString_Throws()
    {
        var act = () => new MySqlHealthCheck(
            null!,
            new MySqlHealthCheckOptions(),
            NullLogger<MySqlHealthCheck>.Instance,
            _ => new FailingDbConnection());

        act.Should().Throw<ArgumentNullException>().WithParameterName("connectionString");
    }

    private sealed class FailingDbConnection : DbConnection
    {
        [AllowNull]
        public override string ConnectionString { get => string.Empty; set => _ = value; }
        public override string Database => "test";
        public override string DataSource => "test";
        public override string ServerVersion => "0";
        public override ConnectionState State => ConnectionState.Closed;

        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open() => throw new InvalidOperationException("Simulated connection failure");
        public override Task OpenAsync(CancellationToken cancellationToken) =>
            Task.FromException(new InvalidOperationException("Simulated connection failure"));

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
            throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
    }
}
