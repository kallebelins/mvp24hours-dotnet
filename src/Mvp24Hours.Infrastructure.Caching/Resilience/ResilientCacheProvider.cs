//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Resilience.Native;
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
    /// Resilience is implemented via <see cref="NativeResiliencePipeline"/> /
    /// <see cref="NativeResiliencePipeline{TResult}"/> (Microsoft.Extensions.Resilience / Polly v8).
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
        private readonly INativeResiliencePipeline<object?>? _pipeline;
        private readonly INativeResiliencePipeline? _pipelineVoid;

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

            if (_options.EnableCircuitBreaker || _options.EnableRetry)
            {
                var nativeOptions = CreateNativeOptions(_options, _logger);
                _pipeline = new NativeResiliencePipeline<object?>(nativeOptions, _logger);
                _pipelineVoid = new NativeResiliencePipeline(nativeOptions, _logger);
            }
        }

        /// <inheritdoc />
        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            try
            {
                if (_pipeline != null)
                {
                    var result = await _pipeline.ExecuteTaskAsync(
                        async ct => (object?)await _innerProvider.GetAsync<T>(key, ct),
                        cancellationToken);
                    return result as T;
                }

                return await _innerProvider.GetAsync<T>(key, cancellationToken);
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
                if (_pipeline != null)
                {
                    var result = await _pipeline.ExecuteTaskAsync(
                        async ct => (object?)await _innerProvider.GetStringAsync(key, ct),
                        cancellationToken);
                    return result as string;
                }

                return await _innerProvider.GetStringAsync(key, cancellationToken);
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
                if (_pipelineVoid != null)
                {
                    await _pipelineVoid.ExecuteTaskAsync(
                        async ct => await _innerProvider.SetAsync(key, value, options, ct),
                        cancellationToken);
                    return;
                }

                await _innerProvider.SetAsync(key, value, options, cancellationToken);
            }
            catch (Exception ex) when (_options.EnableGracefulDegradation)
            {
                HandleFailure(key, "SetAsync", ex);
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
                if (_pipelineVoid != null)
                {
                    await _pipelineVoid.ExecuteTaskAsync(
                        async ct => await _innerProvider.SetStringAsync(key, value, options, ct),
                        cancellationToken);
                    return;
                }

                await _innerProvider.SetStringAsync(key, value, options, cancellationToken);
            }
            catch (Exception ex) when (_options.EnableGracefulDegradation)
            {
                HandleFailure(key, "SetStringAsync", ex);
            }
        }

        /// <inheritdoc />
        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            try
            {
                if (_pipelineVoid != null)
                {
                    await _pipelineVoid.ExecuteTaskAsync(
                        async ct => await _innerProvider.RemoveAsync(key, ct),
                        cancellationToken);
                    return;
                }

                await _innerProvider.RemoveAsync(key, cancellationToken);
            }
            catch (Exception ex) when (_options.EnableGracefulDegradation)
            {
                HandleFailure(key, "RemoveAsync", ex);
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
                if (_pipeline != null)
                {
                    var result = await _pipeline.ExecuteTaskAsync(
                        async ct => (object?)await _innerProvider.ExistsAsync(key, ct),
                        cancellationToken);
                    return result as bool? ?? false;
                }

                return await _innerProvider.ExistsAsync(key, cancellationToken);
            }
            catch (Exception ex) when (_options.EnableGracefulDegradation)
            {
                HandleFailure(key, "ExistsAsync", ex);
                return false;
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
                if (_pipelineVoid != null)
                {
                    await _pipelineVoid.ExecuteTaskAsync(
                        async ct => await _innerProvider.RefreshAsync(key, ct),
                        cancellationToken);
                    return;
                }

                await _innerProvider.RefreshAsync(key, cancellationToken);
            }
            catch (Exception ex) when (_options.EnableGracefulDegradation)
            {
                HandleFailure(key, "RefreshAsync", ex);
            }
        }

        #region Private Helpers

        private static NativeResilienceOptions CreateNativeOptions(
            CacheResilienceOptions options,
            ILogger? logger)
        {
            var cb = options.CircuitBreaker;
            var shouldCountAsFailure = options.ShouldCountAsFailure
                ?? cb.ShouldCountAsFailure
                ?? IsTransientException;

            return new NativeResilienceOptions
            {
                Name = "CacheOperation",
                EnableTimeout = false,
                EnableCircuitBreaker = options.EnableCircuitBreaker,
                CircuitBreakerFailureRatio = cb.FailureRatio,
                CircuitBreakerMinimumThroughput = cb.MinimumThroughput,
                CircuitBreakerSamplingDuration = cb.SamplingDuration,
                CircuitBreakerBreakDuration = cb.BreakDuration,
                ShouldHandleAsCircuitBreakerFailure = shouldCountAsFailure,
                OnCircuitBreakerOpen = _ =>
                {
                    options.OnCircuitBreakerOpen?.Invoke("CacheOperation");
                    logger?.LogWarning(
                        "[Cache] Circuit breaker opened for cache operations.");
                },
                OnCircuitBreakerReset = () =>
                {
                    logger?.LogInformation(
                        "[Cache] Circuit breaker reset for cache operations.");
                },
                EnableRetry = options.EnableRetry,
                RetryMaxAttempts = options.MaxRetries,
                RetryDelay = options.RetryDelay,
                RetryMaxDelay = options.MaxRetryDelay,
                RetryUseJitter = false,
                RetryBackoffType = options.UseExponentialBackoff
                    ? ResilienceBackoffType.Exponential
                    : ResilienceBackoffType.Constant,
                ShouldRetryOnException = options.ShouldRetry ?? IsTransientException,
                OnRetry = (ex, attempt, delay) =>
                {
                    logger?.LogWarning(
                        ex,
                        "[Cache] Retry attempt {Attempt} after {Delay}ms",
                        attempt,
                        delay.TotalMilliseconds);
                }
            };
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
