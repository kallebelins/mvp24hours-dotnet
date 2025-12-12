//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;

namespace Mvp24Hours.Core.Contract.ValueObjects.Logic
{
    /// <summary>
    /// Represents the result of a keyset (cursor-based) pagination query.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The type of the cursor key (must be a value type).</typeparam>
    /// <remarks>
    /// <para>
    /// Keyset pagination is more efficient than offset pagination for large datasets because:
    /// <list type="bullet">
    /// <item>It uses indexed columns for filtering instead of OFFSET/SKIP</item>
    /// <item>Performance is consistent regardless of page number</item>
    /// <item>No duplicate or missing rows when data changes during pagination</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Usage Pattern:</strong>
    /// <code>
    /// // First page
    /// var page1 = repository.GetByKeysetPagination(null, x => x.Id, null, 20);
    /// 
    /// // Next page using the cursor
    /// var page2 = repository.GetByKeysetPagination(null, x => x.Id, page1.LastKey, 20);
    /// </code>
    /// </para>
    /// </remarks>
    public interface IKeysetPageResult<TEntity, TKey>
        where TKey : struct
    {
        /// <summary>
        /// Gets the items in this page.
        /// </summary>
        IReadOnlyList<TEntity> Items { get; }

        /// <summary>
        /// Gets the key of the last item in this page.
        /// Use this value as the <c>lastKey</c> parameter to fetch the next page.
        /// Returns null if the page is empty.
        /// </summary>
        TKey? LastKey { get; }

        /// <summary>
        /// Gets whether there are more items after this page.
        /// </summary>
        bool HasMore { get; }

        /// <summary>
        /// Gets the number of items in this page.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the page size used for this query.
        /// </summary>
        int PageSize { get; }
    }

    /// <summary>
    /// Represents the result of a keyset (cursor-based) pagination query with a string key.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    public interface IKeysetPageResultString<TEntity>
    {
        /// <summary>
        /// Gets the items in this page.
        /// </summary>
        IReadOnlyList<TEntity> Items { get; }

        /// <summary>
        /// Gets the key of the last item in this page.
        /// Use this value as the <c>lastKey</c> parameter to fetch the next page.
        /// Returns null if the page is empty.
        /// </summary>
        string? LastKey { get; }

        /// <summary>
        /// Gets whether there are more items after this page.
        /// </summary>
        bool HasMore { get; }

        /// <summary>
        /// Gets the number of items in this page.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the page size used for this query.
        /// </summary>
        int PageSize { get; }
    }
}
