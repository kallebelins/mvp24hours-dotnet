using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using Mvp24Hours.Infrastructure.Data.EFCore.Resilience;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Extensions;

[Trait("Category", "Unit")]
public class ResilienceDbContextExtensionsTest
{
    private static TestDbContext CreateSqliteContext()
    {
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
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

        Action act = () =>
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

        Func<DbContext> act = () => context.WithTimeout(60);

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

    [Fact]
    public void AddMvp24HoursDbContextWithResilience_ShouldRegisterDbContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursDbContextWithResilience<TestDbContext>(
            "Server=(localdb)\\mssqllocaldb;Database=ResilienceTest;Trusted_Connection=True;",
            o => o.EnableDbContextPooling = false,
            o => o.EnableSensitiveDataLogging());

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<TestDbContext>().Should().NotBeNull();
        provider.GetRequiredService<DbContext>().Should().BeOfType<TestDbContext>();
    }

    [Fact]
    public void AddMvp24HoursDbContextWithResilience_WithPooling_ShouldRegisterPooledContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursDbContextWithResilience<TestDbContext>(
            "Server=(localdb)\\mssqllocaldb;Database=ResiliencePoolTest;Trusted_Connection=True;",
            o =>
            {
                o.EnableDbContextPooling = true;
                o.PoolSize = 64;
            });

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<TestDbContext>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursAzureSqlDbContext_ShouldRegisterDbContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursAzureSqlDbContext<TestDbContext>(
            "Server=tcp:example.database.windows.net;Database=AzureTest;User Id=x;Password=y;");

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<TestDbContext>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursDevDbContext_ShouldRegisterDbContextWithSensitiveLogging()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursDevDbContext<TestDbContext>(
            "Server=(localdb)\\mssqllocaldb;Database=DevTest;Trusted_Connection=True;");

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<TestDbContext>().Should().NotBeNull();
    }

    [Fact]
    public void UseSqlServerWithResilience_ShouldConfigureProvider()
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>();

        DbContextOptionsBuilder result = builder.UseSqlServerWithResilience(
            "Server=(localdb)\\mssqllocaldb;Database=OptionsTest;Trusted_Connection=True;",
            new EFCoreResilienceOptions { CommandTimeoutSeconds = 45, MaxRetryCount = 3 });

        result.Should().BeSameAs(builder);
        builder.Options.Extensions.Should().NotBeEmpty();
    }

    [Fact]
    public void WithCommandTimeout_OnSqliteOptionsBuilder_ShouldSetTimeout()
    {
        DbContextOptionsBuilder<TestDbContext> builder = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite($"Data Source=file:cmd_{Guid.NewGuid():N}?mode=memory&cache=shared");

        builder.WithCommandTimeout(75);

        builder.Options.FindExtension<Microsoft.EntityFrameworkCore.Infrastructure.RelationalOptionsExtension>()
            ?.CommandTimeout.Should().Be(75);
    }
}
