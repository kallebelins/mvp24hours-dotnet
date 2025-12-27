//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Caching
{
    /// <summary>
    /// Publishes cache invalidation events for distributed cache synchronization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface enables event-based cache invalidation across multiple application instances.
    /// When a cache entry is invalidated in one instance, other instances can be notified via pub/sub.
    /// </para>
    /// <para>
    /// <strong>Use Cases:</strong>
    /// <list type="bullet">
    /// <item>Invalidate cache across multiple servers when data changes</item>
    /// <item>Synchronize cache state in a distributed environment</item>
    /// <item>Reduce cache inconsistencies in microservices</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface ICacheInvalidationEventPublisher
    {
        /// <summary>
        /// Publishes a cache invalidation event for a specific key.
        /// </summary>
        /// <param name="key">The cache key that was invalidated.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task PublishKeyInvalidationAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a cache invalidation event for a tag.
        /// </summary>
        /// <param name="tag">The tag that was invalidated.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task PublishTagInvalidationAsync(string tag, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a cache invalidation event for multiple tags.
        /// </summary>
        /// <param name="tags">The tags that were invalidated.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task PublishTagsInvalidationAsync(string[] tags, CancellationToken cancellationToken = default);
    }
}

