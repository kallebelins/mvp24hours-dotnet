//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;

namespace Mvp24Hours.Application.Contract.Pagination
{
    /// <summary>
    /// Represents the result of a cursor-based (keyset) pagination query.
    /// </summary>
    /// <typeparam name="T">The type of items in the result.</typeparam>
    /// <remarks>
    /// <para>
    /// Cursor-based pagination is more efficient than offset-based pagination for large datasets because:
    /// <list type="bullet">
    ///   <item><description>Uses indexed columns for filtering instead of OFFSET/SKIP</description></item>
    ///   <item><description>Performance is consistent regardless of "page" depth</description></item>
    ///   <item><description>No duplicate or missing rows when data changes during pagination</description></item>
    ///   <item><description>Ideal for infinite scroll or "load more" patterns</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>When to use:</strong>
    /// <list type="bullet">
    ///   <item><description>Large datasets (10k+ records)</description></item>
    ///   <item><description>Real-time data that changes frequently</description></item>
    ///   <item><description>Mobile apps with infinite scroll</description></item>
    ///   <item><description>APIs with high pagination depth</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Usage Pattern:</strong>
    /// <code>
    /// // First request (no cursor)
    /// var page1 = await service.GetCursorPagedAsync(pageSize: 20);
    /// 
    /// // Next request (using cursor from previous response)
    /// var page2 = await service.GetCursorPagedAsync(pageSize: 20, afterCursor: page1.NextCursor);
    /// 
    /// // Previous page (for bidirectional navigation)
    /// var prevPage = await service.GetCursorPagedAsync(pageSize: 20, beforeCursor: page2.PreviousCursor);
    /// </code>
    /// </para>
    /// </remarks>
    public interface ICursorPagedResult<out T>
    {
        /// <summary>
        /// Gets the items in the current page.
        /// </summary>
        IReadOnlyList<T> Items { get; }

        /// <summary>
        /// Gets the number of items per page requested.
        /// </summary>
        int PageSize { get; }

        /// <summary>
        /// Gets the number of items returned in this page.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the cursor to use for fetching the next page.
        /// Null if there is no next page.
        /// </summary>
        /// <remarks>
        /// Pass this value as the <c>afterCursor</c> parameter in subsequent requests.
        /// </remarks>
        string? NextCursor { get; }

        /// <summary>
        /// Gets the cursor to use for fetching the previous page.
        /// Null if there is no previous page.
        /// </summary>
        /// <remarks>
        /// Pass this value as the <c>beforeCursor</c> parameter for backward navigation.
        /// </remarks>
        string? PreviousCursor { get; }

        /// <summary>
        /// Gets a value indicating whether there are more items after this page.
        /// </summary>
        bool HasNextPage { get; }

        /// <summary>
        /// Gets a value indicating whether there are items before this page.
        /// </summary>
        bool HasPreviousPage { get; }
    }

    /// <summary>
    /// Represents a cursor-based pagination result with a strongly-typed cursor.
    /// </summary>
    /// <typeparam name="T">The type of items in the result.</typeparam>
    /// <typeparam name="TCursor">The type of the cursor (e.g., Guid, DateTime, int).</typeparam>
    /// <remarks>
    /// <para>
    /// Use this interface when you need type-safe cursor access without string serialization.
    /// </para>
    /// <para>
    /// <strong>Common cursor types:</strong>
    /// <list type="bullet">
    ///   <item><description><c>Guid</c> - For UUID primary keys</description></item>
    ///   <item><description><c>int</c> / <c>long</c> - For auto-increment IDs</description></item>
    ///   <item><description><c>DateTime</c> - For time-based ordering (CreatedAt)</description></item>
    ///   <item><description><c>(DateTime, Guid)</c> - For composite cursors</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface ICursorPagedResult<out T, TCursor> : ICursorPagedResult<T>
        where TCursor : struct
    {
        /// <summary>
        /// Gets the strongly-typed cursor for the next page.
        /// </summary>
        TCursor? NextCursorValue { get; }

        /// <summary>
        /// Gets the strongly-typed cursor for the previous page.
        /// </summary>
        TCursor? PreviousCursorValue { get; }
    }

    /// <summary>
    /// Represents a composite cursor for multi-field cursor-based pagination.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Composite cursors are used when ordering by multiple fields to ensure uniqueness.
    /// Example: Ordering by CreatedAt (may have duplicates) + Id (unique tiebreaker).
    /// </para>
    /// </remarks>
    public interface ICompositeCursor
    {
        /// <summary>
        /// Gets the cursor field values as a dictionary.
        /// </summary>
        IReadOnlyDictionary<string, object?> Fields { get; }

        /// <summary>
        /// Serializes the cursor to a string for URL/API transport.
        /// </summary>
        string Serialize();
    }
}

