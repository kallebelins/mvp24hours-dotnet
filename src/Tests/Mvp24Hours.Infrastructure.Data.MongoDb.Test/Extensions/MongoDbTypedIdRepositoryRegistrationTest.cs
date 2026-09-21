//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Extensions;

/// <summary>
/// Registration coverage for the optional strongly-typed identifier contracts
/// <see cref="IRepository{T, TId}"/> / <see cref="IRepositoryAsync{T, TId}"/>.
/// </summary>
[Trait("Category", "Unit")]
public class MongoDbTypedIdRepositoryRegistrationTest
{
    private const string ConnectionString = "mongodb://127.0.0.1:27017";

    private static ServiceCollection CreateServices(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursDbContext(o =>
        {
            o.ConnectionString = ConnectionString;
            o.DatabaseName = databaseName;
        });
        return services;
    }

    [Fact]
    public void AddMvp24HoursRepository_ShouldResolveTypedIdRepository()
    {
        ServiceCollection services = CreateServices("TypedRepoDb");
        services.AddMvp24HoursRepository();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        IRepository<TestTypedEntity, ObjectId> repository =
            scope.ServiceProvider.GetRequiredService<IRepository<TestTypedEntity, ObjectId>>();

        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IRepository<TestTypedEntity>>();
    }

    [Fact]
    public void AddMvp24HoursRepositoryAsync_ShouldResolveTypedIdRepository()
    {
        ServiceCollection services = CreateServices("TypedRepoAsyncDb");
        services.AddMvp24HoursRepositoryAsync();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        IRepositoryAsync<TestTypedEntity, ObjectId> repository =
            scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestTypedEntity, ObjectId>>();

        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IRepositoryAsync<TestTypedEntity>>();
    }

    [Fact]
    public void AddMvp24HoursRepository_ShouldNotChangeExistingUntypedRegistration()
    {
        ServiceCollection services = CreateServices("UntypedStillWorksDb");
        services.AddMvp24HoursRepository();

        services.Should().Contain(d =>
            d.ServiceType == typeof(IRepository<>) &&
            d.ImplementationType == typeof(Repository<>));
        services.Should().Contain(d =>
            d.ServiceType == typeof(IRepository<,>) &&
            d.ImplementationType == typeof(Repository<,>));
    }

    [Fact]
    public void AddMvp24HoursRepository_WithCustomRepositoryType_ShouldNotRegisterTypedIdContract()
    {
        ServiceCollection services = CreateServices("CustomRepoDb");
        services.AddMvp24HoursRepository(repository: typeof(Repository<>));

        // A custom one-parameter repository has no two-parameter counterpart to map to, so the
        // typed contract stays unregistered instead of silently bypassing the customization.
        services.Should().NotContain(d => d.ServiceType == typeof(IRepository<,>));
    }

    [Fact]
    public void AddMvp24HoursRepositoryAsync_WithCustomRepositoryType_ShouldNotRegisterTypedIdContract()
    {
        ServiceCollection services = CreateServices("CustomRepoAsyncDb");
        services.AddMvp24HoursRepositoryAsync(repositoryAsync: typeof(RepositoryAsync<>));

        services.Should().NotContain(d => d.ServiceType == typeof(IRepositoryAsync<,>));
    }
}
