//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Reflection;
using Mvp24Hours.Core.Contract.Domain.Entity;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Internal;

/// <summary>
/// Helpers to read/copy audit ("*By") fields declared by <see cref="IEntityLog{TForeignKey}"/>
/// without relying on <c>dynamic</c> binding. <see cref="IEntityLog{TForeignKey}"/> is invariant
/// on <c>TForeignKey</c>, so there is no single typed cast that covers every closed generic
/// (<c>IEntityLog&lt;int&gt;</c>, <c>IEntityLog&lt;Guid&gt;</c>, etc). Property access is
/// therefore resolved via reflection, localized to this helper.
/// </summary>
/// <remarks>
/// Mirrors <c>Mvp24Hours.Infrastructure.Data.EFCore.Internal.EntityLogAccessor</c>. Kept as a
/// separate copy per provider to avoid a cross-provider dependency between EF Core and MongoDB.
/// </remarks>
internal static class EntityLogAccessor
{
    /// <summary>
    /// Indicates whether <paramref name="entity"/> implements the open generic
    /// <see cref="IEntityLog{TForeignKey}"/> interface (any closed <c>TForeignKey</c>).
    /// </summary>
    internal static bool HasEntityLog(object entity)
    {
        return entity.GetType().GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityLog<>));
    }

    /// <summary>
    /// Copies the value of <paramref name="propertyName"/> from <paramref name="source"/> to
    /// <paramref name="target"/> when the property is readable on the source and writable on
    /// the target. No-ops silently when the property is missing on either side (mirrors the
    /// forgiving behavior of the previous <c>dynamic</c> based implementation).
    /// </summary>
    internal static void CopyPropertyValue(object source, object target, string propertyName)
    {
        PropertyInfo? sourceProperty = source.GetType().GetProperty(propertyName);
        PropertyInfo? targetProperty = target.GetType().GetProperty(propertyName);

        if (sourceProperty != null && sourceProperty.CanRead
            && targetProperty != null && targetProperty.CanWrite)
        {
            object? value = sourceProperty.GetValue(source);
            targetProperty.SetValue(target, value);
        }
    }

    /// <summary>
    /// Attempts to set <paramref name="value"/> on the property named <paramref name="propertyName"/>
    /// of <paramref name="target"/>. Returns <c>false</c> when the property does not exist, is not
    /// writable, or the value is not assignable to the property type.
    /// </summary>
    internal static bool TrySetPropertyValue(object target, string propertyName, object? value)
    {
        PropertyInfo? property = target.GetType().GetProperty(propertyName);
        if (property == null || !property.CanWrite)
        {
            return false;
        }

        if (value != null && !property.PropertyType.IsInstanceOfType(value))
        {
            return false;
        }

        property.SetValue(target, value);
        return true;
    }
}
