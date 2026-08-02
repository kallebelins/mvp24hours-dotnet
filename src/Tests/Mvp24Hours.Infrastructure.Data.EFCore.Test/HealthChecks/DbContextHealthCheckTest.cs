using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Data.EFCore.HealthChecks;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.HealthChecks;

[Trait("Category", "Unit")]
public class DbContextHealthCheckTest
{
    private static HealthCheckContext CreateHealthCheckContext(string name = "db")
    {
        return new()
        {
            Registration = new HealthCheckRegistration(
                name,
                _ => throw new NotSupportedException("Factory not used in unit tests."),
                failureStatus: HealthStatus.Unhealthy,
                tags: null)
        };
    }

    private static TestDbContext CreateSqliteContext()
    {
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite($"Data Source=file:health_{Guid.NewGuid():N}?mode=memory&cache=shared")
            .Options;
        var context = new TestDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    private static DbContextHealthCheck<TestDbContext> CreateCheck(
        TestDbContext context,
        DbContextHealthCheckOptions? options = null)
    {
        return new(
            context,
            Options.Create(options ?? DbContextHealthCheckOptions.Liveness()),
            NullLogger<DbContextHealthCheck<TestDbContext>>.Instance);
    }

    [Fact]
    public async Task CheckHealthAsync_WithSqliteLiveness_ReturnsHealthy()
    {
        // InMemory cannot use GetDbConnection; SQLite in-memory is used for a true Healthy path.
        await using TestDbContext context = CreateSqliteContext();
        DbContextHealthCheck<TestDbContext> check = CreateCheck(context, DbContextHealthCheckOptions.Liveness());

        HealthCheckResult result = await check.CheckHealthAsync(CreateHealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("healthy");
        result.Data.Should().ContainKey("responseTimeMs");
    }

    [Fact]
    public async Task CheckHealthAsync_WithInMemory_ThrowsBecauseRelationalApisUnavailable()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        DbContextHealthCheck<TestDbContext> check = CreateCheck(context, DbContextHealthCheckOptions.Liveness());

        Func<Task<HealthCheckResult>> act = async () => await check.CheckHealthAsync(CreateHealthCheckContext());

        // GetDbConnection runs before the try/catch in CheckHealthAsync.
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*relational*");
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancelledToken_ReturnsUnhealthy()
    {
        await using TestDbContext context = CreateSqliteContext();
        DbContextHealthCheck<TestDbContext> check = CreateCheck(context, DbContextHealthCheckOptions.Liveness());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        HealthCheckResult result = await check.CheckHealthAsync(CreateHealthCheckContext(), cts.Token);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public void Constructor_WithNullDbContext_Throws()
    {
        Func<DbContextHealthCheck<TestDbContext>> act = () => new DbContextHealthCheck<TestDbContext>(
            null!,
            Options.Create(new DbContextHealthCheckOptions()),
            NullLogger<DbContextHealthCheck<TestDbContext>>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("dbContext");
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();

        Func<DbContextHealthCheck<TestDbContext>> act = () => new DbContextHealthCheck<TestDbContext>(
            context,
            Options.Create(new DbContextHealthCheckOptions()),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullOptions_UsesDefaults()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();

        var check = new DbContextHealthCheck<TestDbContext>(
            context,
            null!,
            NullLogger<DbContextHealthCheck<TestDbContext>>.Instance);

        check.Should().NotBeNull();
    }
}
