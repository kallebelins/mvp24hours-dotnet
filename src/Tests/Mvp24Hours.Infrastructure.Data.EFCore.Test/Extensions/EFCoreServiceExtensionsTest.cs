using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;
using Mvp24Hours.Infrastructure.Data.EFCore.Testing;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Extensions;

[Trait("Category", "Unit")]
public class EFCoreServiceExtensionsTest
{
    [Fact]
    public void AddMvp24HoursRepository_ShouldResolveUnitOfWorkAndRepository()
    {
        using ServiceProvider provider = EfCoreTestHelpers.CreateSyncServices();
        using IServiceScope scope = provider.CreateScope();

        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IRepository<TestEntity> repository = unitOfWork.GetRepository<TestEntity>();

        unitOfWork.Should().NotBeNull();
        repository.Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursRepositoryAsync_ShouldResolveAsyncServices()
    {
        using ServiceProvider provider = EfCoreTestHelpers.CreateAsyncServices();
        using IServiceScope scope = provider.CreateScope();

        IUnitOfWorkAsync unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
        IRepositoryAsync<TestEntity> repository = unitOfWork.GetRepository<TestEntity>();

        unitOfWork.Should().NotBeNull();
        repository.Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursStreamingRepositoryAsync_ShouldResolveStreamingRepository()
    {
        using ServiceProvider provider = EfCoreTestHelpers.CreateStreamingServices();
        using IServiceScope scope = provider.CreateScope();

        IStreamingRepositoryAsync<TestEntity> repository =
            scope.ServiceProvider.GetRequiredService<IStreamingRepositoryAsync<TestEntity>>();

        repository.Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursBulkOperationsRepositoryAsync_ShouldResolveBulkRepository()
    {
        using ServiceProvider provider = EfCoreTestHelpers.CreateBulkServices();
        using IServiceScope scope = provider.CreateScope();

        IBulkOperationsRepositoryAsync<TestEntity> repository =
            scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<TestEntity>>();

        repository.Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursReadOnlyRepository_ShouldResolveReadOnly()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContext>($"ReadOnly_{Guid.NewGuid():N}");
        services.AddMvp24HoursReadOnlyRepository();
        services.AddMvp24HoursReadOnlyRepositoryAsync();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IReadOnlyRepository<TestEntity>>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IReadOnlyRepositoryAsync<TestEntity>>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursDbContext_ShouldRegisterAsDbContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContext>($"DbCtx_{Guid.NewGuid():N}");
        services.AddMvp24HoursDbContext<TestDbContext>();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        DbContext dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
        dbContext.Should().BeOfType<TestDbContext>();
    }

    [Fact]
    public void AddMvp24HoursCqrsRepositories_ShouldResolveReadAndWrite()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContext>($"Cqrs_{Guid.NewGuid():N}");
        services.AddMvp24HoursCqrsRepositories(o => o.MaxQtyByQueryPage = 50);

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IReadOnlyRepositoryAsync<TestEntity>>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntity>>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IOptions<EFCoreRepositoryOptions>>().Value.MaxQtyByQueryPage
            .Should().Be(50);
    }

    [Fact]
    public void AddMvp24HoursTenantProvider_ShouldResolveProvider()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursTenantProvider(_ => EfCoreTestHelpers.CreateTenantProvider().Object);

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ITenantProvider>().TenantId.Should().Be("tenant-1");
    }

    [Fact]
    public void AddMvp24HoursTenantInterceptor_ShouldResolveInterceptor()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursTenantProvider(_ => EfCoreTestHelpers.CreateTenantProvider().Object);
        services.AddMvp24HoursTenantInterceptor(o => o.RequireTenant = true);

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<TenantSaveChangesInterceptor>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursReadOptimizedRepository_ShouldConfigureNoTracking()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContext>($"ReadOpt_{Guid.NewGuid():N}");
        services.AddMvp24HoursReadOptimizedRepository();

        using ServiceProvider provider = services.BuildServiceProvider();
        EFCoreRepositoryOptions options = provider.GetRequiredService<IOptions<EFCoreRepositoryOptions>>().Value;

        options.DefaultTrackingBehavior.Should().Be(QueryTrackingBehavior.NoTracking);
        options.UseSplitQueries.Should().BeTrue();
        options.EnableQueryTags.Should().BeTrue();
    }
}
