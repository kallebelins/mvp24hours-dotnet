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
}
