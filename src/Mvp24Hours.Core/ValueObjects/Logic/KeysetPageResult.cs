//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System.Collections.Generic;

namespace Mvp24Hours.Core.ValueObjects.Logic
{
    /// <summary>
    /// Default implementation of <see cref="IKeysetPageResult{TEntity, TKey}"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The type of the cursor key (must be a value type).</typeparam>
    public class KeysetPageResult<TEntity, TKey> : IKeysetPageResult<TEntity, TKey>
        where TKey : struct
    {
        /// <summary>
        /// Creates a new keyset page result.
        /// </summary>
        /// <param name="items">The items in this page.</param>
        /// <param name="lastKey">The key of the last item (cursor for next page).</param>
        /// <param name="hasMore">Whether there are more items after this page.</param>
        /// <param name="pageSize">The page size used for this query.</param>
        public KeysetPageResult(IReadOnlyList<TEntity> items, TKey? lastKey, bool hasMore, int pageSize)
        {
            Items = items ?? [];
            LastKey = lastKey;
            HasMore = hasMore;
            PageSize = pageSize;
        }

        /// <inheritdoc />
        public IReadOnlyList<TEntity> Items { get; }

        /// <inheritdoc />
        public TKey? LastKey { get; }

        /// <inheritdoc />
        public bool HasMore { get; }

        /// <inheritdoc />
        public int Count => Items.Count;

        /// <inheritdoc />
        public int PageSize { get; }

        /// <summary>
        /// Creates an empty page result.
        /// </summary>
        /// <param name="pageSize">The page size.</param>
        /// <returns>An empty page result.</returns>
        public static KeysetPageResult<TEntity, TKey> Empty(int pageSize)
        {
            return new KeysetPageResult<TEntity, TKey>([], null, false, pageSize);
        }
    }

    /// <summary>
    /// Default implementation of <see cref="IKeysetPageResultString{TEntity}"/> for string keys.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    public class KeysetPageResultString<TEntity> : IKeysetPageResultString<TEntity>
    {
        /// <summary>
        /// Creates a new keyset page result with a string key.
        /// </summary>
        /// <param name="items">The items in this page.</param>
        /// <param name="lastKey">The key of the last item (cursor for next page).</param>
        /// <param name="hasMore">Whether there are more items after this page.</param>
        /// <param name="pageSize">The page size used for this query.</param>
        public KeysetPageResultString(IReadOnlyList<TEntity> items, string? lastKey, bool hasMore, int pageSize)
        {
            Items = items ?? [];
            LastKey = lastKey;
            HasMore = hasMore;
            PageSize = pageSize;
        }

        /// <inheritdoc />
        public IReadOnlyList<TEntity> Items { get; }

        /// <inheritdoc />
        public string? LastKey { get; }

        /// <inheritdoc />
        public bool HasMore { get; }

        /// <inheritdoc />
        public int Count => Items.Count;

        /// <inheritdoc />
        public int PageSize { get; }

        /// <summary>
        /// Creates an empty page result.
        /// </summary>
        /// <param name="pageSize">The page size.</param>
        /// <returns>An empty page result.</returns>
        public static KeysetPageResultString<TEntity> Empty(int pageSize)
        {
            return new KeysetPageResultString<TEntity>([], null, false, pageSize);
        }
    }
}
