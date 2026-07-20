using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Resilience;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Extensions;

[Trait("Category", "Unit")]
public class ResilienceDbContextExtensionsTest
{
    private static TestDbContext CreateSqliteContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite($"Data Source=file:resilience_{Guid.NewGuid():N}?mode=memory&cache=shared")
            .Options;
        var context = new TestDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void WithTimeout_ShouldSetCommandTimeout()
    {
        using TestDbContext context = CreateSqliteContext();

        context.WithTimeout(90);

        context.Database.GetCommandTimeout().Should().Be(90);
        context.WithTimeout(60).Should().BeSameAs(context);
        context.Database.GetCommandTimeout().Should().Be(60);
    }

    [Fact]
    public void CreateTimeoutScope_ShouldRestoreOriginalTimeoutOnDispose()
    {
        using TestDbContext context = CreateSqliteContext();
        context.Database.SetCommandTimeout(30);
        int? original = context.Database.GetCommandTimeout();

        using (context.CreateTimeoutScope(120))
        {
            context.Database.GetCommandTimeout().Should().Be(120);
        }

        context.Database.GetCommandTimeout().Should().Be(original);
    }

    [Fact]
    public void CreateTimeoutScope_DisposeTwice_ShouldBeIdempotent()
    {
        using TestDbContext context = CreateSqliteContext();
        IDisposable scope = context.CreateTimeoutScope(30);

        var act = () =>
        {
            scope.Dispose();
            scope.Dispose();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void WithTimeout_OnInMemory_ThrowsRelationalException()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();

        var act = () => context.WithTimeout(60);

        act.Should().Throw<InvalidOperationException>().WithMessage("*relational*");
    }

    [Fact]
    public void AddMvp24HoursDbContextCircuitBreaker_ShouldResolveBreaker()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursDbContextCircuitBreaker(o =>
        {
            o.EnableCircuitBreaker = true;
            o.CircuitBreakerFailureThreshold = 3;
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<DbContextCircuitBreaker>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursDbContextPoolMonitor_ShouldRegisterHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursDbContextPoolMonitor(o =>
        {
            o.LogPoolStatistics = true;
            o.PoolStatisticsLogIntervalSeconds = 30;
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetServices<IHostedService>().Should().Contain(s => s is DbContextPoolMonitor);
    }

    [Fact]
    public void AddMvp24HoursDbContextResilienceInfrastructure_ShouldRegisterBoth()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursDbContextResilienceInfrastructure(o =>
        {
            o.EnableCircuitBreaker = true;
            o.LogPoolStatistics = true;
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<DbContextCircuitBreaker>().Should().NotBeNull();
        provider.GetServices<IHostedService>().Should().Contain(s => s is DbContextPoolMonitor);
    }
}
