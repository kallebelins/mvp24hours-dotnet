using Mvp24Hours.Application.Contract.Cache;
using Mvp24Hours.Application.Logic.Cache;
using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Application.Test.Logic.Cache;

[Trait("Category", "Unit")]
public class CacheableQueryServiceBaseAsyncTest
{
    [Fact]
    public async Task ListAsync_ShouldCacheResults()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupList(repo, [new AppTestEntity { Id = 1, Name = "Q" }]);
        InMemoryQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableQueryService(uow.Object, cache, new QueryCacheKeyGenerator());

        await service.ListAsync();
        await service.ListAsync();

        repo.Verify(r => r.ListAsync(It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_WithPagingCriteria_ShouldCacheByCriteria()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupList(repo, [new AppTestEntity { Id = 1 }]);
        InMemoryQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableQueryService(uow.Object, cache, new QueryCacheKeyGenerator());
        var criteria = new PagingCriteria(limit: 10, offset: 0);

        await service.ListAsync(criteria);
        await service.ListAsync(criteria);

        repo.Verify(r => r.ListAsync(criteria, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_WithCacheDisabled_ShouldBypassCache()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupList(repo, [new AppTestEntity { Id = 1 }]);
        InMemoryQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableQueryService(uow.Object, cache, new QueryCacheKeyGenerator());
        service.SetCacheEnabled(false);

        await service.ListAsync();
        await service.ListAsync();

        repo.Verify(r => r.ListAsync(It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ListAnyAsync_ShouldCacheResults()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListAny(repo, true);
        InMemoryQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableQueryService(uow.Object, cache, new QueryCacheKeyGenerator());

        await service.ListAnyAsync();
        await service.ListAnyAsync();

        repo.Verify(r => r.ListAnyAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListCountAsync_ShouldCacheResults()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListCount(repo, 4);
        InMemoryQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableQueryService(uow.Object, cache, new QueryCacheKeyGenerator());

        await service.ListCountAsync();
        await service.ListCountAsync();

        repo.Verify(r => r.ListCountAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldCacheResults()
    {
        var entity = new AppTestEntity { Id = 5, Name = "Cached" };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 5, entity);
        InMemoryQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableQueryService(uow.Object, cache, new QueryCacheKeyGenerator());

        await service.GetByIdAsync(5);
        await service.GetByIdAsync(5);

        repo.Verify(r => r.GetByIdAsync(5, It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithPagingCriteria_ShouldCacheByCriteria()
    {
        var entity = new AppTestEntity { Id = 5, Name = "Paged" };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 5, entity);
        InMemoryQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableQueryService(uow.Object, cache, new QueryCacheKeyGenerator());
        var criteria = new PagingCriteria(limit: 5, offset: 0);

        await service.GetByIdAsync(5, criteria);
        await service.GetByIdAsync(5, criteria);

        repo.Verify(r => r.GetByIdAsync(5, criteria, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisableCacheScope_ShouldRestorePreviousState()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupList(repo, [new AppTestEntity { Id = 1 }]);
        InMemoryQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableQueryService(uow.Object, cache, new QueryCacheKeyGenerator());

        using (service.DisableCacheForTest())
        {
            service.IsCacheEnabled.Should().BeFalse();
            await service.ListAsync();
        }

        service.IsCacheEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task ListWithCacheForTest_ShouldUseCustomDuration()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupList(repo, [new AppTestEntity { Id = 1 }]);
        InMemoryQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableQueryService(uow.Object, cache, new QueryCacheKeyGenerator());

        await service.ListWithCacheForTest(TimeSpan.FromMinutes(1));
        await service.ListWithCacheForTest(TimeSpan.FromMinutes(1));

        repo.Verify(r => r.ListAsync(It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByWithCacheForTest_ShouldCacheExpressionQuery()
    {
        var items = new List<AppTestEntity> { new() { Id = 1, Active = true } };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetByAnyExpression(repo, items);
        InMemoryQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableQueryService(uow.Object, cache, new QueryCacheKeyGenerator());
        var options = new QueryCacheEntryOptions { Duration = TimeSpan.FromMinutes(2), Region = "AppTestEntity" };

        await service.GetByWithCacheForTest(e => e.Active, options);
        await service.GetByWithCacheForTest(e => e.Active, options);

        repo.Verify(r => r.GetByAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AppTestEntity, bool>>>(), It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdWithCacheForTest_ShouldCacheWithCustomOptions()
    {
        var entity = new AppTestEntity { Id = 9, Name = "Custom" };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 9, entity);
        InMemoryQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableQueryService(uow.Object, cache, new QueryCacheKeyGenerator());
        var options = new QueryCacheEntryOptions { Duration = TimeSpan.FromMinutes(1), Region = "AppTestEntity" };

        await service.GetByIdWithCacheForTest(9, options);
        await service.GetByIdWithCacheForTest(9, options);

        repo.Verify(r => r.GetByIdAsync(9, It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateCacheForTest_ShouldClearRegionEntries()
    {
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupList(repo, [new AppTestEntity { Id = 1 }]);
        InMemoryQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableQueryService(uow.Object, cache, new QueryCacheKeyGenerator());

        await service.ListAsync();
        await service.InvalidateCacheForTest();
        await service.ListAsync();

        repo.Verify(r => r.ListAsync(It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task InvalidateCacheByIdForTest_ShouldRemoveSpecificEntry()
    {
        var entity = new AppTestEntity { Id = 3, Name = "Evict" };
        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetById(repo, 3, entity);
        InMemoryQueryCacheProvider cache = ApplicationTestHelpers.CreateInMemoryQueryCacheProvider();
        var service = new TestCacheableQueryService(uow.Object, cache, new QueryCacheKeyGenerator());

        await service.GetByIdAsync(3);
        await service.InvalidateCacheByIdForTest(3);
        await service.GetByIdAsync(3);

        repo.Verify(r => r.GetByIdAsync(3, It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public void GenerateCacheKeyForTest_ShouldSerializePagingCriteria()
    {
        (Mock<IUnitOfWorkAsync> uow, _) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();
        var service = new TestCacheableQueryService(uow.Object, ApplicationTestHelpers.CreateInMemoryQueryCacheProvider(), new QueryCacheKeyGenerator());
        var criteria = new PagingCriteria(limit: 20, offset: 5);

        string key = service.GenerateCacheKeyForTest(nameof(TestCacheableQueryService.ListAsync), criteria);

        key.Should().Contain("AppTestEntity");
        key.Should().Contain("p5_s20");
    }
}
