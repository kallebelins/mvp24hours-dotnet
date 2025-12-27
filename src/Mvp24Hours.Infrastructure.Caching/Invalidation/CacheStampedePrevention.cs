//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Invalidation
{
    /// <summary>
    /// Prevents cache stampede by coordinating concurrent cache misses using SemaphoreSlim.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation uses in-memory locks (SemaphoreSlim) to serialize concurrent
    /// cache misses for the same key. Only the first request executes the factory function,
    /// while others wait for the result.
    /// </para>
    /// </remarks>
    public class CacheStampedePrevention : ICacheStampedePrevention
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<object?>> _pendingTasks = new();
        private readonly ILogger<CacheStampedePrevention>? _logger;
        private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Creates a new instance of CacheStampedePrevention.
        /// </summary>
        /// <param name="logger">Optional logger.</param>
        public CacheStampedePrevention(ILogger<CacheStampedePrevention>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<T> ExecuteAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var timeoutValue = timeout ?? _defaultTimeout;
            var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            // Try to acquire the lock
            var lockAcquired = false;
            try
            {
                lockAcquired = await semaphore.WaitAsync(timeoutValue, cancellationToken);

                if (!lockAcquired)
                {
                    _logger?.LogWarning("Timeout waiting for lock on key {Key}", key);
                    throw new TimeoutException($"Timeout waiting for cache stampede lock on key: {key}");
                }

                // Check if there's already a pending task for this key
                if (_pendingTasks.TryGetValue(key, out var pendingTask))
                {
                    // Wait for the pending task to complete
                    try
                    {
                        var result = await pendingTask.Task.WaitAsync(timeoutValue, cancellationToken);
                        if (result is T typedResult)
                        {
                            return typedResult;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error waiting for pending task on key {Key}", key);
                        throw;
                    }
                }

                // Create a new task completion source for this execution
                var tcs = new TaskCompletionSource<object?>();
                _pendingTasks[key] = tcs;

                try
                {
                    // Execute the factory function
                    var result = await factory(cancellationToken);
                    tcs.SetResult(result);
                    return result;
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                    throw;
                }
                finally
                {
                    // Clean up
                    _pendingTasks.TryRemove(key, out _);
                }
            }
            finally
            {
                if (lockAcquired)
                {
                    semaphore.Release();

                    // Clean up semaphore if no longer needed (optional optimization)
                    if (semaphore.CurrentCount == 1 && _pendingTasks.ContainsKey(key) == false)
                    {
                        _locks.TryRemove(key, out _);
                        semaphore.Dispose();
                    }
                }
            }
        }
    }
}

