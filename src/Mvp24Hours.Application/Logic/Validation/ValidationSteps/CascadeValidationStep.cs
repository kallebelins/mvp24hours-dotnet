//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.Contract.Validation;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic.Validation
{
    /// <summary>
    /// Validation step that validates nested objects recursively.
    /// </summary>
    /// <typeparam name="T">The root type to validate.</typeparam>
    public class CascadeValidationStep<T> : IValidationStep<T> where T : class
    {
        private readonly IServiceProvider? _serviceProvider;
        private readonly HashSet<object> _validatedObjects = new();

        /// <summary>
        /// Creates a new cascade validation step.
        /// </summary>
        /// <param name="serviceProvider">Service provider for resolving validators.</param>
        public CascadeValidationStep(IServiceProvider? serviceProvider = null)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc/>
        public int Order => 300;

        /// <inheritdoc/>
        public string Name => "CascadeValidation";

        /// <inheritdoc/>
        public bool IsEnabled => true;

        /// <inheritdoc/>
        public ValidationServiceResult Execute(T instance, ValidationStepContext context)
        {
            if (!context.Options.ValidateNestedObjects)
            {
                return ValidationServiceResult.Success();
            }

            _validatedObjects.Clear();
            var errors = ValidateNestedProperties(instance, context);

            return errors.Any()
                ? ValidationServiceResult.Failure(errors)
                : ValidationServiceResult.Success();
        }

        /// <inheritdoc/>
        public async Task<ValidationServiceResult> ExecuteAsync(T instance, ValidationStepContext context, CancellationToken cancellationToken = default)
        {
            if (!context.Options.ValidateNestedObjects)
            {
                return ValidationServiceResult.Success();
            }

            _validatedObjects.Clear();
            var errors = await ValidateNestedPropertiesAsync(instance, context, cancellationToken);

            return errors.Any()
                ? ValidationServiceResult.Failure(errors)
                : ValidationServiceResult.Success();
        }

        /// <inheritdoc/>
        public bool ShouldExecute(T instance, ValidationStepContext context)
        {
            return context.Options.ValidateNestedObjects &&
                   context.CurrentDepth < context.Options.MaxValidationDepth;
        }

        private IList<IMessageResult> ValidateNestedProperties(object instance, ValidationStepContext context)
        {
            var errors = new List<IMessageResult>();

            // Prevent circular reference validation
            if (_validatedObjects.Contains(instance))
            {
                return errors;
            }
            _validatedObjects.Add(instance);

            if (context.CurrentDepth >= context.Options.MaxValidationDepth)
            {
                return errors;
            }

            var type = instance.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && !IsSimpleType(p.PropertyType));

            foreach (var property in properties)
            {
                object? value;
                try
                {
                    value = property.GetValue(instance);
                }
                catch
                {
                    continue;
                }

                if (value == null)
                {
                    continue;
                }

                // Check if property should be validated
                var validateAttr = property.GetCustomAttribute<ValidateNestedAttribute>();
                var hasNestedValidation = typeof(IHasNestedValidation).IsAssignableFrom(property.PropertyType);

                if (validateAttr == null && !hasNestedValidation)
                {
                    // Check if the property type itself has validators registered
                    if (_serviceProvider == null)
                    {
                        continue;
                    }

                    var validatorType = typeof(IValidator<>).MakeGenericType(property.PropertyType);
                    if (_serviceProvider.GetService(validatorType) == null)
                    {
                        continue;
                    }
                }

                // Build property path
                var newPath = context.Options.IncludePropertyPath
                    ? (string.IsNullOrEmpty(context.PropertyPath) ? property.Name : $"{context.PropertyPath}.{property.Name}")
                    : property.Name;

                // Check max depth from attribute
                var maxDepth = validateAttr?.MaxDepth ?? context.Options.MaxValidationDepth;
                if (context.CurrentDepth >= maxDepth)
                {
                    continue;
                }

                // Handle collections
                if (value is IEnumerable enumerable && !(value is string))
                {
                    var index = 0;
                    foreach (var item in enumerable)
                    {
                        if (item != null && !IsSimpleType(item.GetType()) && !_validatedObjects.Contains(item))
                        {
                            var itemPath = $"{newPath}[{index}]";
                            var childContext = new ValidationStepContext(context.Options, _serviceProvider)
                            {
                                CurrentDepth = context.CurrentDepth + 1,
                                PropertyPath = itemPath
                            };

                            var itemErrors = ValidateObject(item, childContext);
                            errors.AddRange(itemErrors);

                            if (context.Options.StopOnFirstError && errors.Any())
                            {
                                return errors;
                            }
                        }
                        index++;
                    }
                }
                else
                {
                    var childContext = new ValidationStepContext(context.Options, _serviceProvider)
                    {
                        CurrentDepth = context.CurrentDepth + 1,
                        PropertyPath = newPath
                    };

                    var nestedErrors = ValidateObject(value, childContext);
                    errors.AddRange(nestedErrors);

                    if (context.Options.StopOnFirstError && errors.Any())
                    {
                        return errors;
                    }
                }
            }

            return errors;
        }

        private async Task<IList<IMessageResult>> ValidateNestedPropertiesAsync(
            object instance,
            ValidationStepContext context,
            CancellationToken cancellationToken)
        {
            var errors = new List<IMessageResult>();

            // Prevent circular reference validation
            if (_validatedObjects.Contains(instance))
            {
                return errors;
            }
            _validatedObjects.Add(instance);

            if (context.CurrentDepth >= context.Options.MaxValidationDepth)
            {
                return errors;
            }

            var type = instance.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && !IsSimpleType(p.PropertyType));

            foreach (var property in properties)
            {
                cancellationToken.ThrowIfCancellationRequested();

                object? value;
                try
                {
                    value = property.GetValue(instance);
                }
                catch
                {
                    continue;
                }

                if (value == null)
                {
                    continue;
                }

                // Check if property should be validated
                var validateAttr = property.GetCustomAttribute<ValidateNestedAttribute>();
                var hasNestedValidation = typeof(IHasNestedValidation).IsAssignableFrom(property.PropertyType);

                if (validateAttr == null && !hasNestedValidation)
                {
                    // Check if the property type itself has validators registered
                    if (_serviceProvider == null)
                    {
                        continue;
                    }

                    var validatorType = typeof(IValidator<>).MakeGenericType(property.PropertyType);
                    if (_serviceProvider.GetService(validatorType) == null)
                    {
                        continue;
                    }
                }

                // Build property path
                var newPath = context.Options.IncludePropertyPath
                    ? (string.IsNullOrEmpty(context.PropertyPath) ? property.Name : $"{context.PropertyPath}.{property.Name}")
                    : property.Name;

                // Check max depth from attribute
                var maxDepth = validateAttr?.MaxDepth ?? context.Options.MaxValidationDepth;
                if (context.CurrentDepth >= maxDepth)
                {
                    continue;
                }

                // Handle collections
                if (value is IEnumerable enumerable && !(value is string))
                {
                    var index = 0;
                    foreach (var item in enumerable)
                    {
                        if (item != null && !IsSimpleType(item.GetType()) && !_validatedObjects.Contains(item))
                        {
                            var itemPath = $"{newPath}[{index}]";
                            var childContext = new ValidationStepContext(context.Options, _serviceProvider)
                            {
                                CurrentDepth = context.CurrentDepth + 1,
                                PropertyPath = itemPath
                            };

                            var itemErrors = await ValidateObjectAsync(item, childContext, cancellationToken);
                            errors.AddRange(itemErrors);

                            if (context.Options.StopOnFirstError && errors.Any())
                            {
                                return errors;
                            }
                        }
                        index++;
                    }
                }
                else
                {
                    var childContext = new ValidationStepContext(context.Options, _serviceProvider)
                    {
                        CurrentDepth = context.CurrentDepth + 1,
                        PropertyPath = newPath
                    };

                    var nestedErrors = await ValidateObjectAsync(value, childContext, cancellationToken);
                    errors.AddRange(nestedErrors);

                    if (context.Options.StopOnFirstError && errors.Any())
                    {
                        return errors;
                    }
                }
            }

            return errors;
        }

        private IList<IMessageResult> ValidateObject(object instance, ValidationStepContext context)
        {
            var errors = new List<IMessageResult>();

            // Validate with DataAnnotations
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(instance, null, null);

            if (!Validator.TryValidateObject(instance, validationContext, validationResults, true))
            {
                foreach (var result in validationResults)
                {
                    var propertyName = result.MemberNames.Any()
                        ? string.Join(", ", result.MemberNames)
                        : "Unknown";

                    var fullPath = context.Options.IncludePropertyPath
                        ? $"{context.PropertyPath}.{propertyName}"
                        : propertyName;

                    errors.Add(new MessageResult(
                        fullPath,
                        result.ErrorMessage ?? "Validation failed",
                        Core.Enums.MessageType.Error));

                    if (context.Options.StopOnFirstError)
                    {
                        return errors;
                    }
                }
            }

            // Validate with FluentValidation if available
            if (_serviceProvider != null)
            {
                var validatorType = typeof(IValidator<>).MakeGenericType(instance.GetType());
                var validator = _serviceProvider.GetService(validatorType);

                if (validator != null)
                {
                    var instanceType = instance.GetType();
                    var contextType = typeof(ValidationContext<>).MakeGenericType(instanceType);
                    var fluentContext = Activator.CreateInstance(contextType, instance);

                    var validateMethod = validator.GetType()
                        .GetMethod("Validate", new[] { contextType });

                    if (validateMethod != null && fluentContext != null)
                    {
                        var result = validateMethod.Invoke(validator, new[] { fluentContext })
                            as FluentValidation.Results.ValidationResult;

                        if (result != null && !result.IsValid)
                        {
                            foreach (var failure in result.Errors)
                            {
                                var fullPath = context.Options.IncludePropertyPath
                                    ? $"{context.PropertyPath}.{failure.PropertyName}"
                                    : failure.PropertyName;

                                errors.Add(new MessageResult(
                                    fullPath,
                                    failure.ErrorMessage,
                                    Core.Enums.MessageType.Error));

                                if (context.Options.StopOnFirstError)
                                {
                                    return errors;
                                }
                            }
                        }
                    }
                }
            }

            // Recursively validate nested properties
            var nestedErrors = ValidateNestedProperties(instance, context);
            errors.AddRange(nestedErrors);

            return errors;
        }

        private async Task<IList<IMessageResult>> ValidateObjectAsync(
            object instance,
            ValidationStepContext context,
            CancellationToken cancellationToken)
        {
            var errors = new List<IMessageResult>();

            // Validate with DataAnnotations
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(instance, null, null);

            if (!Validator.TryValidateObject(instance, validationContext, validationResults, true))
            {
                foreach (var result in validationResults)
                {
                    var propertyName = result.MemberNames.Any()
                        ? string.Join(", ", result.MemberNames)
                        : "Unknown";

                    var fullPath = context.Options.IncludePropertyPath
                        ? $"{context.PropertyPath}.{propertyName}"
                        : propertyName;

                    errors.Add(new MessageResult(
                        fullPath,
                        result.ErrorMessage ?? "Validation failed",
                        Core.Enums.MessageType.Error));

                    if (context.Options.StopOnFirstError)
                    {
                        return errors;
                    }
                }
            }

            // Validate with FluentValidation if available
            if (_serviceProvider != null)
            {
                var validatorType = typeof(IValidator<>).MakeGenericType(instance.GetType());
                var validator = _serviceProvider.GetService(validatorType);

                if (validator != null)
                {
                    var instanceType = instance.GetType();
                    var contextType = typeof(ValidationContext<>).MakeGenericType(instanceType);
                    var fluentContext = Activator.CreateInstance(contextType, instance);

                    var validateMethod = validator.GetType()
                        .GetMethod("ValidateAsync", new[] { contextType, typeof(CancellationToken) });

                    if (validateMethod != null && fluentContext != null)
                    {
                        var task = validateMethod.Invoke(validator, new[] { fluentContext, cancellationToken }) as Task;
                        if (task != null)
                        {
                            await task;
                            var resultProperty = task.GetType().GetProperty("Result");
                            var result = resultProperty?.GetValue(task) as FluentValidation.Results.ValidationResult;

                            if (result != null && !result.IsValid)
                            {
                                foreach (var failure in result.Errors)
                                {
                                    var fullPath = context.Options.IncludePropertyPath
                                        ? $"{context.PropertyPath}.{failure.PropertyName}"
                                        : failure.PropertyName;

                                    errors.Add(new MessageResult(
                                        fullPath,
                                        failure.ErrorMessage,
                                        Core.Enums.MessageType.Error));

                                    if (context.Options.StopOnFirstError)
                                    {
                                        return errors;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Recursively validate nested properties
            var nestedErrors = await ValidateNestedPropertiesAsync(instance, context, cancellationToken);
            errors.AddRange(nestedErrors);

            return errors;
        }

        private static bool IsSimpleType(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            type = underlyingType ?? type;

            return type.IsPrimitive ||
                   type.IsEnum ||
                   type == typeof(string) ||
                   type == typeof(decimal) ||
                   type == typeof(DateTime) ||
                   type == typeof(DateTimeOffset) ||
                   type == typeof(TimeSpan) ||
                   type == typeof(Guid) ||
                   type == typeof(DateOnly) ||
                   type == typeof(TimeOnly);
        }
    }
}

