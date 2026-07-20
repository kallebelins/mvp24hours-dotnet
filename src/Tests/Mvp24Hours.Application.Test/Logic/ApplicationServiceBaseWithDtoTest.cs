using Mvp24Hours.Application.Test.Support;

namespace Mvp24Hours.Application.Test.Logic;

[Trait("Category", "Unit")]
public class ApplicationServiceBaseWithDtoTest
{
    [Fact]
    public void List_ShouldMapEntitiesToDtos()
    {
        var items = new List<AppTestEntity>
        {
            new() { Id = 1, Name = "A", Active = true },
            new() { Id = 2, Name = "B", Active = false }
        };
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupList(repo, items);
        var service = new TestApplicationServiceWithDto(uow.Object, ApplicationTestHelpers.CreateAppEntityMapper());

        IBusinessResult<IList<AppTestEntityDto>> result = service.List();

        result.Data.Should().HaveCount(2);
        result.Data!.Should().Contain(d => d.Name == "A" && d.Active);
    }

    [Fact]
    public void GetById_ShouldMapEntityToDto()
    {
        var entity = new AppTestEntity { Id = 5, Name = "Mapped", Active = true };
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 5, entity);
        var service = new TestApplicationServiceWithDto(uow.Object, ApplicationTestHelpers.CreateAppEntityMapper());

        IBusinessResult<AppTestEntityDto> result = service.GetById(5);

        result.Data.Name.Should().Be("Mapped");
        result.Data.Active.Should().BeTrue();
    }

    [Fact]
    public void GetBy_ShouldMapFilteredEntitiesToDtos()
    {
        var items = new List<AppTestEntity> { new() { Id = 1, Name = "Active", Active = true } };
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetByAnyExpression(repo, items);
        var service = new TestApplicationServiceWithDto(uow.Object, ApplicationTestHelpers.CreateAppEntityMapper());

        IBusinessResult<IList<AppTestEntityDto>> result = service.GetBy(e => e.Active);

        result.Data.Should().ContainSingle().Which.Name.Should().Be("Active");
    }

    [Fact]
    public void Add_ValidDto_ShouldPersistAndSaveChanges()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationServiceWithDto(
            uow.Object,
            ApplicationTestHelpers.CreateAppEntityMapper(),
            new AppTestEntityValidator(),
            new AppTestEntityDtoValidator());
        var dto = new AppTestEntityDto { Name = "Valid", Active = true };

        IBusinessResult<int> result = service.Add(dto);

        result.Data.Should().Be(1);
        repo.Verify(r => r.Add(It.Is<AppTestEntity>(e => e.Name == "Valid")), Times.Once);
        uow.Verify(u => u.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Add_InvalidDto_ShouldReturnValidationErrors()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationServiceWithDto(
            uow.Object,
            ApplicationTestHelpers.CreateAppEntityMapper(),
            dtoValidator: new AppTestEntityDtoValidator());

        IBusinessResult<int> result = service.Add(new AppTestEntityDto { Name = "" });

        result.HasErrors.Should().BeTrue();
        repo.Verify(r => r.Add(It.IsAny<AppTestEntity>()), Times.Never);
    }

    [Fact]
    public void Add_EmptyList_ShouldReturnZeroWithoutSave()
    {
        (Mock<IUnitOfWork> uow, _) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationServiceWithDto(uow.Object, ApplicationTestHelpers.CreateAppEntityMapper());

        IBusinessResult<int> result = service.Add([]);

        result.Data.Should().Be(0);
        uow.Verify(u => u.SaveChanges(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Add_List_ShouldPersistAllDtos()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationServiceWithDto(
            uow.Object,
            ApplicationTestHelpers.CreateAppEntityMapper(),
            new AppTestEntityValidator(),
            new AppTestEntityDtoValidator());
        IList<AppTestEntityDto> dtos =
        [
            new() { Name = "One", Active = true },
            new() { Name = "Two", Active = true }
        ];

        IBusinessResult<int> result = service.Add(dtos);

        result.Data.Should().Be(1);
        repo.Verify(r => r.Add(It.IsAny<AppTestEntity>()), Times.Exactly(2));
    }

    [Fact]
    public void Modify_ValidDto_ShouldUpdateAndSave()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationServiceWithDto(
            uow.Object,
            ApplicationTestHelpers.CreateAppEntityMapper(),
            new AppTestEntityValidator(),
            new AppTestEntityDtoValidator());
        var dto = new AppTestEntityDto { Name = "Updated", Active = true };

        IBusinessResult<int> result = service.Modify(dto);

        result.Data.Should().Be(1);
        repo.Verify(r => r.Modify(It.Is<AppTestEntity>(e => e.Name == "Updated")), Times.Once);
    }

    [Fact]
    public void Modify_InvalidDto_ShouldReturnValidationErrors()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationServiceWithDto(
            uow.Object,
            ApplicationTestHelpers.CreateAppEntityMapper(),
            dtoValidator: new AppTestEntityDtoValidator());

        IBusinessResult<int> result = service.Modify(new AppTestEntityDto { Name = "" });

        result.HasErrors.Should().BeTrue();
        repo.Verify(r => r.Modify(It.IsAny<AppTestEntity>()), Times.Never);
    }

    [Fact]
    public void Remove_ShouldRemoveMappedEntityAndSave()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationServiceWithDto(uow.Object, ApplicationTestHelpers.CreateAppEntityMapper());
        var dto = new AppTestEntityDto { Name = "Delete", Active = true };

        IBusinessResult<int> result = service.Remove(dto);

        result.Data.Should().Be(1);
        repo.Verify(r => r.Remove(It.Is<AppTestEntity>(e => e.Name == "Delete")), Times.Once);
    }

    [Fact]
    public void RemoveById_ShouldRemoveByIdAndSave()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationServiceWithDto(uow.Object, ApplicationTestHelpers.CreateAppEntityMapper());

        IBusinessResult<int> result = service.RemoveById(99);

        result.Data.Should().Be(1);
        repo.Verify(r => r.RemoveById(99), Times.Once);
    }

    [Fact]
    public void ListAny_ShouldReturnRepositoryResult()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListAny(repo, true);
        var service = new TestApplicationServiceWithDto(uow.Object, ApplicationTestHelpers.CreateAppEntityMapper());

        IBusinessResult<bool> result = service.ListAny();

        result.Data.Should().BeTrue();
    }
}
