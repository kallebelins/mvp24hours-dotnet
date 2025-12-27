//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Caching
{
    /// <summary>
    /// Subscribes to cache invalidation events for distributed cache synchronization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface enables receiving cache invalidation events from other application instances.
    /// When an event is received, the local cache is invalidated accordingly.
    /// </para>
    /// </remarks>
    public interface ICacheInvalidationEventSubscriber
    {
        /// <summary>
        /// Subscribes to cache invalidation events.
        /// </summary>
        /// <param name="onKeyInvalidated">Callback invoked when a key is invalidated.</param>
        /// <param name="onTagInvalidated">Callback invoked when a tag is invalidated.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SubscribeAsync(
            Func<string, CancellationToken, Task> onKeyInvalidated,
            Func<string, CancellationToken, Task> onTagInvalidated,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Unsubscribes from cache invalidation events.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UnsubscribeAsync(CancellationToken cancellationToken = default);
    }
}

