using Mvp24Hours.Application.Test.Support;

namespace Mvp24Hours.Application.Test.Logic;

[Trait("Category", "Unit")]
public class CommandServiceBaseTest
{
    [Fact]
    public void Add_ValidEntity_ShouldPersistAndSaveChanges()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestSyncCommandService(uow.Object, new AppTestEntityValidator());
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
        var service = new TestSyncCommandService(uow.Object, new AppTestEntityValidator());

        IBusinessResult<int> result = service.Add(new AppTestEntity { Name = "" });

        result.HasErrors.Should().BeTrue();
        repo.Verify(r => r.Add(It.IsAny<AppTestEntity>()), Times.Never);
    }

    [Fact]
    public void Add_EmptyList_ShouldReturnZeroWithoutSave()
    {
        (Mock<IUnitOfWork> uow, _) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestSyncCommandService(uow.Object);

        IBusinessResult<int> result = service.Add([]);

        result.Data.Should().Be(0);
        uow.Verify(u => u.SaveChanges(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Modify_ValidEntity_ShouldUpdateAndSave()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestSyncCommandService(uow.Object, new AppTestEntityValidator());
        var entity = new AppTestEntity { Id = 1, Name = "Updated" };

        IBusinessResult<int> result = service.Modify(entity);

        result.Data.Should().Be(1);
        repo.Verify(r => r.Modify(entity), Times.Once);
    }

    [Fact]
    public void Remove_ShouldRemoveAndSave()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestSyncCommandService(uow.Object);
        var entity = new AppTestEntity { Id = 1, Name = "Delete" };

        IBusinessResult<int> result = service.Remove(entity);

        result.Data.Should().Be(1);
        repo.Verify(r => r.Remove(entity), Times.Once);
    }

    [Fact]
    public void RemoveById_ShouldRemoveByIdAndSave()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestSyncCommandService(uow.Object);

        IBusinessResult<int> result = service.RemoveById(42);

        result.Data.Should().Be(1);
        repo.Verify(r => r.RemoveById(42), Times.Once);
    }

    [Fact]
    public void RemoveById_EmptyIds_ShouldReturnZero()
    {
        (Mock<IUnitOfWork> uow, _) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestSyncCommandService(uow.Object);

        IBusinessResult<int> result = service.RemoveById([]);

        result.Data.Should().Be(0);
    }
}
