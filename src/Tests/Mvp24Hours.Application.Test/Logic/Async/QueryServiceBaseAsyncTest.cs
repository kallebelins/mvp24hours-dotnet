using System.Linq.Expressions;
using Mvp24Hours.Application.Test.Support;

namespace Mvp24Hours.Application.Test.Logic.Async;

[Trait("Category", "Unit")]
public class QueryServiceBaseAsyncTest
{
    [Fact]
    public void Constructor_WithNullUnitOfWork_ShouldThrowBeforeUse()
    {
        Func<TestQueryService> act = () => new TestQueryService(null!);
        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public async Task ListAnyAsync_ShouldReturnRepositoryResultAsBusiness()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListAny(repo, true);
        var service = new TestQueryService(uow.Object);

        IBusinessResult<bool> result = await service.ListAnyAsync();

        result.Data.Should().BeTrue();
        repo.Verify(r => r.ListAnyAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListCountAsync_ShouldReturnRepositoryCount()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListCount(repo, 7);
        var service = new TestQueryService(uow.Object);

        IBusinessResult<int> result = await service.ListCountAsync();

        result.Data.Should().Be(7);
    }

    [Fact]
    public async Task ListAsync_ShouldReturnEntities()
    {
        var items = new List<AppTestEntity> { new() { Id = 1, Name = "A" } };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupList(repo, items);
        var service = new TestQueryService(uow.Object);

        IBusinessResult<IList<AppTestEntity>> result = await service.ListAsync();

        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntity()
    {
        var entity = new AppTestEntity { Id = 5, Name = "Item" };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 5, entity);
        var service = new TestQueryService(uow.Object);

        IBusinessResult<AppTestEntity?> result = await service.GetByIdAsync(5);

        result.Data!.Name.Should().Be("Item");
    }

    [Fact]
    public async Task GetByAsync_ShouldFilterByClause()
    {
        var items = new List<AppTestEntity> { new() { Id = 1, Active = true } };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetByAnyExpression(repo, items);
        var service = new TestQueryService(uow.Object);

        IBusinessResult<IList<AppTestEntity>> result = await service.GetByAsync(e => e.Active);

        result.Data.Should().ContainSingle();
    }

    [Fact]
    public async Task AnyBySpecificationAsync_WithNullSpecification_ShouldReturnFalse()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = new TestQueryService(uow.Object);

        IBusinessResult<bool> result = await service.AnyBySpecificationAsync<ActiveAppTestEntitySpec>(null!);

        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task AnyBySpecificationAsync_WithReadOnlyRepository_ShouldUseSpecificationMethod()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        repo.Setup(r => r.GetByAnyAsync(It.IsAny<Expression<Func<AppTestEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = new TestQueryService(uow.Object);

        IBusinessResult<bool> result = await service.AnyBySpecificationAsync(new ActiveAppTestEntitySpec());

        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task GetSingleBySpecificationAsync_WithMultipleMatches_ShouldThrow()
    {
        var items = new List<AppTestEntity>
        {
            new() { Id = 1, Active = true },
            new() { Id = 2, Active = true }
        };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetByAnyExpression(repo, items);
        var service = new TestQueryService(uow.Object);

        Func<Task> act = async () => await service.GetSingleBySpecificationAsync(new ActiveAppTestEntitySpec());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetFirstBySpecificationAsync_ShouldReturnFirstMatch()
    {
        var items = new List<AppTestEntity>
        {
            new() { Id = 1, Name = "First", Active = true },
            new() { Id = 2, Name = "Second", Active = true }
        };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetByAnyExpression(repo, items);
        var service = new TestQueryService(uow.Object);

        IBusinessResult<AppTestEntity?> result = await service.GetFirstBySpecificationAsync(new ActiveAppTestEntitySpec());

        result.Data!.Name.Should().Be("First");
    }

    [Fact]
    public async Task CountBySpecificationAsync_WithoutReadOnlyRepository_ShouldFallbackToGetByCountAsync()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        repo.Setup(r => r.GetByCountAsync(It.IsAny<Expression<Func<AppTestEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        var service = new TestQueryService(uow.Object);

        IBusinessResult<int> result = await service.CountBySpecificationAsync(new ActiveAppTestEntitySpec());

        result.Data.Should().Be(5);
        repo.Verify(r => r.GetByCountAsync(It.IsAny<Expression<Func<AppTestEntity, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBySpecificationAsync_WithoutReadOnlyRepository_ShouldFallbackToGetByAsync()
    {
        var items = new List<AppTestEntity> { new() { Id = 1, Name = "Only", Active = true } };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetByAnyExpression(repo, items);
        var service = new TestQueryService(uow.Object);

        IBusinessResult<IList<AppTestEntity>> result = await service.GetBySpecificationAsync(new ActiveAppTestEntitySpec());

        result.Data.Should().ContainSingle().Which.Name.Should().Be("Only");
    }
}
