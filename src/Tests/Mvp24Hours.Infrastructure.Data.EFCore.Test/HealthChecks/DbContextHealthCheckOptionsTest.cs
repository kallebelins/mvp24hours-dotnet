using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mvp24Hours.Infrastructure.Data.EFCore.HealthChecks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.HealthChecks;

[Trait("Category", "Unit")]
public class DbContextHealthCheckOptionsTest
{
    [Fact]
    public void Defaults_ShouldMatchExpected()
    {
        var options = new DbContextHealthCheckOptions();

        options.HealthQuery.Should().Be("SELECT 1");
        options.QueryTimeoutSeconds.Should().Be(5);
        options.DegradedThresholdMs.Should().Be(500);
        options.FailureThresholdMs.Should().Be(2000);
        options.CheckPendingMigrations.Should().BeFalse();
        options.FailOnPendingMigrations.Should().BeFalse();
        options.Name.Should().BeNull();
        options.FailureStatus.Should().Be(HealthStatus.Unhealthy);
        options.Tags.Should().BeEquivalentTo(["db", "database", "efcore"]);
    }

    [Fact]
    public void SqlServer_ShouldSetProviderTagsAndDefaults()
    {
        var options = DbContextHealthCheckOptions.SqlServer();

        options.HealthQuery.Should().Be("SELECT 1");
        options.QueryTimeoutSeconds.Should().Be(5);
        options.DegradedThresholdMs.Should().Be(500);
        options.FailureThresholdMs.Should().Be(2000);
        options.Tags.Should().BeEquivalentTo(["db", "database", "efcore", "sqlserver"]);
    }

    [Fact]
    public void PostgreSql_ShouldSetProviderTagsAndDefaults()
    {
        var options = DbContextHealthCheckOptions.PostgreSql();

        options.HealthQuery.Should().Be("SELECT 1");
        options.Tags.Should().BeEquivalentTo(["db", "database", "efcore", "postgresql"]);
    }

    [Fact]
    public void MySql_ShouldSetProviderTagsAndDefaults()
    {
        var options = DbContextHealthCheckOptions.MySql();

        options.HealthQuery.Should().Be("SELECT 1");
        options.Tags.Should().BeEquivalentTo(["db", "database", "efcore", "mysql"]);
    }

    [Fact]
    public void Strict_ShouldEnableMigrationChecksAndReadyTag()
    {
        var options = DbContextHealthCheckOptions.Strict();

        options.QueryTimeoutSeconds.Should().Be(3);
        options.DegradedThresholdMs.Should().Be(300);
        options.FailureThresholdMs.Should().Be(1000);
        options.CheckPendingMigrations.Should().BeTrue();
        options.FailOnPendingMigrations.Should().BeTrue();
        options.Tags.Should().BeEquivalentTo(["db", "database", "efcore", "ready"]);
    }

    [Fact]
    public void Liveness_ShouldDisableQueryAndMigrations()
    {
        var options = DbContextHealthCheckOptions.Liveness();

        options.HealthQuery.Should().BeNull();
        options.QueryTimeoutSeconds.Should().Be(3);
        options.DegradedThresholdMs.Should().Be(1000);
        options.FailureThresholdMs.Should().Be(5000);
        options.CheckPendingMigrations.Should().BeFalse();
        options.Tags.Should().BeEquivalentTo(["db", "live"]);
    }
}
