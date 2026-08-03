using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Data.MongoDb.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Interceptors;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Extensions;

[Trait("Category", "Unit")]
public class MongoDbInterceptorExtensionsTest
{
    [Fact]
    public void AddMongoDbInterceptorPipeline_ShouldRegisterPipeline()
    {
        var services = new ServiceCollection();

        services.AddMongoDbInterceptorPipeline();

        services.Should().Contain(d =>
            d.ServiceType == typeof(IMongoDbInterceptorPipeline) &&
            d.ImplementationType == typeof(MongoDbInterceptorPipeline));
    }

    [Fact]
    public void AddMongoDbInterceptorPipeline_CalledTwice_ShouldRegisterSinglePipeline()
    {
        var services = new ServiceCollection();

        services.AddMongoDbInterceptorPipeline();
        services.AddMongoDbInterceptorPipeline();

        services.Count(d => d.ServiceType == typeof(IMongoDbInterceptorPipeline)).Should().Be(1);
    }

    [Fact]
    public void AddMongoDbAuditInterceptor_ShouldRegisterAuditInterceptor()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMongoDbAuditInterceptor("AuditUser");

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMongoDbInterceptorPipeline>().Should().NotBeNull();
        scope.ServiceProvider.GetServices<IMongoDbInterceptor>().Should().NotBeEmpty();
    }

    [Fact]
    public void AddMongoDbSoftDeleteInterceptor_ShouldRegisterSoftDeleteInterceptor()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMongoDbSoftDeleteInterceptor("DeleteUser");

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetServices<IMongoDbInterceptor>().Should().NotBeEmpty();
    }

    [Fact]
    public void AddMongoDbCommandLogger_ShouldRegisterCommandLoggerInterceptor()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMongoDbCommandLogger(TimeSpan.FromSeconds(2), logAllOperations: false);

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetServices<IMongoDbInterceptor>().Should().NotBeEmpty();
    }

    [Fact]
    public void AddMongoDbAuditTrail_ShouldRegisterAuditTrailInterceptor()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMongoDbAuditTrail(logEntityData: true);

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetServices<IMongoDbInterceptor>().Should().NotBeEmpty();
    }

    [Fact]
    public void AddMongoDbInterceptor_Generic_ShouldRegisterCustomInterceptor()
    {
        var services = new ServiceCollection();

        services.AddMongoDbInterceptor<StubMongoDbInterceptor>();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetServices<IMongoDbInterceptor>()
            .Should().Contain(i => i is StubMongoDbInterceptor);
    }

    [Fact]
    public void AddMongoDbInterceptor_WithFactory_ShouldRegisterFactoryInterceptor()
    {
        var services = new ServiceCollection();

        services.AddMongoDbInterceptor(_ => new StubMongoDbInterceptor());

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetServices<IMongoDbInterceptor>()
            .Should().Contain(i => i is StubMongoDbInterceptor);
    }

    [Fact]
    public void AddAllMongoDbInterceptors_WithAllEnabled_ShouldRegisterAllInterceptors()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAllMongoDbInterceptors(options =>
        {
            options.EnableTenantInterceptor = true;
            options.EnableAuditInterceptor = true;
            options.EnableSoftDelete = true;
            options.EnableCommandLogger = true;
            options.EnableAuditTrail = true;
            options.DefaultUser = "AllUser";
            options.TenantValidateOnUpdate = false;
            options.TenantValidateOnDelete = false;
            options.TenantThrowOnMissing = false;
            options.LogEntityDataInAuditTrail = true;
            options.LogSlowOperationsOnly = true;
            options.SlowOperationThreshold = TimeSpan.FromSeconds(1);
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetServices<IMongoDbInterceptor>().Should().HaveCountGreaterThan(3);
    }

    [Fact]
    public void AddAllMongoDbInterceptors_WithAllDisabled_ShouldRegisterOnlyPipeline()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAllMongoDbInterceptors(options =>
        {
            options.EnableTenantInterceptor = false;
            options.EnableAuditInterceptor = false;
            options.EnableSoftDelete = false;
            options.EnableCommandLogger = false;
            options.EnableAuditTrail = false;
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMongoDbInterceptorPipeline>().Should().NotBeNull();
        scope.ServiceProvider.GetServices<IMongoDbInterceptor>().Should().BeEmpty();
    }

    [Fact]
    public void AddMongoDbTenantInterceptor_ShouldRegisterTenantInterceptor()
    {
        var services = new ServiceCollection();

        services.AddMongoDbTenantInterceptor(
            validateOnUpdate: false,
            validateOnDelete: true,
            throwOnMissingTenant: false);

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetServices<IMongoDbInterceptor>().Should().NotBeEmpty();
    }

    [Fact]
    public void AddMongoDbTenantInterceptor_WithGenericProvider_ShouldRegisterProviderAndInterceptor()
    {
        var services = new ServiceCollection();

        services.AddMongoDbTenantInterceptor<StubTenantProvider>();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ITenantProvider>().Should().BeOfType<StubTenantProvider>();
        scope.ServiceProvider.GetServices<IMongoDbInterceptor>().Should().NotBeEmpty();
    }

    [Fact]
    public void AddMongoDbAsyncLocalTenantProvider_ShouldRegisterAsyncLocalProvider()
    {
        var services = new ServiceCollection();

        services.AddMongoDbAsyncLocalTenantProvider();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ITenantProvider>().Should().BeSameAs(AsyncLocalTenantProvider.Instance);
    }

    private sealed class StubMongoDbInterceptor : IMongoDbInterceptor
    {
        public Task OnBeforeInsertAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : class, IEntityBase
        {
            return Task.CompletedTask;
        }

        public Task OnAfterInsertAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : class, IEntityBase
        {
            return Task.CompletedTask;
        }

        public Task OnBeforeUpdateAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : class, IEntityBase
        {
            return Task.CompletedTask;
        }

        public Task OnAfterUpdateAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : class, IEntityBase
        {
            return Task.CompletedTask;
        }

        public Task<DeleteInterceptionResult> OnBeforeDeleteAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : class, IEntityBase
        {
            return Task.FromResult(DeleteInterceptionResult.Proceed());
        }

        public Task OnAfterDeleteAsync<T>(T entity, bool wasSoftDeleted, CancellationToken cancellationToken = default)
            where T : class, IEntityBase
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubTenantProvider : ITenantProvider
    {
        public string TenantId { get; set; } = "stub-tenant";

        public bool HasTenant => !string.IsNullOrEmpty(TenantId);

        public string ConnectionString { get; set; } = string.Empty;

        public string Schema { get; set; } = string.Empty;
    }
}
