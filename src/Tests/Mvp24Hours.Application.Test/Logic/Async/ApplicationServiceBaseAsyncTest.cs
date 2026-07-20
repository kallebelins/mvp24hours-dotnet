using System.Linq.Expressions;
using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Application.Test.Logic.Async;

[Trait("Category", "Unit")]
public class ApplicationServiceBaseAsyncTest
{
    [Fact]
    public async Task ListAnyAsync_ShouldReturnRepositoryResultAsBusiness()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListAny(repo, true);
        var service = new TestApplicationServiceAsync(uow.Object);

        IBusinessResult<bool> result = await service.ListAnyAsync();

        result.Data.Should().BeTrue();
        repo.Verify(r => r.ListAnyAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListCountAsync_ShouldReturnRepositoryCount()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListCount(repo, 7);
        var service = new TestApplicationServiceAsync(uow.Object);

        IBusinessResult<int> result = await service.ListCountAsync();

        result.Data.Should().Be(7);
    }

    [Fact]
    public async Task ListAsync_ShouldReturnEntities()
    {
        var items = new List<AppTestEntity> { new() { Id = 1, Name = "A" } };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupList(repo, items);
        var service = new TestApplicationServiceAsync(uow.Object);

        IBusinessResult<IList<AppTestEntity>> result = await service.ListAsync();

        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByAsync_ShouldFilterByClause()
    {
        var items = new List<AppTestEntity> { new() { Id = 1, Active = true } };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetByAnyExpression(repo, items);
        var service = new TestApplicationServiceAsync(uow.Object);

        IBusinessResult<IList<AppTestEntity>> result = await service.GetByAsync(e => e.Active);

        result.Data.Should().ContainSingle();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntity()
    {
        var entity = new AppTestEntity { Id = 5, Name = "Item" };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 5, entity);
        var service = new TestApplicationServiceAsync(uow.Object);

        IBusinessResult<AppTestEntity?> result = await service.GetByIdAsync(5);

        result.Data!.Name.Should().Be("Item");
    }

    [Fact]
    public async Task AddAsync_ValidEntity_ShouldPersistAndSaveChanges()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationServiceAsync(uow.Object, new AppTestEntityValidator());
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
        var service = new TestApplicationServiceAsync(uow.Object, new AppTestEntityValidator());

        IBusinessResult<int> result = await service.AddAsync(new AppTestEntity { Name = "" });

        result.HasErrors.Should().BeTrue();
        repo.Verify(r => r.AddAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_EmptyList_ShouldReturnZeroWithoutSave()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationServiceAsync(uow.Object);

        IBusinessResult<int> result = await service.AddAsync([]);

        result.Data.Should().Be(0);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ModifyAsync_ValidEntity_ShouldUpdateAndSave()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationServiceAsync(uow.Object, new AppTestEntityValidator());
        var entity = new AppTestEntity { Id = 1, Name = "Updated" };

        IBusinessResult<int> result = await service.ModifyAsync(entity);

        result.Data.Should().Be(1);
        repo.Verify(r => r.ModifyAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveAndSave()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationServiceAsync(uow.Object);
        var entity = new AppTestEntity { Id = 1, Name = "Delete" };

        IBusinessResult<int> result = await service.RemoveAsync(entity);

        result.Data.Should().Be(1);
        repo.Verify(r => r.RemoveAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveByIdAsync_ShouldRemoveByIdAndSave()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationServiceAsync(uow.Object);

        IBusinessResult<int> result = await service.RemoveByIdAsync(42);

        result.Data.Should().Be(1);
        repo.Verify(r => r.RemoveByIdAsync(42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveByIdAsync_EmptyIds_ShouldReturnZero()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationServiceAsync(uow.Object);

        IBusinessResult<int> result = await service.RemoveByIdAsync([]);

        result.Data.Should().Be(0);
    }

    [Fact]
    public async Task ListAsync_ShouldPassCancellationTokenToRepository()
    {
        using var cts = new CancellationTokenSource();
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupList(repo, []);
        var service = new TestApplicationServiceAsync(uow.Object);

        await service.ListAsync(cts.Token);

        repo.Verify(r => r.ListAsync(It.IsAny<IPagingCriteria?>(), It.Is<CancellationToken>(ct => ct == cts.Token)), Times.Once);
    }

    [Fact]
    public async Task AddAsync_ShouldPassCancellationTokenToRepository()
    {
        using var cts = new CancellationTokenSource();
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationServiceAsync(uow.Object, new AppTestEntityValidator());
        var entity = new AppTestEntity { Name = "Valid" };

        await service.AddAsync(entity, cts.Token);

        repo.Verify(r => r.AddAsync(entity, It.Is<CancellationToken>(ct => ct == cts.Token)), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.Is<CancellationToken>(ct => ct == cts.Token)), Times.Once);
    }

    [Fact]
    public async Task AnyBySpecificationAsync_WithNullSpecification_ShouldReturnFalse()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = new TestApplicationServiceAsync(uow.Object);

        IBusinessResult<bool> result = await service.AnyBySpecificationAsync<ActiveAppTestEntitySpec>(null!);

        result.Data.Should().BeFalse();
    }
}
