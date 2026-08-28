using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test;

/// <summary>
/// Covers the optional strongly-typed identifier contracts
/// <see cref="IRepository{T, TId}"/> / <see cref="IRepositoryAsync{T, TId}"/>.
/// Every typed member must behave exactly like the <see cref="object"/>-based member
/// it delegates to.
/// </summary>
[Trait("Category", "Unit")]
public class RepositoryTypedIdTest : IDisposable
{
    private readonly ServiceProvider _syncProvider;
    private readonly ServiceProvider _asyncProvider;

    public RepositoryTypedIdTest()
    {
        _syncProvider = EfCoreTestHelpers.CreateSyncServices($"SyncTypedRepo_{Guid.NewGuid():N}");
        _asyncProvider = EfCoreTestHelpers.CreateAsyncServices($"AsyncTypedRepo_{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        _syncProvider.Dispose();
        _asyncProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    #region [ Sync ]

    [Fact]
    public void TypedRepository_ShouldResolveFromContainer()
    {
        using IServiceScope scope = _syncProvider.CreateScope();

        IRepository<TestTypedEntity, int> repository =
            scope.ServiceProvider.GetRequiredService<IRepository<TestTypedEntity, int>>();

        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IRepository<TestTypedEntity>>();
    }

    [Fact]
    public void GetById_WithTypedId_ReturnsSameEntityAsObjectOverload()
    {
        int id = SeedTypedEntities(3).First().Id;

        using IServiceScope scope = _syncProvider.CreateScope();
        IRepository<TestTypedEntity, int> repository =
            scope.ServiceProvider.GetRequiredService<IRepository<TestTypedEntity, int>>();

        TestTypedEntity? typed = repository.GetById(id);
        TestTypedEntity? untyped = ((IRepository<TestTypedEntity>)repository).GetById((object)id);

        typed.Should().NotBeNull();
        typed!.Id.Should().Be(id);
        typed.Name.Should().Be(untyped!.Name);
    }

    [Fact]
    public void GetById_WithTypedIdAndPagingCriteria_ReturnsEntity()
    {
        int id = SeedTypedEntities(2).Last().Id;

        using IServiceScope scope = _syncProvider.CreateScope();
        IRepository<TestTypedEntity, int> repository =
            scope.ServiceProvider.GetRequiredService<IRepository<TestTypedEntity, int>>();

        TestTypedEntity? found = repository.GetById(id, new PagingCriteria(limit: 1, offset: 0));

        found.Should().NotBeNull();
        found!.Id.Should().Be(id);
    }

    [Fact]
    public void GetById_WithTypedIdNotFound_ReturnsNull()
    {
        SeedTypedEntities(1);

        using IServiceScope scope = _syncProvider.CreateScope();
        IRepository<TestTypedEntity, int> repository =
            scope.ServiceProvider.GetRequiredService<IRepository<TestTypedEntity, int>>();

        repository.GetById(int.MaxValue).Should().BeNull();
    }

    [Fact]
    public void RemoveById_WithTypedId_BehavesLikeObjectOverload()
    {
        List<TestTypedEntity> seeded = SeedTypedEntities(2);

        using IServiceScope scope = _syncProvider.CreateScope();
        IRepository<TestTypedEntity, int> repository =
            scope.ServiceProvider.GetRequiredService<IRepository<TestTypedEntity, int>>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        repository.RemoveById(seeded[0].Id);
        unitOfWork.SaveChanges();

        repository.ListCount().Should().Be(1);
        repository.GetById(seeded[0].Id).Should().BeNull();
        repository.GetById(seeded[1].Id).Should().NotBeNull();
    }

    [Fact]
    public void RemoveById_WithTypedIdList_RemovesAll()
    {
        List<TestTypedEntity> seeded = SeedTypedEntities(3);

        using IServiceScope scope = _syncProvider.CreateScope();
        IRepository<TestTypedEntity, int> repository =
            scope.ServiceProvider.GetRequiredService<IRepository<TestTypedEntity, int>>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        repository.RemoveById(seeded.Select(e => e.Id).ToList());
        unitOfWork.SaveChanges();

        repository.ListAny().Should().BeFalse();
    }

    [Fact]
    public void RemoveById_WithEmptyTypedIdList_DoesNotThrow()
    {
        SeedTypedEntities(1);

        using IServiceScope scope = _syncProvider.CreateScope();
        IRepository<TestTypedEntity, int> repository =
            scope.ServiceProvider.GetRequiredService<IRepository<TestTypedEntity, int>>();

        Action act = () => repository.RemoveById(new List<int>());

        act.Should().NotThrow();
        repository.ListCount().Should().Be(1);
    }

    #endregion

    #region [ Async ]

    [Fact]
    public void TypedRepositoryAsync_ShouldResolveFromContainer()
    {
        using IServiceScope scope = _asyncProvider.CreateScope();

        IRepositoryAsync<TestTypedEntity, int> repository =
            scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestTypedEntity, int>>();

        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IRepositoryAsync<TestTypedEntity>>();
    }

    [Fact]
    public async Task GetByIdAsync_WithTypedId_ReturnsSameEntityAsObjectOverload()
    {
        int id = (await SeedTypedEntitiesAsync(3)).First().Id;

        using IServiceScope scope = _asyncProvider.CreateScope();
        IRepositoryAsync<TestTypedEntity, int> repository =
            scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestTypedEntity, int>>();

        TestTypedEntity? typed = await repository.GetByIdAsync(id);
        TestTypedEntity? untyped = await ((IRepositoryAsync<TestTypedEntity>)repository).GetByIdAsync((object)id);

        typed.Should().NotBeNull();
        typed!.Id.Should().Be(id);
        typed.Name.Should().Be(untyped!.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithTypedIdAndPagingCriteria_ReturnsEntity()
    {
        int id = (await SeedTypedEntitiesAsync(2)).Last().Id;

        using IServiceScope scope = _asyncProvider.CreateScope();
        IRepositoryAsync<TestTypedEntity, int> repository =
            scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestTypedEntity, int>>();

        TestTypedEntity? found = await repository.GetByIdAsync(id, new PagingCriteria(limit: 1, offset: 0));

        found.Should().NotBeNull();
        found!.Id.Should().Be(id);
    }

    [Fact]
    public async Task RemoveByIdAsync_WithTypedId_BehavesLikeObjectOverload()
    {
        List<TestTypedEntity> seeded = await SeedTypedEntitiesAsync(2);

        using IServiceScope scope = _asyncProvider.CreateScope();
        IRepositoryAsync<TestTypedEntity, int> repository =
            scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestTypedEntity, int>>();
        IUnitOfWorkAsync unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();

        await repository.RemoveByIdAsync(seeded[0].Id);
        await unitOfWork.SaveChangesAsync();

        (await repository.ListCountAsync()).Should().Be(1);
        (await repository.GetByIdAsync(seeded[0].Id)).Should().BeNull();
        (await repository.GetByIdAsync(seeded[1].Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task RemoveByIdAsync_WithTypedIdList_RemovesAll()
    {
        List<TestTypedEntity> seeded = await SeedTypedEntitiesAsync(3);

        using IServiceScope scope = _asyncProvider.CreateScope();
        IRepositoryAsync<TestTypedEntity, int> repository =
            scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestTypedEntity, int>>();
        IUnitOfWorkAsync unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();

        await repository.RemoveByIdAsync(seeded.Select(e => e.Id).ToList());
        await unitOfWork.SaveChangesAsync();

        (await repository.ListAnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task RemoveByIdAsync_WithEmptyTypedIdList_DoesNotThrow()
    {
        await SeedTypedEntitiesAsync(1);

        using IServiceScope scope = _asyncProvider.CreateScope();
        IRepositoryAsync<TestTypedEntity, int> repository =
            scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestTypedEntity, int>>();

        Func<Task> act = () => repository.RemoveByIdAsync(new List<int>());

        await act.Should().NotThrowAsync();
        (await repository.ListCountAsync()).Should().Be(1);
    }

    #endregion

    #region [ Seeding ]

    private List<TestTypedEntity> SeedTypedEntities(int count)
    {
        using IServiceScope scope = _syncProvider.CreateScope();
        IRepository<TestTypedEntity> repository =
            scope.ServiceProvider.GetRequiredService<IRepository<TestTypedEntity>>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        List<TestTypedEntity> entities = CreateTypedEntities(count);
        repository.Add(entities);
        unitOfWork.SaveChanges();
        return entities;
    }

    private async Task<List<TestTypedEntity>> SeedTypedEntitiesAsync(int count)
    {
        using IServiceScope scope = _asyncProvider.CreateScope();
        IRepositoryAsync<TestTypedEntity> repository =
            scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestTypedEntity>>();
        IUnitOfWorkAsync unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();

        List<TestTypedEntity> entities = CreateTypedEntities(count);
        await repository.AddAsync(entities);
        await unitOfWork.SaveChangesAsync();
        return entities;
    }

    private static List<TestTypedEntity> CreateTypedEntities(int count)
    {
        return [.. Enumerable.Range(1, count).Select(i => new TestTypedEntity { Name = $"Typed-{i}" })];
    }

    #endregion
}
