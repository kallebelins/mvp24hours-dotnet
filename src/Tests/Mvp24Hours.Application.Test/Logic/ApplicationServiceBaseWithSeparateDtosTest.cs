using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Application.Test.Logic;

[Trait("Category", "Unit")]
public class ApplicationServiceBaseWithSeparateDtosTest
{
    [Fact]
    public void List_ShouldMapEntitiesToReadDtos()
    {
        var items = new List<AppTestEntity>
        {
            new() { Id = 1, Name = "A", Active = true },
            new() { Id = 2, Name = "B", Active = false }
        };
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupList(repo, items);
        var service = new TestApplicationServiceWithSeparateDtos(uow.Object, ApplicationTestHelpers.CreateAppEntityMapper());

        IBusinessResult<IList<AppTestEntityDto>> result = service.List();

        result.Data.Should().HaveCount(2);
        result.Data!.Should().Contain(d => d.Name == "A");
    }

    [Fact]
    public void GetById_WithNullEntity_ShouldReturnEmptyResult()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 99, null);
        var service = new TestApplicationServiceWithSeparateDtos(uow.Object, ApplicationTestHelpers.CreateAppEntityMapper());

        IBusinessResult<AppTestEntityDto> result = service.GetById(99);

        result.Data.Should().BeNull();
        result.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void GetById_WithEntity_ShouldReturnReadDto()
    {
        var entity = new AppTestEntity { Id = 1, Name = "Found", Active = true };
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 1, entity);
        var service = new TestApplicationServiceWithSeparateDtos(uow.Object, ApplicationTestHelpers.CreateAppEntityMapper());

        IBusinessResult<AppTestEntityDto> result = service.GetById(1);

        result.Data!.Name.Should().Be("Found");
    }

    [Fact]
    public void Add_CreateDto_ShouldReturnReadDto()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationServiceWithSeparateDtos(
            uow.Object,
            ApplicationTestHelpers.CreateAppEntityMapper(),
            new AppTestEntityValidator(),
            new AppTestCreateDtoValidator());
        var createDto = new AppTestCreateDto { Name = "Created" };

        IBusinessResult<AppTestEntityDto> result = service.Add(createDto);

        result.HasErrors.Should().BeFalse();
        result.Data!.Name.Should().Be("Created");
        result.Data.Active.Should().BeTrue();
        repo.Verify(r => r.Add(It.Is<AppTestEntity>(e => e.Name == "Created" && e.Active)), Times.Once);
    }

    [Fact]
    public void Add_List_ShouldPersistAllCreateDtos()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationServiceWithSeparateDtos(
            uow.Object,
            ApplicationTestHelpers.CreateAppEntityMapper(),
            new AppTestEntityValidator(),
            new AppTestCreateDtoValidator());
        IList<AppTestCreateDto> dtos =
        [
            new() { Name = "One" },
            new() { Name = "Two" }
        ];

        IBusinessResult<int> result = service.Add(dtos);

        result.Data.Should().Be(1);
        repo.Verify(r => r.Add(It.IsAny<AppTestEntity>()), Times.Exactly(2));
    }

    [Fact]
    public void Add_EmptyList_ShouldReturnZeroWithoutSave()
    {
        (Mock<IUnitOfWork> uow, _) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationServiceWithSeparateDtos(uow.Object, ApplicationTestHelpers.CreateAppEntityMapper());

        IBusinessResult<int> result = service.Add([]);

        result.Data.Should().Be(0);
        uow.Verify(u => u.SaveChanges(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Modify_NotFound_ShouldReturnFailure()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 1, null);
        var service = new TestApplicationServiceWithSeparateDtos(
            uow.Object,
            ApplicationTestHelpers.CreateAppEntityMapper(),
            updateValidator: new AppTestUpdateDtoValidator());

        IBusinessResult<AppTestEntityDto> result = service.Modify(1, new AppTestUpdateDto { Id = 1, Name = "Updated" });

        result.HasErrors.Should().BeTrue();
        repo.Verify(r => r.Modify(It.IsAny<AppTestEntity>()), Times.Never);
    }

    [Fact]
    public void Modify_Success_ShouldUpdateAndReturnReadDto()
    {
        var existing = new AppTestEntity { Id = 1, Name = "Original", Active = true };
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 1, existing);
        var service = new TestApplicationServiceWithSeparateDtos(
            uow.Object,
            ApplicationTestHelpers.CreateAppEntityMapper(),
            new AppTestEntityValidator(),
            updateValidator: new AppTestUpdateDtoValidator());

        IBusinessResult<AppTestEntityDto> result = service.Modify(1, new AppTestUpdateDto { Id = 1, Name = "Updated" });

        result.HasErrors.Should().BeFalse();
        result.Data!.Name.Should().Be("Updated");
        repo.Verify(r => r.Modify(existing), Times.Once);
    }

    [Fact]
    public void Patch_Success_ShouldApplyPartialUpdate()
    {
        var existing = new AppTestEntity { Id = 1, Name = "Original", Active = true };
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 1, existing);
        var service = new TestApplicationServiceWithSeparateDtos(
            uow.Object,
            ApplicationTestHelpers.CreateAppEntityMapper(),
            new AppTestEntityValidator());
        var patchDto = new AppTestUpdateDto { Id = 1, Name = "Patched" };

        IBusinessResult<AppTestEntityDto> result = service.Patch(1, patchDto);

        result.HasErrors.Should().BeFalse();
        result.Data!.Name.Should().Be("Patched");
        existing.Active.Should().BeTrue();
        repo.Verify(r => r.Modify(existing), Times.Once);
    }

    [Fact]
    public void RemoveById_ShouldRemoveByIdAndSave()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationServiceWithSeparateDtos(uow.Object, ApplicationTestHelpers.CreateAppEntityMapper());

        IBusinessResult<int> result = service.RemoveById(7);

        result.Data.Should().Be(1);
        repo.Verify(r => r.RemoveById(7), Times.Once);
    }

    [Fact]
    public void Add_InvalidCreateDto_ShouldReturnValidationErrors()
    {
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationServiceWithSeparateDtos(
            uow.Object,
            ApplicationTestHelpers.CreateAppEntityMapper(),
            createValidator: new AppTestCreateDtoValidator());

        IBusinessResult<AppTestEntityDto> result = service.Add(new AppTestCreateDto { Name = "" });

        result.HasErrors.Should().BeTrue();
        repo.Verify(r => r.Add(It.IsAny<AppTestEntity>()), Times.Never);
    }

    [Fact]
    public void Modify_InvalidUpdateDto_ShouldReturnValidationErrors()
    {
        var existing = new AppTestEntity { Id = 1, Name = "Original", Active = true };
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 1, existing);
        var service = new TestApplicationServiceWithSeparateDtos(
            uow.Object,
            ApplicationTestHelpers.CreateAppEntityMapper(),
            updateValidator: new AppTestUpdateDtoValidator());

        IBusinessResult<AppTestEntityDto> result = service.Modify(1, new AppTestUpdateDto { Id = 1, Name = "" });

        result.HasErrors.Should().BeTrue();
        repo.Verify(r => r.Modify(It.IsAny<AppTestEntity>()), Times.Never);
    }
}
