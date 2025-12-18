//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Contract.Pagination;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Mvp24Hours.Application.Logic.Pagination
{
    /// <summary>
    /// Represents a paginated result with enriched metadata for navigation and display.
    /// </summary>
    /// <typeparam name="T">The type of items in the result.</typeparam>
    /// <remarks>
    /// <para>
    /// This class provides comprehensive pagination information including:
    /// <list type="bullet">
    ///   <item><description>Navigation: HasNextPage, HasPreviousPage, IsFirstPage, IsLastPage</description></item>
    ///   <item><description>Counts: TotalCount, TotalPages, CurrentPage, PageSize</description></item>
    ///   <item><description>Range: StartIndex, EndIndex (for "showing X-Y of Z" displays)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Creating a paged result
    /// var items = await repository.GetAll().Skip(offset).Take(pageSize).ToListAsync();
    /// var totalCount = await repository.CountAsync();
    /// 
    /// var result = PagedResult&lt;Customer&gt;.Create(items, page: 1, pageSize: 20, totalCount: 150);
    /// 
    /// // Displaying pagination info
    /// Console.WriteLine($"Page {result.CurrentPage} of {result.TotalPages}");
    /// Console.WriteLine($"Showing {result.StartIndex}-{result.EndIndex} of {result.TotalCount}");
    /// 
    /// if (result.HasNextPage) Console.WriteLine("More results available");
    /// </code>
    /// </example>
    [DataContract, Serializable]
    public class PagedResult<T> : IPagedResult<T>
    {
        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of the <see cref="PagedResult{T}"/> class.
        /// </summary>
        protected PagedResult()
        {
            Items = Array.Empty<T>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PagedResult{T}"/> class with the specified parameters.
        /// </summary>
        /// <param name="items">The items in the current page.</param>
        /// <param name="currentPage">The current page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="totalCount">The total number of items across all pages.</param>
        /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when currentPage or pageSize is less than 1.</exception>
        public PagedResult(IReadOnlyList<T> items, int currentPage, int pageSize, int totalCount)
        {
            Guard.Against.Null(items, nameof(items));
            Guard.Against.LessThan(currentPage, 1, nameof(currentPage));
            Guard.Against.LessThan(pageSize, 1, nameof(pageSize));
            Guard.Against.Negative(totalCount, nameof(totalCount));

            Items = items;
            CurrentPage = currentPage;
            PageSize = pageSize;
            TotalCount = totalCount;
        }

        #endregion

        #region [ Properties ]

        /// <inheritdoc/>
        [DataMember]
        public IReadOnlyList<T> Items { get; init; }

        /// <inheritdoc/>
        [DataMember]
        public int CurrentPage { get; init; }

        /// <inheritdoc/>
        [DataMember]
        public int PageSize { get; init; }

        /// <inheritdoc/>
        [DataMember]
        public int TotalCount { get; init; }

        /// <inheritdoc/>
        [DataMember]
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

        /// <inheritdoc/>
        [DataMember]
        public bool HasNextPage => CurrentPage < TotalPages;

        /// <inheritdoc/>
        [DataMember]
        public bool HasPreviousPage => CurrentPage > 1;

        /// <inheritdoc/>
        [DataMember]
        public bool IsFirstPage => CurrentPage == 1;

        /// <inheritdoc/>
        [DataMember]
        public bool IsLastPage => CurrentPage >= TotalPages;

        /// <inheritdoc/>
        [DataMember]
        public int StartIndex => TotalCount == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;

        /// <inheritdoc/>
        [DataMember]
        public int EndIndex => Math.Min(CurrentPage * PageSize, TotalCount);

        /// <inheritdoc/>
        [DataMember]
        public int Count => Items?.Count ?? 0;

        #endregion

        #region [ Factory Methods ]

        /// <summary>
        /// Creates a new <see cref="PagedResult{T}"/> from the specified parameters.
        /// </summary>
        /// <param name="items">The items in the current page.</param>
        /// <param name="page">The current page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="totalCount">The total number of items across all pages.</param>
        /// <returns>A new paged result instance.</returns>
        public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
        {
            return new PagedResult<T>(items, page, pageSize, totalCount);
        }

        /// <summary>
        /// Creates a new <see cref="PagedResult{T}"/> from a list with offset-based pagination.
        /// </summary>
        /// <param name="items">The items in the current page.</param>
        /// <param name="offset">The offset (0-based page index).</param>
        /// <param name="limit">The limit (page size).</param>
        /// <param name="totalCount">The total number of items.</param>
        /// <returns>A new paged result instance.</returns>
        public static PagedResult<T> CreateFromOffset(IReadOnlyList<T> items, int offset, int limit, int totalCount)
        {
            // Convert offset to 1-based page number
            var page = limit > 0 ? (offset / limit) + 1 : 1;
            return new PagedResult<T>(items, page, limit, totalCount);
        }

        /// <summary>
        /// Creates an empty <see cref="PagedResult{T}"/>.
        /// </summary>
        /// <param name="pageSize">The page size (default: 20).</param>
        /// <returns>An empty paged result.</returns>
        public static PagedResult<T> Empty(int pageSize = 20)
        {
            return new PagedResult<T>(Array.Empty<T>(), 1, pageSize, 0);
        }

        #endregion

        #region [ Conversion Methods ]

        /// <summary>
        /// Maps the items to a different type.
        /// </summary>
        /// <typeparam name="TResult">The target type.</typeparam>
        /// <param name="mapper">The mapping function.</param>
        /// <returns>A new paged result with mapped items.</returns>
        public PagedResult<TResult> Map<TResult>(Func<T, TResult> mapper)
        {
            Guard.Against.Null(mapper, nameof(mapper));

            var mappedItems = new List<TResult>(Items.Count);
            foreach (var item in Items)
            {
                mappedItems.Add(mapper(item));
            }

            return new PagedResult<TResult>(mappedItems, CurrentPage, PageSize, TotalCount);
        }

        /// <summary>
        /// Creates pagination metadata without items (useful for header-only responses).
        /// </summary>
        /// <returns>A dictionary with pagination metadata.</returns>
        public IDictionary<string, object> ToMetadata()
        {
            return new Dictionary<string, object>
            {
                ["currentPage"] = CurrentPage,
                ["pageSize"] = PageSize,
                ["totalCount"] = TotalCount,
                ["totalPages"] = TotalPages,
                ["hasNextPage"] = HasNextPage,
                ["hasPreviousPage"] = HasPreviousPage,
                ["isFirstPage"] = IsFirstPage,
                ["isLastPage"] = IsLastPage,
                ["startIndex"] = StartIndex,
                ["endIndex"] = EndIndex,
                ["count"] = Count
            };
        }

        #endregion
    }

    /// <summary>
    /// Represents a paginated result wrapped in a business result.
    /// </summary>
    /// <typeparam name="T">The type of items in the result.</typeparam>
    [DataContract, Serializable]
    public class PagedBusinessResult<T> : IPagedBusinessResult<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PagedBusinessResult{T}"/> class.
        /// </summary>
        /// <param name="pagedResult">The paged result.</param>
        /// <param name="messages">Optional business messages.</param>
        /// <param name="token">Optional transaction token.</param>
        public PagedBusinessResult(
            IPagedResult<T>? pagedResult,
            IReadOnlyCollection<IMessageResult>? messages = null,
            string? token = null)
        {
            Data = pagedResult;
            Messages = messages;
            Token = token;
        }

        /// <inheritdoc/>
        [DataMember]
        public IPagedResult<T>? Data { get; }

        /// <inheritdoc/>
        [JsonIgnore]
        [IgnoreDataMember]
        public IPagedResult<T>? PagedData => Data;

        /// <inheritdoc/>
        [DataMember]
        public IReadOnlyCollection<IMessageResult>? Messages { get; }

        /// <inheritdoc/>
        [DataMember]
        public bool HasErrors => Messages != null && Messages.Count > 0;

        /// <inheritdoc/>
        [DataMember]
        public string? Token { get; private set; }

        /// <inheritdoc/>
        public void SetToken(string? token)
        {
            if (string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(token))
            {
                Token = token;
            }
        }

        /// <summary>
        /// Creates a successful paged business result.
        /// </summary>
        public static PagedBusinessResult<T> Success(IPagedResult<T> pagedResult, string? token = null)
        {
            return new PagedBusinessResult<T>(pagedResult, null, token);
        }

        /// <summary>
        /// Creates a failed paged business result.
        /// </summary>
        public static PagedBusinessResult<T> Failure(IReadOnlyCollection<IMessageResult> messages, string? token = null)
        {
            return new PagedBusinessResult<T>(null, messages, token);
        }
    }
}

