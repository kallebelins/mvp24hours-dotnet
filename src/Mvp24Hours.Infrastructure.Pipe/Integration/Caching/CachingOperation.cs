//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Typed;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Integration.Caching
{
    /// <summary>
    /// A typed operation that wraps another operation and caches its results.
    /// </summary>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <typeparam name="TOutput">The output type.</typeparam>
    public class CachingOperation<TInput, TOutput> : ITypedOperationAsync<TInput, TOutput>
    {
        private readonly ITypedOperationAsync<TInput, TOutput> _innerOperation;
        private readonly IDistributedCache _cache;
        private readonly Func<TInput, string> _keyGenerator;
        private readonly ILogger<CachingOperation<TInput, TOutput>>? _logger;
        private readonly CacheOperationOptions _options;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Creates a new caching operation.
        /// </summary>
        /// <param name="innerOperation">The operation to wrap.</param>
        /// <param name="cache">The distributed cache.</param>
        /// <param name="keyGenerator">Function to generate cache keys from input.</param>
        /// <param name="logger">Optional logger.</param>
        /// <param name="options">Optional cache options.</param>
        public CachingOperation(
            ITypedOperationAsync<TInput, TOutput> innerOperation,
            IDistributedCache cache,
            Func<TInput, string> keyGenerator,
            ILogger<CachingOperation<TInput, TOutput>>? logger = null,
            CacheOperationOptions? options = null)
        {
            _innerOperation = innerOperation ?? throw new ArgumentNullException(nameof(innerOperation));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
            _logger = logger;
            _options = options ?? new CacheOperationOptions();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };
        }

        /// <inheritdoc/>
        public bool IsRequired => _innerOperation.IsRequired;

        /// <summary>
        /// Gets or sets the absolute expiration for this operation's cache entries.
        /// </summary>
        public TimeSpan? AbsoluteExpiration { get; set; }

        /// <summary>
        /// Gets or sets the sliding expiration for this operation's cache entries.
        /// </summary>
        public TimeSpan? SlidingExpiration { get; set; }

        /// <inheritdoc/>
        public async Task<IOperationResult<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default)
        {
            var cacheKey = GenerateCacheKey(input);
            
            _logger?.LogDebug("CachingOperation: Checking cache. Key: {CacheKey}", cacheKey);

            // Try to get from cache
            try
            {
                var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
                if (cachedData != null)
                {
                    _logger?.LogDebug("CachingOperation: Cache hit. Key: {CacheKey}", cacheKey);
                    
                    var cachedResult = DeserializeResult(cachedData);
                    if (cachedResult != null)
                    {
                        return cachedResult;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error reading from cache for key {CacheKey}", cacheKey);
            }

            _logger?.LogDebug("CachingOperation: Cache miss. Key: {CacheKey}", cacheKey);

            // Execute inner operation
            var result = await _innerOperation.ExecuteAsync(input, cancellationToken);

            // Cache result if successful (or if caching failures is enabled)
            if (result.IsSuccess || _options.CacheFailedResults)
            {
                try
                {
                    var serialized = SerializeResult(result);
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = AbsoluteExpiration ?? _options.DefaultAbsoluteExpiration,
                        SlidingExpiration = SlidingExpiration ?? _options.DefaultSlidingExpiration
                    };

                    await _cache.SetStringAsync(cacheKey, serialized, cacheOptions, cancellationToken);
                    
                    _logger?.LogDebug("CachingOperation: Cached result. Key: {CacheKey}", cacheKey);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error writing to cache for key {CacheKey}", cacheKey);
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public Task RollbackAsync(TInput input, CancellationToken cancellationToken = default)
        {
            return _innerOperation.RollbackAsync(input, cancellationToken);
        }

        /// <summary>
        /// Invalidates the cache entry for the given input.
        /// </summary>
        /// <param name="input">The input to invalidate cache for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task InvalidateCacheAsync(TInput input, CancellationToken cancellationToken = default)
        {
            var cacheKey = GenerateCacheKey(input);
            
            try
            {
                await _cache.RemoveAsync(cacheKey, cancellationToken);
                _logger?.LogDebug("Invalidated cache for key {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error invalidating cache for key {CacheKey}", cacheKey);
            }
        }

        private string GenerateCacheKey(TInput input)
        {
            var key = _keyGenerator(input);
            return $"{_options.CacheKeyPrefix}{typeof(TInput).Name}:{key}";
        }

        private string SerializeResult(IOperationResult<TOutput> result)
        {
            var cacheEntry = new CachedOperationResult<TOutput>
            {
                IsSuccess = result.IsSuccess,
                Value = result.Value,
                ErrorMessage = result.ErrorMessage
            };

            return JsonSerializer.Serialize(cacheEntry, _jsonOptions);
        }

        private IOperationResult<TOutput>? DeserializeResult(string data)
        {
            try
            {
                var cacheEntry = JsonSerializer.Deserialize<CachedOperationResult<TOutput>>(data, _jsonOptions);
                if (cacheEntry == null)
                    return null;

                return cacheEntry.IsSuccess
                    ? OperationResult<TOutput>.Success(cacheEntry.Value!)
                    : OperationResult<TOutput>.Failure(cacheEntry.ErrorMessage ?? "Cached failure");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error deserializing cached result");
                return null;
            }
        }

        private class CachedOperationResult<T>
        {
            public bool IsSuccess { get; set; }
            public T? Value { get; set; }
            public string? ErrorMessage { get; set; }
        }
    }
}

