//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Caching.Attributes
{
    /// <summary>
    /// Marks a repository method as cacheable, enabling automatic caching of results.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When applied to a repository method, the result will be cached using the configured
    /// cache provider. The cache key is automatically generated based on the method name,
    /// entity type, and method parameters.
    /// </para>
    /// <para>
    /// <strong>Cache Key Generation:</strong>
    /// <code>
    /// Format: "{EntityType}:{MethodName}:{ParameterHash}"
    /// Example: "Customer:GetById:123" or "Product:GetBy:category=electronics"
    /// </code>
    /// </para>
    /// <para>
    /// <strong>Cache Invalidation:</strong>
    /// Cache entries are automatically invalidated when:
    /// <list type="bullet">
    /// <item>Modify/Remove operations are performed on the same entity type</item>
    /// <item>Manual invalidation via <see cref="CacheInvalidateAttribute"/></item>
    /// <item>Cache expiration time is reached</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class CustomerRepository : IRepository&lt;Customer&gt;
    /// {
    ///     [Cacheable(DurationSeconds = 300, Region = "Customers")]
    ///     public Customer GetById(int id)
    ///     {
    ///         // Result will be cached for 5 minutes
    ///         return dbContext.Customers.Find(id);
    ///     }
    ///     
    ///     [Cacheable(KeyTemplate = "customer_{id}", DurationSeconds = 600)]
    ///     public Customer GetCustomer(int id)
    ///     {
    ///         // Custom cache key template
    ///         return dbContext.Customers.Find(id);
    ///     }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class CacheableAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the cache duration in seconds.
        /// Default is 300 seconds (5 minutes).
        /// </summary>
        public int DurationSeconds { get; set; } = 300;

        /// <summary>
        /// Gets or sets the cache region/namespace for grouping related cache entries.
        /// Used for bulk invalidation by region.
        /// </summary>
        public string? Region { get; set; }

        /// <summary>
        /// Gets or sets a custom cache key template.
        /// Supports placeholders like {id}, {methodName}, {entityType}.
        /// </summary>
        public string? KeyTemplate { get; set; }

        /// <summary>
        /// Gets or sets cache tags for tag-based invalidation.
        /// Multiple tags can be specified separated by commas.
        /// </summary>
        public string? Tags { get; set; }

        /// <summary>
        /// Gets or sets whether to use sliding expiration instead of absolute expiration.
        /// Default is false (absolute expiration).
        /// </summary>
        public bool UseSlidingExpiration { get; set; } = false;
    }
}

