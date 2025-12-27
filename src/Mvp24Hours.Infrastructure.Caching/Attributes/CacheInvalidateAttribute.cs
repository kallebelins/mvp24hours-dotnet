//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Caching.Attributes
{
    /// <summary>
    /// Marks a repository method to invalidate cache entries after execution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When applied to a repository method (typically Modify, Remove, Add operations),
    /// cache entries matching the specified criteria will be invalidated after the method executes.
    /// </para>
    /// <para>
    /// <strong>Invalidation Strategies:</strong>
    /// <list type="bullet">
    /// <item><strong>By Region:</strong> Invalidates all cache entries in a specific region</item>
    /// <item><strong>By Key Pattern:</strong> Invalidates entries matching a key pattern (supports wildcards)</item>
    /// <item><strong>By Tags:</strong> Invalidates entries with specific tags</item>
    /// <item><strong>By Entity Type:</strong> Automatically invalidates all entries for the entity type</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class ProductRepository : IRepository&lt;Product&gt;
    /// {
    ///     [CacheInvalidate(Region = "Products")]
    ///     public void Modify(Product product)
    ///     {
    ///         // Invalidates all cache entries in "Products" region
    ///         dbContext.Products.Update(product);
    ///     }
    ///     
    ///     [CacheInvalidate(KeyPattern = "product_{id}*")]
    ///     public void RemoveById(int id)
    ///     {
    ///         // Invalidates cache entries matching pattern "product_{id}*"
    ///         var product = GetById(id);
    ///         if (product != null) Remove(product);
    ///     }
    ///     
    ///     [CacheInvalidate(Tags = "products,catalog")]
    ///     public void BulkUpdate(IList&lt;Product&gt; products)
    ///     {
    ///         // Invalidates entries tagged with "products" or "catalog"
    ///         dbContext.Products.UpdateRange(products);
    ///     }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class CacheInvalidateAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the cache region to invalidate.
        /// If specified, all cache entries in this region will be invalidated.
        /// </summary>
        public string? Region { get; set; }

        /// <summary>
        /// Gets or sets a key pattern for selective invalidation.
        /// Supports wildcards: * (matches any characters), ? (matches single character).
        /// </summary>
        /// <example>
        /// <code>
        /// KeyPattern = "product_*" // Invalidates all keys starting with "product_"
        /// KeyPattern = "customer_{id}" // Invalidates specific customer key
        /// </code>
        /// </example>
        public string? KeyPattern { get; set; }

        /// <summary>
        /// Gets or sets cache tags for tag-based invalidation.
        /// Multiple tags can be specified separated by commas.
        /// </summary>
        public string? Tags { get; set; }

        /// <summary>
        /// Gets or sets whether to invalidate all cache entries for the entity type.
        /// Default is true - automatically invalidates entity-specific cache entries.
        /// </summary>
        public bool InvalidateEntityType { get; set; } = true;
    }
}

