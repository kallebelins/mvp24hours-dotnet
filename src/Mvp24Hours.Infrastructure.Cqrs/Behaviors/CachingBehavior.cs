//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using System.Text.Json;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Marker interface for queries that should be cached.
/// Apply this interface to queries that are safe to cache.
/// </summary>
/// <remarks>
/// <para>
/// <strong>When to use:</strong>
/// <list type="bullet">
/// <item>Read-only queries that don't modify state</item>
/// <item>Queries with predictable, stable results</item>
/// <item>Queries that are called frequently</item>
/// </list>
/// </para>
/// <para>
/// <strong>When NOT to use:</strong>
/// <list type="bullet">
/// <item>Queries that need real-time data</item>
/// <item>Queries with user-specific or sensitive data</item>
/// <item>Queries with rapidly changing results</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class GetProductByIdQuery : IMediatorQuery&lt;Product&gt;, ICacheable
/// {
///     public int ProductId { get; init; }
///     
///     // Cache for 5 minutes
///     public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
///     
///     // Optional: Custom cache key
///     public string CacheKey => $"product:{ProductId}";
/// }
/// </code>
/// </example>
public interface ICacheable
{
    /// <summary>
    /// Gets the cache key for this request.
    /// If null, a key will be generated from the request type and properties.
    /// </summary>
    string? CacheKey => null;

    /// <summary>
    /// Gets the cache duration for this request.
    /// If null, the default duration from options will be used.
    /// </summary>
    TimeSpan? CacheDuration => null;
}

/// <summary>
/// Pipeline behavior that caches query responses.
/// Only applies to requests that implement <see cref="ICacheable"/>.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior uses <see cref="IDistributedCache"/> for caching, allowing
/// integration with various cache providers (Memory, Redis, SQL Server, etc.).
/// </para>
/// <para>
/// <strong>Cache Key Generation:</strong>
/// If the request doesn't provide a custom cache key, one is generated from:
/// <list type="bullet">
/// <item>The request type name</item>
/// <item>The JSON serialization of the request properties</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(CachingBehavior&lt;,&gt;));
/// 
/// // Configure cache
/// services.AddDistributedMemoryCache();
/// // Or for Redis:
/// // services.AddStackExchangeRedisCache(...);
/// </code>
/// </example>
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>>? _logger;
    private readonly TimeSpan _defaultCacheDuration;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Creates a new instance of the CachingBehavior.
    /// </summary>
    /// <param name="cache">The distributed cache.</param>
    /// <param name="logger">Optional logger for recording cache operations.</param>
    /// <param name="defaultCacheDurationMinutes">Default cache duration in minutes (default: 5).</param>
    public CachingBehavior(
        IDistributedCache cache,
        ILogger<CachingBehavior<TRequest, TResponse>>? logger = null,
        int defaultCacheDurationMinutes = 5)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger;
        _defaultCacheDuration = TimeSpan.FromMinutes(defaultCacheDurationMinutes);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Only cache if the request implements ICacheable
        if (request is not ICacheable cacheable)
        {
            return await next();
        }

        var cacheKey = GetCacheKey(request, cacheable);
        var requestName = typeof(TRequest).Name;

        // Try to get from cache
        try
        {
            var cachedValue = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedValue))
            {
                _logger?.LogDebug(
                    "[Cache] HIT for {RequestName} (Key: {CacheKey})",
                    requestName,
                    cacheKey);

                var cachedResponse = JsonSerializer.Deserialize<TResponse>(cachedValue, _jsonOptions);
                if (cachedResponse != null)
                {
                    return cachedResponse;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                ex,
                "[Cache] Error reading from cache for {RequestName}: {Message}",
                requestName,
                ex.Message);
        }

        _logger?.LogDebug(
            "[Cache] MISS for {RequestName} (Key: {CacheKey})",
            requestName,
            cacheKey);

        // Execute the handler
        var response = await next();

        // Cache the response
        try
        {
            if (response != null)
            {
                var duration = cacheable.CacheDuration ?? _defaultCacheDuration;
                var serialized = JsonSerializer.Serialize(response, _jsonOptions);

                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = duration
                };

                await _cache.SetStringAsync(cacheKey, serialized, options, cancellationToken);

                _logger?.LogDebug(
                    "[Cache] SET for {RequestName} (Key: {CacheKey}, Duration: {Duration})",
                    requestName,
                    cacheKey,
                    duration);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                ex,
                "[Cache] Error writing to cache for {RequestName}: {Message}",
                requestName,
                ex.Message);
        }

        return response;
    }

    private string GetCacheKey(TRequest request, ICacheable cacheable)
    {
        if (!string.IsNullOrEmpty(cacheable.CacheKey))
        {
            return $"mediator:{cacheable.CacheKey}";
        }

        // Generate key from request type and properties
        var typeName = typeof(TRequest).Name;
        var requestHash = JsonSerializer.Serialize(request, _jsonOptions);
        var hashCode = requestHash.GetHashCode();

        return $"mediator:{typeName}:{hashCode}";
    }
}

/// <summary>
/// Marker interface for commands that should invalidate cache entries.
/// </summary>
/// <example>
/// <code>
/// public class UpdateProductCommand : IMediatorCommand&lt;Product&gt;, ICacheInvalidator
/// {
///     public int ProductId { get; init; }
///     public string Name { get; init; } = string.Empty;
///     
///     // Invalidate these cache keys after successful execution
///     public IEnumerable&lt;string&gt; CacheKeysToInvalidate => 
///         new[] { $"product:{ProductId}", "products:all" };
/// }
/// </code>
/// </example>
public interface ICacheInvalidator
{
    /// <summary>
    /// Gets the cache keys to invalidate after successful execution.
    /// </summary>
    IEnumerable<string> CacheKeysToInvalidate { get; }
}

/// <summary>
/// Pipeline behavior that invalidates cache entries after successful command execution.
/// Only applies to requests that implement <see cref="ICacheInvalidator"/>.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
public sealed class CacheInvalidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheInvalidationBehavior<TRequest, TResponse>>? _logger;

    /// <summary>
    /// Creates a new instance of the CacheInvalidationBehavior.
    /// </summary>
    /// <param name="cache">The distributed cache.</param>
    /// <param name="logger">Optional logger.</param>
    public CacheInvalidationBehavior(
        IDistributedCache cache,
        ILogger<CacheInvalidationBehavior<TRequest, TResponse>>? logger = null)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Execute the handler first
        var response = await next();

        // Only invalidate if the request implements ICacheInvalidator
        if (request is ICacheInvalidator invalidator)
        {
            var requestName = typeof(TRequest).Name;

            foreach (var key in invalidator.CacheKeysToInvalidate)
            {
                var fullKey = $"mediator:{key}";

                try
                {
                    await _cache.RemoveAsync(fullKey, cancellationToken);

                    _logger?.LogDebug(
                        "[Cache] INVALIDATED {Key} for {RequestName}",
                        fullKey,
                        requestName);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(
                        ex,
                        "[Cache] Error invalidating cache key {Key}: {Message}",
                        fullKey,
                        ex.Message);
                }
            }
        }

        return response;
    }
}

