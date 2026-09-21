//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Contract.Pagination;
using Mvp24Hours.Application.Logic.Pagination;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Application.Test.Extensions;

[Trait("Category", "Unit")]
public class PagedResultExtensionsTest
{
    /// <summary>
    /// Bare <see cref="IPagedResult{T}"/> implementation that is NOT the concrete
    /// <see cref="PagedResult{T}"/> type, used to exercise the generic-interface fallback
    /// branch of <see cref="PagedResultExtensions.MapTo{TSource, TDest}(IPagedResult{TSource}, Func{TSource, TDest})"/>.
    /// </summary>
    private sealed class FakePagedResult<T>(IReadOnlyList<T> items, int currentPage, int pageSize, int totalCount) : IPagedResult<T>
    {
        public IReadOnlyList<T> Items { get; } = items;
        public int CurrentPage { get; } = currentPage;
        public int PageSize { get; } = pageSize;
        public int TotalCount { get; } = totalCount;
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasNextPage => CurrentPage < TotalPages;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool IsFirstPage => CurrentPage == 1;
        public bool IsLastPage => CurrentPage >= TotalPages;
        public int StartIndex => TotalCount == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
        public int EndIndex => Math.Min(CurrentPage * PageSize, TotalCount);
        public int Count => Items?.Count ?? 0;
    }

    /// <summary>
    /// Bare <see cref="ICursorPagedResult{T}"/> implementation that is NOT the concrete
    /// <see cref="CursorPagedResult{T}"/> type, used to exercise the generic-interface fallback
    /// branch of the cursor <c>MapTo</c> overload.
    /// </summary>
    private sealed class FakeCursorPagedResult<T>(
        IReadOnlyList<T> items,
        int pageSize,
        bool hasNextPage,
        string? nextCursor,
        string? previousCursor,
        bool hasPreviousPage) : ICursorPagedResult<T>
    {
        public IReadOnlyList<T> Items { get; } = items;
        public int PageSize { get; } = pageSize;
        public int Count => Items?.Count ?? 0;
        public string? NextCursor { get; } = nextCursor;
        public string? PreviousCursor { get; } = previousCursor;
        public bool HasNextPage { get; } = hasNextPage;
        public bool HasPreviousPage { get; } = hasPreviousPage;
    }

    #region [ IEnumerable Extensions ]

    [Fact]
    public void ToPagedResult_FromEnumerable_ShouldWrapItemsWithProvidedMetadata()
    {
        string[] items = ["a", "b"];

        IPagedResult<string> result = items.ToPagedResult(page: 2, pageSize: 10, totalCount: 25);

        result.Items.Should().BeEquivalentTo(items);
        result.CurrentPage.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(25);
    }

    [Fact]
    public void ToPagedResult_FromEnumerable_WithNonListSource_ShouldMaterializeItems()
    {
        IEnumerable<int> items = Enumerable.Range(1, 3);

        IPagedResult<int> result = items.ToPagedResult(page: 1, pageSize: 5, totalCount: 3);

        result.Items.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public void ToPagedResultInMemory_ShouldPaginateFullSourceInMemory()
    {
        int[] items = Enumerable.Range(1, 25).ToArray();

        IPagedResult<int> result = items.ToPagedResultInMemory(page: 2, pageSize: 10);

        result.Items.Should().BeEquivalentTo(Enumerable.Range(11, 10));
        result.TotalCount.Should().Be(25);
        result.CurrentPage.Should().Be(2);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public void ToPagedResultInMemory_OnLastPartialPage_ShouldReturnRemainingItems()
    {
        int[] items = Enumerable.Range(1, 25).ToArray();

        IPagedResult<int> result = items.ToPagedResultInMemory(page: 3, pageSize: 10);

        result.Items.Should().BeEquivalentTo(Enumerable.Range(21, 5));
        result.TotalCount.Should().Be(25);
    }

    #endregion

    #region [ IQueryable Extensions ]

    [Fact]
    public void ToPagedResult_FromQueryable_ShouldCountAndPaginate()
    {
        IQueryable<int> query = Enumerable.Range(1, 47).AsQueryable();

        IPagedResult<int> result = query.ToPagedResult(page: 3, pageSize: 20);

        result.Items.Should().BeEquivalentTo(Enumerable.Range(41, 7));
        result.TotalCount.Should().Be(47);
        result.CurrentPage.Should().Be(3);
        result.PageSize.Should().Be(20);
    }

    #endregion

    #region [ MapTo - PagedResult ]

    [Fact]
    public void MapTo_WithConcretePagedResult_ShouldUseFastPathMap()
    {
        var source = PagedResult<int>.Create([1, 2, 3], 1, 10, 3);

        IPagedResult<string> mapped = source.MapTo(x => x.ToString());

        mapped.Should().BeOfType<PagedResult<string>>();
        mapped.Items.Should().BeEquivalentTo("1", "2", "3");
        mapped.CurrentPage.Should().Be(source.CurrentPage);
        mapped.PageSize.Should().Be(source.PageSize);
        mapped.TotalCount.Should().Be(source.TotalCount);
    }

    [Fact]
    public void MapTo_WithNonConcretePagedResult_ShouldUseGenericFallback()
    {
        IPagedResult<int> source = new FakePagedResult<int>([1, 2, 3], currentPage: 2, pageSize: 10, totalCount: 23);

        IPagedResult<string> mapped = source.MapTo(x => x.ToString());

        mapped.Items.Should().BeEquivalentTo("1", "2", "3");
        mapped.CurrentPage.Should().Be(2);
        mapped.PageSize.Should().Be(10);
        mapped.TotalCount.Should().Be(23);
    }

    #endregion

    #region [ MapTo - CursorPagedResult ]

    [Fact]
    public void MapTo_WithConcreteCursorPagedResult_ShouldUseFastPathMap()
    {
        var source = CursorPagedResult<int>.Create([1, 2], pageSize: 10, hasMore: true, nextCursor: "next", previousCursor: "prev", hasPreviousPage: true);

        ICursorPagedResult<string> mapped = source.MapTo(x => x.ToString());

        mapped.Should().BeOfType<CursorPagedResult<string>>();
        mapped.Items.Should().BeEquivalentTo("1", "2");
        mapped.NextCursor.Should().Be("next");
        mapped.PreviousCursor.Should().Be("prev");
        mapped.HasNextPage.Should().BeTrue();
        mapped.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void MapTo_WithNonConcreteCursorPagedResult_ShouldUseGenericFallback()
    {
        ICursorPagedResult<int> source = new FakeCursorPagedResult<int>(
            [1, 2], pageSize: 5, hasNextPage: true, nextCursor: "n1", previousCursor: "p1", hasPreviousPage: false);

        ICursorPagedResult<string> mapped = source.MapTo(x => x.ToString());

        mapped.Items.Should().BeEquivalentTo("1", "2");
        mapped.PageSize.Should().Be(5);
        mapped.NextCursor.Should().Be("n1");
        mapped.PreviousCursor.Should().Be("p1");
        mapped.HasNextPage.Should().BeTrue();
        mapped.HasPreviousPage.Should().BeFalse();
    }

    #endregion

    #region [ BusinessResult Conversions ]

    [Fact]
    public void ToBusinessResult_ShouldWrapPagedResultWithoutErrors()
    {
        var source = PagedResult<int>.Create([1, 2], 1, 10, 2);

        IBusinessResult<IPagedResult<int>> result = source.ToBusinessResult("token-123");

        result.Data.Should().BeSameAs(source);
        result.HasErrors.Should().BeFalse();
        result.Token.Should().Be("token-123");
    }

    [Fact]
    public void ToPagedBusinessResult_ShouldWrapAsPagedBusinessResult()
    {
        var source = PagedResult<int>.Create([1, 2], 1, 10, 2);

        IPagedBusinessResult<int> result = source.ToPagedBusinessResult();

        result.PagedData.Should().BeSameAs(source);
        result.HasErrors.Should().BeFalse();
    }

    #endregion

    #region [ Link Headers ]

    [Fact]
    public void CreateLinkHeaders_OnMiddlePage_ShouldIncludeAllFourLinks()
    {
        var source = PagedResult<int>.Create([1], 5, 10, 100);

        IDictionary<string, string> links = source.CreateLinkHeaders("https://api.test/items");

        links.Should().ContainKey("first").WhoseValue.Should().Be("https://api.test/items?page=1&pageSize=10");
        links.Should().ContainKey("last").WhoseValue.Should().Be("https://api.test/items?page=10&pageSize=10");
        links.Should().ContainKey("next").WhoseValue.Should().Be("https://api.test/items?page=6&pageSize=10");
        links.Should().ContainKey("prev").WhoseValue.Should().Be("https://api.test/items?page=4&pageSize=10");
    }

    [Fact]
    public void CreateLinkHeaders_OnFirstPage_ShouldNotIncludePrevLink()
    {
        var source = PagedResult<int>.Create([1], 1, 10, 100);

        IDictionary<string, string> links = source.CreateLinkHeaders("https://api.test/items");

        links.Should().ContainKey("first");
        links.Should().ContainKey("last");
        links.Should().ContainKey("next");
        links.Should().NotContainKey("prev");
    }

    [Fact]
    public void CreateLinkHeaders_OnLastPage_ShouldNotIncludeNextLink()
    {
        var source = PagedResult<int>.Create([1], 10, 10, 100);

        IDictionary<string, string> links = source.CreateLinkHeaders("https://api.test/items");

        links.Should().ContainKey("first");
        links.Should().ContainKey("last");
        links.Should().NotContainKey("next");
        links.Should().ContainKey("prev");
    }

    [Fact]
    public void CreateLinkHeaders_WithZeroTotalPages_ShouldNotIncludeLastLink()
    {
        var source = PagedResult<int>.Empty(10);

        IDictionary<string, string> links = source.CreateLinkHeaders("https://api.test/items");

        links.Should().ContainKey("first");
        links.Should().NotContainKey("last");
        links.Should().NotContainKey("next");
        links.Should().NotContainKey("prev");
    }

    [Fact]
    public void CreateLinkHeaders_WithBaseUrlContainingQueryString_ShouldUseAmpersandSeparator()
    {
        var source = PagedResult<int>.Create([1], 1, 10, 5);

        IDictionary<string, string> links = source.CreateLinkHeaders("https://api.test/items?filter=active");

        links["first"].Should().Be("https://api.test/items?filter=active&page=1&pageSize=10");
    }

    [Fact]
    public void CreateLinkHeaders_WithCustomParamNames_ShouldUseThoseNames()
    {
        var source = PagedResult<int>.Create([1], 1, 10, 5);

        IDictionary<string, string> links = source.CreateLinkHeaders(
            "https://api.test/items", pageParam: "p", pageSizeParam: "ps");

        links["first"].Should().Be("https://api.test/items?p=1&ps=10");
    }

    [Fact]
    public void CreateLinkHeaderValue_ShouldFormatAsRfc5988LinkHeader()
    {
        var source = PagedResult<int>.Create([1], 1, 10, 5);

        string headerValue = source.CreateLinkHeaderValue("https://api.test/items");

        headerValue.Should().Contain("<https://api.test/items?page=1&pageSize=10>; rel=\"first\"");
    }

    #endregion

    #region [ Cursor Navigation ]

    [Fact]
    public void GetNavigationInfo_ShouldMapAllCursorFields()
    {
        var source = CursorPagedResult<int>.Create(
            [1, 2, 3], pageSize: 10, hasMore: true, nextCursor: "next-cursor", previousCursor: "prev-cursor", hasPreviousPage: true);

        CursorNavigationInfo info = source.GetNavigationInfo();

        info.NextCursor.Should().Be("next-cursor");
        info.PreviousCursor.Should().Be("prev-cursor");
        info.HasNextPage.Should().BeTrue();
        info.HasPreviousPage.Should().BeTrue();
        info.Count.Should().Be(3);
        info.PageSize.Should().Be(10);
    }

    #endregion
}
