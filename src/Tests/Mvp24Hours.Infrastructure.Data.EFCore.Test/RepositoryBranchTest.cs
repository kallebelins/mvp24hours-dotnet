using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;
using Mvp24Hours.Infrastructure.Data.EFCore.Testing;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test;

[Trait("Category", "Unit")]
public class RepositoryBranchTest : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly string _databaseName = $"BranchRepo_{Guid.NewGuid():N}";

    public RepositoryBranchTest()
    {
        _provider = EfCoreTestHelpers.CreateSyncServices(_databaseName);
    }

    public void Dispose()
    {
        _provider.Dispose();
    }

    [Fact]
    public void Add_NullEntity_ShouldNoOp()
    {
        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();

        repository.Add((TestEntity)null!);
        repository.ListCount().Should().Be(0);
    }

    [Fact]
    public void Modify_List_ShouldUpdateAllEntities()
    {
        List<TestEntity> entities = SeedEntities(3);

        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        foreach (TestEntity entity in entities)
        {
            entity.Name = $"Updated-{entity.Id}";
        }

        repository.Modify(entities);
        unitOfWork.SaveChanges();

        repository.List().Should().OnlyContain(e => e.Name.StartsWith("Updated-"));
    }

    [Fact]
    public void Remove_List_ShouldRemoveAllEntities()
    {
        List<TestEntity> entities = SeedEntities(3);

        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        repository.Remove(entities);
        unitOfWork.SaveChanges();

        repository.ListAny().Should().BeFalse();
    }

    [Fact]
    public void RemoveById_List_ShouldRemoveEachEntity()
    {
        List<TestEntity> entities = SeedEntities(3);

        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        repository.RemoveById([.. entities.Select(e => (object)e.Id)]);
        unitOfWork.SaveChanges();

        repository.ListAny().Should().BeFalse();
    }

    [Fact]
    public void RemoveById_MissingId_ShouldNoOp()
    {
        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();

        repository.RemoveById(99999);

        repository.ListAny().Should().BeFalse();
    }

    [Fact]
    public void GetByAny_WithNullClause_ShouldReturnAnyEntity()
    {
        SeedEntities(2);

        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();

        repository.GetByAny(null!).Should().BeTrue();
        repository.GetByCount(null!).Should().Be(2);
    }

    [Fact]
    public void Modify_TestEntityLog_ShouldPreserveCreatedAuditFields()
    {
        using ServiceProvider provider = CreateUserLogProvider();
        using IServiceScope seedScope = provider.CreateScope();
        IRepository<TestEntityLog> seedRepository = seedScope.ServiceProvider.GetRequiredService<IRepository<TestEntityLog>>();
        IUnitOfWork seedUnitOfWork = seedScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var entity = new TestEntityLog { Name = "Original", CreatedBy = "seed-user" };
        seedRepository.Add(entity);
        seedUnitOfWork.SaveChanges();
        DateTime createdBeforeModify = entity.Created;

        using IServiceScope scope = provider.CreateScope();
        IRepository<TestEntityLog> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntityLog>>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        TestEntityLog persisted = repository.GetById(entity.Id)!;
        persisted.Name = "Changed";

        repository.Modify(persisted);
        unitOfWork.SaveChanges();

        TestEntityLog updated = repository.GetById(entity.Id)!;
        updated.Name.Should().Be("Changed");
        updated.Created.Should().Be(createdBeforeModify);
    }

    [Fact]
    public void Remove_TestEntityLog_ShouldSoftDeleteWithUser()
    {
        using ServiceProvider provider = CreateUserLogProvider();
        using IServiceScope seedScope = provider.CreateScope();
        IRepository<TestEntityLog> seedRepository = seedScope.ServiceProvider.GetRequiredService<IRepository<TestEntityLog>>();
        IUnitOfWork seedUnitOfWork = seedScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var entity = new TestEntityLog { Name = "ToRemove", CreatedBy = "seed-user" };
        seedRepository.Add(entity);
        seedUnitOfWork.SaveChanges();

        using IServiceScope scope = provider.CreateScope();
        IRepository<TestEntityLog> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntityLog>>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        TestDbContextWithFixedUser context = scope.ServiceProvider.GetRequiredService<TestDbContextWithFixedUser>();

        repository.Remove(repository.GetById(entity.Id)!);
        unitOfWork.SaveChanges();

        repository.ListCount().Should().Be(0);
        TestEntityLog? deleted = context.EntityLogs.IgnoreQueryFilters().SingleOrDefault(e => e.Id == entity.Id);
        deleted.Should().NotBeNull();
        deleted!.Removed.Should().NotBeNull();
        deleted.RemovedBy.Should().Be("operator");
    }

    private static ServiceProvider CreateUserLogProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContextWithFixedUser>($"UserLog_{Guid.NewGuid():N}");
        services.AddMvp24HoursRepository(o => o.MaxQtyByQueryPage = 100);
        return services.BuildServiceProvider();
    }

    private List<TestEntity> SeedEntities(int count)
    {
        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        List<TestEntity> entities = EfCoreTestHelpers.CreateEntities(count, "Branch");
        repository.Add(entities);
        unitOfWork.SaveChanges();
        return entities;
    }
}
