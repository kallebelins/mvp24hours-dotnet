//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Mvp24Hours.Application.Logic.Internal;

/// <summary>
/// Resolves and caches the DTO to entity property pairs used by reflection-based PATCH
/// (partial update) operations in the application service bases.
/// </summary>
/// <remarks>
/// <para>
/// The pair list is resolved once per <c>(updateDtoType, entityType)</c> combination and
/// reused for every subsequent request, replacing the full reflection scan that previously
/// ran on each PATCH call.
/// </para>
/// <para>
/// A property pair is only produced when all of the conditions below hold. These are the
/// exact same filters applied by the original per-request implementation:
/// <list type="bullet">
/// <item>the DTO property is readable (<see cref="PropertyInfo.CanRead"/>);</item>
/// <item>the entity exposes a public instance property with the same name;</item>
/// <item>the entity property is writable (<see cref="PropertyInfo.CanWrite"/>);</item>
/// <item>the entity property type is assignable from the DTO property type.</item>
/// </list>
/// </para>
/// <para>
/// This type is thread-safe.
/// </para>
/// </remarks>
internal static class PatchPropertyMap
{
    private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

    private static readonly ConcurrentDictionary<(Type Dto, Type Entity), (PropertyInfo Source, PropertyInfo Target)[]> _cache = new();

    /// <summary>
    /// Gets the cached property pairs for the informed DTO/entity combination, building
    /// the map on first use.
    /// </summary>
    /// <param name="dtoType">The update DTO type (source of the values).</param>
    /// <param name="entityType">The entity type (target of the values).</param>
    /// <param name="logger">Optional logger used only while the map is being built.</param>
    /// <returns>The resolved property pairs, in DTO declaration order.</returns>
    internal static (PropertyInfo Source, PropertyInfo Target)[] Get(Type dtoType, Type entityType, ILogger? logger = null)
    {
        return _cache.TryGetValue((dtoType, entityType), out (PropertyInfo Source, PropertyInfo Target)[]? cached)
            ? cached
            : _cache.GetOrAdd((dtoType, entityType), Build(dtoType, entityType, logger));
    }

    /// <summary>
    /// Applies every non-null DTO value to the matching entity property.
    /// </summary>
    /// <param name="dtoType">The update DTO type (source of the values).</param>
    /// <param name="entityType">The entity type (target of the values).</param>
    /// <param name="dto">The DTO instance carrying the partial data.</param>
    /// <param name="entity">The entity instance to be updated.</param>
    /// <param name="logger">Optional logger used only while the map is being built.</param>
    internal static void Apply(Type dtoType, Type entityType, object dto, object entity, ILogger? logger = null)
    {
        foreach ((PropertyInfo source, PropertyInfo target) in Get(dtoType, entityType, logger))
        {
            object? dtoValue = source.GetValue(dto);

            // Skip null values for PATCH
            if (dtoValue == null)
            {
                continue;
            }

            target.SetValue(entity, dtoValue);
        }
    }

    private static (PropertyInfo Source, PropertyInfo Target)[] Build(Type dtoType, Type entityType, ILogger? logger)
    {
        List<(PropertyInfo Source, PropertyInfo Target)> pairs = [];

        foreach (PropertyInfo dtoProperty in dtoType.GetProperties(PublicInstance))
        {
            if (!dtoProperty.CanRead)
            {
                LogSkipped(logger, dtoType, entityType, dtoProperty, "the DTO property is not readable");
                continue;
            }

            // Find matching property in entity
            PropertyInfo? entityProperty = entityType.GetProperty(dtoProperty.Name, PublicInstance);
            if (entityProperty == null)
            {
                LogSkipped(logger, dtoType, entityType, dtoProperty, "the entity has no property with the same name");
                continue;
            }

            if (!entityProperty.CanWrite)
            {
                LogSkipped(logger, dtoType, entityType, dtoProperty, "the entity property is not writable");
                continue;
            }

            // Check if types are compatible
            if (!entityProperty.PropertyType.IsAssignableFrom(dtoProperty.PropertyType))
            {
                LogSkipped(logger, dtoType, entityType, dtoProperty, "the entity property type is not assignable from the DTO property type");
                continue;
            }

            pairs.Add((dtoProperty, entityProperty));
        }

        return [.. pairs];
    }

    private static void LogSkipped(ILogger? logger, Type dtoType, Type entityType, PropertyInfo dtoProperty, string reason)
    {
        logger?.LogDebug(
            "application-patch-map-skipped: {DtoType}.{PropertyName} is not mapped to {EntityType} because {Reason}",
            dtoType.Name,
            dtoProperty.Name,
            entityType.Name,
            reason);
    }
}
