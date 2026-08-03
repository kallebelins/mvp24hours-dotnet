using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.HealthChecks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.HealthChecks;

[Trait("Category", "Unit")]
public class SqlServerHealthCheckExtendedTest
{
    [Fact]
    public void AddMvp24HoursSqlServerCheck_WithFactory_ShouldRegisterHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddMvp24HoursSqlServerCheck(
                _ => "Server=127.0.0.1,1;Database=x;Connect Timeout=1;TrustServerCertificate=True;",
                "sqlserver-factory",
                configureOptions: o =>
                {
                    o.CheckDatabaseState = false;
                    o.CheckBlockingSessions = false;
                    o.CheckLongRunningQueries = false;
                });

        using ServiceProvider provider = services.BuildServiceProvider();
        HealthCheckService healthCheckService = provider.GetRequiredService<HealthCheckService>();

        healthCheckService.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancelledToken_ShouldReturnUnhealthy()
    {
        const string connectionString =
            "Server=127.0.0.1,1;Database=NonExistent;User Id=sa;Password=invalid;Connect Timeout=30;TrustServerCertificate=True;";

        var check = new SqlServerHealthCheck(
            connectionString,
            new SqlServerHealthCheckOptions
            {
                CheckDatabaseState = false,
                CheckBlockingSessions = false,
                CheckLongRunningQueries = false
            },
            NullLogger<SqlServerHealthCheck>.Instance);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), cts.Token);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public void Constructor_WithNullConnectionString_ShouldThrow()
    {
        Action act = () => _ = new SqlServerHealthCheck(
            null!,
            new SqlServerHealthCheckOptions(),
            NullLogger<SqlServerHealthCheck>.Instance);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        Action act = () => _ = new SqlServerHealthCheck(
            "Server=.;Database=x;",
            new SqlServerHealthCheckOptions(),
            null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CheckHealthAsync_WithInvalidConnection_ShouldReturnUnhealthy()
    {
        var check = new SqlServerHealthCheck(
            "Server=127.0.0.1,1;Database=NonExistent;Connect Timeout=1;TrustServerCertificate=True;",
            new SqlServerHealthCheckOptions
            {
                CheckDatabaseState = false,
                CheckBlockingSessions = false,
                CheckLongRunningQueries = false
            },
            NullLogger<SqlServerHealthCheck>.Instance);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
}
