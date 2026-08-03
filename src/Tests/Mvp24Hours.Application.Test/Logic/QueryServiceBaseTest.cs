using Mvp24Hours.Application.Test.Support;

namespace Mvp24Hours.Application.Test.Logic;

[Trait("Category", "Unit")]
public class QueryServiceBaseTest
{
    [Fact]
    public void Constructor_WithNullUnitOfWork_ShouldThrowBeforeUse()
    {
        Func<TestSyncQueryService> act = () => new TestSyncQueryService(null!);
        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void ListAny_ShouldReturnRepositoryResultAsBusiness()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListAny(repo, true);
        var service = new TestSyncQueryService(uow.Object);

        IBusinessResult<bool> result = service.ListAny();

        result.Data.Should().BeTrue();
        repo.Verify(r => r.ListAny(), Times.Once);
    }

    [Fact]
    public void ListCount_ShouldReturnRepositoryCount()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListCount(repo, 7);
        var service = new TestSyncQueryService(uow.Object);

        IBusinessResult<int> result = service.ListCount();

        result.Data.Should().Be(7);
    }

    [Fact]
    public void List_ShouldReturnEntities()
    {
        var items = new List<AppTestEntity> { new() { Id = 1, Name = "A" } };
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupList(repo, items);
        var service = new TestSyncQueryService(uow.Object);

        IBusinessResult<IList<AppTestEntity>> result = service.List();

        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public void GetById_ShouldReturnEntity()
    {
        var entity = new AppTestEntity { Id = 5, Name = "Item" };
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 5, entity);
        var service = new TestSyncQueryService(uow.Object);

        IBusinessResult<AppTestEntity?> result = service.GetById(5);

        result.Data!.Name.Should().Be("Item");
    }

    [Fact]
    public void GetBy_ShouldFilterByClause()
    {
        var items = new List<AppTestEntity> { new() { Id = 1, Active = true } };
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetByAnyExpression(repo, items);
        var service = new TestSyncQueryService(uow.Object);

        IBusinessResult<IList<AppTestEntity>> result = service.GetBy(e => e.Active);

        result.Data.Should().ContainSingle();
    }

    [Fact]
    public void AnyBySpecification_WithNullSpecification_ShouldReturnFalse()
    {
        (Mock<IUnitOfWork> uow, _) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestSyncQueryService(uow.Object);

        IBusinessResult<bool> result = service.AnyBySpecification<ActiveAppTestEntitySpec>(null!);

        result.Data.Should().BeFalse();
    }

    [Fact]
    public void GetFirstBySpecification_ShouldReturnFirstMatch()
    {
        var items = new List<AppTestEntity>
        {
            new() { Id = 1, Name = "First", Active = true },
            new() { Id = 2, Name = "Second", Active = true }
        };
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetByAnyExpression(repo, items);
        var service = new TestSyncQueryService(uow.Object);

        IBusinessResult<AppTestEntity?> result = service.GetFirstBySpecification(new ActiveAppTestEntitySpec());

        result.Data!.Name.Should().Be("First");
    }
}
