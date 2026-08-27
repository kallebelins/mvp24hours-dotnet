using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;
using Mvp24Hours.Infrastructure.Data.EFCore.Testing;

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

    [Fact]
    public async Task ModifyAsync_TestEntityLog_ShouldPreserveCreatedAuditFields()
    {
        using ServiceProvider provider = CreateUserLogProvider();
        using IServiceScope seedScope = provider.CreateScope();
        IRepositoryAsync<TestEntityLog> seedRepository = seedScope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntityLog>>();
        IUnitOfWorkAsync seedUnitOfWork = seedScope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
        var entity = new TestEntityLog { Name = "Original", CreatedBy = "seed-user" };
        await seedRepository.AddAsync(entity);
        await seedUnitOfWork.SaveChangesAsync();
        DateTime createdBeforeModify = entity.Created;

        using IServiceScope scope = provider.CreateScope();
        IRepositoryAsync<TestEntityLog> repository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntityLog>>();
        IUnitOfWorkAsync unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
        TestEntityLog persisted = (await repository.GetByIdAsync(entity.Id))!;
        persisted.Name = "Changed";

        await repository.ModifyAsync(persisted);
        await unitOfWork.SaveChangesAsync();

        TestEntityLog updated = (await repository.GetByIdAsync(entity.Id))!;
        updated.Name.Should().Be("Changed");
        updated.Created.Should().Be(createdBeforeModify);
    }

    [Fact]
    public async Task ModifyAsync_TestEntityLogGuid_PreservesCreatedBy()
    {
        // Covers the case where IEntityLog<TForeignKey> is closed over a value type
        // other than `object` (here Guid). A cast such as `(IEntityLog<object>)entity`
        // would throw InvalidCastException for this entity; reflection-based access must not.
        using ServiceProvider provider = CreateUserLogProvider();
        using IServiceScope seedScope = provider.CreateScope();
        IRepositoryAsync<TestEntityLogGuid> seedRepository = seedScope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntityLogGuid>>();
        IUnitOfWorkAsync seedUnitOfWork = seedScope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
        Guid createdBy = Guid.NewGuid();
        var entity = new TestEntityLogGuid { Name = "Original", CreatedBy = createdBy };
        await seedRepository.AddAsync(entity);
        await seedUnitOfWork.SaveChangesAsync();

        using IServiceScope scope = provider.CreateScope();
        IRepositoryAsync<TestEntityLogGuid> repository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntityLogGuid>>();
        IUnitOfWorkAsync unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
        TestEntityLogGuid persisted = (await repository.GetByIdAsync(entity.Id))!;
        persisted.Name = "Changed";

        await repository.ModifyAsync(persisted);
        await unitOfWork.SaveChangesAsync();

        TestEntityLogGuid updated = (await repository.GetByIdAsync(entity.Id))!;
        updated.Name.Should().Be("Changed");
        updated.CreatedBy.Should().Be(createdBy);
    }

    [Fact]
    public async Task RemoveAsync_TestEntityLog_ShouldSoftDeleteWithUser()
    {
        using ServiceProvider provider = CreateUserLogProvider();
        using IServiceScope seedScope = provider.CreateScope();
        IRepositoryAsync<TestEntityLog> seedRepository = seedScope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntityLog>>();
        IUnitOfWorkAsync seedUnitOfWork = seedScope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
        var entity = new TestEntityLog { Name = "ToRemove", CreatedBy = "seed-user" };
        await seedRepository.AddAsync(entity);
        await seedUnitOfWork.SaveChangesAsync();

        using IServiceScope scope = provider.CreateScope();
        IRepositoryAsync<TestEntityLog> repository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntityLog>>();
        IUnitOfWorkAsync unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
        TestDbContextWithFixedUser context = scope.ServiceProvider.GetRequiredService<TestDbContextWithFixedUser>();

        await repository.RemoveAsync((await repository.GetByIdAsync(entity.Id))!);
        await unitOfWork.SaveChangesAsync();

        (await repository.ListCountAsync()).Should().Be(0);
        TestEntityLog? deleted = await context.EntityLogs.IgnoreQueryFilters().SingleOrDefaultAsync(e => e.Id == entity.Id);
        deleted.Should().NotBeNull();
        deleted!.Removed.Should().NotBeNull();
        deleted.RemovedBy.Should().Be("operator");
    }

    [Fact]
    public async Task RemoveAsync_TestEntityLog_WhenEntityLogByUnavailable_ThrowsInvalidOperationException()
    {
        // Seed with a context that has a valid EntityLogBy (Add/ApplyLogRules stamps CreatedBy
        // from EntityLogBy), then exercise Remove from a second context pointed at the same
        // in-memory database but without an EntityLogBy configured.
        string databaseName = $"UserLogNullAsync_{Guid.NewGuid():N}";

        using ServiceProvider seedProvider = CreateUserLogProvider(databaseName);
        using IServiceScope seedScope = seedProvider.CreateScope();
        IRepositoryAsync<TestEntityLog> seedRepository = seedScope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntityLog>>();
        IUnitOfWorkAsync seedUnitOfWork = seedScope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
        var entity = new TestEntityLog { Name = "ToRemove", CreatedBy = "seed-user" };
        await seedRepository.AddAsync(entity);
        await seedUnitOfWork.SaveChangesAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContextWithUser>(databaseName);
        services.AddMvp24HoursRepositoryAsync(o => o.MaxQtyByQueryPage = 100);
        using ServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        IRepositoryAsync<TestEntityLog> repository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntityLog>>();

        Func<Task> act = async () => await repository.RemoveAsync((await repository.GetByIdAsync(entity.Id))!);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("EntityLogBy is not available.");
    }

    private static ServiceProvider CreateUserLogProvider(string? databaseName = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContextWithFixedUser>(databaseName ?? $"UserLogAsync_{Guid.NewGuid():N}");
        services.AddMvp24HoursRepositoryAsync(o => o.MaxQtyByQueryPage = 100);
        return services.BuildServiceProvider();
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
