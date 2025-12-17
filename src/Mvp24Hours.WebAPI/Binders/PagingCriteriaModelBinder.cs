//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.DTOs.Models;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Queries;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Binders
{
    /// <summary>
    /// Model binder for <see cref="IPagingCriteria"/> and related types.
    /// Binds pagination parameters from query string to paging criteria objects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This binder supports binding:
    /// - <see cref="IPagingCriteria"/> (interface)
    /// - <see cref="PagingCriteria"/> (concrete implementation)
    /// - <see cref="PagingCriteriaRequest"/> (DTO)
    /// - <see cref="PaginatedQuery{TResponse}"/> (CQRS query base class)
    /// </para>
    /// <para>
    /// Query string parameters:
    /// - <c>limit</c> or <c>pageSize</c>: Number of items per page
    /// - <c>offset</c> or <c>page</c>: Page number (0-based) or offset
    /// - <c>orderBy</c>: Comma-separated list of fields to order by (supports multiple values)
    /// - <c>navigation</c>: Comma-separated list of navigation properties to include (supports multiple values)
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Query string: ?limit=20&offset=0&orderBy=Name,Email&navigation=Orders
    /// app.MapGet("/customers", (IPagingCriteria paging) => { ... });
    /// </code>
    /// </example>
    public class PagingCriteriaModelBinder : IModelBinder
    {
        /// <inheritdoc />
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var modelType = bindingContext.ModelType;
            var modelName = bindingContext.ModelName;
            var valueProvider = bindingContext.ValueProvider;

            // Get values from query string
            var limitValue = GetValue(valueProvider, modelName, "limit") ?? GetValue(valueProvider, modelName, "pageSize");
            var offsetValue = GetValue(valueProvider, modelName, "offset") ?? GetValue(valueProvider, modelName, "page");
            var orderByValue = GetValue(valueProvider, modelName, "orderBy");
            var navigationValue = GetValue(valueProvider, modelName, "navigation");

            // Parse limit (default: 20)
            int limit = 20;
            if (!string.IsNullOrWhiteSpace(limitValue))
            {
                if (!int.TryParse(limitValue, out limit) || limit < 1)
                {
                    bindingContext.ModelState.TryAddModelError(
                        $"{modelName}.limit",
                        $"Invalid limit value: '{limitValue}'. Must be a positive integer.");
                    limit = 20; // Use default
                }
            }

            // Parse offset/page (default: 0)
            int offset = 0;
            if (!string.IsNullOrWhiteSpace(offsetValue))
            {
                if (!int.TryParse(offsetValue, out offset) || offset < 0)
                {
                    bindingContext.ModelState.TryAddModelError(
                        $"{modelName}.offset",
                        $"Invalid offset value: '{offsetValue}'. Must be a non-negative integer.");
                    offset = 0; // Use default
                }
            }

            // Parse orderBy (comma-separated or multiple values)
            IReadOnlyCollection<string> orderBy = null;
            if (!string.IsNullOrWhiteSpace(orderByValue))
            {
                var orderByList = orderByValue
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                if (orderByList.Count > 0)
                {
                    orderBy = new ReadOnlyCollection<string>(orderByList);
                }
            }

            // Parse navigation (comma-separated or multiple values)
            IReadOnlyCollection<string> navigation = null;
            if (!string.IsNullOrWhiteSpace(navigationValue))
            {
                var navigationList = navigationValue
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                if (navigationList.Count > 0)
                {
                    navigation = new ReadOnlyCollection<string>(navigationList);
                }
            }

            // Create the appropriate instance based on model type
            object result = null;

            if (modelType == typeof(IPagingCriteria) || modelType == typeof(PagingCriteria))
            {
                result = new PagingCriteria(limit, offset, orderBy, navigation);
            }
            else if (modelType == typeof(PagingCriteriaRequest))
            {
                result = new PagingCriteriaRequest
                {
                    Limit = limit,
                    Offset = offset,
                    OrderBy = orderBy?.ToList(),
                    Navigation = navigation?.ToList()
                };
            }
            else if (IsPaginatedQueryType(modelType))
            {
                // For PaginatedQuery<TResponse>, we need to create an instance
                // This is more complex and may require reflection
                // For now, we'll create a PagingCriteriaRequest and let the user convert it
                result = new PagingCriteriaRequest
                {
                    Limit = limit,
                    Offset = offset,
                    OrderBy = orderBy?.ToList(),
                    Navigation = navigation?.ToList()
                };
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(
                    modelName,
                    $"Type {modelType.Name} is not supported by PagingCriteriaModelBinder.");
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }

        private static string GetValue(IValueProvider valueProvider, string modelName, string propertyName)
        {
            var fullName = string.IsNullOrEmpty(modelName) ? propertyName : $"{modelName}.{propertyName}";
            var result = valueProvider.GetValue(fullName);
            if (result != ValueProviderResult.None)
            {
                return result.FirstValue;
            }

            // Try without model name prefix
            result = valueProvider.GetValue(propertyName);
            return result != ValueProviderResult.None ? result.FirstValue : null;
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
                    if (name.StartsWith("PaginatedQuery`"))
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

