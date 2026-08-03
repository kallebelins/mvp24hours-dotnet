using Mvp24Hours.Application.Test.Support;

namespace Mvp24Hours.Application.Test.Logic.Async;

[Trait("Category", "Unit")]
public class CommandServiceBaseAsyncTest
{
    [Fact]
    public async Task AddAsync_ValidEntity_ShouldPersistAndSaveChanges()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = new TestCommandService(uow.Object, new AppTestEntityValidator());
        var entity = new AppTestEntity { Name = "Valid" };

        IBusinessResult<int> result = await service.AddAsync(entity);

        result.Data.Should().Be(1);
        repo.Verify(r => r.AddAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_InvalidEntity_ShouldReturnValidationErrors()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = new TestCommandService(uow.Object, new AppTestEntityValidator());

        IBusinessResult<int> result = await service.AddAsync(new AppTestEntity { Name = "" });

        result.HasErrors.Should().BeTrue();
        repo.Verify(r => r.AddAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_EmptyList_ShouldReturnZeroWithoutSave()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = new TestCommandService(uow.Object);

        IBusinessResult<int> result = await service.AddAsync([]);

        result.Data.Should().Be(0);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ModifyAsync_ValidEntity_ShouldUpdateAndSave()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = new TestCommandService(uow.Object, new AppTestEntityValidator());
        var entity = new AppTestEntity { Id = 1, Name = "Updated" };

        IBusinessResult<int> result = await service.ModifyAsync(entity);

        result.Data.Should().Be(1);
        repo.Verify(r => r.ModifyAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveAndSave()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = new TestCommandService(uow.Object);
        var entity = new AppTestEntity { Id = 1, Name = "Delete" };

        IBusinessResult<int> result = await service.RemoveAsync(entity);

        result.Data.Should().Be(1);
        repo.Verify(r => r.RemoveAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveByIdAsync_ShouldRemoveByIdAndSave()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = new TestCommandService(uow.Object);

        IBusinessResult<int> result = await service.RemoveByIdAsync(42);

        result.Data.Should().Be(1);
        repo.Verify(r => r.RemoveByIdAsync(42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveByIdAsync_EmptyIds_ShouldReturnZero()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = new TestCommandService(uow.Object);

        IBusinessResult<int> result = await service.RemoveByIdAsync([]);

        result.Data.Should().Be(0);
    }
}
