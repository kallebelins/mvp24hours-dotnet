//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Prefetching
{
    /// <summary>
    /// Implementation of cache prefetching that loads values asynchronously.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This prefetcher uses the cache-aside pattern to load values in the background.
    /// It checks if values exist in cache first, and only calls the factory if missing.
    /// </para>
    /// </remarks>
    public class CachePrefetcher : ICachePrefetcher
    {
        private readonly ICacheProvider _cacheProvider;
        private readonly ILogger<CachePrefetcher>? _logger;

        /// <summary>
        /// Creates a new instance of CachePrefetcher.
        /// </summary>
        /// <param name="cacheProvider">The cache provider to use.</param>
        /// <param name="logger">Optional logger.</param>
        public CachePrefetcher(
            ICacheProvider cacheProvider,
            ILogger<CachePrefetcher>? logger = null)
        {
            _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task PrefetchAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> valueFactory,
            CacheEntryOptions? options = null,
            CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (valueFactory == null)
                throw new ArgumentNullException(nameof(valueFactory));

            try
            {
                // Check if already cached
                var cached = await _cacheProvider.GetAsync<T>(key, cancellationToken);
                if (cached != null)
                {
                    _logger?.LogDebug("Prefetch skipped - value already cached for key: {Key}", key);
                    return;
                }

                // Load and cache the value
                _logger?.LogDebug("Prefetching value for key: {Key}", key);
                var value = await valueFactory(cancellationToken);
                if (value != null)
                {
                    await _cacheProvider.SetAsync(key, value, options, cancellationToken);
                    _logger?.LogDebug("Prefetch completed for key: {Key}", key);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error prefetching value for key: {Key}", key);
                // Don't throw - prefetch failures shouldn't break the application
            }
        }

        /// <inheritdoc />
        public async Task PrefetchManyAsync<T>(
            IEnumerable<PrefetchRequest<T>> prefetchRequests,
            CacheEntryOptions? options = null,
            int maxConcurrency = 10,
            CancellationToken cancellationToken = default) where T : class
        {
            if (prefetchRequests == null)
                throw new ArgumentNullException(nameof(prefetchRequests));
            if (maxConcurrency < 1)
                throw new ArgumentException("Max concurrency must be at least 1.", nameof(maxConcurrency));

            var requests = prefetchRequests.ToList();
            if (requests.Count == 0)
                return;

            _logger?.LogDebug("Prefetching {Count} values with max concurrency: {MaxConcurrency}", 
                requests.Count, maxConcurrency);

            using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var tasks = requests.Select(async request =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var requestOptions = request.Options ?? options;
                    await PrefetchAsync(request.Key, request.ValueFactory, requestOptions, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            _logger?.LogDebug("Prefetch completed for {Count} values", requests.Count);
        }
    }
}

