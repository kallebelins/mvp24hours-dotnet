using FluentValidation;
using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Application.Test.Logic.Async;

[Trait("Category", "Unit")]
public class ApplicationServiceBaseWithDtoAsyncTest
{
    private static TestApplicationServiceWithDtoAsync CreateService(
        Mock<IUnitOfWorkAsync> uow,
        IValidator<AppTestEntity>? entityValidator = null,
        IValidator<AppTestEntityDto>? dtoValidator = null)
    {
        return new(
                uow.Object,
                ApplicationTestHelpers.CreateAppEntityMapper(),
                entityValidator,
                dtoValidator);
    }

    [Fact]
    public async Task ListAsync_ShouldMapEntitiesToDtos()
    {
        var items = new List<AppTestEntity> { new() { Id = 1, Name = "Mapped", Active = true } };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupList(repo, items);
        TestApplicationServiceWithDtoAsync service = CreateService(uow);

        IBusinessResult<IList<AppTestEntityDto>> result = await service.ListAsync();

        result.Data.Should().ContainSingle()
            .Which.Name.Should().Be("Mapped");
    }

    [Fact]
    public async Task ListAsync_EmptyList_ShouldReturnEmptyDtos()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupList(repo, []);
        TestApplicationServiceWithDtoAsync service = CreateService(uow);

        IBusinessResult<IList<AppTestEntityDto>> result = await service.ListAsync();

        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByAsync_ShouldMapEntitiesToDtos()
    {
        var items = new List<AppTestEntity> { new() { Id = 2, Name = "Filtered", Active = false } };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetByAnyExpression(repo, items);
        TestApplicationServiceWithDtoAsync service = CreateService(uow);

        IBusinessResult<IList<AppTestEntityDto>> result = await service.GetByAsync(e => e.Active == false);

        result.Data.Should().ContainSingle()
            .Which.Active.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ShouldReturnDto()
    {
        var entity = new AppTestEntity { Id = 3, Name = "ById", Active = true };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 3, entity);
        TestApplicationServiceWithDtoAsync service = CreateService(uow);

        IBusinessResult<AppTestEntityDto> result = await service.GetByIdAsync(3);

        result.Data!.Name.Should().Be("ById");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ShouldReturnEmptyResult()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 99, null);
        TestApplicationServiceWithDtoAsync service = CreateService(uow);

        IBusinessResult<AppTestEntityDto> result = await service.GetByIdAsync(99);

        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ValidDto_ShouldPersistAndSaveChanges()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestApplicationServiceWithDtoAsync service = CreateService(uow, new AppTestEntityValidator(), new AppTestEntityDtoValidator());
        var dto = new AppTestEntityDto { Name = "Valid", Active = true };

        IBusinessResult<int> result = await service.AddAsync(dto);

        result.Data.Should().Be(1);
        repo.Verify(r => r.AddAsync(It.Is<AppTestEntity>(e => e.Name == "Valid"), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_InvalidDto_ShouldReturnValidationErrors()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestApplicationServiceWithDtoAsync service = CreateService(uow, new AppTestEntityValidator(), new AppTestEntityDtoValidator());

        IBusinessResult<int> result = await service.AddAsync(new AppTestEntityDto { Name = "" });

        result.HasErrors.Should().BeTrue();
        repo.Verify(r => r.AddAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_EmptyList_ShouldReturnZeroWithoutSave()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestApplicationServiceWithDtoAsync service = CreateService(uow);

        IBusinessResult<int> result = await service.AddAsync([]);

        result.Data.Should().Be(0);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ModifyAsync_ValidDto_ShouldUpdateAndSave()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestApplicationServiceWithDtoAsync service = CreateService(uow, new AppTestEntityValidator(), new AppTestEntityDtoValidator());
        var dto = new AppTestEntityDto { Name = "Updated", Active = true };

        IBusinessResult<int> result = await service.ModifyAsync(dto);

        result.Data.Should().Be(1);
        repo.Verify(r => r.ModifyAsync(It.Is<AppTestEntity>(e => e.Name == "Updated"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ModifyAsync_InvalidDto_ShouldReturnValidationErrors()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestApplicationServiceWithDtoAsync service = CreateService(uow, new AppTestEntityValidator(), new AppTestEntityDtoValidator());

        IBusinessResult<int> result = await service.ModifyAsync(new AppTestEntityDto { Name = "" });

        result.HasErrors.Should().BeTrue();
        repo.Verify(r => r.ModifyAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ModifyAsync_EmptyList_ShouldReturnZeroWithoutSave()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestApplicationServiceWithDtoAsync service = CreateService(uow);

        IBusinessResult<int> result = await service.ModifyAsync([]);

        result.Data.Should().Be(0);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveAndSave()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestApplicationServiceWithDtoAsync service = CreateService(uow);
        var dto = new AppTestEntityDto { Name = "Delete", Active = true };

        IBusinessResult<int> result = await service.RemoveAsync(dto);

        result.Data.Should().Be(1);
        repo.Verify(r => r.RemoveAsync(It.Is<AppTestEntity>(e => e.Name == "Delete"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_EmptyList_ShouldReturnZeroWithoutSave()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestApplicationServiceWithDtoAsync service = CreateService(uow);

        IBusinessResult<int> result = await service.RemoveAsync([]);

        result.Data.Should().Be(0);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveByIdAsync_ShouldRemoveByIdAndSave()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        TestApplicationServiceWithDtoAsync service = CreateService(uow);

        IBusinessResult<int> result = await service.RemoveByIdAsync(10);

        result.Data.Should().Be(1);
        repo.Verify(r => r.RemoveByIdAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }
}
