using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.Data.EFCore.HealthChecks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.HealthChecks;

[Trait("Category", "Unit")]
public class SqlServerHealthCheckTest
{
    private static HealthCheckContext CreateContext() =>
        new()
        {
            Registration = new HealthCheckRegistration(
                "sqlserver",
                _ => throw new NotSupportedException(),
                HealthStatus.Unhealthy,
                null)
        };

    [Fact]
    public void Options_Defaults_ShouldMatchExpected()
    {
        var options = new SqlServerHealthCheckOptions();

        options.HealthQuery.Should().Be("SELECT 1");
        options.QueryTimeoutSeconds.Should().Be(5);
        options.DegradedThresholdMs.Should().Be(500);
        options.FailureThresholdMs.Should().Be(2000);
        options.CheckDatabaseState.Should().BeTrue();
        options.CheckBlockingSessions.Should().BeFalse();
        options.BlockingSessionThreshold.Should().Be(5);
        options.CheckLongRunningQueries.Should().BeFalse();
        options.LongRunningQuerySeconds.Should().Be(30);
        options.LongRunningQueryThreshold.Should().Be(3);
        options.Tags.Should().BeEquivalentTo(["db", "database", "sqlserver", "ready"]);
    }

    [Fact]
    public async Task CheckHealthAsync_WithInvalidConnection_ReturnsUnhealthy()
    {
        const string connectionString =
            "Server=127.0.0.1,1;Database=NonExistent;User Id=sa;Password=invalid;Connect Timeout=1;TrustServerCertificate=True;";

        var options = new SqlServerHealthCheckOptions
        {
            QueryTimeoutSeconds = 1,
            CheckDatabaseState = false,
            CheckBlockingSessions = false,
            CheckLongRunningQueries = false
        };

        var check = new SqlServerHealthCheck(
            connectionString,
            options,
            NullLogger<SqlServerHealthCheck>.Instance);

        HealthCheckResult result = await check.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("failed");
        result.Data.Should().ContainKey("error");
    }

    [Fact]
    public void Constructor_WithNullConnectionString_Throws()
    {
        var act = () => new SqlServerHealthCheck(
            null!,
            new SqlServerHealthCheckOptions(),
            NullLogger<SqlServerHealthCheck>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("connectionString");
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        var act = () => new SqlServerHealthCheck(
            "Server=.;Database=x;",
            new SqlServerHealthCheckOptions(),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }
}
