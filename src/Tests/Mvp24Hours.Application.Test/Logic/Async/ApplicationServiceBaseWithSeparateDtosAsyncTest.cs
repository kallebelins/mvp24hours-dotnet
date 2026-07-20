using FluentValidation;
using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Application.Test.Logic.Async;

[Trait("Category", "Unit")]
public class ApplicationServiceBaseWithSeparateDtosAsyncTest
{
    private static TestApplicationServiceWithSeparateDtosAsync CreateService(
        Mock<IUnitOfWorkAsync> uow,
        IValidator<AppTestEntity>? entityValidator = null,
        IValidator<AppTestCreateDto>? createValidator = null,
        IValidator<AppTestUpdateDto>? updateValidator = null)
        => new(
            uow.Object,
            ApplicationTestHelpers.CreateAppEntityMapper(),
            entityValidator,
            createValidator,
            updateValidator);

    [Fact]
    public async Task AddAsync_ValidCreateDto_ShouldReturnMappedDto()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = CreateService(uow, new AppTestEntityValidator(), new AppTestCreateDtoValidator());
        var createDto = new AppTestCreateDto { Name = "Created" };

        IBusinessResult<AppTestEntityDto> result = await service.AddAsync(createDto);

        result.Data.Name.Should().Be("Created");
        result.Data.Active.Should().BeTrue();
        repo.Verify(r => r.AddAsync(It.Is<AppTestEntity>(e => e.Name == "Created" && e.Active), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_InvalidCreateDto_ShouldReturnValidationErrors()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = CreateService(uow, new AppTestEntityValidator(), new AppTestCreateDtoValidator());

        IBusinessResult<AppTestEntityDto> result = await service.AddAsync(new AppTestCreateDto { Name = "" });

        result.HasErrors.Should().BeTrue();
        repo.Verify(r => r.AddAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_EmptyList_ShouldReturnZeroWithoutSave()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = CreateService(uow);

        IBusinessResult<int> result = await service.AddAsync([]);

        result.Data.Should().Be(0);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ModifyAsync_WhenEntityNotFound_ShouldReturnFailure()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 404, null);
        var service = CreateService(uow, new AppTestEntityValidator(), null, new AppTestUpdateDtoValidator());

        IBusinessResult<AppTestEntityDto> result = await service.ModifyAsync(404, new AppTestUpdateDto { Id = 404, Name = "Missing" });

        result.HasErrors.Should().BeTrue();
        result.Messages.Should().Contain(m => m.Key == "NotFound" && m.Type == MessageType.Error);
        repo.Verify(r => r.ModifyAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ModifyAsync_ValidUpdate_ShouldReturnUpdatedDto()
    {
        var existing = new AppTestEntity { Id = 1, Name = "Original", Active = true };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 1, existing);
        var service = CreateService(uow, new AppTestEntityValidator(), null, new AppTestUpdateDtoValidator());

        IBusinessResult<AppTestEntityDto> result = await service.ModifyAsync(1, new AppTestUpdateDto { Id = 1, Name = "Updated" });

        result.Data.Name.Should().Be("Updated");
        repo.Verify(r => r.ModifyAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PatchAsync_ShouldApplyNonNullValuesAndReturnDto()
    {
        var existing = new AppTestEntity { Id = 2, Name = "Original", Active = true };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 2, existing);
        var service = CreateService(uow, new AppTestEntityValidator());

        IBusinessResult<AppTestEntityDto> result = await service.PatchAsync(2, new AppTestUpdateDto { Id = 2, Name = "Patched" });

        result.Data.Name.Should().Be("Patched");
        existing.Name.Should().Be("Patched");
        repo.Verify(r => r.ModifyAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PatchAsync_WhenEntityNotFound_ShouldReturnFailure()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 404, null);
        var service = CreateService(uow, new AppTestEntityValidator());

        IBusinessResult<AppTestEntityDto> result = await service.PatchAsync(404, new AppTestUpdateDto { Id = 404, Name = "Missing" });

        result.HasErrors.Should().BeTrue();
        repo.Verify(r => r.ModifyAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveByIdAsync_ShouldRemoveByIdAndSave()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = CreateService(uow);

        IBusinessResult<int> result = await service.RemoveByIdAsync(7);

        result.Data.Should().Be(1);
        repo.Verify(r => r.RemoveByIdAsync(7, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveByIdAsync_EmptyIds_ShouldReturnZero()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = CreateService(uow);

        IBusinessResult<int> result = await service.RemoveByIdAsync([]);

        result.Data.Should().Be(0);
    }

    [Fact]
    public async Task ListAsync_ShouldMapEntitiesToDtos()
    {
        var items = new List<AppTestEntity> { new() { Id = 1, Name = "Read", Active = true } };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupList(repo, items);
        var service = CreateService(uow);

        IBusinessResult<IList<AppTestEntityDto>> result = await service.ListAsync();

        result.Data.Should().ContainSingle()
            .Which.Name.Should().Be("Read");
    }

    [Fact]
    public async Task AddAsync_ShouldPassCancellationTokenToRepository()
    {
        using var cts = new CancellationTokenSource();
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = CreateService(uow, new AppTestEntityValidator(), new AppTestCreateDtoValidator());

        await service.AddAsync(new AppTestCreateDto { Name = "Token" }, cts.Token);

        repo.Verify(r => r.AddAsync(It.IsAny<AppTestEntity>(), It.Is<CancellationToken>(ct => ct == cts.Token)), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.Is<CancellationToken>(ct => ct == cts.Token)), Times.Once);
    }

    [Fact]
    public async Task ModifyAsync_ShouldPassCancellationTokenToRepository()
    {
        using var cts = new CancellationTokenSource();
        var existing = new AppTestEntity { Id = 5, Name = "Original", Active = true };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 5, existing);
        var service = CreateService(uow, new AppTestEntityValidator(), null, new AppTestUpdateDtoValidator());

        await service.ModifyAsync(5, new AppTestUpdateDto { Id = 5, Name = "Updated" }, cts.Token);

        repo.Verify(r => r.GetByIdAsync(5, It.Is<CancellationToken>(ct => ct == cts.Token)), Times.Once);
        repo.Verify(r => r.ModifyAsync(existing, It.Is<CancellationToken>(ct => ct == cts.Token)), Times.Once);
    }
}
