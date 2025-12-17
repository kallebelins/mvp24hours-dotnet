//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.DTOs.Models;
using Mvp24Hours.Core.ValueObjects;
using Mvp24Hours.Core.ValueObjects.Logic;
using System;

namespace Mvp24Hours.WebAPI.Binders
{
    /// <summary>
    /// Model binder provider that registers custom binders for Mvp24Hours types.
    /// </summary>
    /// <remarks>
    /// This provider automatically detects and registers binders for:
    /// - <see cref="DateOnly"/>
    /// - <see cref="TimeOnly"/>
    /// - <see cref="DateTimeOffset"/>
    /// - <see cref="EntityId{TSelf, TValue}"/> and derived types
    /// - <see cref="IPagingCriteria"/> and related types
    /// </remarks>
    public class Mvp24HoursModelBinderProvider : IModelBinderProvider
    {
        /// <inheritdoc />
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var modelType = context.Metadata.ModelType;

            // DateOnly
            if (modelType == typeof(DateOnly) || modelType == typeof(DateOnly?))
            {
                return new DateOnlyModelBinder();
            }

            // TimeOnly
            if (modelType == typeof(TimeOnly) || modelType == typeof(TimeOnly?))
            {
                return new TimeOnlyModelBinder();
            }

            // DateTimeOffset
            if (modelType == typeof(DateTimeOffset) || modelType == typeof(DateTimeOffset?))
            {
                return new DateTimeOffsetModelBinder();
            }

            // EntityId types (strongly-typed IDs)
            if (IsEntityIdType(modelType))
            {
                return new EntityIdModelBinder();
            }

            // IPagingCriteria and related types
            if (modelType == typeof(IPagingCriteria) ||
                modelType == typeof(PagingCriteria) ||
                modelType == typeof(PagingCriteriaRequest) ||
                IsPaginatedQueryType(modelType))
            {
                return new PagingCriteriaModelBinder();
            }

            return null;
        }

        private static bool IsEntityIdType(Type type)
        {
            if (type == null)
                return false;

            // Handle nullable types
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);
            }

            if (type == null)
                return false;

            // Check if it's EntityId<TSelf, TValue> or a derived type
            var baseType = type.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                if (baseType.IsGenericType)
                {
                    var genericTypeDefinition = baseType.GetGenericTypeDefinition();
                    if (genericTypeDefinition == typeof(EntityId<,>))
                    {
                        return true;
                    }
                }
                baseType = baseType.BaseType;
            }

            return false;
        }

        private static bool IsPaginatedQueryType(Type type)
        {
            if (type == null)
                return false;

            // Check if it's PaginatedQuery<TResponse> or a derived type
            var baseType = type.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                if (baseType.IsGenericType)
                {
                    var genericTypeDefinition = baseType.GetGenericTypeDefinition();
                    var name = genericTypeDefinition.Name;
                    if (name.StartsWith("PaginatedQuery`", StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
                baseType = baseType.BaseType;
            }

            return false;
        }
    }
}

