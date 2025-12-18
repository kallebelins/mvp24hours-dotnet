//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Mvp24Hours.Application.Contract.Cache
{
    /// <summary>
    /// Generates cache keys for query caching.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The cache key generator is responsible for creating unique, deterministic
    /// cache keys based on:
    /// <list type="bullet">
    /// <item>Entity type being queried</item>
    /// <item>Method being executed</item>
    /// <item>Method parameters</item>
    /// <item>Optional custom key templates</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example keys generated:</strong>
    /// <list type="bullet">
    /// <item><c>Product:GetById:123</c></item>
    /// <item><c>Product:GetByCategory:Electronics</c></item>
    /// <item><c>Customer:List:page=1,size=10</c></item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IQueryCacheKeyGenerator
    {
        /// <summary>
        /// Generates a cache key for the given cacheable query.
        /// </summary>
        /// <typeparam name="TQuery">The query type.</typeparam>
        /// <param name="query">The cacheable query instance.</param>
        /// <returns>A unique cache key string.</returns>
        string GenerateKey<TQuery>(TQuery query) where TQuery : ICacheableQuery;

        /// <summary>
        /// Generates a cache key from a method and its parameters.
        /// </summary>
        /// <param name="method">The method being cached.</param>
        /// <param name="parameters">The method parameter values.</param>
        /// <param name="entityType">The entity type being queried.</param>
        /// <returns>A unique cache key string.</returns>
        string GenerateKey(MethodInfo method, object?[] parameters, Type entityType);

        /// <summary>
        /// Generates a cache key from a template and parameters.
        /// </summary>
        /// <param name="template">The key template with placeholders.</param>
        /// <param name="parameters">The parameter values to substitute.</param>
        /// <returns>A unique cache key string.</returns>
        string GenerateKeyFromTemplate(string template, IDictionary<string, object?> parameters);

        /// <summary>
        /// Generates a cache region key for an entity type.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <returns>A cache region key.</returns>
        string GenerateRegionKey<TEntity>();

        /// <summary>
        /// Generates a cache region key for an entity type.
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        /// <returns>A cache region key.</returns>
        string GenerateRegionKey(Type entityType);

        /// <summary>
        /// Generates a pattern for invalidating related cache entries.
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        /// <param name="operation">Optional operation name to include in pattern.</param>
        /// <returns>A pattern string for cache invalidation.</returns>
        string GenerateInvalidationPattern(Type entityType, string? operation = null);
    }
}

