using System.Linq.Expressions;
using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Application.Test.Logic;

[Trait("Category", "Unit")]
public class ApplicationServiceBaseTest
{
    [Fact]
    public void ListAny_ShouldReturnRepositoryResultAsBusiness()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListAny(repo, true);
        var service = new TestApplicationService(uow.Object);

        IBusinessResult<bool> result = service.ListAny();

        result.Data.Should().BeTrue();
        repo.Verify(r => r.ListAny(), Times.Once);
    }

    [Fact]
    public void ListCount_ShouldReturnRepositoryCount()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListCount(repo, 7);
        var service = new TestApplicationService(uow.Object);

        IBusinessResult<int> result = service.ListCount();

        result.Data.Should().Be(7);
    }

    [Fact]
    public void List_ShouldReturnEntities()
    {
        var items = new List<AppTestEntity> { new() { Id = 1, Name = "A" } };
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupList(repo, items);
        var service = new TestApplicationService(uow.Object);

        IBusinessResult<IList<AppTestEntity>> result = service.List();

        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public void GetById_ShouldReturnEntity()
    {
        var entity = new AppTestEntity { Id = 5, Name = "Item" };
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 5, entity);
        var service = new TestApplicationService(uow.Object);

        IBusinessResult<AppTestEntity?> result = service.GetById(5);

        result.Data!.Name.Should().Be("Item");
    }

    [Fact]
    public void GetBy_ShouldFilterByClause()
    {
        var items = new List<AppTestEntity> { new() { Id = 1, Active = true } };
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetByAnyExpression(repo, items);
        var service = new TestApplicationService(uow.Object);

        IBusinessResult<IList<AppTestEntity>> result = service.GetBy(e => e.Active);

        result.Data.Should().ContainSingle();
    }

    [Fact]
    public void GetByAny_ShouldReturnRepositoryResult()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        repo.Setup(r => r.GetByAny(It.IsAny<Expression<Func<AppTestEntity, bool>>>())).Returns(true);
        var service = new TestApplicationService(uow.Object);

        IBusinessResult<bool> result = service.GetByAny(e => e.Active);

        result.Data.Should().BeTrue();
    }

    [Fact]
    public void Add_ValidEntity_ShouldPersistAndSaveChanges()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationService(uow.Object, new AppTestEntityValidator());
        var entity = new AppTestEntity { Name = "Valid" };

        IBusinessResult<int> result = service.Add(entity);

        result.Data.Should().Be(1);
        repo.Verify(r => r.Add(entity), Times.Once);
        uow.Verify(u => u.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Add_InvalidEntity_ShouldReturnValidationErrors()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationService(uow.Object, new AppTestEntityValidator());

        IBusinessResult<int> result = service.Add(new AppTestEntity { Name = "" });

        result.HasErrors.Should().BeTrue();
        repo.Verify(r => r.Add(It.IsAny<AppTestEntity>()), Times.Never);
    }

    [Fact]
    public void Add_EmptyList_ShouldReturnZeroWithoutSave()
    {
        (Mock<IUnitOfWork> uow, _) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationService(uow.Object);

        IBusinessResult<int> result = service.Add([]);

        result.Data.Should().Be(0);
        uow.Verify(u => u.SaveChanges(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Modify_ValidEntity_ShouldUpdateAndSave()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationService(uow.Object, new AppTestEntityValidator());
        var entity = new AppTestEntity { Id = 1, Name = "Updated" };

        IBusinessResult<int> result = service.Modify(entity);

        result.Data.Should().Be(1);
        repo.Verify(r => r.Modify(entity), Times.Once);
    }

    [Fact]
    public void Modify_InvalidEntity_ShouldReturnValidationErrors()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationService(uow.Object, new AppTestEntityValidator());

        IBusinessResult<int> result = service.Modify(new AppTestEntity { Name = "" });

        result.HasErrors.Should().BeTrue();
        repo.Verify(r => r.Modify(It.IsAny<AppTestEntity>()), Times.Never);
    }

    [Fact]
    public void Remove_ShouldRemoveAndSave()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationService(uow.Object);
        var entity = new AppTestEntity { Id = 1, Name = "Delete" };

        IBusinessResult<int> result = service.Remove(entity);

        result.Data.Should().Be(1);
        repo.Verify(r => r.Remove(entity), Times.Once);
    }

    [Fact]
    public void RemoveById_ShouldRemoveByIdAndSave()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationService(uow.Object);

        IBusinessResult<int> result = service.RemoveById(42);

        result.Data.Should().Be(1);
        repo.Verify(r => r.RemoveById(42), Times.Once);
    }

    [Fact]
    public void AnyBySpecification_WithNullSpecification_ShouldReturnFalse()
    {
        (Mock<IUnitOfWork> uow, _) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationService(uow.Object);

        IBusinessResult<bool> result = service.AnyBySpecification<ActiveAppTestEntitySpec>(null!);

        result.Data.Should().BeFalse();
    }

    [Fact]
    public void AnyBySpecification_WithFallback_ShouldUseGetByAny()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        repo.Setup(r => r.GetByAny(It.IsAny<Expression<Func<AppTestEntity, bool>>>())).Returns(true);
        var service = new TestApplicationService(uow.Object);

        IBusinessResult<bool> result = service.AnyBySpecification(new ActiveAppTestEntitySpec());

        result.Data.Should().BeTrue();
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
        var service = new TestApplicationService(uow.Object);

        IBusinessResult<AppTestEntity?> result = service.GetFirstBySpecification(new ActiveAppTestEntitySpec());

        result.Data!.Name.Should().Be("First");
    }

    [Fact]
    public void GetFirstBySpecification_WithReadOnlyRepository_ShouldUseSpecificationMethod()
    {
        var entity = new AppTestEntity { Id = 3, Name = "Spec", Active = true };
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupReadOnlySpecification<AppTestEntity, ActiveAppTestEntitySpec>(repo, firstResult: entity);
        var service = new TestApplicationService(uow.Object);

        IBusinessResult<AppTestEntity?> result = service.GetFirstBySpecification(new ActiveAppTestEntitySpec());

        result.Data!.Name.Should().Be("Spec");
    }

    [Fact]
    public void GetByCount_ShouldReturnRepositoryCount()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        repo.Setup(r => r.GetByCount(It.IsAny<Expression<Func<AppTestEntity, bool>>>())).Returns(4);
        var service = new TestApplicationService(uow.Object);

        IBusinessResult<int> result = service.GetByCount(e => e.Active);

        result.Data.Should().Be(4);
    }

    [Fact]
    public void List_WithPagingCriteria_ShouldPassCriteriaToRepository()
    {
        var items = new List<AppTestEntity> { new() { Id = 1, Name = "Paged" } };
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupList(repo, items);
        var service = new TestApplicationService(uow.Object);
        var criteria = new PagingCriteria(limit: 10, offset: 0);

        IBusinessResult<IList<AppTestEntity>> result = service.List(criteria);

        result.Data.Should().HaveCount(1);
        repo.Verify(r => r.List(criteria), Times.Once);
    }

    [Fact]
    public void Modify_BatchValidEntities_ShouldUpdateAll()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationService(uow.Object, new AppTestEntityValidator());

        IBusinessResult<int> result = service.Modify([
            new AppTestEntity { Id = 1, Name = "A" },
            new AppTestEntity { Id = 2, Name = "B" }
        ]);

        result.Data.Should().Be(1);
        repo.Verify(r => r.Modify(It.IsAny<AppTestEntity>()), Times.Exactly(2));
    }

    [Fact]
    public void Modify_EmptyBatch_ShouldReturnZero()
    {
        (Mock<IUnitOfWork> uow, _) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationService(uow.Object);

        IBusinessResult<int> result = service.Modify([]);

        result.Data.Should().Be(0);
    }

    [Fact]
    public void Remove_Batch_ShouldRemoveAllEntities()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationService(uow.Object);
        var entities = new List<AppTestEntity>
        {
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" }
        };

        IBusinessResult<int> result = service.Remove(entities);

        result.Data.Should().Be(1);
        repo.Verify(r => r.Remove(It.IsAny<AppTestEntity>()), Times.Exactly(2));
    }

    [Fact]
    public void Remove_EmptyBatch_ShouldReturnZero()
    {
        (Mock<IUnitOfWork> uow, _) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationService(uow.Object);

        IBusinessResult<int> result = service.Remove([]);

        result.Data.Should().Be(0);
    }

    [Fact]
    public void RemoveById_Batch_ShouldRemoveAllIds()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationService(uow.Object);

        IBusinessResult<int> result = service.RemoveById([1, 2]);

        result.Data.Should().Be(1);
        repo.Verify(r => r.RemoveById(It.IsAny<object>()), Times.Exactly(2));
    }

    [Fact]
    public void CountBySpecification_WithNullSpec_ShouldReturnZero()
    {
        (Mock<IUnitOfWork> uow, _) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationService(uow.Object);

        IBusinessResult<int> result = service.CountBySpecification<ActiveAppTestEntitySpec>(null!);

        result.Data.Should().Be(0);
    }

    [Fact]
    public void GetBySpecification_WithNullSpec_ShouldReturnEmptyList()
    {
        (Mock<IUnitOfWork> uow, _) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationService(uow.Object);

        IBusinessResult<IList<AppTestEntity>> result = service.GetBySpecification<ActiveAppTestEntitySpec>(null!);

        result.Data.Should().BeEmpty();
    }

    [Fact]
    public void GetSingleBySpecification_WithNullSpec_ShouldReturnNull()
    {
        (Mock<IUnitOfWork> uow, _) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationService(uow.Object);

        IBusinessResult<AppTestEntity?> result = service.GetSingleBySpecification<ActiveAppTestEntitySpec>(null!);

        result.Data.Should().BeNull();
    }

    [Fact]
    public void GetSingleBySpecification_WithFallback_ShouldReturnSingleMatch()
    {
        var items = new List<AppTestEntity> { new() { Id = 1, Name = "Single", Active = true } };
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetByAnyExpression(repo, items);
        var service = new TestApplicationService(uow.Object);

        IBusinessResult<AppTestEntity?> result = service.GetSingleBySpecification(new ActiveAppTestEntitySpec());

        result.Data!.Name.Should().Be("Single");
    }
}
