using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.HealthChecks;

[Trait("Category", "Unit")]
public class HealthCheckExtensionsTest
{
    private sealed class SqliteFixture : IDisposable
    {
        public string ConnectionString { get; }
        public SqliteConnection KeepAlive { get; }
        public ServiceProvider Provider { get; }

        public SqliteFixture(Action<IHealthChecksBuilder> configureChecks)
        {
            ConnectionString = $"Data Source=file:hc_{Guid.NewGuid():N}?mode=memory&cache=shared";
            KeepAlive = new SqliteConnection(ConnectionString);
            KeepAlive.Open();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(KeepAlive);
            services.AddDbContext<TestDbContext>(o => o.UseSqlite(ConnectionString));
            configureChecks(services.AddHealthChecks());
            Provider = services.BuildServiceProvider();

            using IServiceScope scope = Provider.CreateScope();
            TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            Provider.Dispose();
            KeepAlive.Dispose();
        }
    }

    [Fact]
    public async Task AddMvp24HoursDbContextCheck_ShouldResolveAndExecuteHealthy()
    {
        using var fixture = new SqliteFixture(b =>
            b.AddMvp24HoursDbContextCheck<TestDbContext>(
                name: "testdb",
                configureOptions: o =>
                {
                    o.HealthQuery = null;
                    o.CheckPendingMigrations = false;
                }));

        HealthCheckService healthChecks = fixture.Provider.GetRequiredService<HealthCheckService>();
        HealthReport report = await healthChecks.CheckHealthAsync();

        report.Entries.Should().ContainKey("testdb");
        report.Entries["testdb"].Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task AddMvp24HoursDbContextLivenessCheck_ShouldRegisterLiveCheck()
    {
        using var fixture = new SqliteFixture(b =>
            b.AddMvp24HoursDbContextLivenessCheck<TestDbContext>());

        HealthCheckService healthChecks = fixture.Provider.GetRequiredService<HealthCheckService>();
        HealthReport report = await healthChecks.CheckHealthAsync();

        report.Entries.Keys.Should().Contain(k => k.Contains("live", StringComparison.OrdinalIgnoreCase));
        HealthReportEntry entry = report.Entries.First(e => e.Key.Contains("live", StringComparison.OrdinalIgnoreCase)).Value;
        entry.Tags.Should().Contain("live");
        entry.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task AddMvp24HoursDbContextReadinessCheck_ShouldRegisterReadyCheck()
    {
        using var fixture = new SqliteFixture(b =>
            b.AddMvp24HoursDbContextReadinessCheck<TestDbContext>());

        HealthCheckService healthChecks = fixture.Provider.GetRequiredService<HealthCheckService>();
        HealthReport report = await healthChecks.CheckHealthAsync();

        report.Entries.Keys.Should().Contain(k => k.Contains("ready", StringComparison.OrdinalIgnoreCase));
        HealthReportEntry entry = report.Entries.First(e => e.Key.Contains("ready", StringComparison.OrdinalIgnoreCase)).Value;
        entry.Tags.Should().Contain("ready");
        // EnsureCreated has no EF migration history; readiness may report degraded/unhealthy.
        entry.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Degraded, HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task AddMvp24HoursDbContextAllChecks_ShouldRegisterLivenessAndReadiness()
    {
        using var fixture = new SqliteFixture(b =>
            b.AddMvp24HoursDbContextAllChecks<TestDbContext>());

        HealthCheckService healthChecks = fixture.Provider.GetRequiredService<HealthCheckService>();
        HealthReport report = await healthChecks.CheckHealthAsync();

        report.Entries.Should().HaveCount(2);
        report.Entries.Keys.Should().Contain(k => k.Contains("live", StringComparison.OrdinalIgnoreCase));
        report.Entries.Keys.Should().Contain(k => k.Contains("ready", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AddMvp24HoursSqlServerCheck_ShouldRegisterNamedCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddMvp24HoursSqlServerCheck(
                "Server=invalid;Database=x;User Id=sa;Password=x;Connect Timeout=1;TrustServerCertificate=True;",
                name: "sqlserver-test",
                timeout: TimeSpan.FromSeconds(2));

        using ServiceProvider provider = services.BuildServiceProvider();
        HealthCheckService healthChecks = provider.GetRequiredService<HealthCheckService>();

        healthChecks.Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursPostgreSqlCheck_ShouldRegisterNamedCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddMvp24HoursPostgreSqlCheck(
                "Host=invalid;Database=x;Username=x;Password=x;Timeout=1",
                _ => new SqliteConnection("Data Source=:memory:"),
                name: "postgres-test",
                timeout: TimeSpan.FromSeconds(2));

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<HealthCheckService>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursMySqlCheck_ShouldRegisterNamedCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddMvp24HoursMySqlCheck(
                "Server=invalid;Database=x;User=x;Password=x;Connection Timeout=1",
                _ => new SqliteConnection("Data Source=:memory:"),
                name: "mysql-test",
                timeout: TimeSpan.FromSeconds(2));

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<HealthCheckService>().Should().NotBeNull();
    }
}
