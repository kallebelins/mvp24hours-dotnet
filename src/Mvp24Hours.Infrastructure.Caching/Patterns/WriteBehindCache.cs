//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Patterns
{
    /// <summary>
    /// Represents a pending write operation in the write-behind queue.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    internal class PendingWrite<T> where T : class
    {
        public string Key { get; }
        public T Value { get; }
        public DateTime EnqueuedAt { get; }

        public PendingWrite(string key, T value)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Value = value ?? throw new ArgumentNullException(nameof(value));
            EnqueuedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Base implementation of the Write-Behind caching pattern.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <remarks>
    /// <para>
    /// This class provides a generic implementation of the Write-Behind pattern.
    /// Writes are immediately cached and queued for background processing.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create write-behind cache
    /// var writeBehindCache = new WriteBehindCache&lt;Product&gt;(
    ///     cacheProvider,
    ///     async (key, value, ct) => 
    ///     {
    ///         await _repository.SaveAsync(value, ct);
    ///     },
    ///     options => CacheEntryOptions.FromDuration(TimeSpan.FromMinutes(5))
    /// );
    /// 
    /// // Use it
    /// await writeBehindCache.SetAsync("product:123", product);
    /// </code>
    /// </example>
    public class WriteBehindCache<T> : IWriteBehindCache<T> where T : class
    {
        private readonly ICacheProvider _cache;
        private readonly Func<string, T, CancellationToken, Task> _saveToSource;
        private readonly Func<string, CacheEntryOptions>? _getCacheOptions;
        private readonly ILogger<WriteBehindCache<T>>? _logger;
        private readonly ConcurrentQueue<PendingWrite<T>> _writeQueue;
        private readonly SemaphoreSlim _flushSemaphore;
        private volatile bool _isFlushing;

        /// <summary>
        /// Gets the number of pending writes in the queue.
        /// </summary>
        public int PendingWritesCount => _writeQueue.Count;

        /// <summary>
        /// Creates a new instance of WriteBehindCache.
        /// </summary>
        /// <param name="cache">The cache provider.</param>
        /// <param name="saveToSource">The function to persist data to the data source.</param>
        /// <param name="getCacheOptions">Optional function to get cache options for a given key.</param>
        /// <param name="logger">Optional logger.</param>
        /// <exception cref="ArgumentNullException">Thrown when cache or saveToSource is null.</exception>
        public WriteBehindCache(
            ICacheProvider cache,
            Func<string, T, CancellationToken, Task> saveToSource,
            Func<string, CacheEntryOptions>? getCacheOptions = null,
            ILogger<WriteBehindCache<T>>? logger = null)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _saveToSource = saveToSource ?? throw new ArgumentNullException(nameof(saveToSource));
            _getCacheOptions = getCacheOptions;
            _logger = logger;
            _writeQueue = new ConcurrentQueue<PendingWrite<T>>();
            _flushSemaphore = new SemaphoreSlim(1, 1);
        }

        /// <inheritdoc />
        public async Task SetAsync(string key, T value, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            try
            {
                // Write to cache immediately
                var options = _getCacheOptions?.Invoke(key) ?? CacheEntryOptions.FromDuration(TimeSpan.FromMinutes(5));
                await _cache.SetAsync(key, value, options, cancellationToken);

                _logger?.LogDebug("Write-Behind: Cached value for key: {Key}", key);

                // Queue for background write
                _writeQueue.Enqueue(new PendingWrite<T>(key, value));
                _logger?.LogDebug("Write-Behind: Queued write for key: {Key} (Queue size: {QueueSize})", key, _writeQueue.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in Write-Behind cache for key: {Key}", key);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            await _flushSemaphore.WaitAsync(cancellationToken);
            try
            {
                _isFlushing = true;
                _logger?.LogInformation("Write-Behind: Starting flush of {Count} pending writes", _writeQueue.Count);

                var processed = 0;
                var failed = 0;

                while (_writeQueue.TryDequeue(out var pendingWrite))
                {
                    try
                    {
                        await _saveToSource(pendingWrite.Key, pendingWrite.Value, cancellationToken);
                        processed++;
                        _logger?.LogDebug("Write-Behind: Flushed key: {Key}", pendingWrite.Key);
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        _logger?.LogError(ex, "Write-Behind: Failed to flush key: {Key}", pendingWrite.Key);
                        
                        // Optionally re-queue failed writes (could implement retry logic here)
                        // For now, we just log the error
                    }
                }

                _logger?.LogInformation("Write-Behind: Flush completed. Processed: {Processed}, Failed: {Failed}", processed, failed);
            }
            finally
            {
                _isFlushing = false;
                _flushSemaphore.Release();
            }
        }

        /// <summary>
        /// Processes pending writes in batches. This method is typically called by a background service.
        /// </summary>
        /// <param name="batchSize">The maximum number of writes to process in one batch.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ProcessPendingWritesAsync(int batchSize = 100, CancellationToken cancellationToken = default)
        {
            if (_isFlushing)
                return;

            if (_writeQueue.IsEmpty)
                return;

            await _flushSemaphore.WaitAsync(cancellationToken);
            try
            {
                _isFlushing = true;
                var batch = new System.Collections.Generic.List<PendingWrite<T>>();
                
                // Dequeue up to batchSize items
                while (batch.Count < batchSize && _writeQueue.TryDequeue(out var pendingWrite))
                {
                    batch.Add(pendingWrite);
                }

                if (batch.Count == 0)
                    return;

                _logger?.LogDebug("Write-Behind: Processing batch of {Count} writes", batch.Count);

                var tasks = batch.Select(async pendingWrite =>
                {
                    try
                    {
                        await _saveToSource(pendingWrite.Key, pendingWrite.Value, cancellationToken);
                        _logger?.LogDebug("Write-Behind: Processed key: {Key}", pendingWrite.Key);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Write-Behind: Failed to process key: {Key}", pendingWrite.Key);
                        // Re-queue failed writes
                        _writeQueue.Enqueue(pendingWrite);
                    }
                });

                await Task.WhenAll(tasks);
            }
            finally
            {
                _isFlushing = false;
                _flushSemaphore.Release();
            }
        }
    }
}

