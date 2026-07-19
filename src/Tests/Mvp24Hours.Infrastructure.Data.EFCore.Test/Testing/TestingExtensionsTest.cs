using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;
using Mvp24Hours.Infrastructure.Data.EFCore.Testing;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Testing;

[Trait("Category", "Unit")]
public class TestingExtensionsTest
{
    [Fact]
    public void AddMvp24HoursTestInfrastructure_ShouldResolveDbContextAndRepository()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursTestInfrastructure<TestDbContext>("IntegrationTestDb");

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
        var repository = unitOfWork.GetRepository<TestEntity>();

        context.Should().NotBeNull();
        dbContext.Should().BeOfType<TestDbContext>();
        repository.Should().NotBeNull();
        context.Database.IsInMemory().Should().BeTrue();
    }

    [Fact]
    public void AddMvp24HoursFakeRepository_ShouldResolveSyncRepository()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursFakeRepository();

        using ServiceProvider provider = services.BuildServiceProvider();

        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var repository = unitOfWork.GetRepository<TestEntity>();

        repository.Should().BeOfType<RepositoryFake<TestEntity>>();
    }

    [Fact]
    public void AddMvp24HoursFakeRepositoryAsync_ShouldResolveAsyncRepository()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursFakeRepositoryAsync();

        using ServiceProvider provider = services.BuildServiceProvider();

        var unitOfWork = provider.GetRequiredService<IUnitOfWorkAsync>();
        var repository = unitOfWork.GetRepository<TestEntity>();

        repository.Should().BeOfType<RepositoryFakeAsync<TestEntity>>();
    }

    [Fact]
    public void AddMvp24HoursTestDbContextFactory_ShouldResolveFactory()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursTestDbContextFactory<TestDbContext>(o =>
        {
            o.CreateNewDatabasePerTest = true;
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<ITestDbContextFactory<TestDbContext>>();
        using TestDbContext context = factory.CreateContext();

        context.Database.IsInMemory().Should().BeTrue();
    }

    [Fact]
    public void AddMvp24HoursInMemoryDbContextFactory_ShouldResolveFactory()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursInMemoryDbContextFactory<TestDbContext>();

        using ServiceProvider provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<InMemoryDbContextFactory<TestDbContext>>();
        using TestDbContext context = factory.CreateContextWithData<TestEntitySeeder>();

        context.Entities.Should().HaveCount(TestEntitySeeder.DefaultSeedCount);
    }
}
