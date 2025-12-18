//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Contract.Cache
{
    /// <summary>
    /// Provides automatic cache invalidation for command operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The cache invalidator is responsible for:
    /// <list type="bullet">
    /// <item>Automatically invalidating cache when commands modify data</item>
    /// <item>Supporting region-based invalidation</item>
    /// <item>Supporting tag-based invalidation</item>
    /// <item>Supporting pattern-based invalidation</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface ICacheInvalidator
    {
        /// <summary>
        /// Invalidates all cache entries for an entity type.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task InvalidateEntityAsync<TEntity>(CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates all cache entries for an entity type.
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task InvalidateEntityAsync(Type entityType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates a specific cache entry by ID.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="id">The entity ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task InvalidateByIdAsync<TEntity>(object id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates cache entries in a specific region.
        /// </summary>
        /// <param name="region">The cache region.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task InvalidateRegionAsync(string region, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates cache entries with specific tags.
        /// </summary>
        /// <param name="tags">The tags to invalidate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task InvalidateByTagsAsync(string[] tags, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates cache entries matching a pattern.
        /// </summary>
        /// <param name="pattern">The pattern to match.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates specific cache keys.
        /// </summary>
        /// <param name="keys">The keys to invalidate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task InvalidateKeysAsync(string[] keys, CancellationToken cancellationToken = default);
    }
}

