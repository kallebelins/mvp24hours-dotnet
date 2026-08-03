using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Data.MongoDb.Interceptors;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Extensions;

[Trait("Category", "Unit")]
public class MongoDbServiceExtensionsTest
{
    private const string ConnectionString = "mongodb://127.0.0.1:27017";

    private static ServiceCollection CreateServices(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        configure?.Invoke(services);
        return services;
    }

    [Fact]
    public void AddMvp24HoursDbContext_WithoutOptions_ShouldRegisterContext()
    {
        ServiceCollection services = CreateServices();
        services.AddMvp24HoursDbContext();

        services.Should().Contain(d => d.ServiceType == typeof(Mvp24HoursContext));
    }

    [Fact]
    public void AddMvp24HoursDbContext_WithOptions_ShouldConfigureMongoDbOptions()
    {
        ServiceCollection services = CreateServices();
        services.AddMvp24HoursDbContext(options =>
        {
            options.DatabaseName = "ConfiguredDb";
            options.ConnectionString = ConnectionString;
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        MongoDbOptions options = provider.GetRequiredService<IOptions<MongoDbOptions>>().Value;

        options.DatabaseName.Should().Be("ConfiguredDb");
        options.ConnectionString.Should().Be(ConnectionString);
    }

    [Fact]
    public void AddMvp24HoursDbContext_WithDbFactory_ShouldUseFactoryInstance()
    {
        ServiceCollection services = CreateServices();
        Mvp24HoursContext? factoryContext = null;

        services.AddMvp24HoursDbContext(
            options =>
            {
                options.DatabaseName = "Ignored";
                options.ConnectionString = ConnectionString;
            },
            _ =>
            {
                factoryContext = new Mvp24HoursContext("FactoryDb", ConnectionString);
                return factoryContext;
            });

        using ServiceProvider provider = services.BuildServiceProvider();
        Mvp24HoursContext resolved = provider.GetRequiredService<Mvp24HoursContext>();

        resolved.Should().BeSameAs(factoryContext);
        resolved.DatabaseName.Should().Be("FactoryDb");
    }

    [Fact]
    public void AddMvp24HoursDbContext_WithSingletonLifetime_ShouldRegisterSingleton()
    {
        ServiceCollection services = CreateServices();
        services.AddMvp24HoursDbContext(lifetime: ServiceLifetime.Singleton);

        ServiceDescriptor descriptor = services.Single(d => d.ServiceType == typeof(Mvp24HoursContext));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddMvp24HoursRepository_ShouldRegisterUnitOfWorkAndRepository()
    {
        ServiceCollection services = CreateServices();
        services.AddMvp24HoursDbContext(o =>
        {
            o.ConnectionString = ConnectionString;
            o.DatabaseName = "RepoDb";
        });
        services.AddMvp24HoursRepository();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursRepository_WithCustomTypesAndOptions_ShouldApplyConfiguration()
    {
        ServiceCollection services = CreateServices();
        services.AddMvp24HoursRepository(
            repositoryOptions => repositoryOptions.MaxQtyByQueryPage = 25,
            repository: typeof(Repository<>),
            unitOfWork: typeof(UnitOfWork),
            lifetime: ServiceLifetime.Transient);

        services.Should().Contain(d =>
            d.ServiceType == typeof(IUnitOfWork) &&
            d.ImplementationType == typeof(UnitOfWork) &&
            d.Lifetime == ServiceLifetime.Transient);
        services.Should().Contain(d =>
            d.ServiceType == typeof(IRepository<>) &&
            d.ImplementationType == typeof(Repository<>));

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IOptions<MongoDbRepositoryOptions>>().Value.MaxQtyByQueryPage.Should().Be(25);
    }

    [Fact]
    public void AddMvp24HoursRepositoryAsync_ShouldRegisterAsyncServices()
    {
        ServiceCollection services = CreateServices();
        services.AddMvp24HoursDbContext(o =>
        {
            o.ConnectionString = ConnectionString;
            o.DatabaseName = "AsyncRepoDb";
        });
        services.AddMvp24HoursRepositoryAsync();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntity>>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursRepositoryAsync_WithCustomTypes_ShouldRegisterCustomImplementations()
    {
        ServiceCollection services = CreateServices();
        services.AddMvp24HoursRepositoryAsync(
            repositoryOptions => repositoryOptions.MaxQtyByQueryPage = 40,
            repositoryAsync: typeof(RepositoryAsync<>),
            unitOfWorkAsync: typeof(UnitOfWorkAsync));

        services.Should().Contain(d =>
            d.ServiceType == typeof(IUnitOfWorkAsync) &&
            d.ImplementationType == typeof(UnitOfWorkAsync));
        services.Should().Contain(d =>
            d.ServiceType == typeof(IRepositoryAsync<>) &&
            d.ImplementationType == typeof(RepositoryAsync<>));
    }

    [Fact]
    public void AddMvp24HoursRepositoryAsyncWithInterceptors_ShouldRegisterPipelineAndRepository()
    {
        ServiceCollection services = CreateServices();
        services.AddMvp24HoursDbContext(o =>
        {
            o.ConnectionString = ConnectionString;
            o.DatabaseName = "InterceptorDb";
        });
        services.AddMvp24HoursRepositoryAsyncWithInterceptors();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMongoDbInterceptorPipeline>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntity>>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursBulkOperationsRepositoryAsync_ShouldRegisterBulkServices()
    {
        ServiceCollection services = CreateServices();
        services.AddMvp24HoursDbContext(o =>
        {
            o.ConnectionString = ConnectionString;
            o.DatabaseName = "BulkDb";
        });
        services.AddMvp24HoursBulkOperationsRepositoryAsync();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IBulkOperationsMongoDbAsync<TestEntity>>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IBulkOperationsAsync<TestEntity>>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntity>>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursBulkOperationsRepositoryAsync_WithCustomRepository_ShouldRegisterCustomType()
    {
        ServiceCollection services = CreateServices();
        services.AddMvp24HoursBulkOperationsRepositoryAsync(
            bulkOperationsRepositoryAsync: typeof(BulkOperationsRepositoryAsync<>),
            unitOfWorkAsync: typeof(UnitOfWorkAsync));

        services.Should().Contain(d =>
            d.ServiceType == typeof(IBulkOperationsMongoDbAsync<>) &&
            d.ImplementationType == typeof(BulkOperationsRepositoryAsync<>));
    }

    [Fact]
    public void AddMvp24HoursBulkOperationsRepositoryAsyncWithInterceptors_ShouldRegisterPipelineAndBulkRepository()
    {
        ServiceCollection services = CreateServices();
        services.AddMvp24HoursDbContext(o =>
        {
            o.ConnectionString = ConnectionString;
            o.DatabaseName = "BulkInterceptorDb";
        });
        services.AddMvp24HoursBulkOperationsRepositoryAsyncWithInterceptors();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMongoDbInterceptorPipeline>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IBulkOperationsMongoDbAsync<TestEntity>>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursReadOnlyRepository_ShouldRegisterReadOnlyRepository()
    {
        ServiceCollection services = CreateServices();
        services.AddMvp24HoursDbContext(o =>
        {
            o.ConnectionString = ConnectionString;
            o.DatabaseName = "ReadOnlyDb";
        });
        services.AddMvp24HoursReadOnlyRepository();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IReadOnlyRepository<TestEntity>>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursReadOnlyRepositoryAsync_ShouldRegisterAsyncReadOnlyRepository()
    {
        ServiceCollection services = CreateServices();
        services.AddMvp24HoursDbContext(o =>
        {
            o.ConnectionString = ConnectionString;
            o.DatabaseName = "ReadOnlyAsyncDb";
        });
        services.AddMvp24HoursReadOnlyRepositoryAsync();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IReadOnlyRepositoryAsync<TestEntity>>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursReadOnlyRepositories_ShouldRegisterSyncAndAsyncReadOnlyRepositories()
    {
        ServiceCollection services = CreateServices();
        services.AddMvp24HoursDbContext(o =>
        {
            o.ConnectionString = ConnectionString;
            o.DatabaseName = "ReadOnlyBothDb";
        });
        services.AddMvp24HoursReadOnlyRepositories(
            repositoryOptions => repositoryOptions.MaxQtyByQueryPage = 15);

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IReadOnlyRepository<TestEntity>>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IReadOnlyRepositoryAsync<TestEntity>>().Should().NotBeNull();
        provider.GetRequiredService<IOptions<MongoDbRepositoryOptions>>().Value.MaxQtyByQueryPage.Should().Be(15);
    }
}
