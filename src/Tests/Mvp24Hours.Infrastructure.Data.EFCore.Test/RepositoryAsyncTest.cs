using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test;

[Trait("Category", "Unit")]
public class RepositoryAsyncTest : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly string _databaseName = $"AsyncRepo_{Guid.NewGuid():N}";

    public RepositoryAsyncTest()
    {
        _provider = EfCoreTestHelpers.CreateAsyncServices(_databaseName);
    }

    public void Dispose()
    {
        _provider.Dispose();
    }

    [Fact]
    public async Task AddAsync_SingleEntity_ShouldPersistAndBeRetrievableById()
    {
        using IServiceScope scope = _provider.CreateScope();
        IRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntity>>();
        IUnitOfWorkAsync unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();

        var entity = new TestEntity { Name = "Async-Alpha", Active = true, Score = 50 };
        await repository.AddAsync(entity);
        await unitOfWork.SaveChangesAsync();

        entity.Id.Should().BeGreaterThan(0);
        (await repository.GetByIdAsync(entity.Id))!.Name.Should().Be("Async-Alpha");
    }

    [Fact]
    public async Task AddAsync_List_ShouldPersistAllEntities()
    {
        using IServiceScope scope = _provider.CreateScope();
        IRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntity>>();
        IUnitOfWorkAsync unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();

        List<TestEntity> entities = EfCoreTestHelpers.CreateEntities(5, "Async");
        await repository.AddAsync(entities);
        await unitOfWork.SaveChangesAsync();

        (await repository.ListCountAsync()).Should().Be(5);
    }

    [Fact]
    public async Task ListAsync_GetByAsync_ListAnyAsync_ListCountAsync_GetByAnyAsync_GetByCountAsync_ShouldReturnExpectedResults()
    {
        await SeedTestEntitiesAsync(6);

        using IServiceScope scope = _provider.CreateScope();
        IRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntity>>();

        (await repository.ListAnyAsync()).Should().BeTrue();
        (await repository.ListCountAsync()).Should().Be(6);
        (await repository.ListAsync()).Should().HaveCount(6);

        (await repository.GetByAnyAsync(e => e.Active)).Should().BeTrue();
        (await repository.GetByCountAsync(e => e.Active)).Should().Be(3);
        (await repository.GetByAsync(e => e.Active)).Should().OnlyContain(e => e.Active);
    }

    [Fact]
    public async Task ListAsync_WithPagingCriteria_ShouldApplySkipAndTake()
    {
        await SeedTestEntitiesAsync(10);

        using IServiceScope scope = _provider.CreateScope();
        IRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntity>>();
        var paging = new PagingCriteria(limit: 3, offset: 1);

        IList<TestEntity> page = await repository.ListAsync(paging);

        page.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetByIdAsync_And_GetByAsync_WithPaging_ShouldReturnEntity()
    {
        List<TestEntity> seeded = await SeedTestEntitiesAsync(4);
        int entityId = seeded.First().Id;

        using IServiceScope scope = _provider.CreateScope();
        IRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntity>>();

        (await repository.GetByIdAsync(entityId))!.Id.Should().Be(entityId);
        (await repository.GetByAsync(e => e.Id == entityId, new PagingCriteria(limit: 1, offset: 0)))
            .Should().ContainSingle(e => e.Id == entityId);
    }

    [Fact]
    public async Task ModifyAsync_SingleEntity_ShouldUpdatePersistedValues()
    {
        TestEntity entity = (await SeedTestEntitiesAsync(1)).Single();

        using IServiceScope scope = _provider.CreateScope();
        IRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntity>>();
        IUnitOfWorkAsync unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();

        entity.Name = "Async-Updated";
        entity.Score = 777;
        await repository.ModifyAsync(entity);
        await unitOfWork.SaveChangesAsync();

        (await repository.GetByIdAsync(entity.Id))!.Name.Should().Be("Async-Updated");
        (await repository.GetByIdAsync(entity.Id))!.Score.Should().Be(777);
    }

    [Fact]
    public async Task RemoveAsync_TestEntity_ShouldHardDelete()
    {
        TestEntity entity = (await SeedTestEntitiesAsync(1)).Single();

        using IServiceScope scope = _provider.CreateScope();
        IRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntity>>();
        IUnitOfWorkAsync unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();

        await repository.RemoveAsync(entity);
        await unitOfWork.SaveChangesAsync();

        (await repository.ListCountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task RemoveAsync_TestLogEntity_ShouldSoftDeleteBySettingRemoved()
    {
        using IServiceScope seedScope = _provider.CreateScope();
        IRepositoryAsync<TestLogEntity> seedRepository = seedScope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestLogEntity>>();
        IUnitOfWorkAsync seedUnitOfWork = seedScope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
        var logEntity = new TestLogEntity { Name = "Async-Log-1" };
        await seedRepository.AddAsync(logEntity);
        await seedUnitOfWork.SaveChangesAsync();

        using IServiceScope scope = _provider.CreateScope();
        IRepositoryAsync<TestLogEntity> repository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestLogEntity>>();
        IUnitOfWorkAsync unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
        TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        TestLogEntity persisted = (await repository.GetByIdAsync(logEntity.Id))!;
        await repository.RemoveAsync(persisted);
        await unitOfWork.SaveChangesAsync();

        (await repository.ListCountAsync()).Should().Be(0);

        TestLogEntity? deleted = await context.LogEntities
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(e => e.Id == logEntity.Id);

        deleted.Should().NotBeNull();
        deleted!.Removed.Should().NotBeNull();
    }

    [Fact]
    public async Task RemoveByIdAsync_ShouldRemoveEntity()
    {
        TestEntity entity = (await SeedTestEntitiesAsync(1)).Single();

        using IServiceScope scope = _provider.CreateScope();
        IRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntity>>();
        IUnitOfWorkAsync unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();

        await repository.RemoveByIdAsync(entity.Id);
        await unitOfWork.SaveChangesAsync();

        (await repository.ListAnyAsync()).Should().BeFalse();
    }

    private async Task<List<TestEntity>> SeedTestEntitiesAsync(int count)
    {
        using IServiceScope scope = _provider.CreateScope();
        IRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntity>>();
        IUnitOfWorkAsync unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
        List<TestEntity> entities = EfCoreTestHelpers.CreateEntities(count, "AsyncSeed");
        await repository.AddAsync(entities);
        await unitOfWork.SaveChangesAsync();
        return entities;
    }
}
