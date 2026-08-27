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

    [Fact]
    public void Modify_TestEntityLogGuid_PreservesCreatedBy()
    {
        // Covers the case where IEntityLog<TForeignKey> is closed over a value type
        // other than `object` (here Guid). A cast such as `(IEntityLog<object>)entity`
        // would throw InvalidCastException for this entity; reflection-based access must not.
        using ServiceProvider provider = CreateUserLogProvider();
        using IServiceScope seedScope = provider.CreateScope();
        IRepository<TestEntityLogGuid> seedRepository = seedScope.ServiceProvider.GetRequiredService<IRepository<TestEntityLogGuid>>();
        IUnitOfWork seedUnitOfWork = seedScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        Guid createdBy = Guid.NewGuid();
        var entity = new TestEntityLogGuid { Name = "Original", CreatedBy = createdBy };
        seedRepository.Add(entity);
        seedUnitOfWork.SaveChanges();

        using IServiceScope scope = provider.CreateScope();
        IRepository<TestEntityLogGuid> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntityLogGuid>>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        TestEntityLogGuid persisted = repository.GetById(entity.Id)!;
        persisted.Name = "Changed";

        repository.Modify(persisted);
        unitOfWork.SaveChanges();

        TestEntityLogGuid updated = repository.GetById(entity.Id)!;
        updated.Name.Should().Be("Changed");
        updated.CreatedBy.Should().Be(createdBy);
    }

    [Fact]
    public void Remove_TestEntityLog_WhenEntityLogByUnavailable_ThrowsInvalidOperationException()
    {
        // Seed with a context that has a valid EntityLogBy (Add/ApplyLogRules stamps CreatedBy
        // from EntityLogBy), then exercise Remove from a second context pointed at the same
        // in-memory database but without an EntityLogBy configured.
        string databaseName = $"UserLogNull_{Guid.NewGuid():N}";

        using ServiceProvider seedProvider = CreateUserLogProvider(databaseName);
        using IServiceScope seedScope = seedProvider.CreateScope();
        IRepository<TestEntityLog> seedRepository = seedScope.ServiceProvider.GetRequiredService<IRepository<TestEntityLog>>();
        IUnitOfWork seedUnitOfWork = seedScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var entity = new TestEntityLog { Name = "ToRemove", CreatedBy = "seed-user" };
        seedRepository.Add(entity);
        seedUnitOfWork.SaveChanges();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContextWithUser>(databaseName);
        services.AddMvp24HoursRepository(o => o.MaxQtyByQueryPage = 100);
        using ServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        IRepository<TestEntityLog> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestEntityLog>>();

        Action act = () => repository.Remove(repository.GetById(entity.Id)!);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("EntityLogBy is not available.");
    }

    private static ServiceProvider CreateUserLogProvider(string? databaseName = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContextWithFixedUser>(databaseName ?? $"UserLog_{Guid.NewGuid():N}");
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
