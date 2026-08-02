using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using Mvp24Hours.Infrastructure.Data.EFCore.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Testing;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

public static class EfCoreTestHelpers
{
    public static IOptions<EFCoreRepositoryOptions> CreateRepositoryOptions(
        Action<EFCoreRepositoryOptions>? configure = null)
    {
        var options = new EFCoreRepositoryOptions
        {
            MaxQtyByQueryPage = 100
        };
        configure?.Invoke(options);
        return Options.Create(options);
    }

    public static ServiceProvider CreateSyncServices(
        string? databaseName = null,
        Action<DbContextOptionsBuilder>? configureDb = null,
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug().SetMinimumLevel(LogLevel.Debug));
        services.AddMvp24HoursInMemoryDbContext<TestDbContext>(databaseName ?? $"Sync_{Guid.NewGuid():N}", configureDb);
        services.AddMvp24HoursRepository(o => o.MaxQtyByQueryPage = 100);
        configureServices?.Invoke(services);
        return services.BuildServiceProvider();
    }

    public static ServiceProvider CreateAsyncServices(
        string? databaseName = null,
        Action<DbContextOptionsBuilder>? configureDb = null,
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug().SetMinimumLevel(LogLevel.Debug));
        services.AddMvp24HoursInMemoryDbContext<TestDbContext>(databaseName ?? $"Async_{Guid.NewGuid():N}", configureDb);
        services.AddMvp24HoursRepositoryAsync(o => o.MaxQtyByQueryPage = 100);
        configureServices?.Invoke(services);
        return services.BuildServiceProvider();
    }

    public static ServiceProvider CreateBulkServices(string? databaseName = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContext>(databaseName ?? $"Bulk_{Guid.NewGuid():N}");
        services.AddMvp24HoursBulkOperationsRepositoryAsync();
        return services.BuildServiceProvider();
    }

    public static ServiceProvider CreateStreamingServices(string? databaseName = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContext>(databaseName ?? $"Stream_{Guid.NewGuid():N}");
        services.AddMvp24HoursStreamingRepositoryAsync();
        return services.BuildServiceProvider();
    }

    public static TestDbContext CreateContext(string? databaseName = null, Action<DbContextOptionsBuilder>? configure = null)
    {
        DbContextOptionsBuilder<TestDbContext> optionsBuilder = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName ?? $"Ctx_{Guid.NewGuid():N}")
            .EnableSensitiveDataLogging();

        configure?.Invoke(optionsBuilder);
        var context = new TestDbContext(optionsBuilder.Options);
        context.Database.EnsureCreated();
        return context;
    }

    public static Mock<ICurrentUserProvider> CreateUserProvider(string userId = "test-user")
    {
        var mock = new Mock<ICurrentUserProvider>();
        mock.Setup(x => x.UserId).Returns(userId);
        mock.Setup(x => x.UserName).Returns(userId);
        return mock;
    }

    public static Mock<ITenantProvider> CreateTenantProvider(string tenantId = "tenant-1")
    {
        var mock = new Mock<ITenantProvider>();
        mock.Setup(x => x.TenantId).Returns(tenantId);
        mock.Setup(x => x.HasTenant).Returns(true);
        return mock;
    }

    public static Mock<IClock> CreateClock(DateTime? utcNow = null)
    {
        DateTime now = utcNow ?? new DateTime(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);
        var mock = new Mock<IClock>();
        mock.Setup(x => x.UtcNow).Returns(now);
        mock.Setup(x => x.Now).Returns(now.ToLocalTime());
        mock.Setup(x => x.UtcToday).Returns(now.Date);
        return mock;
    }

    public static List<TestEntity> CreateEntities(int count, string prefix = "Entity")
    {
        return [.. Enumerable.Range(1, count)
            .Select(i => new TestEntity
            {
                Name = $"{prefix}-{i}",
                Active = i % 2 == 0,
                Score = i * 10
            })];
    }
}
