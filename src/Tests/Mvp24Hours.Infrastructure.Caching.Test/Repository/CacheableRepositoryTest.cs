using System.Linq.Expressions;
using Moq;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Repository;
using Mvp24Hours.Infrastructure.Caching.Test.Support;

namespace Mvp24Hours.Infrastructure.Caching.Test.Repository;

[Trait("Category", "Unit")]
public class CacheableRepositoryTest
{
    [Fact]
    public void List_WithEnableCacheByDefault_ShouldCacheSecondCall()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        inner.Setup(x => x.List()).Returns([new CacheRepositoryEntity { Id = 1, Name = "One" }]);
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var options = new CacheableRepositoryOptions { EnableCacheByDefault = true };
        var repository = new CacheableRepository<CacheRepositoryEntity>(inner.Object, cache, options: options);

        IList<CacheRepositoryEntity> first = repository.List();
        IList<CacheRepositoryEntity> second = repository.List();

        first.Should().HaveCount(1);
        second.Should().HaveCount(1);
        inner.Verify(x => x.List(), Times.Once);
    }

    [Fact]
    public void GetById_WithEnableCacheByDefault_ShouldUseCache()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        inner.Setup(x => x.GetById(10)).Returns(new CacheRepositoryEntity { Id = 10, Name = "Ten" });
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var options = new CacheableRepositoryOptions { EnableCacheByDefault = true };
        var repository = new CacheableRepository<CacheRepositoryEntity>(inner.Object, cache, options: options);

        CacheRepositoryEntity first = repository.GetById(10);
        CacheRepositoryEntity second = repository.GetById(10);

        first.Name.Should().Be("Ten");
        second.Name.Should().Be("Ten");
        inner.Verify(x => x.GetById(10), Times.Once);
    }

    [Fact]
    public void ListAny_ShouldNotUseCache()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        inner.Setup(x => x.ListAny()).Returns(true);
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var options = new CacheableRepositoryOptions { EnableCacheByDefault = true };
        var repository = new CacheableRepository<CacheRepositoryEntity>(inner.Object, cache, options: options);

        repository.ListAny();
        repository.ListAny();

        inner.Verify(x => x.ListAny(), Times.Exactly(2));
    }

    [Fact]
    public void Modify_ShouldNotThrowWhenInvalidating()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        var entity = new CacheRepositoryEntity { Id = 5, Name = "Modify" };
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var options = new CacheableRepositoryOptions { EnableCacheByDefault = true };
        var repository = new CacheableRepository<CacheRepositoryEntity>(inner.Object, cache, options: options);
        repository.GetById(5);

        Action act = () => repository.Modify(entity);

        act.Should().NotThrow();
        inner.Verify(x => x.Modify(entity), Times.Once);
    }

    [Fact]
    public void GetBy_ShouldCacheWithCriteria()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        Expression<Func<CacheRepositoryEntity, bool>> clause = e => e.Name == "A";
        inner.Setup(x => x.GetBy(clause))
            .Returns([new CacheRepositoryEntity { Id = 1, Name = "A" }]);
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var options = new CacheableRepositoryOptions { EnableCacheByDefault = true };
        var repository = new CacheableRepository<CacheRepositoryEntity>(inner.Object, cache, options: options);

        repository.GetBy(clause);
        repository.GetBy(clause);

        inner.Verify(x => x.GetBy(clause), Times.Once);
    }

    [Fact]
    public void DefaultOptions_ShouldDisableCacheByDefault()
    {
        var options = new CacheableRepositoryOptions();

        options.EnableCacheByDefault.Should().BeFalse();
        options.DefaultCacheDurationSeconds.Should().Be(300);
        options.InvalidateAllOnModify.Should().BeFalse();
    }
}
