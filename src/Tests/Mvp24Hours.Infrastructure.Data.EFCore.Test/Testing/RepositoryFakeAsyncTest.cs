using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;
using Mvp24Hours.Infrastructure.Data.EFCore.Testing;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Testing;

[Trait("Category", "Unit")]
public class RepositoryFakeAsyncTest
{
    [Fact]
    public void SeedData_WithEntities_ShouldPopulateRepository()
    {
        var repository = new RepositoryFakeAsync<TestEntity>();

        repository.SeedData(EfCoreTestHelpers.CreateEntities(2, "AsyncSeed"));

        repository.AllEntities.Should().HaveCount(2);
    }

    [Fact]
    public async Task Add_List_GetById_Modify_Remove_CommitChangesAsync_ShouldPersistChanges()
    {
        var repository = new RepositoryFakeAsync<TestEntity>();
        var entity = new TestEntity { Id = 10, Name = "Async", Active = true, Score = 100 };

        await repository.AddAsync(entity);
        (await repository.CommitChangesAsync()).Should().Be(1);

        (await repository.GetByIdAsync(10))!.Name.Should().Be("Async");
        (await repository.ListAsync()).Should().ContainSingle();

        entity.Name = "Async-Updated";
        await repository.ModifyAsync(entity);
        (await repository.CommitChangesAsync()).Should().Be(1);
        (await repository.GetByIdAsync(10))!.Name.Should().Be("Async-Updated");

        await repository.RemoveAsync(entity);
        (await repository.CommitChangesAsync()).Should().Be(1);
        (await repository.GetByIdAsync(10)).Should().BeNull();
    }

    [Fact]
    public async Task GetByAsync_ShouldFilterEntities()
    {
        var repository = new RepositoryFakeAsync<TestEntity>(EfCoreTestHelpers.CreateEntities(3));

        IList<TestEntity> active = await repository.GetByAsync(e => e.Active);
        active.Should().HaveCount(1);

        (await repository.ListCountAsync()).Should().Be(3);
        (await repository.ListAnyAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task RemoveByIdAsync_ShouldQueueRemoval()
    {
        var repository = new RepositoryFakeAsync<TestEntity>([new TestEntity { Id = 7, Name = "RemoveMe" }]);

        await repository.RemoveByIdAsync(7);
        (await repository.CommitChangesAsync()).Should().Be(1);

        (await repository.GetByIdAsync(7)).Should().BeNull();
    }
}
