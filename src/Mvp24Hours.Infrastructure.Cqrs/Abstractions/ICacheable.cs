//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

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
