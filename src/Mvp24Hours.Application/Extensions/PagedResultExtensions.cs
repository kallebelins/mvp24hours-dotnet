//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Contract.Pagination;
using Mvp24Hours.Application.Logic.Pagination;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for paged results and pagination operations.
    /// </summary>
    public static class PagedResultExtensions
    {
        #region [ IEnumerable Extensions ]

        /// <summary>
        /// Creates a paged result from an enumerable with manual pagination.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="source">The source enumerable (already paginated items).</param>
        /// <param name="page">The current page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="totalCount">The total count of all items.</param>
        /// <returns>A paged result.</returns>
        public static IPagedResult<T> ToPagedResult<T>(
            this IEnumerable<T> source,
            int page,
            int pageSize,
            int totalCount)
        {
            var items = source as IReadOnlyList<T> ?? source.ToList();
            return PagedResult<T>.Create(items, page, pageSize, totalCount);
        }

        /// <summary>
        /// Creates a paged result from an enumerable by performing pagination in memory.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="source">The complete source enumerable.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A paged result.</returns>
        /// <remarks>
        /// <para>
        /// ⚠️ Warning: This loads all items into memory. For large datasets,
        /// perform pagination at the database level instead.
        /// </para>
        /// </remarks>
        public static IPagedResult<T> ToPagedResultInMemory<T>(
            this IEnumerable<T> source,
            int page,
            int pageSize)
        {
            var allItems = source as IList<T> ?? source.ToList();
            var totalCount = allItems.Count;

            var offset = PaginationHelper.CalculateOffset(page, pageSize);
            var items = allItems.Skip(offset).Take(pageSize).ToList();

            return PagedResult<T>.Create(items, page, pageSize, totalCount);
        }

        #endregion

        #region [ IQueryable Extensions (Sync) ]

        /// <summary>
        /// Creates a paged result from a queryable.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="query">The queryable.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A paged result.</returns>
        /// <remarks>
        /// Uses .Count() and .Skip().Take() on the queryable.
        /// For EF Core, use the async version with cancellation token.
        /// </remarks>
        public static IPagedResult<T> ToPagedResult<T>(
            this IQueryable<T> query,
            int page,
            int pageSize)
        {
            var totalCount = query.Count();
            var offset = PaginationHelper.CalculateOffset(page, pageSize);
            var items = query.Skip(offset).Take(pageSize).ToList();

            return PagedResult<T>.Create(items, page, pageSize, totalCount);
        }

        #endregion

        #region [ Mapping Extensions ]

        /// <summary>
        /// Maps a paged result to a different type.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDest">The destination type.</typeparam>
        /// <param name="source">The source paged result.</param>
        /// <param name="mapper">The mapping function.</param>
        /// <returns>A new paged result with mapped items.</returns>
        public static IPagedResult<TDest> MapTo<TSource, TDest>(
            this IPagedResult<TSource> source,
            Func<TSource, TDest> mapper)
        {
            if (source is PagedResult<TSource> pagedResult)
            {
                return pagedResult.Map(mapper);
            }

            var mappedItems = source.Items.Select(mapper).ToList();
            return PagedResult<TDest>.Create(
                mappedItems,
                source.CurrentPage,
                source.PageSize,
                source.TotalCount);
        }

        /// <summary>
        /// Maps a cursor paged result to a different type.
        /// </summary>
        public static ICursorPagedResult<TDest> MapTo<TSource, TDest>(
            this ICursorPagedResult<TSource> source,
            Func<TSource, TDest> mapper)
        {
            if (source is CursorPagedResult<TSource> cursorResult)
            {
                return cursorResult.Map(mapper);
            }

            var mappedItems = source.Items.Select(mapper).ToList();
            return CursorPagedResult<TDest>.Create(
                mappedItems,
                source.PageSize,
                source.HasNextPage,
                source.NextCursor,
                source.PreviousCursor,
                source.HasPreviousPage);
        }

        #endregion

        #region [ BusinessResult Extensions ]

        /// <summary>
        /// Converts a paged result to a business result.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="source">The paged result.</param>
        /// <param name="token">Optional transaction token.</param>
        /// <returns>A business result containing the paged result.</returns>
        public static IBusinessResult<IPagedResult<T>> ToBusinessResult<T>(
            this IPagedResult<T> source,
            string? token = null)
        {
            return new BusinessResult<IPagedResult<T>>(source, null, token);
        }

        /// <summary>
        /// Converts a paged result to a paged business result.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="source">The paged result.</param>
        /// <param name="token">Optional transaction token.</param>
        /// <returns>A paged business result.</returns>
        public static IPagedBusinessResult<T> ToPagedBusinessResult<T>(
            this IPagedResult<T> source,
            string? token = null)
        {
            return PagedBusinessResult<T>.Success(source, token);
        }

        #endregion

        #region [ Header Metadata Extensions ]

        /// <summary>
        /// Creates HTTP-style pagination link headers.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="source">The paged result.</param>
        /// <param name="baseUrl">The base URL for links.</param>
        /// <param name="pageParam">The page parameter name (default: "page").</param>
        /// <param name="pageSizeParam">The page size parameter name (default: "pageSize").</param>
        /// <returns>A dictionary of link relation to URL.</returns>
        /// <example>
        /// <code>
        /// var links = pagedResult.CreateLinkHeaders("https://api.example.com/customers");
        /// // Returns: { "first": "...?page=1", "last": "...?page=10", "next": "...?page=3", "prev": "...?page=1" }
        /// </code>
        /// </example>
        public static IDictionary<string, string> CreateLinkHeaders<T>(
            this IPagedResult<T> source,
            string baseUrl,
            string pageParam = "page",
            string pageSizeParam = "pageSize")
        {
            var links = new Dictionary<string, string>();
            var separator = baseUrl.Contains('?') ? "&" : "?";

            string BuildUrl(int page) =>
                $"{baseUrl}{separator}{pageParam}={page}&{pageSizeParam}={source.PageSize}";

            // First page
            links["first"] = BuildUrl(1);

            // Last page
            if (source.TotalPages > 0)
            {
                links["last"] = BuildUrl(source.TotalPages);
            }

            // Next page
            if (source.HasNextPage)
            {
                links["next"] = BuildUrl(source.CurrentPage + 1);
            }

            // Previous page
            if (source.HasPreviousPage)
            {
                links["prev"] = BuildUrl(source.CurrentPage - 1);
            }

            return links;
        }

        /// <summary>
        /// Creates RFC 5988 compliant Link header value.
        /// </summary>
        public static string CreateLinkHeaderValue<T>(
            this IPagedResult<T> source,
            string baseUrl,
            string pageParam = "page",
            string pageSizeParam = "pageSize")
        {
            var links = source.CreateLinkHeaders(baseUrl, pageParam, pageSizeParam);
            return string.Join(", ", links.Select(l => $"<{l.Value}>; rel=\"{l.Key}\""));
        }

        #endregion

        #region [ Cursor Navigation Extensions ]

        /// <summary>
        /// Creates navigation info for cursor-based pagination responses.
        /// </summary>
        public static CursorNavigationInfo GetNavigationInfo<T>(this ICursorPagedResult<T> source)
        {
            return new CursorNavigationInfo
            {
                NextCursor = source.NextCursor,
                PreviousCursor = source.PreviousCursor,
                HasNextPage = source.HasNextPage,
                HasPreviousPage = source.HasPreviousPage,
                Count = source.Count,
                PageSize = source.PageSize
            };
        }

        #endregion
    }

    /// <summary>
    /// Represents cursor-based navigation information for API responses.
    /// </summary>
    public class CursorNavigationInfo
    {
        /// <summary>
        /// Gets or sets the cursor for the next page.
        /// </summary>
        public string? NextCursor { get; set; }

        /// <summary>
        /// Gets or sets the cursor for the previous page.
        /// </summary>
        public string? PreviousCursor { get; set; }

        /// <summary>
        /// Gets or sets whether there is a next page.
        /// </summary>
        public bool HasNextPage { get; set; }

        /// <summary>
        /// Gets or sets whether there is a previous page.
        /// </summary>
        public bool HasPreviousPage { get; set; }

        /// <summary>
        /// Gets or sets the count of items in the current page.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets the requested page size.
        /// </summary>
        public int PageSize { get; set; }
    }
}

