using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Testing;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test;

[Trait("Category", "Unit")]
public class ReadOnlyRepositoryTest : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly string _databaseName = $"ReadOnly_{Guid.NewGuid():N}";

    public ReadOnlyRepositoryTest()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContext>(_databaseName);
        services.AddMvp24HoursReadOnlyRepository(o => o.MaxQtyByQueryPage = 100);
        services.AddMvp24HoursReadOnlyRepositoryAsync(o => o.MaxQtyByQueryPage = 100);
        _provider = services.BuildServiceProvider();

        SeedData();
    }

    public void Dispose() => _provider.Dispose();

    [Fact]
    public void Sync_List_GetBy_GetById_Any_Count_ShouldReturnSeededData()
    {
        using IServiceScope scope = _provider.CreateScope();
        IReadOnlyRepository<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IReadOnlyRepository<TestEntity>>();

        repository.ListAny().Should().BeTrue();
        repository.ListCount().Should().Be(6);
        repository.List().Should().HaveCount(6);
        repository.GetByAny(e => e.Active).Should().BeTrue();
        repository.GetByCount(e => e.Active).Should().Be(3);
        repository.GetBy(e => e.Score >= 30).Should().HaveCountGreaterThan(0);
        repository.GetById(1).Should().NotBeNull();
    }

    [Fact]
    public void Sync_List_ShouldReturnDetachedEntities()
    {
        using IServiceScope scope = _provider.CreateScope();
        IReadOnlyRepository<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IReadOnlyRepository<TestEntity>>();
        TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        IList<TestEntity> entities = repository.List();

        entities.Should().NotBeEmpty();
        entities.Should().OnlyContain(e => context.Entry(e).State == EntityState.Detached);
    }

    [Fact]
    public void Sync_GetBySpecification_ShouldFilterBySpecification()
    {
        using IServiceScope scope = _provider.CreateScope();
        IReadOnlyRepository<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IReadOnlyRepository<TestEntity>>();
        var specification = new ActiveTestEntitySpecification();

        IList<TestEntity> activeEntities = repository.GetBySpecification(specification);

        activeEntities.Should().HaveCount(3);
        activeEntities.Should().OnlyContain(e => e.Active);
    }

    [Fact]
    public async Task Async_List_GetBy_GetById_Any_Count_ShouldReturnSeededData()
    {
        using IServiceScope scope = _provider.CreateScope();
        IReadOnlyRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IReadOnlyRepositoryAsync<TestEntity>>();

        (await repository.ListAnyAsync()).Should().BeTrue();
        (await repository.ListCountAsync()).Should().Be(6);
        (await repository.ListAsync()).Should().HaveCount(6);
        (await repository.GetByAnyAsync(e => e.Active)).Should().BeTrue();
        (await repository.GetByCountAsync(e => e.Active)).Should().Be(3);
        (await repository.GetByAsync(e => e.Score >= 30)).Should().HaveCountGreaterThan(0);
        (await repository.GetByIdAsync(1)).Should().NotBeNull();
    }

    [Fact]
    public async Task Async_List_ShouldReturnDetachedEntities()
    {
        using IServiceScope scope = _provider.CreateScope();
        IReadOnlyRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IReadOnlyRepositoryAsync<TestEntity>>();
        TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        IList<TestEntity> entities = await repository.ListAsync();

        entities.Should().NotBeEmpty();
        entities.Should().OnlyContain(e => context.Entry(e).State == EntityState.Detached);
    }

    [Fact]
    public async Task Async_GetBySpecificationAsync_ShouldFilterBySpecification()
    {
        using IServiceScope scope = _provider.CreateScope();
        IReadOnlyRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IReadOnlyRepositoryAsync<TestEntity>>();
        var specification = new ActiveTestEntitySpecification();

        IList<TestEntity> activeEntities = await repository.GetBySpecificationAsync(specification);

        activeEntities.Should().HaveCount(3);
        activeEntities.Should().OnlyContain(e => e.Active);
    }

    private void SeedData()
    {
        using IServiceScope scope = _provider.CreateScope();
        TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        context.Entities.AddRange(EfCoreTestHelpers.CreateEntities(6, "ReadOnly"));
        context.SaveChanges();
    }
}
