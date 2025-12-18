//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Application.Contract.Cache
{
    /// <summary>
    /// Marks a command method to invalidate cache entries upon successful execution.
    /// Use this to ensure cache consistency when data is modified.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute enables automatic cache invalidation after command operations.
    /// You can invalidate by:
    /// <list type="bullet">
    /// <item><strong>Region:</strong> Invalidate all entries in a cache region</item>
    /// <item><strong>Tags:</strong> Invalidate entries with specific tags</item>
    /// <item><strong>Keys:</strong> Invalidate specific cache keys</item>
    /// <item><strong>Pattern:</strong> Invalidate keys matching a pattern</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example usage:</strong>
    /// <code>
    /// // Invalidate entire region when adding a product
    /// [CacheInvalidate(Region = "Products")]
    /// public async Task&lt;IBusinessResult&lt;int&gt;&gt; AddProductAsync(Product product)
    /// {
    ///     await _repository.AddAsync(product);
    ///     return await _unitOfWork.SaveChangesAsync().ToBusinessAsync();
    /// }
    /// 
    /// // Invalidate specific key pattern when updating
    /// [CacheInvalidate(KeyPattern = "product_{id}*")]
    /// public async Task&lt;IBusinessResult&lt;int&gt;&gt; UpdateProductAsync(int id, Product product)
    /// {
    ///     await _repository.ModifyAsync(product);
    ///     return await _unitOfWork.SaveChangesAsync().ToBusinessAsync();
    /// }
    /// 
    /// // Invalidate by tags
    /// [CacheInvalidate(Tags = "products,catalog")]
    /// public async Task&lt;IBusinessResult&lt;int&gt;&gt; DeleteProductAsync(int id)
    /// {
    ///     await _repository.RemoveByIdAsync(id);
    ///     return await _unitOfWork.SaveChangesAsync().ToBusinessAsync();
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class CacheInvalidateAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the cache region to invalidate.
        /// All entries in this region will be removed from cache.
        /// </summary>
        /// <value>The cache region name to invalidate.</value>
        public string? Region { get; set; }

        /// <summary>
        /// Gets or sets the tags to invalidate.
        /// All entries with any of these tags will be removed.
        /// Multiple tags can be specified separated by commas.
        /// </summary>
        /// <value>Comma-separated list of tags.</value>
        public string? Tags { get; set; }

        /// <summary>
        /// Gets or sets specific cache keys to invalidate.
        /// Supports parameter interpolation using placeholders.
        /// Multiple keys can be specified separated by commas.
        /// </summary>
        /// <value>Comma-separated list of cache keys.</value>
        /// <example>"product_{id}" or "user_{userId},userOrders_{userId}"</example>
        public string? Keys { get; set; }

        /// <summary>
        /// Gets or sets a pattern for key-based invalidation.
        /// Supports wildcard characters (* and ?).
        /// </summary>
        /// <value>The pattern to match cache keys.</value>
        /// <example>"products_*" or "user_{userId}_*"</example>
        public string? KeyPattern { get; set; }

        /// <summary>
        /// Gets or sets when to perform the invalidation.
        /// </summary>
        /// <value>The timing of cache invalidation. Default is AfterSuccess.</value>
        public CacheInvalidationTiming Timing { get; set; } = CacheInvalidationTiming.AfterSuccess;

        /// <summary>
        /// Gets or sets a value indicating whether to invalidate all caches for the entity type.
        /// When true, invalidates all entries in the region matching the entity type name.
        /// </summary>
        /// <value>True to invalidate all entity caches; default is false.</value>
        public bool InvalidateAll { get; set; }

        /// <summary>
        /// Gets the tags as an array.
        /// </summary>
        /// <returns>An array of tag strings, or an empty array if no tags are specified.</returns>
        public string[] GetTags()
        {
            if (string.IsNullOrWhiteSpace(Tags))
            {
                return Array.Empty<string>();
            }
            return Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        /// <summary>
        /// Gets the keys as an array.
        /// </summary>
        /// <returns>An array of key strings, or an empty array if no keys are specified.</returns>
        public string[] GetKeys()
        {
            if (string.IsNullOrWhiteSpace(Keys))
            {
                return Array.Empty<string>();
            }
            return Keys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }

    /// <summary>
    /// Specifies when cache invalidation should occur.
    /// </summary>
    public enum CacheInvalidationTiming
    {
        /// <summary>
        /// Invalidate cache before executing the command.
        /// Use when you need to ensure fresh data is fetched after the command.
        /// </summary>
        Before,

        /// <summary>
        /// Invalidate cache only after successful command execution.
        /// This is the default and safest option.
        /// </summary>
        AfterSuccess,

        /// <summary>
        /// Always invalidate cache after command execution, regardless of success/failure.
        /// Use with caution as failed commands may leave cache in inconsistent state.
        /// </summary>
        Always
    }
}

