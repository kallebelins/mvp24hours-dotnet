using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;
using Mvp24Hours.Infrastructure.Data.EFCore.Testing;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Testing;

[Trait("Category", "Unit")]
public class RepositoryFakeTest
{
    [Fact]
    public void SeedData_WithEntities_ShouldPopulateRepository()
    {
        var repository = new RepositoryFake<TestEntity>();

        repository.SeedData(EfCoreTestHelpers.CreateEntities(2, "Seed"));

        repository.ListCount().Should().Be(2);
        repository.List().Should().HaveCount(2);
        repository.GetBy(e => e.Name.StartsWith("Seed")).Should().HaveCount(2);
    }

    [Fact]
    public void SeedData_WithAction_ShouldPopulateRepository()
    {
        var repository = new RepositoryFake<TestEntity>();

        repository.SeedData(list =>
        {
            list.Add(new TestEntity { Id = 42, Name = "Action-Seed" });
        });

        repository.GetById(42)!.Name.Should().Be("Action-Seed");
    }

    [Fact]
    public void Add_List_GetById_Modify_Remove_CommitChanges_ShouldPersistChanges()
    {
        var repository = new RepositoryFake<TestEntity>();
        var entity = new TestEntity { Id = 1, Name = "Original", Active = true, Score = 5 };

        repository.Add(entity);
        repository.CommitChanges().Should().Be(1);
        repository.GetById(1)!.Name.Should().Be("Original");

        entity.Name = "Updated";
        repository.Modify(entity);
        repository.CommitChanges().Should().Be(1);
        repository.GetById(1)!.Name.Should().Be("Updated");

        repository.Remove(entity);
        repository.CommitChanges().Should().Be(1);
        repository.GetById(1).Should().BeNull();
        repository.List().Should().BeEmpty();
    }

    [Fact]
    public void ListAndGetBy_ShouldFilterEntities()
    {
        var repository = new RepositoryFake<TestEntity>(EfCoreTestHelpers.CreateEntities(4));

        repository.List().Should().HaveCount(4);
        repository.GetBy(e => e.Active).Should().HaveCount(2);
        repository.GetByAny(e => e.Score > 20).Should().BeTrue();
    }

    [Fact]
    public void ResetPendingChanges_ShouldDiscardUncommittedChanges()
    {
        var repository = new RepositoryFake<TestEntity>();
        repository.SeedData([new TestEntity { Id = 1, Name = "Existing" }]);

        repository.Add(new TestEntity { Id = 2, Name = "Pending" });
        repository.PendingAdds.Should().HaveCount(1);

        repository.ResetPendingChanges();

        repository.PendingAdds.Should().BeEmpty();
        repository.ListCount().Should().Be(1);
    }

    [Fact]
    public void Dispose_ShouldClearEntities()
    {
        var repository = new RepositoryFake<TestEntity>([new TestEntity { Id = 1, Name = "One" }]);

        repository.Dispose();

        repository.ListCount().Should().Be(0);
    }
}
