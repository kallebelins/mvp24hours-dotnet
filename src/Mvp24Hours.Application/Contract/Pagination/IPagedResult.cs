//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System.Collections.Generic;

namespace Mvp24Hours.Application.Contract.Pagination
{
    /// <summary>
    /// Represents a paginated result with enriched metadata for navigation and display.
    /// </summary>
    /// <typeparam name="T">The type of items in the result.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface provides comprehensive pagination metadata including:
    /// <list type="bullet">
    ///   <item><description>Total count and total pages calculation</description></item>
    ///   <item><description>Navigation flags (HasNext, HasPrevious)</description></item>
    ///   <item><description>Current page context (page number, page size)</description></item>
    ///   <item><description>Range information (start/end index)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Usage Pattern:</strong>
    /// <code>
    /// IPagedResult&lt;CustomerDto&gt; result = await service.GetPagedCustomersAsync(page: 1, pageSize: 20);
    /// 
    /// Console.WriteLine($"Showing {result.StartIndex}-{result.EndIndex} of {result.TotalCount}");
    /// Console.WriteLine($"Page {result.CurrentPage} of {result.TotalPages}");
    /// 
    /// if (result.HasNextPage)
    ///     Console.WriteLine("Next page available");
    /// </code>
    /// </para>
    /// </remarks>
    public interface IPagedResult<out T>
    {
        /// <summary>
        /// Gets the items in the current page.
        /// </summary>
        IReadOnlyList<T> Items { get; }

        /// <summary>
        /// Gets the current page number (1-based).
        /// </summary>
        int CurrentPage { get; }

        /// <summary>
        /// Gets the number of items per page.
        /// </summary>
        int PageSize { get; }

        /// <summary>
        /// Gets the total number of items across all pages.
        /// </summary>
        int TotalCount { get; }

        /// <summary>
        /// Gets the total number of pages.
        /// </summary>
        int TotalPages { get; }

        /// <summary>
        /// Gets a value indicating whether there is a next page.
        /// </summary>
        bool HasNextPage { get; }

        /// <summary>
        /// Gets a value indicating whether there is a previous page.
        /// </summary>
        bool HasPreviousPage { get; }

        /// <summary>
        /// Gets a value indicating whether this is the first page.
        /// </summary>
        bool IsFirstPage { get; }

        /// <summary>
        /// Gets a value indicating whether this is the last page.
        /// </summary>
        bool IsLastPage { get; }

        /// <summary>
        /// Gets the 1-based index of the first item in this page.
        /// </summary>
        int StartIndex { get; }

        /// <summary>
        /// Gets the 1-based index of the last item in this page.
        /// </summary>
        int EndIndex { get; }

        /// <summary>
        /// Gets the number of items in the current page.
        /// </summary>
        int Count { get; }
    }

    /// <summary>
    /// Represents a paginated result wrapped in a business result with enriched metadata.
    /// </summary>
    /// <typeparam name="T">The type of items in the result.</typeparam>
    /// <remarks>
    /// <para>
    /// Combines <see cref="IPagedResult{T}"/> with <see cref="IBusinessResult{T}"/> 
    /// to provide both pagination metadata and business operation context (messages, errors).
    /// </para>
    /// </remarks>
    public interface IPagedBusinessResult<T> : IBusinessResult<IPagedResult<T>>
    {
        /// <summary>
        /// Gets the paged result data (alias for Data property).
        /// </summary>
        IPagedResult<T>? PagedData { get; }
    }
}

