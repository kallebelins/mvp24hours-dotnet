//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Resilience.Contract;
using Mvp24Hours.Infrastructure.Resilience.Implementations;
using Mvp24Hours.Infrastructure.Resilience.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Resilience
{
    /// <summary>
    /// Resilient cache provider wrapper that adds circuit breaker, retry, and graceful degradation
    /// to any ICacheProvider implementation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider wraps an existing ICacheProvider and adds resilience patterns:
    /// <list type="bullet">
    /// <item><strong>Circuit Breaker:</strong> Prevents cascading failures when cache is unavailable</item>
    /// <item><strong>Retry:</strong> Automatically retries transient failures with exponential backoff</item>
    /// <item><strong>Graceful Degradation:</strong> Returns null/default instead of throwing when cache fails</item>
    /// <item><strong>Fallback Strategy:</strong> Falls back to source or default value when cache unavailable</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Resilience Features:</strong>
    /// <list type="bullet">
    /// <item>Circuit breaker opens after configurable failure threshold</item>
    /// <item>Automatic retry with exponential backoff for transient errors</item>
    /// <item>Graceful degradation: cache misses don't break the application</item>
    /// <item>Fallback to source function or default value when cache unavailable</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register resilient cache provider
    /// services.AddSingleton&lt;ICacheProvider&gt;(sp =>
    /// {
    ///     var baseProvider = new DistributedCacheProvider(...);
    ///     var options = new CacheResilienceOptions
    ///     {
    ///         EnableCircuitBreaker = true,
    ///         EnableRetry = true,
    ///         EnableGracefulDegradation = true
    ///     };
    ///     return new ResilientCacheProvider(baseProvider, options, logger);
    /// });
    /// 
    /// // Use with fallback
    /// var value = await cache.GetOrSetAsync("key", 
    ///     async () => await LoadFromSourceAsync(), // Fallback to source
    ///     TimeSpan.FromMinutes(5));
    /// </code>
    /// </example>
    public class ResilientCacheProvider : ICacheProvider
    {
        private readonly ICacheProvider _innerProvider;
        private readonly CacheResilienceOptions _options;
        private readonly ILogger<ResilientCacheProvider>? _logger;
        private readonly ICircuitBreaker<object?>? _circuitBreaker;
        private readonly ICircuitBreaker? _circuitBreakerVoid;

        /// <summary>
        /// Creates a new instance of ResilientCacheProvider.
        /// </summary>
        /// <param name="innerProvider">The underlying cache provider to wrap.</param>
        /// <param name="options">Resilience options (null uses defaults).</param>
        /// <param name="logger">Optional logger.</param>
        public ResilientCacheProvider(
            ICacheProvider innerProvider,
            CacheResilienceOptions? options = null,
            ILogger<ResilientCacheProvider>? logger = null)
        {
            _innerProvider = innerProvider ?? throw new ArgumentNullException(nameof(innerProvider));
            _options = options ?? new CacheResilienceOptions();
            _logger = logger;

            // Initialize circuit breaker if enabled
            if (_options.EnableCircuitBreaker)
            {
                var cbOptions = _options.CircuitBreaker;
                cbOptions.ShouldCountAsFailure ??= IsTransientException;
                cbOptions.OnBreak = (info) =>
                {
                    _options.OnCircuitBreakerOpen?.Invoke(info.OperationName);
                    _logger?.LogWarning(
                        "[Cache] Circuit breaker opened for cache operations. Reason: {Reason}",
                        info.Reason);
                };
                cbOptions.OnReset = (info) =>
                {
                    _logger?.LogInformation(
                        "[Cache] Circuit breaker reset for cache operations.");
                };

                _circuitBreaker = new CircuitBreaker<object?>(cbOptions, "CacheOperation", _logger);
                _circuitBreakerVoid = new CircuitBreaker(cbOptions, "CacheOperation", _logger);
            }
        }

        /// <inheritdoc />
        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            try
            {
                if (_options.EnableCircuitBreaker && _circuitBreaker != null)
                {
                    var result = await _circuitBreaker.ExecuteAsync(
                        async ct => (object?)await ExecuteWithRetryAsync(
                            async () => await _innerProvider.GetAsync<T>(key, ct),
                            key,
                            "GetAsync",
                            ct),
                        cancellationToken);
                    return result as T;
                }

                return await ExecuteWithRetryAsync(
                    async () => await _innerProvider.GetAsync<T>(key, cancellationToken),
                    key,
                    "GetAsync",
                    cancellationToken);
            }
            catch (Exception ex) when (_options.EnableGracefulDegradation)
            {
                HandleFailure(key, "GetAsync", ex);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            try
            {
                if (_options.EnableCircuitBreaker && _circuitBreaker != null)
                {
                    var result = await _circuitBreaker.ExecuteAsync(
                        async ct => (object?)await ExecuteWithRetryAsync(
                            async () => await _innerProvider.GetStringAsync(key, ct),
                            key,
                            "GetStringAsync",
                            ct),
                        cancellationToken);
                    return result as string;
                }

                return await ExecuteWithRetryAsync(
                    async () => await _innerProvider.GetStringAsync(key, cancellationToken),
                    key,
                    "GetStringAsync",
                    cancellationToken);
            }
            catch (Exception ex) when (_options.EnableGracefulDegradation)
            {
                HandleFailure(key, "GetStringAsync", ex);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            try
            {
                if (_options.EnableCircuitBreaker && _circuitBreakerVoid != null)
                {
                    await _circuitBreakerVoid.ExecuteAsync(
                        async ct => await ExecuteWithRetryAsync(
                            async () =>
                            {
                                await _innerProvider.SetAsync(key, value, options, ct);
                                return Task.CompletedTask;
                            },
                            key,
                            "SetAsync",
                            ct),
                        cancellationToken);
                    return;
                }

                await ExecuteWithRetryAsync(
                    async () =>
                    {
                        await _innerProvider.SetAsync(key, value, options, cancellationToken);
                        return Task.CompletedTask;
                    },
                    key,
                    "SetAsync",
                    cancellationToken);
            }
            catch (Exception ex) when (_options.EnableGracefulDegradation)
            {
                HandleFailure(key, "SetAsync", ex);
                // Don't throw - graceful degradation means we silently fail on set
            }
        }

        /// <inheritdoc />
        public async Task SetStringAsync(string key, string value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            try
            {
                if (_options.EnableCircuitBreaker && _circuitBreakerVoid != null)
                {
                    await _circuitBreakerVoid.ExecuteAsync(
                        async ct => await ExecuteWithRetryAsync(
                            async () =>
                            {
                                await _innerProvider.SetStringAsync(key, value, options, ct);
                                return Task.CompletedTask;
                            },
                            key,
                            "SetStringAsync",
                            ct),
                        cancellationToken);
                    return;
                }

                await ExecuteWithRetryAsync(
                    async () =>
                    {
                        await _innerProvider.SetStringAsync(key, value, options, cancellationToken);
                        return Task.CompletedTask;
                    },
                    key,
                    "SetStringAsync",
                    cancellationToken);
            }
            catch (Exception ex) when (_options.EnableGracefulDegradation)
            {
                HandleFailure(key, "SetStringAsync", ex);
                // Don't throw - graceful degradation means we silently fail on set
            }
        }

        /// <inheritdoc />
        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            try
            {
                if (_options.EnableCircuitBreaker && _circuitBreakerVoid != null)
                {
                    await _circuitBreakerVoid.ExecuteAsync(
                        async ct => await ExecuteWithRetryAsync(
                            async () =>
                            {
                                await _innerProvider.RemoveAsync(key, ct);
                                return Task.CompletedTask;
                            },
                            key,
                            "RemoveAsync",
                            ct),
                        cancellationToken);
                    return;
                }

                await ExecuteWithRetryAsync(
                    async () =>
                    {
                        await _innerProvider.RemoveAsync(key, cancellationToken);
                        return Task.CompletedTask;
                    },
                    key,
                    "RemoveAsync",
                    cancellationToken);
            }
            catch (Exception ex) when (_options.EnableGracefulDegradation)
            {
                HandleFailure(key, "RemoveAsync", ex);
                // Don't throw - graceful degradation means we silently fail on remove
            }
        }

        /// <inheritdoc />
        public async Task RemoveManyAsync(string[] keys, CancellationToken cancellationToken = default)
        {
            if (keys == null || keys.Length == 0)
                return;

            var tasks = keys
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(key => RemoveAsync(key, cancellationToken));

            await Task.WhenAll(tasks);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            try
            {
                if (_options.EnableCircuitBreaker && _circuitBreaker != null)
                {
                    var result = await _circuitBreaker.ExecuteAsync(
                        async ct => (object?)await ExecuteWithRetryAsync(
                            async () => await _innerProvider.ExistsAsync(key, ct),
                            key,
                            "ExistsAsync",
                            ct),
                        cancellationToken);
                    return result as bool? ?? false;
                }

                return await ExecuteWithRetryAsync(
                    async () => await _innerProvider.ExistsAsync(key, cancellationToken),
                    key,
                    "ExistsAsync",
                    cancellationToken);
            }
            catch (Exception ex) when (_options.EnableGracefulDegradation)
            {
                HandleFailure(key, "ExistsAsync", ex);
                return false; // Assume doesn't exist on failure
            }
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, T>> GetManyAsync<T>(string[] keys, CancellationToken cancellationToken = default) where T : class
        {
            if (keys == null || keys.Length == 0)
                return new Dictionary<string, T>();

            var result = new Dictionary<string, T>();

            var tasks = keys
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(async key =>
                {
                    var value = await GetAsync<T>(key, cancellationToken);
                    return new { Key = key, Value = value };
                });

            var results = await Task.WhenAll(tasks);

            foreach (var item in results)
            {
                if (item.Value != null)
                {
                    result[item.Key] = item.Value;
                }
            }

            return result;
        }

        /// <inheritdoc />
        public async Task SetManyAsync<T>(Dictionary<string, T> values, CacheEntryOptions? options = null, CancellationToken cancellationToken = default) where T : class
        {
            if (values == null || values.Count == 0)
                return;

            var tasks = values.Select(kvp => SetAsync(kvp.Key, kvp.Value, options, cancellationToken));
            await Task.WhenAll(tasks);
        }

        /// <inheritdoc />
        public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            try
            {
                if (_options.EnableCircuitBreaker && _circuitBreakerVoid != null)
                {
                    await _circuitBreakerVoid.ExecuteAsync(
                        async ct => await ExecuteWithRetryAsync(
                            async () =>
                            {
                                await _innerProvider.RefreshAsync(key, ct);
                                return Task.CompletedTask;
                            },
                            key,
                            "RefreshAsync",
                            ct),
                        cancellationToken);
                    return;
                }

                await ExecuteWithRetryAsync(
                    async () =>
                    {
                        await _innerProvider.RefreshAsync(key, cancellationToken);
                        return Task.CompletedTask;
                    },
                    key,
                    "RefreshAsync",
                    cancellationToken);
            }
            catch (Exception ex) when (_options.EnableGracefulDegradation)
            {
                HandleFailure(key, "RefreshAsync", ex);
                // Don't throw - graceful degradation
            }
        }

        #region Private Helpers

        private async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> operation,
            string key,
            string operationName,
            CancellationToken cancellationToken)
        {
            if (!_options.EnableRetry)
            {
                return await operation();
            }

            Exception? lastException = null;
            var maxAttempts = _options.MaxRetries + 1; // Initial attempt + retries

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    if (attempt > 1)
                    {
                        var delay = CalculateRetryDelay(attempt);
                        _logger?.LogDebug(
                            "[Cache] Retry attempt {Attempt}/{MaxAttempts} for {Operation} on key '{Key}' after {Delay}ms",
                            attempt,
                            maxAttempts,
                            operationName,
                            key,
                            delay.TotalMilliseconds);

                        await Task.Delay(delay, cancellationToken);
                    }

                    return await operation();
                }
                catch (Exception ex) when (attempt < maxAttempts && ShouldRetryException(ex))
                {
                    lastException = ex;
                    _logger?.LogWarning(
                        ex,
                        "[Cache] Transient failure on {Operation} for key '{Key}' (attempt {Attempt}/{MaxAttempts})",
                        operationName,
                        key,
                        attempt,
                        maxAttempts);
                }
            }

            _logger?.LogError(
                lastException,
                "[Cache] All {MaxAttempts} retry attempts failed for {Operation} on key '{Key}'",
                maxAttempts,
                operationName,
                key);

            throw lastException!;
        }

        private TimeSpan CalculateRetryDelay(int attempt)
        {
            var delay = _options.UseExponentialBackoff
                ? TimeSpan.FromMilliseconds(_options.RetryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1))
                : _options.RetryDelay;

            // Cap at max delay
            return delay > _options.MaxRetryDelay ? _options.MaxRetryDelay : delay;
        }

        private bool ShouldRetryException(Exception exception)
        {
            if (_options.ShouldRetry != null)
            {
                return _options.ShouldRetry(exception);
            }

            // Default: retry on transient exceptions
            return IsTransientException(exception);
        }

        private static bool IsTransientException(Exception exception)
        {
            return exception is TimeoutException ||
                   exception is System.IO.IOException ||
                   exception is System.Net.Sockets.SocketException ||
                   (exception.InnerException != null && IsTransientException(exception.InnerException));
        }

        private void HandleFailure(string key, string operationName, Exception exception)
        {
            if (_options.LogFailures)
            {
                _logger?.LogWarning(
                    exception,
                    "[Cache] Graceful degradation: {Operation} failed for key '{Key}'. Returning null/default.",
                    operationName,
                    key);
            }

            _options.OnFallback?.Invoke(key, exception);
        }

        #endregion
    }
}

