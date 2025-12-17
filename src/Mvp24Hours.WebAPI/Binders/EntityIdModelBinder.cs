//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mvp24Hours.Core.ValueObjects;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Binders
{
    /// <summary>
    /// Model binder for strongly-typed entity identifiers (<see cref="EntityId{TSelf, TValue}"/>).
    /// Supports binding from query strings, route parameters, and form data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This binder automatically detects the underlying value type (Guid, int, long, string)
    /// and creates the appropriate strongly-typed ID instance.
    /// </para>
    /// <para>
    /// The binder looks for a constructor that accepts the underlying value type
    /// (e.g., <c>CustomerId(Guid value)</c>).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Query string: ?customerId=550e8400-e29b-41d4-a716-446655440000
    /// app.MapGet("/customers/{customerId}", (CustomerId customerId) => { ... });
    /// </code>
    /// </example>
    public class EntityIdModelBinder : IModelBinder
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
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;

            if (string.IsNullOrWhiteSpace(value))
            {
                return Task.CompletedTask;
            }

            // Check if the model type is an EntityId
            if (!IsEntityIdType(modelType))
            {
                bindingContext.ModelState.TryAddModelError(
                    modelName,
                    $"Type {modelType.Name} is not a valid EntityId type.");
                return Task.CompletedTask;
            }

            // Get the underlying value type (Guid, int, long, string)
            var valueType = GetEntityIdValueType(modelType);
            if (valueType == null)
            {
                bindingContext.ModelState.TryAddModelError(
                    modelName,
                    $"Could not determine underlying value type for {modelType.Name}.");
                return Task.CompletedTask;
            }

            // Parse the string value to the underlying type
            object parsedValue = null;
            try
            {
                parsedValue = ParseValue(value, valueType);
            }
            catch (Exception ex)
            {
                bindingContext.ModelState.TryAddModelError(
                    modelName,
                    $"Could not parse '{value}' as {valueType.Name}: {ex.Message}");
                return Task.CompletedTask;
            }

            if (parsedValue == null)
            {
                bindingContext.ModelState.TryAddModelError(
                    modelName,
                    $"Could not parse '{value}' as {valueType.Name}.");
                return Task.CompletedTask;
            }

            // Find constructor that accepts the underlying value type
            var constructor = modelType.GetConstructor(new[] { valueType });
            if (constructor == null)
            {
                bindingContext.ModelState.TryAddModelError(
                    modelName,
                    $"No constructor found for {modelType.Name} that accepts {valueType.Name}.");
                return Task.CompletedTask;
            }

            // Create the EntityId instance
            try
            {
                var instance = constructor.Invoke(new[] { parsedValue });
                bindingContext.Result = ModelBindingResult.Success(instance);
            }
            catch (Exception ex)
            {
                bindingContext.ModelState.TryAddModelError(
                    modelName,
                    $"Failed to create {modelType.Name} instance: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        private static bool IsEntityIdType(Type type)
        {
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

        private static Type GetEntityIdValueType(Type entityIdType)
        {
            var baseType = entityIdType.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                if (baseType.IsGenericType)
                {
                    var genericTypeDefinition = baseType.GetGenericTypeDefinition();
                    if (genericTypeDefinition == typeof(EntityId<,>))
                    {
                        var genericArguments = baseType.GetGenericArguments();
                        if (genericArguments.Length == 2)
                        {
                            // The second generic argument is the value type
                            return genericArguments[1];
                        }
                    }
                }
                baseType = baseType.BaseType;
            }

            return null;
        }

        private static object ParseValue(string value, Type valueType)
        {
            if (valueType == typeof(Guid))
            {
                return Guid.Parse(value);
            }
            else if (valueType == typeof(int))
            {
                return int.Parse(value);
            }
            else if (valueType == typeof(long))
            {
                return long.Parse(value);
            }
            else if (valueType == typeof(string))
            {
                return value;
            }
            else if (valueType.IsEnum)
            {
                return Enum.Parse(valueType, value, true);
            }
            else
            {
                // Try Convert.ChangeType as fallback
                return Convert.ChangeType(value, valueType);
            }
        }
    }
}

