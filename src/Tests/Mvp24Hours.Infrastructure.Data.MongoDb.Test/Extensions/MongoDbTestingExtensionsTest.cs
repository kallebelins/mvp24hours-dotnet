using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;
using Mvp24Hours.Infrastructure.Data.MongoDb.Testing;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Extensions;

[Trait("Category", "Unit")]
public class MongoDbTestingExtensionsTest
{
    private const string ConnectionString = "mongodb://127.0.0.1:27017";

    [Fact]
    public void AddMvp24HoursMongoFakeRepository_ShouldRegisterFakeSyncServices()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursMongoFakeRepository();

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<MongoUnitOfWorkFake>().Should().NotBeNull();
        provider.GetRequiredService<IUnitOfWork>().Should().BeOfType<MongoUnitOfWorkFake>();
        provider.GetRequiredService<IRepository<TestEntity>>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursMongoFakeRepositoryAsync_ShouldRegisterFakeAsyncServices()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursMongoFakeRepositoryAsync();

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<MongoUnitOfWorkFakeAsync>().Should().NotBeNull();
        provider.GetRequiredService<IUnitOfWorkAsync>().Should().BeOfType<MongoUnitOfWorkFakeAsync>();
        provider.GetRequiredService<IRepositoryAsync<TestEntity>>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursMongoFakeRepositoryWithData_ShouldSeedRepository()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursMongoFakeRepositoryWithData<TestEntity>(repo => repo.SeedData(entities => entities.Add(new TestEntity { Name = "Seeded" })));

        using ServiceProvider provider = services.BuildServiceProvider();
        IRepository<TestEntity> repository = provider.GetRequiredService<IRepository<TestEntity>>();

        repository.List().Should().ContainSingle(e => e.Name == "Seeded");
    }

    [Fact]
    public async Task AddMvp24HoursMongoFakeRepositoryAsyncWithData_ShouldSeedAsyncRepository()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursMongoFakeRepositoryAsyncWithData<TestEntity>(repo => repo.SeedData(entities => entities.Add(new TestEntity { Name = "AsyncSeeded" })));

        using ServiceProvider provider = services.BuildServiceProvider();
        IRepositoryAsync<TestEntity> repository = provider.GetRequiredService<IRepositoryAsync<TestEntity>>();

        (await repository.ListAsync()).Should().ContainSingle(e => e.Name == "AsyncSeeded");
    }

    [Fact]
    public void AddMvp24HoursMongoInMemoryProvider_ShouldRegisterProviderAndOptions()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursMongoInMemoryProvider(options =>
        {
            options.DatabaseNamePrefix = "UnitTest";
            options.UseUniqueDatabaseName = true;
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<MongoDbInMemoryProvider>().Should().NotBeNull();
        provider.GetRequiredService<MongoDbInMemoryOptions>().DatabaseNamePrefix.Should().Be("UnitTest");
    }

    [Fact]
    public void AddMvp24HoursMongoContextFactory_WithConnectionString_ShouldRegisterFactory()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursMongoContextFactory(ConnectionString, options =>
        {
            options.UseUniqueDatabaseName = true;
            options.EnableLogging = true;
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<MongoDbContextFactory>().Should().NotBeNull();
        provider.GetRequiredService<MongoDbInMemoryOptions>().ConnectionString.Should().Be(ConnectionString);
    }

    [Fact]
    public void AddMvp24HoursMongoContextFactory_WithCustomFactory_ShouldRegisterCustomFactory()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursMongoContextFactory(
            options => new Mvp24HoursContext(options),
            configureOptions: options => options.ConnectionString = ConnectionString);

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<MongoDbContextFactory>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursMongoTestInfrastructure_ShouldRegisterDbContextAndAsyncRepository()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvp24HoursMongoTestInfrastructure(ConnectionString, options => options.ReadPreference = "primary");

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<Mvp24HoursContext>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntity>>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursMongoTestInfrastructureWithSeeder_ShouldRegisterSeeder()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvp24HoursMongoTestInfrastructureWithSeeder<TestEntitySeeder>(ConnectionString);

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IMongoDataSeeder>().Should().BeOfType<TestEntitySeeder>();
    }

    [Fact]
    public void AddMvp24HoursMongoFakeTestInfrastructure_ShouldRegisterSyncAndAsyncFakeServices()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursMongoFakeTestInfrastructure();

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IUnitOfWork>().Should().BeOfType<MongoUnitOfWorkFake>();
        provider.GetRequiredService<IUnitOfWorkAsync>().Should().BeOfType<MongoUnitOfWorkFakeAsync>();
        provider.GetRequiredService<IRepository<TestEntity>>().Should().NotBeNull();
        provider.GetRequiredService<IRepositoryAsync<TestEntity>>().Should().NotBeNull();
    }

    private sealed class TestEntitySeeder : IMongoDataSeeder
    {
        public void Seed(Mvp24HoursContext context)
        {
        }
    }
}
