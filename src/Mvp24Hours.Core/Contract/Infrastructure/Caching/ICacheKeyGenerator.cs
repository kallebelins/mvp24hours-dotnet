//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.Contract.Infrastructure.Caching
{
    /// <summary>
    /// Interface for generating cache keys with different strategies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides a flexible way to generate cache keys using various strategies:
    /// <list type="bullet">
    /// <item>Prefix-based keys (namespace/region separation)</item>
    /// <item>Hash-based keys (for long/complex keys)</item>
    /// <item>Composite keys (multiple parts combined)</item>
    /// <item>Custom strategies via implementation</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register key generator
    /// services.AddSingleton&lt;ICacheKeyGenerator&gt;(new DefaultCacheKeyGenerator("MyApp"));
    /// 
    /// // Use in service
    /// public class MyService
    /// {
    ///     private readonly ICacheKeyGenerator _keyGenerator;
    ///     
    ///     public async Task&lt;T&gt; GetCachedAsync(int id)
    ///     {
    ///         var key = _keyGenerator.Generate("users", id.ToString());
    ///         // Use key for caching
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface ICacheKeyGenerator
    {
        /// <summary>
        /// Generates a cache key from multiple parts.
        /// </summary>
        /// <param name="parts">The parts to combine into a key.</param>
        /// <returns>The generated cache key.</returns>
        string Generate(params string[] parts);

        /// <summary>
        /// Generates a cache key with a prefix/namespace.
        /// </summary>
        /// <param name="prefix">The prefix/namespace for the key.</param>
        /// <param name="key">The base key.</param>
        /// <returns>The generated cache key with prefix.</returns>
        string GenerateWithPrefix(string prefix, string key);

        /// <summary>
        /// Generates a hash-based cache key for long or complex keys.
        /// </summary>
        /// <param name="key">The key to hash.</param>
        /// <returns>The hashed cache key.</returns>
        string GenerateHash(string key);

        /// <summary>
        /// Generates a cache key from an object by serializing it.
        /// </summary>
        /// <param name="prefix">The prefix/namespace.</param>
        /// <param name="obj">The object to generate a key from.</param>
        /// <returns>The generated cache key.</returns>
        string GenerateFromObject(string prefix, object obj);

        /// <summary>
        /// Gets or sets the default prefix/namespace for all generated keys.
        /// </summary>
        string? DefaultPrefix { get; set; }

        /// <summary>
        /// Gets or sets the separator used to join key parts.
        /// </summary>
        string Separator { get; set; }
    }
}

