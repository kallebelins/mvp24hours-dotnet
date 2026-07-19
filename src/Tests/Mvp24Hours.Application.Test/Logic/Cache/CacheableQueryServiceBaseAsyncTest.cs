using Mvp24Hours.Application.Logic.Cache;
using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

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
}
