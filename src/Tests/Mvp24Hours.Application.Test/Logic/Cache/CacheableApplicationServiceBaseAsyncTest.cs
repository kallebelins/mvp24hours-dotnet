using Mvp24Hours.Application.Contract.Cache;
using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace Mvp24Hours.Application.Test.Logic.Cache;

[Trait("Category", "Unit")]
public class CacheableApplicationServiceBaseAsyncTest
{
    [Fact]
    public async Task ListAsync_WithCacheEnabled_ShouldUseCacheProvider()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupList(repo, [new AppTestEntity { Id = 1, Name = "Cached" }]);
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        CacheInvalidator invalidator = ApplicationTestHelpers.CreateCacheInvalidator(cache);
        var service = new TestCacheableApplicationService(uow.Object, cache, invalidator, new QueryCacheKeyGenerator());

        IBusinessResult<IList<AppTestEntity>> first = await service.ListAsync();
        IBusinessResult<IList<AppTestEntity>> second = await service.ListAsync();

        first.Data.Should().ContainSingle();
        second.Data.Should().ContainSingle();
        repo.Verify(r => r.ListAsync(It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_OnSuccess_ShouldInvalidateEntityCache()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        CacheInvalidator invalidator = ApplicationTestHelpers.CreateCacheInvalidator(cache);
        var service = new TestCacheableApplicationService(uow.Object, cache, invalidator, new QueryCacheKeyGenerator());
        await cache.SetAsync("AppTestEntity:ListAsync", "cached",
            new QueryCacheEntryOptions { Duration = TimeSpan.FromMinutes(5), Region = "AppTestEntity" });

        await service.AddAsync(new AppTestEntity { Name = "New" });

        (await cache.ExistsAsync("AppTestEntity:ListAsync")).Should().BeFalse();
    }

    [Fact]
    public async Task AnyBySpecificationAsync_WithNullSpec_ShouldReturnFalse()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator());

        IBusinessResult<bool> result = await service.AnyBySpecificationAsync<ActiveAppTestEntitySpec>(null!);

        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task ListAsync_WithCacheDisabled_ShouldBypassCacheProvider()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupList(repo, [new AppTestEntity { Id = 1, Name = "Direct" }]);
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator());
        service.SetCacheEnabled(false);

        await service.ListAsync();
        await service.ListAsync();

        repo.Verify(r => r.ListAsync(It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetByIdAsync_WithCacheEnabled_ShouldUseCacheProvider()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var entity = new AppTestEntity { Id = 5, Name = "CachedId" };
        ApplicationTestHelpers.SetupGetById(repo, 5, entity);
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator());

        await service.GetByIdAsync(5);
        await service.GetByIdAsync(5);

        repo.Verify(r => r.GetByIdAsync(5, It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ModifyAsync_OnSuccess_ShouldInvalidateEntityCache()
    {
        (Mock<IUnitOfWorkAsync> uow, _) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        CacheInvalidator invalidator = ApplicationTestHelpers.CreateCacheInvalidator(cache);
        var service = new TestCacheableApplicationService(
            uow.Object, cache, invalidator, new QueryCacheKeyGenerator(), new AppTestEntityValidator());
        await cache.SetAsync("AppTestEntity:ListAsync", "cached",
            new QueryCacheEntryOptions { Duration = TimeSpan.FromMinutes(5), Region = "AppTestEntity" });

        await service.ModifyAsync(new AppTestEntity { Id = 1, Name = "Updated" });

        (await cache.ExistsAsync("AppTestEntity:ListAsync")).Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_InvalidEntity_ShouldReturnValidationErrors()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator(),
            new AppTestEntityValidator());

        IBusinessResult<int> result = await service.AddAsync(new AppTestEntity { Name = "" });

        result.HasErrors.Should().BeTrue();
        repo.Verify(r => r.AddAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CountBySpecificationAsync_WithNullSpec_ShouldReturnZero()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator());

        IBusinessResult<int> result = await service.CountBySpecificationAsync<ActiveAppTestEntitySpec>(null!);

        result.Data.Should().Be(0);
    }

    [Fact]
    public async Task RemoveAsync_EmptyBatch_ShouldReturnZero()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator());

        IBusinessResult<int> result = await service.RemoveAsync([]);

        result.Data.Should().Be(0);
        repo.Verify(r => r.RemoveAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListAnyAsync_WithCacheEnabled_ShouldUseCacheProvider()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListAny(repo, true);
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator());

        await service.ListAnyAsync();
        await service.ListAnyAsync();

        repo.Verify(r => r.ListAnyAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByAsync_WithCacheEnabled_ShouldUseCacheProvider()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetByAnyExpression(repo, [new AppTestEntity { Id = 1, Name = "Active", Active = true }]);
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator());

        await service.GetByAsync(e => e.Active);
        await service.GetByAsync(e => e.Active);

        repo.Verify(r => r.GetByAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AppTestEntity, bool>>>(), It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBySpecificationAsync_WithCacheEnabled_ShouldUseCacheProvider()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetByAnyExpression(repo, [new AppTestEntity { Id = 1, Name = "Active", Active = true }]);
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator());
        var spec = new ActiveAppTestEntitySpec();

        await service.GetBySpecificationAsync(spec);
        await service.GetBySpecificationAsync(spec);

        repo.Verify(r => r.GetByAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AppTestEntity, bool>>>(), It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveByIdAsync_OnSuccess_ShouldInvalidateEntityAndIdCache()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        CacheInvalidator invalidator = ApplicationTestHelpers.CreateCacheInvalidator(cache);
        var service = new TestCacheableApplicationService(uow.Object, cache, invalidator, new QueryCacheKeyGenerator());
        await cache.SetAsync("AppTestEntity:ListAsync", "cached",
            new QueryCacheEntryOptions { Duration = TimeSpan.FromMinutes(5), Region = "AppTestEntity" });

        await service.RemoveByIdAsync(42);

        (await cache.ExistsAsync("AppTestEntity:ListAsync")).Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_WithAutoInvalidateDisabled_ShouldNotClearCache()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator());
        service.SetAutoInvalidateOnCommand(false);
        await cache.SetAsync("AppTestEntity:ListAsync", "cached",
            new QueryCacheEntryOptions { Duration = TimeSpan.FromMinutes(5), Region = "AppTestEntity" });

        await service.AddAsync(new AppTestEntity { Name = "New" });

        (await cache.ExistsAsync("AppTestEntity:ListAsync")).Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_EmptyBatch_ShouldReturnZero()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator());

        IBusinessResult<int> result = await service.AddAsync([]);

        result.Data.Should().Be(0);
        repo.Verify(r => r.AddAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListCountAsync_WithCacheEnabled_ShouldUseCacheProvider()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListCount(repo, 7);
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator());

        await service.ListCountAsync();
        await service.ListCountAsync();

        repo.Verify(r => r.ListCountAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByAnyAsync_WithCacheEnabled_ShouldUseCacheProvider()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetByAnyExpression(repo, [new AppTestEntity { Id = 1, Name = "Active", Active = true }]);
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator());

        await service.GetByAnyAsync(e => e.Active);
        await service.GetByAnyAsync(e => e.Active);

        repo.Verify(r => r.GetByAnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AppTestEntity, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSingleBySpecificationAsync_WithCacheEnabled_ShouldUseCacheProvider()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetByAnyExpression(repo, [new AppTestEntity { Id = 1, Name = "Single", Active = true }]);
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator());
        var spec = new ActiveAppTestEntitySpec();

        await service.GetSingleBySpecificationAsync(spec);
        await service.GetSingleBySpecificationAsync(spec);

        repo.Verify(r => r.GetByAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AppTestEntity, bool>>>(), It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFirstBySpecificationAsync_WithNullSpec_ShouldReturnNull()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator());

        IBusinessResult<AppTestEntity?> result = await service.GetFirstBySpecificationAsync<ActiveAppTestEntitySpec>(null!);

        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task ModifyAsync_Batch_OnSuccess_ShouldInvalidateCache()
    {
        (Mock<IUnitOfWorkAsync> uow, _) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator(), new AppTestEntityValidator());
        await cache.SetAsync("AppTestEntity:ListAsync", "cached",
            new QueryCacheEntryOptions { Duration = TimeSpan.FromMinutes(5), Region = "AppTestEntity" });

        await service.ModifyAsync([new AppTestEntity { Id = 1, Name = "Batch" }]);

        (await cache.ExistsAsync("AppTestEntity:ListAsync")).Should().BeFalse();
    }

    [Fact]
    public async Task RemoveAsync_SingleEntity_OnSuccess_ShouldInvalidateCache()
    {
        (Mock<IUnitOfWorkAsync> uow, _) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator());
        await cache.SetAsync("AppTestEntity:ListAsync", "cached",
            new QueryCacheEntryOptions { Duration = TimeSpan.FromMinutes(5), Region = "AppTestEntity" });

        await service.RemoveAsync(new AppTestEntity { Id = 3, Name = "Remove" });

        (await cache.ExistsAsync("AppTestEntity:ListAsync")).Should().BeFalse();
    }

    [Fact]
    public async Task GetByCountAsync_WithCacheEnabled_ShouldUseCacheProvider()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetByAnyExpression(repo, [new AppTestEntity { Id = 1, Name = "Active", Active = true }]);
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator());

        await service.GetByCountAsync(e => e.Active);
        await service.GetByCountAsync(e => e.Active);

        repo.Verify(r => r.GetByCountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AppTestEntity, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBySpecificationAsync_WithNullSpec_ShouldReturnEmptyList()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator());

        IBusinessResult<IList<AppTestEntity>> result = await service.GetBySpecificationAsync<ActiveAppTestEntitySpec>(null!);

        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task ModifyAsync_InvalidEntity_ShouldReturnValidationErrors()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator(),
            new AppTestEntityValidator());

        IBusinessResult<int> result = await service.ModifyAsync(new AppTestEntity { Id = 1, Name = "" });

        result.HasErrors.Should().BeTrue();
        repo.Verify(r => r.ModifyAsync(It.IsAny<AppTestEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_Batch_OnSuccess_ShouldInvalidateCache()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator());
        await cache.SetAsync("AppTestEntity:ListAsync", "cached",
            new QueryCacheEntryOptions { Duration = TimeSpan.FromMinutes(5), Region = "AppTestEntity" });

        await service.RemoveAsync(
        [
            new AppTestEntity { Id = 1, Name = "One" },
            new AppTestEntity { Id = 2, Name = "Two" }
        ]);

        (await cache.ExistsAsync("AppTestEntity:ListAsync")).Should().BeFalse();
    }

    [Fact]
    public async Task GetFirstBySpecificationAsync_WithCacheEnabled_ShouldUseCacheProvider()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) =
            ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetByAnyExpression(repo, [new AppTestEntity { Id = 1, Name = "First", Active = true }]);
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator());
        var spec = new ActiveAppTestEntitySpec();

        await service.GetFirstBySpecificationAsync(spec);
        await service.GetFirstBySpecificationAsync(spec);

        repo.Verify(r => r.GetByAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AppTestEntity, bool>>>(), It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveByIdAsync_EmptyBatch_ShouldReturnZero()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        IQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableApplicationService(
            uow.Object, cache, ApplicationTestHelpers.CreateCacheInvalidator(cache), new QueryCacheKeyGenerator());

        IBusinessResult<int> result = await service.RemoveByIdAsync([]);

        result.Data.Should().Be(0);
    }
}
