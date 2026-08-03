using System.Linq.Expressions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
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

    [Fact]
    public void List_WithPagingCriteria_ShouldCacheSecondCall()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        IPagingCriteria criteria = Mock.Of<IPagingCriteria>();
        inner.Setup(x => x.List(criteria)).Returns([new CacheRepositoryEntity { Id = 2, Name = "Two" }]);
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var options = new CacheableRepositoryOptions { EnableCacheByDefault = true };
        var repository = new CacheableRepository<CacheRepositoryEntity>(inner.Object, cache, options: options);

        repository.List(criteria);
        repository.List(criteria);

        inner.Verify(x => x.List(criteria), Times.Once);
    }

    [Fact]
    public void GetById_WithPagingCriteria_ShouldCacheSecondCall()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        IPagingCriteria criteria = Mock.Of<IPagingCriteria>();
        inner.Setup(x => x.GetById(7, criteria)).Returns(new CacheRepositoryEntity { Id = 7, Name = "Seven" });
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var options = new CacheableRepositoryOptions { EnableCacheByDefault = true };
        var repository = new CacheableRepository<CacheRepositoryEntity>(inner.Object, cache, options: options);

        repository.GetById(7, criteria);
        repository.GetById(7, criteria);

        inner.Verify(x => x.GetById(7, criteria), Times.Once);
    }

    [Fact]
    public void ListCount_ShouldNotUseCache()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        inner.Setup(x => x.ListCount()).Returns(42);
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var repository = new CacheableRepository<CacheRepositoryEntity>(
            inner.Object,
            cache,
            options: new CacheableRepositoryOptions { EnableCacheByDefault = true });

        repository.ListCount();
        repository.ListCount();

        inner.Verify(x => x.ListCount(), Times.Exactly(2));
    }

    [Fact]
    public void GetByAny_ShouldNotUseCache()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        Expression<Func<CacheRepositoryEntity, bool>> clause = e => e.Id > 0;
        inner.Setup(x => x.GetByAny(clause)).Returns(true);
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var repository = new CacheableRepository<CacheRepositoryEntity>(
            inner.Object,
            cache,
            options: new CacheableRepositoryOptions { EnableCacheByDefault = true });

        repository.GetByAny(clause);
        repository.GetByAny(clause);

        inner.Verify(x => x.GetByAny(clause), Times.Exactly(2));
    }

    [Fact]
    public void GetByCount_ShouldNotUseCache()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        Expression<Func<CacheRepositoryEntity, bool>> clause = e => e.Name == "X";
        inner.Setup(x => x.GetByCount(clause)).Returns(3);
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var repository = new CacheableRepository<CacheRepositoryEntity>(
            inner.Object,
            cache,
            options: new CacheableRepositoryOptions { EnableCacheByDefault = true });

        repository.GetByCount(clause);
        repository.GetByCount(clause);

        inner.Verify(x => x.GetByCount(clause), Times.Exactly(2));
    }

    [Fact]
    public void Add_ShouldDelegateToInnerRepository()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        var entity = new CacheRepositoryEntity { Id = 1, Name = "Add" };
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var repository = new CacheableRepository<CacheRepositoryEntity>(inner.Object, cache);

        repository.Add(entity);

        inner.Verify(x => x.Add(entity), Times.Once);
    }

    [Fact]
    public void Add_MultipleEntities_ShouldDelegateToInnerRepository()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        IList<CacheRepositoryEntity> entities = [new() { Id = 1 }, new() { Id = 2 }];
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var repository = new CacheableRepository<CacheRepositoryEntity>(inner.Object, cache);

        repository.Add(entities);

        inner.Verify(x => x.Add(entities), Times.Once);
    }

    [Fact]
    public void Remove_ShouldDelegateToInnerRepository()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        var entity = new CacheRepositoryEntity { Id = 3, Name = "Remove" };
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var repository = new CacheableRepository<CacheRepositoryEntity>(inner.Object, cache);

        repository.Remove(entity);

        inner.Verify(x => x.Remove(entity), Times.Once);
    }

    [Fact]
    public void RemoveById_ShouldDelegateToInnerRepository()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var repository = new CacheableRepository<CacheRepositoryEntity>(inner.Object, cache);

        repository.RemoveById(99);

        inner.Verify(x => x.RemoveById(99), Times.Once);
    }

    [Fact]
    public void RemoveById_MultipleIds_ShouldDelegateToInnerRepository()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        IList<object> ids = [1, 2, 3];
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var repository = new CacheableRepository<CacheRepositoryEntity>(inner.Object, cache);

        repository.RemoveById(ids);

        inner.Verify(x => x.RemoveById(ids), Times.Once);
    }

    [Fact]
    public void Modify_MultipleEntities_ShouldDelegateToInnerRepository()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        IList<CacheRepositoryEntity> entities = [new() { Id = 4, Name = "A" }];
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var repository = new CacheableRepository<CacheRepositoryEntity>(inner.Object, cache);

        repository.Modify(entities);

        inner.Verify(x => x.Modify(entities), Times.Once);
    }

    [Fact]
    public void LoadRelation_ShouldDelegateToInnerRepository()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        var entity = new CacheRepositoryEntity { Id = 1, Name = "Parent" };
        Expression<Func<CacheRepositoryEntity, string>> property = e => e.Name;
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var repository = new CacheableRepository<CacheRepositoryEntity>(inner.Object, cache);

        repository.LoadRelation(entity, property);

        inner.Verify(x => x.LoadRelation(entity, property), Times.Once);
    }

    [Fact]
    public void LoadRelationCollection_ShouldDelegateToInnerRepository()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        var entity = new CacheRepositoryEntity { Id = 1 };
        Expression<Func<CacheRepositoryEntity, IEnumerable<CacheRepositoryEntity>>> property =
            e => Enumerable.Empty<CacheRepositoryEntity>();
        Expression<Func<CacheRepositoryEntity, bool>> clause = e => e.Id > 0;
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var repository = new CacheableRepository<CacheRepositoryEntity>(inner.Object, cache);

        repository.LoadRelation(entity, property, clause, 10);

        inner.Verify(x => x.LoadRelation(entity, property, clause, 10), Times.Once);
    }

    [Fact]
    public void LoadRelationSortByAscending_ShouldDelegateToInnerRepository()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        var entity = new CacheRepositoryEntity { Id = 1 };
        Expression<Func<CacheRepositoryEntity, IEnumerable<CacheRepositoryEntity>>> property =
            e => Enumerable.Empty<CacheRepositoryEntity>();
        Expression<Func<CacheRepositoryEntity, int>> orderKey = e => e.Id;
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var repository = new CacheableRepository<CacheRepositoryEntity>(inner.Object, cache);

        repository.LoadRelationSortByAscending(entity, property, orderKey);

        inner.Verify(x => x.LoadRelationSortByAscending(entity, property, orderKey, null, 0), Times.Once);
    }

    [Fact]
    public void LoadRelationSortByDescending_ShouldDelegateToInnerRepository()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        var entity = new CacheRepositoryEntity { Id = 1 };
        Expression<Func<CacheRepositoryEntity, IEnumerable<CacheRepositoryEntity>>> property =
            e => Enumerable.Empty<CacheRepositoryEntity>();
        Expression<Func<CacheRepositoryEntity, int>> orderKey = e => e.Id;
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var repository = new CacheableRepository<CacheRepositoryEntity>(inner.Object, cache);

        repository.LoadRelationSortByDescending(entity, property, orderKey);

        inner.Verify(x => x.LoadRelationSortByDescending(entity, property, orderKey, null, 0), Times.Once);
    }

    [Fact]
    public void List_WhenCacheDisabledByDefault_ShouldNotCache()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        inner.Setup(x => x.List()).Returns([new CacheRepositoryEntity { Id = 1 }]);
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var repository = new CacheableRepository<CacheRepositoryEntity>(inner.Object, cache);

        repository.List();
        repository.List();

        inner.Verify(x => x.List(), Times.Exactly(2));
    }

    [Fact]
    public void List_WhenCacheProviderThrows_ShouldFallbackToInnerRepository()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        inner.Setup(x => x.List()).Returns([new CacheRepositoryEntity { Id = 1, Name = "Fallback" }]);
        var cache = new Mock<ICacheProvider>();
        cache.Setup(x => x.GetAsync<IList<CacheRepositoryEntity>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("cache down"));
        cache.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<IList<CacheRepositoryEntity>>(), It.IsAny<CacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var options = new CacheableRepositoryOptions { EnableCacheByDefault = true };
        var repository = new CacheableRepository<CacheRepositoryEntity>(
            inner.Object,
            cache.Object,
            NullLogger<CacheableRepository<CacheRepositoryEntity>>.Instance,
            options);

        IList<CacheRepositoryEntity> result = repository.List();

        result.Should().HaveCount(1);
        inner.Verify(x => x.List(), Times.Once);
    }

    [Fact]
    public void Modify_WithInvalidateAllOnModify_ShouldNotThrow()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();
        var entity = new CacheRepositoryEntity { Id = 8, Name = "InvalidateAll" };
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var options = new CacheableRepositoryOptions
        {
            EnableCacheByDefault = true,
            InvalidateAllOnModify = true
        };
        var repository = new CacheableRepository<CacheRepositoryEntity>(
            inner.Object,
            cache,
            NullLogger<CacheableRepository<CacheRepositoryEntity>>.Instance,
            options);
        repository.GetById(8);

        Action act = () => repository.Modify(entity);

        act.Should().NotThrow();
        inner.Verify(x => x.Modify(entity), Times.Once);
    }

    [Fact]
    public void Constructor_WithNullRepository_ShouldThrow()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();

        Action act = () => _ = new CacheableRepository<CacheRepositoryEntity>(null!, cache);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullCacheProvider_ShouldThrow()
    {
        var inner = new Mock<IRepository<CacheRepositoryEntity>>();

        Action act = () => _ = new CacheableRepository<CacheRepositoryEntity>(inner.Object, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
