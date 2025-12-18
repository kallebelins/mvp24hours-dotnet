//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Application.Contract.Cache
{
    /// <summary>
    /// Marks a query method as cacheable. When applied, query results will be cached
    /// using the configured cache provider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute enables declarative caching for query methods. The cache key
    /// is automatically generated based on the method name and parameters, or you
    /// can specify a custom key template.
    /// </para>
    /// <para>
    /// <strong>Key Template Placeholders:</strong>
    /// <list type="bullet">
    /// <item><c>{MethodName}</c> - The name of the decorated method</item>
    /// <item><c>{TypeName}</c> - The name of the containing type</item>
    /// <item><c>{0}</c>, <c>{1}</c>, etc. - Method parameter values by position</item>
    /// <item><c>{paramName}</c> - Method parameter value by name</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example usage:</strong>
    /// <code>
    /// [Cacheable(DurationSeconds = 300, Region = "Products")]
    /// public async Task&lt;IBusinessResult&lt;IList&lt;Product&gt;&gt;&gt; GetActiveProductsAsync()
    /// {
    ///     return await _repository.GetByAsync(p => p.IsActive).ToBusinessAsync();
    /// }
    /// 
    /// [Cacheable(KeyTemplate = "product_{id}", DurationSeconds = 600)]
    /// public async Task&lt;IBusinessResult&lt;Product&gt;&gt; GetByIdAsync(int id)
    /// {
    ///     return await _repository.GetByIdAsync(id).ToBusinessAsync();
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class CacheableAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the cache duration in seconds.
        /// </summary>
        /// <value>The duration in seconds. Default is 300 (5 minutes).</value>
        public int DurationSeconds { get; set; } = 300;

        /// <summary>
        /// Gets or sets a value indicating whether to use sliding expiration.
        /// </summary>
        /// <value>True to use sliding expiration; false for absolute expiration.</value>
        public bool UseSlidingExpiration { get; set; }

        /// <summary>
        /// Gets or sets the cache region for grouping related entries.
        /// When not specified, the containing type name is used.
        /// </summary>
        /// <value>The cache region name.</value>
        public string? Region { get; set; }

        /// <summary>
        /// Gets or sets the key template for cache key generation.
        /// When not specified, the key is auto-generated from method name and parameters.
        /// </summary>
        /// <value>The cache key template.</value>
        /// <example>
        /// "products_category_{categoryId}" or "user_{0}_orders_{1}"
        /// </example>
        public string? KeyTemplate { get; set; }

        /// <summary>
        /// Gets or sets the tags for cache invalidation grouping.
        /// Multiple tags can be specified separated by commas.
        /// </summary>
        /// <value>Comma-separated list of tags.</value>
        /// <example>"products,catalog" or "user,profile"</example>
        public string? Tags { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable cache stampede prevention.
        /// </summary>
        /// <value>True to enable stampede prevention; default is true.</value>
        public bool EnableStampedePrevention { get; set; } = true;

        /// <summary>
        /// Gets the cache duration as a <see cref="TimeSpan"/>.
        /// </summary>
        public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);

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
        /// Creates <see cref="QueryCacheEntryOptions"/> from this attribute's configuration.
        /// </summary>
        /// <param name="defaultRegion">The default region if none is specified.</param>
        /// <returns>Cache entry options configured from this attribute.</returns>
        public QueryCacheEntryOptions ToOptions(string? defaultRegion = null)
        {
            return new QueryCacheEntryOptions
            {
                Duration = Duration,
                UseSlidingExpiration = UseSlidingExpiration,
                Region = Region ?? defaultRegion,
                Tags = GetTags(),
                EnableStampedePrevention = EnableStampedePrevention
            };
        }
    }
}

