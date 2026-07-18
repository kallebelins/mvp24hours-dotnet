//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace Mvp24Hours.Core.ValueObjects.Logic;

/// <summary>
/// Default implementation of <see cref="IKeysetPageResult{TEntity, TKey}"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The type of the cursor key (must be a value type).</typeparam>
/// <remarks>
/// Creates a new keyset page result.
/// </remarks>
/// <param name="items">The items in this page.</param>
/// <param name="lastKey">The key of the last item (cursor for next page).</param>
/// <param name="hasMore">Whether there are more items after this page.</param>
/// <param name="pageSize">The page size used for this query.</param>
public class KeysetPageResult<TEntity, TKey>(IReadOnlyList<TEntity> items, TKey? lastKey, bool hasMore, int pageSize) : IKeysetPageResult<TEntity, TKey>
    where TKey : struct
{

    /// <inheritdoc />
    public IReadOnlyList<TEntity> Items { get; } = items ?? [];

    /// <inheritdoc />
    public TKey? LastKey { get; } = lastKey;

    /// <inheritdoc />
    public bool HasMore { get; } = hasMore;

    /// <inheritdoc />
    public int Count => Items.Count;

    /// <inheritdoc />
    public int PageSize { get; } = pageSize;

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
/// <remarks>
/// Creates a new keyset page result with a string key.
/// </remarks>
/// <param name="items">The items in this page.</param>
/// <param name="lastKey">The key of the last item (cursor for next page).</param>
/// <param name="hasMore">Whether there are more items after this page.</param>
/// <param name="pageSize">The page size used for this query.</param>
public class KeysetPageResultString<TEntity>(IReadOnlyList<TEntity> items, string? lastKey, bool hasMore, int pageSize) : IKeysetPageResultString<TEntity>
{

    /// <inheritdoc />
    public IReadOnlyList<TEntity> Items { get; } = items ?? [];

    /// <inheritdoc />
    public string? LastKey { get; } = lastKey;

    /// <inheritdoc />
    public bool HasMore { get; } = hasMore;

    /// <inheritdoc />
    public int Count => Items.Count;

    /// <inheritdoc />
    public int PageSize { get; } = pageSize;

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
