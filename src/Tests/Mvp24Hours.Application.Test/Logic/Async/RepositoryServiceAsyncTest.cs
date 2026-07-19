using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace Mvp24Hours.Application.Test.Logic.Async;

[Trait("Category", "Unit")]
public class RepositoryServiceAsyncTest
{
    [Fact]
    public void Constructor_WithNullUnitOfWork_ShouldThrowBeforeUse()
    {
        Func<TestRepositoryService> act = () => new TestRepositoryService(null!);
        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public async Task ListAnyAsync_ShouldDelegateToRepository()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListAny(repo, true);
        var service = new TestRepositoryService(uow.Object);

        IBusinessResult<bool> result = await service.ListAnyAsync();

        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_ValidEntity_ShouldSaveChanges()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = new TestRepositoryService(uow.Object, new AppTestEntityValidator());
        var entity = new AppTestEntity { Name = "Repo" };

        IBusinessResult<int> result = await service.AddAsync(entity);

        result.Data.Should().Be(1);
        repo.Verify(r => r.AddAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ModifyAsync_InvalidEntity_ShouldReturnErrors()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = new TestRepositoryService(uow.Object, new AppTestEntityValidator());

        IBusinessResult<int> result = await service.ModifyAsync(new AppTestEntity { Name = "" });

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveAsync_List_ShouldRemoveAllEntities()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = new TestRepositoryService(uow.Object);
        var entities = new List<AppTestEntity>
        {
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" }
        };

        IBusinessResult<int> result = await service.RemoveAsync(entities);

        result.Data.Should().Be(1);
        repo.Verify(r => r.RemoveAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
