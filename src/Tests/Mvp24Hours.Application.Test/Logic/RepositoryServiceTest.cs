using Mvp24Hours.Application.Test.Support;

namespace Mvp24Hours.Application.Test.Logic;

[Trait("Category", "Unit")]
public class RepositoryServiceTest
{
    [Fact]
    public void Constructor_WithNullUnitOfWork_ShouldThrowBeforeUse()
    {
        Func<TestSyncRepositoryService> act = () => new TestSyncRepositoryService(null!);
        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void ListAny_ShouldDelegateToRepository()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListAny(repo, true);
        var service = new TestSyncRepositoryService(uow.Object);

        IBusinessResult<bool> result = service.ListAny();

        result.Data.Should().BeTrue();
    }

    [Fact]
    public void Add_ValidEntity_ShouldSaveChanges()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestSyncRepositoryService(uow.Object, new AppTestEntityValidator());
        var entity = new AppTestEntity { Name = "Repo" };

        IBusinessResult<int> result = service.Add(entity);

        result.Data.Should().Be(1);
        repo.Verify(r => r.Add(entity), Times.Once);
    }

    [Fact]
    public void Modify_InvalidEntity_ShouldReturnErrors()
    {
        (Mock<IUnitOfWork> uow, _) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestSyncRepositoryService(uow.Object, new AppTestEntityValidator());

        IBusinessResult<int> result = service.Modify(new AppTestEntity { Name = "" });

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public void Remove_List_ShouldRemoveAllEntities()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestSyncRepositoryService(uow.Object);
        var entities = new List<AppTestEntity>
        {
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" }
        };

        IBusinessResult<int> result = service.Remove(entities);

        result.Data.Should().Be(1);
        repo.Verify(r => r.Remove(It.IsAny<AppTestEntity>()), Times.Exactly(2));
    }
}
