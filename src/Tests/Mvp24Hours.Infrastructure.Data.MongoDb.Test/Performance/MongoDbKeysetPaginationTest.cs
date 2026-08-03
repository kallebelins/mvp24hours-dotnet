using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Moq;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Pagination;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Performance;

[Trait("Category", "Unit")]
public class MongoDbKeysetPaginationUnitTest
{
    [Fact]
    public void Create_WithNullCollection_ShouldThrow()
    {
        Action act = () => MongoDbKeysetPagination<KeysetOrder>.Create(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void KeysetPagedResult_Count_ShouldReflectItems()
    {
        var result = new KeysetPagedResult<KeysetOrder>
        {
            Items = [new KeysetOrder { Id = 1, CreatedAt = DateTime.UtcNow }],
            PageSize = 10
        };

        result.Count.Should().Be(1);
    }

    [Fact]
    public void KeysetPagedResult_Count_WithNullItems_ShouldReturnZero()
    {
        var result = new KeysetPagedResult<KeysetOrder> { Items = null!, PageSize = 10 };

        result.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetPageAsync_ShouldReturnItemsWithCursorsAndHasNextPage()
    {
        DateTime baseTime = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var orders = Enumerable.Range(1, 6)
            .Select(i => new KeysetOrder { Id = i, CreatedAt = baseTime.AddMinutes(i), Name = $"Order-{i}" })
            .ToList();

        Mock<IMongoCollection<KeysetOrder>> collectionMock = CreateCollectionMock(orders);
        MongoDbKeysetPagination<KeysetOrder> paginator = CreatePaginator(collectionMock)
            .OrderByDescending(o => o.CreatedAt)
            .ThenBy(o => o.Id);

        KeysetPagedResult<KeysetOrder> page = await paginator.GetPageAsync(pageSize: 5);

        page.Items.Should().HaveCount(5);
        page.HasNextPage.Should().BeTrue();
        page.FirstCursor.Should().ContainKey("CreatedAt");
        page.LastCursor.Should().NotBeNull();
        page.LastCursor.Should().ContainKey("CreatedAt");
        page.PageSize.Should().Be(5);
    }

    [Fact]
    public async Task GetPageAsync_WithEmptyCollection_ShouldReturnEmptyResult()
    {
        Mock<IMongoCollection<KeysetOrder>> collectionMock = CreateCollectionMock([]);
        MongoDbKeysetPagination<KeysetOrder> paginator = CreatePaginator(collectionMock)
            .OrderBy(o => o.Id);

        KeysetPagedResult<KeysetOrder> page = await paginator.GetPageAsync(pageSize: 10);

        page.Items.Should().BeEmpty();
        page.HasNextPage.Should().BeFalse();
        page.FirstCursor.Should().BeNull();
        page.LastCursor.Should().BeNull();
    }

    [Fact]
    public async Task GetNextPageAsync_ShouldCombineBaseFilterWithCursorFilter()
    {
        DateTime baseTime = new(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var orders = Enumerable.Range(1, 4)
            .Select(i => new KeysetOrder { Id = i, CreatedAt = baseTime.AddMinutes(i), Name = $"Order-{i}", Active = true })
            .ToList();

        Mock<IMongoCollection<KeysetOrder>> collectionMock = CreateCollectionMock(orders);
        MongoDbKeysetPagination<KeysetOrder> paginator = CreatePaginator(collectionMock)
            .Where(o => o.Active)
            .Where(Builders<KeysetOrder>.Filter.Eq(o => o.Active, true))
            .OrderBy(o => o.Id);

        KeysetPagedResult<KeysetOrder> firstPage = await paginator.GetPageAsync(pageSize: 2);
        KeysetPagedResult<KeysetOrder> nextPage = await paginator.GetNextPageAsync(firstPage.LastCursor!, pageSize: 2);

        firstPage.Items.Should().HaveCount(2);
        nextPage.Should().NotBeNull();
        nextPage.Items.Should().NotBeNull();
        collectionMock.Verify(c => c.FindAsync(
            It.IsAny<FilterDefinition<KeysetOrder>>(),
            It.IsAny<FindOptions<KeysetOrder, KeysetOrder>>(),
            It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task GetPreviousPageAsync_ShouldReverseSortAndItems()
    {
        DateTime baseTime = new(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var orders = Enumerable.Range(1, 5)
            .Select(i => new KeysetOrder { Id = i, CreatedAt = baseTime.AddMinutes(i), Name = $"Order-{i}" })
            .ToList();

        Mock<IMongoCollection<KeysetOrder>> collectionMock = CreateCollectionMock(orders);
        MongoDbKeysetPagination<KeysetOrder> paginator = CreatePaginator(collectionMock)
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id);

        KeysetPagedResult<KeysetOrder> firstPage = await paginator.GetPageAsync(pageSize: 3);
        KeysetPagedResult<KeysetOrder> previousPage = await paginator.GetPreviousPageAsync(firstPage.FirstCursor!, pageSize: 3);

        firstPage.Items.Should().NotBeEmpty();
        previousPage.Should().NotBeNull();
        previousPage.FirstCursor.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPageAsync_WithProjection_ShouldPassProjectionToFindOptions()
    {
        var orders = new List<KeysetOrder> { new() { Id = 1, CreatedAt = DateTime.UtcNow, Name = "Only" } };
        FindOptions<KeysetOrder, KeysetOrder>? capturedOptions = null;

        var collectionMock = new Mock<IMongoCollection<KeysetOrder>>();
        collectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<KeysetOrder>>(),
                It.IsAny<FindOptions<KeysetOrder, KeysetOrder>>(),
                It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<KeysetOrder>, FindOptions<KeysetOrder, KeysetOrder>, CancellationToken>((_, options, _) =>
                capturedOptions = options)
            .ReturnsAsync(new FakeAsyncCursor<KeysetOrder>(orders));

        ProjectionDefinition<KeysetOrder> projection = Builders<KeysetOrder>.Projection.Include(o => o.Name);
        MongoDbKeysetPagination<KeysetOrder> paginator = MongoDbKeysetPagination<KeysetOrder>.Create(collectionMock.Object)
            .OrderBy(o => o.Id)
            .Project(projection);

        await paginator.GetPageAsync(pageSize: 1);

        capturedOptions.Should().NotBeNull();
        capturedOptions!.Projection.Should().NotBeNull();
        capturedOptions.Limit.Should().Be(2);
    }

    [Fact]
    public async Task GetPageAsync_WithExactPageSize_ShouldNotSetHasNextPage()
    {
        var orders = new List<KeysetOrder>
        {
            new() { Id = 1, CreatedAt = DateTime.UtcNow, Name = "A" },
            new() { Id = 2, CreatedAt = DateTime.UtcNow.AddMinutes(1), Name = "B" }
        };

        Mock<IMongoCollection<KeysetOrder>> collectionMock = CreateCollectionMock(orders);
        MongoDbKeysetPagination<KeysetOrder> paginator = CreatePaginator(collectionMock)
            .OrderBy(o => o.Id);

        KeysetPagedResult<KeysetOrder> page = await paginator.GetPageAsync(pageSize: 2);

        page.Items.Should().HaveCount(2);
        page.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task Builder_ShouldSupportThenByDescending()
    {
        var orders = new List<KeysetOrder>
        {
            new() { Id = 2, CreatedAt = DateTime.UtcNow, Name = "B" },
            new() { Id = 1, CreatedAt = DateTime.UtcNow, Name = "A" }
        };

        Mock<IMongoCollection<KeysetOrder>> collectionMock = CreateCollectionMock(orders);
        MongoDbKeysetPagination<KeysetOrder> paginator = CreatePaginator(collectionMock)
            .OrderBy(o => o.CreatedAt)
            .ThenByDescending(o => o.Id);

        KeysetPagedResult<KeysetOrder> page = await paginator.GetPageAsync(pageSize: 10);

        page.Items.Should().HaveCount(2);
        page.LastCursor.Should().NotBeNull();
    }

    private static MongoDbKeysetPagination<KeysetOrder> CreatePaginator(Mock<IMongoCollection<KeysetOrder>> collectionMock)
    {
        ProjectionDefinition<KeysetOrder> projection = Builders<KeysetOrder>.Projection
            .Include(o => o.Id)
            .Include(o => o.CreatedAt)
            .Include(o => o.Name)
            .Include(o => o.Active);

        return MongoDbKeysetPagination<KeysetOrder>.Create(collectionMock.Object).Project(projection);
    }

    private static Mock<IMongoCollection<KeysetOrder>> CreateCollectionMock(IReadOnlyList<KeysetOrder> items)
    {
        var collectionMock = new Mock<IMongoCollection<KeysetOrder>>();
        collectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<KeysetOrder>>(),
                It.IsAny<FindOptions<KeysetOrder, KeysetOrder>>(),
                It.IsAny<CancellationToken>()))
            .Returns((FilterDefinition<KeysetOrder> _, FindOptions<KeysetOrder, KeysetOrder> options, CancellationToken _) =>
            {
                int limit = options.Limit ?? items.Count;
                IReadOnlyList<KeysetOrder> pageItems = [.. items.Take(limit)];
                return Task.FromResult<IAsyncCursor<KeysetOrder>>(new FakeAsyncCursor<KeysetOrder>(pageItems));
            });
        return collectionMock;
    }

    private sealed class FakeAsyncCursor<T>(IReadOnlyList<T> items) : IAsyncCursor<T>
    {
        private readonly IReadOnlyList<T> _items = items;
        private bool _hasMoved;

        public IEnumerable<T> Current => _items;

        public void Dispose()
        {
        }

        public bool MoveNext(CancellationToken cancellationToken = default)
        {
            if (!_hasMoved)
            {
                _hasMoved = true;
                return _items.Count > 0;
            }

            return false;
        }

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(MoveNext(cancellationToken));
        }
    }
}

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class MongoDbKeysetPaginationIntegrationTest(MongoDbIntegrationFixture fixture)
{
    [DockerFact]
    public async Task GetPageAsync_ShouldReturnOrderedPageWithCursors()
    {
        IMongoCollection<KeysetOrder> collection = fixture.GetCollection<KeysetOrder>("keyset_orders");
        await collection.DeleteManyAsync(FilterDefinition<KeysetOrder>.Empty);

        DateTime baseTime = DateTime.UtcNow.AddHours(-1);
        var orders = Enumerable.Range(1, 15)
            .Select(i => new KeysetOrder
            {
                Id = i,
                CreatedAt = baseTime.AddMinutes(i),
                Name = $"Order-{i}"
            })
            .ToList();
        await collection.InsertManyAsync(orders);

        MongoDbKeysetPagination<KeysetOrder> paginator = MongoDbKeysetPagination<KeysetOrder>.Create(collection)
            .OrderByDescending(o => o.CreatedAt)
            .ThenBy(o => o.Id)
            .Project(CreateKeysetProjection());

        KeysetPagedResult<KeysetOrder> firstPage = await paginator.GetPageAsync(pageSize: 5);

        firstPage.Items.Should().HaveCount(5);
        firstPage.HasNextPage.Should().BeTrue();
        firstPage.FirstCursor.Should().NotBeNull();
        firstPage.LastCursor.Should().NotBeNull();
        firstPage.Items[0].Id.Should().Be(15);
    }

    [DockerFact]
    public async Task GetNextPageAsync_ShouldReturnFollowingPage()
    {
        IMongoCollection<KeysetOrder> collection = fixture.GetCollection<KeysetOrder>("keyset_orders_next");
        await collection.DeleteManyAsync(FilterDefinition<KeysetOrder>.Empty);

        DateTime baseTime = DateTime.UtcNow.AddHours(-2);
        await collection.InsertManyAsync(Enumerable.Range(1, 12).Select(i => new KeysetOrder
        {
            Id = i,
            CreatedAt = baseTime.AddMinutes(i),
            Name = $"Order-{i}"
        }));

        MongoDbKeysetPagination<KeysetOrder> paginator = MongoDbKeysetPagination<KeysetOrder>.Create(collection)
            .OrderBy(o => o.CreatedAt)
            .ThenBy(o => o.Id)
            .Project(CreateKeysetProjection());

        KeysetPagedResult<KeysetOrder> firstPage = await paginator.GetPageAsync(pageSize: 5);
        KeysetPagedResult<KeysetOrder> secondPage = await paginator.GetNextPageAsync(firstPage.LastCursor!, pageSize: 5);

        secondPage.Items.Should().HaveCount(5);
        var combinedIds = firstPage.Items.Select(i => i.Id)
            .Concat(secondPage.Items.Select(i => i.Id))
            .Distinct()
            .ToList();
        combinedIds.Count.Should().BeGreaterThanOrEqualTo(9);
        secondPage.HasNextPage.Should().BeTrue();
    }

    [DockerFact]
    public async Task GetPreviousPageAsync_ShouldReturnPreviousPage()
    {
        IMongoCollection<KeysetOrder> collection = fixture.GetCollection<KeysetOrder>("keyset_orders_prev");
        await collection.DeleteManyAsync(FilterDefinition<KeysetOrder>.Empty);

        DateTime baseTime = DateTime.UtcNow.AddHours(-3);
        await collection.InsertManyAsync(Enumerable.Range(1, 10).Select(i => new KeysetOrder
        {
            Id = i,
            CreatedAt = baseTime.AddMinutes(i),
            Name = $"Order-{i}"
        }));

        MongoDbKeysetPagination<KeysetOrder> paginator = MongoDbKeysetPagination<KeysetOrder>.Create(collection)
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Project(CreateKeysetProjection());

        KeysetPagedResult<KeysetOrder> firstPage = await paginator.GetPageAsync(pageSize: 4);
        KeysetPagedResult<KeysetOrder> previousPage = await paginator.GetPreviousPageAsync(
            firstPage.FirstCursor!,
            pageSize: 4);

        previousPage.Items.Should().NotBeEmpty();
        previousPage.FirstCursor.Should().NotBeNull();
    }

    [DockerFact]
    public async Task WhereAndProject_ShouldApplyFilterAndProjection()
    {
        IMongoCollection<KeysetOrder> collection = fixture.GetCollection<KeysetOrder>("keyset_orders_filter");
        await collection.DeleteManyAsync(FilterDefinition<KeysetOrder>.Empty);

        await collection.InsertManyAsync(
        [
            new KeysetOrder { Id = 1, CreatedAt = DateTime.UtcNow, Name = "Alpha", Active = true },
            new KeysetOrder { Id = 2, CreatedAt = DateTime.UtcNow, Name = "Beta", Active = false },
            new KeysetOrder { Id = 3, CreatedAt = DateTime.UtcNow, Name = "Gamma", Active = true }
        ]);

        ProjectionDefinition<KeysetOrder> projection = Builders<KeysetOrder>.Projection
            .Include(o => o.Id)
            .Include(o => o.Name);

        MongoDbKeysetPagination<KeysetOrder> paginator = MongoDbKeysetPagination<KeysetOrder>.Create(collection)
            .Where(o => o.Active)
            .Where(Builders<KeysetOrder>.Filter.Eq(o => o.Active, true))
            .OrderBy(o => o.Id)
            .Project(projection);

        KeysetPagedResult<KeysetOrder> page = await paginator.GetPageAsync(pageSize: 10);

        page.Items.Should().HaveCount(2);
        page.Items.Should().OnlyContain(o => o.Active);
    }

    private static ProjectionDefinition<KeysetOrder> CreateKeysetProjection()
    {
        return Builders<KeysetOrder>.Projection
            .Include(o => o.Id)
            .Include(o => o.CreatedAt)
            .Include(o => o.Name)
            .Include(o => o.Active);
    }
}

public class KeysetOrder
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool Active { get; set; } = true;
}
