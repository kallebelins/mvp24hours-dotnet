//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Integration.Caching
{
    /// <summary>
    /// Pipeline middleware that caches pipeline message state using IDistributedCache.
    /// Note: This middleware caches based on the message token.
    /// </summary>
    public class CacheResultsMiddleware : IPipelineMiddleware
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<CacheResultsMiddleware>? _logger;
        private readonly CacheOperationOptions _options;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Creates a new cache results middleware.
        /// </summary>
        /// <param name="cache">The distributed cache.</param>
        /// <param name="logger">Optional logger.</param>
        /// <param name="options">Optional cache options.</param>
        public CacheResultsMiddleware(
            IDistributedCache cache,
            ILogger<CacheResultsMiddleware>? logger = null,
            CacheOperationOptions? options = null)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger;
            _options = options ?? new CacheOperationOptions();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };
        }

        /// <inheritdoc/>
        public int Order => 100; // Execute after other middleware

        /// <inheritdoc/>
        public async Task ExecuteAsync(
            IPipelineMessage message,
            Func<Task> next,
            CancellationToken cancellationToken = default)
        {
            // Check if message has a token for caching
            var cacheKey = GetCacheKey(message);
            if (string.IsNullOrEmpty(cacheKey))
            {
                await next();
                return;
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "cache-middleware-check", $"Key: {cacheKey}");

            // Try to get from cache
            try
            {
                var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
                if (cachedData != null)
                {
                    _logger?.LogDebug("Cache hit for key {CacheKey}", cacheKey);
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "cache-middleware-hit", $"Key: {cacheKey}");
                    
                    var restored = RestoreMessageState(cachedData, message);
                    if (restored)
                    {
                        return; // Skip execution, use cached state
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error reading from cache for key {CacheKey}", cacheKey);
            }

            _logger?.LogDebug("Cache miss for key {CacheKey}", cacheKey);
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "cache-middleware-miss", $"Key: {cacheKey}");

            // Execute pipeline
            await next();

            // Cache result if not locked (or if caching failures is enabled)
            if (!message.IsLocked || _options.CacheFailedResults)
            {
                try
                {
                    var serialized = SerializeMessageState(message);
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = _options.DefaultAbsoluteExpiration,
                        SlidingExpiration = _options.DefaultSlidingExpiration
                    };

                    await _cache.SetStringAsync(cacheKey, serialized, cacheOptions, cancellationToken);
                    
                    _logger?.LogDebug("Cached result for key {CacheKey}", cacheKey);
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "cache-middleware-stored", $"Key: {cacheKey}");
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error writing to cache for key {CacheKey}", cacheKey);
                }
            }
        }

        private string? GetCacheKey(IPipelineMessage message)
        {
            if (message.Token != null)
            {
                return $"{_options.CacheKeyPrefix}msg:{message.Token}";
            }

            return null;
        }

        private string SerializeMessageState(IPipelineMessage message)
        {
            var cacheEntry = new CacheEntry
            {
                IsLocked = message.IsLocked,
                IsFaulty = message.IsFaulty,
                Token = message.Token?.ToString()
            };

            return JsonSerializer.Serialize(cacheEntry, _jsonOptions);
        }

        private bool RestoreMessageState(string data, IPipelineMessage message)
        {
            try
            {
                var cacheEntry = JsonSerializer.Deserialize<CacheEntry>(data, _jsonOptions);
                if (cacheEntry == null)
                    return false;

                if (cacheEntry.IsLocked)
                {
                    message.SetLock();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error deserializing cached message");
                return false;
            }
        }

        private class CacheEntry
        {
            public bool IsLocked { get; set; }
            public bool IsFaulty { get; set; }
            public string? Token { get; set; }
        }
    }

    /// <summary>
    /// Attribute to mark operations for result caching.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CacheResultAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the cache duration in seconds.
        /// </summary>
        public int DurationSeconds { get; set; } = 300;

        /// <summary>
        /// Gets or sets the cache key pattern.
        /// Use {PropertyName} placeholders for message properties.
        /// </summary>
        public string? KeyPattern { get; set; }

        /// <summary>
        /// Gets or sets whether to use the message token as the cache key.
        /// </summary>
        public bool UseTokenAsKey { get; set; } = true;

        /// <summary>
        /// Generates a cache key for the given message.
        /// </summary>
        /// <param name="message">The pipeline message.</param>
        /// <returns>The cache key.</returns>
        public string? GenerateKey(IPipelineMessage message)
        {
            if (UseTokenAsKey && message.Token != null)
            {
                return message.Token.ToString();
            }

            return KeyPattern;
        }
    }
}
