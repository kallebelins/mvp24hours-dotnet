using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test;

[Trait("Category", "Unit")]
public class RepositoryTest : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly string _databaseName = $"SyncRepo_{Guid.NewGuid():N}";

    public RepositoryTest()
    {
        _provider = EfCoreTestHelpers.CreateSyncServices(_databaseName);
    }

    public void Dispose()
    {
        _provider.Dispose();
    }

    [Fact]
    public void Add_SingleEntity_ShouldPersistAndBeRetrievableById()
    {
        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var entity = new TestEntity { Name = "Alpha", Active = true, Score = 100 };
        repository.Add(entity);
        unitOfWork.SaveChanges();

        entity.Id.Should().BeGreaterThan(0);
        repository.GetById(entity.Id)!.Name.Should().Be("Alpha");
    }

    [Fact]
    public void Modify_WhenKeyNotFoundInDatabase_ShouldThrow()
    {
        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();

        var entity = new TestEntity { Id = 99999, Name = "Missing" };

        Action act = () => repository.Modify(entity);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Key value not found.");
    }

    [Fact]
    public void Add_List_ShouldPersistAllEntities()
    {
        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        List<TestEntity> entities = EfCoreTestHelpers.CreateEntities(5);
        repository.Add(entities);
        unitOfWork.SaveChanges();

        repository.ListCount().Should().Be(5);
    }

    [Fact]
    public void List_GetBy_ListAny_ListCount_GetByAny_GetByCount_ShouldReturnExpectedResults()
    {
        SeedTestEntities(6);

        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();

        repository.ListAny().Should().BeTrue();
        repository.ListCount().Should().Be(6);
        repository.List().Should().HaveCount(6);

        repository.GetByAny(e => e.Active).Should().BeTrue();
        repository.GetByCount(e => e.Active).Should().Be(3);
        repository.GetBy(e => e.Active).Should().OnlyContain(e => e.Active);
    }

    [Fact]
    public void List_WithPagingCriteria_ShouldApplySkipAndTake()
    {
        SeedTestEntities(10);

        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        var paging = new PagingCriteria(limit: 3, offset: 1);

        IList<TestEntity> page = repository.List(paging);

        page.Should().HaveCount(3);
    }

    [Fact]
    public void GetById_And_GetBy_WithPaging_ShouldReturnEntity()
    {
        int entityId = SeedTestEntities(4).First().Id;

        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();

        repository.GetById(entityId)!.Id.Should().Be(entityId);
        repository.GetBy(e => e.Id == entityId, new PagingCriteria(limit: 1, offset: 0))
            .Should().ContainSingle(e => e.Id == entityId);
    }

    [Fact]
    public void Modify_SingleEntity_ShouldUpdatePersistedValues()
    {
        TestEntity entity = SeedTestEntities(1).Single();

        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        entity.Name = "Updated";
        entity.Score = 999;
        repository.Modify(entity);
        unitOfWork.SaveChanges();

        repository.GetById(entity.Id)!.Name.Should().Be("Updated");
        repository.GetById(entity.Id)!.Score.Should().Be(999);
    }

    [Fact]
    public void Remove_TestEntity_ShouldHardDelete()
    {
        TestEntity entity = SeedTestEntities(1).Single();

        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        repository.Remove(entity);
        unitOfWork.SaveChanges();

        repository.ListCount().Should().Be(0);
    }

    [Fact]
    public void Remove_TestLogEntity_ShouldSoftDeleteBySettingRemoved()
    {
        using IServiceScope seedScope = _provider.CreateScope();
        IRepository<TestLogEntity> seedRepository = seedScope.ServiceProvider.GetRequiredService<IRepository<TestLogEntity>>();
        IUnitOfWork seedUnitOfWork = seedScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var logEntity = new TestLogEntity { Name = "Log-1" };
        seedRepository.Add(logEntity);
        seedUnitOfWork.SaveChanges();

        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestLogEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestLogEntity>>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        TestLogEntity persisted = repository.GetById(logEntity.Id)!;
        repository.Remove(persisted);
        unitOfWork.SaveChanges();

        repository.ListCount().Should().Be(0);

        TestLogEntity? deleted = context.LogEntities
            .IgnoreQueryFilters()
            .SingleOrDefault(e => e.Id == logEntity.Id);

        deleted.Should().NotBeNull();
        deleted!.Removed.Should().NotBeNull();
    }

    [Fact]
    public void RemoveById_ShouldRemoveEntity()
    {
        TestEntity entity = SeedTestEntities(1).Single();

        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        repository.RemoveById(entity.Id);
        unitOfWork.SaveChanges();

        repository.ListAny().Should().BeFalse();
    }

    private List<TestEntity> SeedTestEntities(int count)
    {
        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        List<TestEntity> entities = EfCoreTestHelpers.CreateEntities(count);
        repository.Add(entities);
        unitOfWork.SaveChanges();
        return entities;
    }
}
