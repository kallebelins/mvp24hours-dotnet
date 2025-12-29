//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Synchronization
{
    /// <summary>
    /// In-memory cache synchronizer for single-instance scenarios or testing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation uses in-memory event handlers to synchronize cache invalidation
    /// within a single application instance. It's useful for:
    /// <list type="bullet">
    /// <item>Single-instance applications</item>
    /// <item>Unit testing</item>
    /// <item>Development environments</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Limitations:</strong>
    /// <list type="bullet">
    /// <item>Does not synchronize across multiple instances</item>
    /// <item>Events are lost on application restart</item>
    /// <item>Not suitable for distributed scenarios</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class InMemoryCacheSynchronizer : ICacheSynchronizer
    {
        private readonly ILogger<InMemoryCacheSynchronizer>? _logger;
        private readonly ConcurrentBag<Func<string, CancellationToken, Task>> _subscribers = new();
        private readonly object _lock = new object();

        /// <summary>
        /// Creates a new instance of InMemoryCacheSynchronizer.
        /// </summary>
        /// <param name="logger">Optional logger.</param>
        public InMemoryCacheSynchronizer(ILogger<InMemoryCacheSynchronizer>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public Task PublishInvalidationAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Task.CompletedTask;

            _logger?.LogDebug("[InMemoryCacheSynchronizer] Publishing invalidation for {Key}", key);

            lock (_lock)
            {
                var tasks = new List<Task>();
                foreach (var subscriber in _subscribers)
                {
                    try
                    {
                        var task = subscriber(key, cancellationToken);
                        tasks.Add(task);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "[InMemoryCacheSynchronizer] Error notifying subscriber for {Key}", key);
                    }
                }

                return Task.WhenAll(tasks);
            }
        }

        /// <inheritdoc />
        public async Task PublishInvalidationManyAsync(string[] keys, CancellationToken cancellationToken = default)
        {
            if (keys == null || keys.Length == 0)
                return;

            var tasks = keys.Select(key => PublishInvalidationAsync(key, cancellationToken));
            await Task.WhenAll(tasks);
        }

        /// <inheritdoc />
        public Task SubscribeAsync(Func<string, CancellationToken, Task> onInvalidation, CancellationToken cancellationToken = default)
        {
            if (onInvalidation == null)
                throw new ArgumentNullException(nameof(onInvalidation));

            lock (_lock)
            {
                _subscribers.Add(onInvalidation);
            }

            _logger?.LogDebug("[InMemoryCacheSynchronizer] Subscriber added. Total subscribers: {Count}", _subscribers.Count);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task UnsubscribeAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _subscribers.Clear();
            }

            _logger?.LogDebug("[InMemoryCacheSynchronizer] All subscribers removed");
            return Task.CompletedTask;
        }
    }
}

