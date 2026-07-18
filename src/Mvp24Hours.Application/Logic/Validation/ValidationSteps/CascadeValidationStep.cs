//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using FluentValidation;
using Mvp24Hours.Application.Contract.Validation;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Application.Logic.Validation;

/// <summary>
/// Validation step that validates nested objects recursively.
/// </summary>
/// <typeparam name="T">The root type to validate.</typeparam>
/// <remarks>
/// Creates a new cascade validation step.
/// </remarks>
/// <param name="serviceProvider">Service provider for resolving validators.</param>
public class CascadeValidationStep<T>(IServiceProvider? serviceProvider = null) : IValidationStep<T> where T : class
{
    private readonly IServiceProvider? _serviceProvider = serviceProvider;
    private readonly HashSet<object> _validatedObjects = [];

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
        IList<IMessageResult> errors = ValidateNestedProperties(instance, context);

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
        IList<IMessageResult> errors = await ValidateNestedPropertiesAsync(instance, context, cancellationToken);

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

        Type type = instance.GetType();
        IEnumerable<PropertyInfo> properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !IsSimpleType(p.PropertyType));

        foreach (PropertyInfo? property in properties)
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
            ValidateNestedAttribute? validateAttr = property.GetCustomAttribute<ValidateNestedAttribute>();
            bool hasNestedValidation = typeof(IHasNestedValidation).IsAssignableFrom(property.PropertyType);

            if (validateAttr == null && !hasNestedValidation)
            {
                // Check if the property type itself has validators registered
                if (_serviceProvider == null)
                {
                    continue;
                }

                Type validatorType = typeof(IValidator<>).MakeGenericType(property.PropertyType);
                if (_serviceProvider.GetService(validatorType) == null)
                {
                    continue;
                }
            }

            // Build property path
            string newPath = context.Options.IncludePropertyPath
                ? (string.IsNullOrEmpty(context.PropertyPath) ? property.Name : $"{context.PropertyPath}.{property.Name}")
                : property.Name;

            // Check max depth from attribute
            int maxDepth = validateAttr?.MaxDepth ?? context.Options.MaxValidationDepth;
            if (context.CurrentDepth >= maxDepth)
            {
                continue;
            }

            // Handle collections
            if (value is IEnumerable enumerable and not string)
            {
                int index = 0;
                foreach (object? item in enumerable)
                {
                    if (item != null && !IsSimpleType(item.GetType()) && !_validatedObjects.Contains(item))
                    {
                        string itemPath = $"{newPath}[{index}]";
                        var childContext = new ValidationStepContext(context.Options, _serviceProvider)
                        {
                            CurrentDepth = context.CurrentDepth + 1,
                            PropertyPath = itemPath
                        };

                        IList<IMessageResult> itemErrors = ValidateObject(item, childContext);
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

                IList<IMessageResult> nestedErrors = ValidateObject(value, childContext);
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

        Type type = instance.GetType();
        IEnumerable<PropertyInfo> properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !IsSimpleType(p.PropertyType));

        foreach (PropertyInfo? property in properties)
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
            ValidateNestedAttribute? validateAttr = property.GetCustomAttribute<ValidateNestedAttribute>();
            bool hasNestedValidation = typeof(IHasNestedValidation).IsAssignableFrom(property.PropertyType);

            if (validateAttr == null && !hasNestedValidation)
            {
                // Check if the property type itself has validators registered
                if (_serviceProvider == null)
                {
                    continue;
                }

                Type validatorType = typeof(IValidator<>).MakeGenericType(property.PropertyType);
                if (_serviceProvider.GetService(validatorType) == null)
                {
                    continue;
                }
            }

            // Build property path
            string newPath = context.Options.IncludePropertyPath
                ? (string.IsNullOrEmpty(context.PropertyPath) ? property.Name : $"{context.PropertyPath}.{property.Name}")
                : property.Name;

            // Check max depth from attribute
            int maxDepth = validateAttr?.MaxDepth ?? context.Options.MaxValidationDepth;
            if (context.CurrentDepth >= maxDepth)
            {
                continue;
            }

            // Handle collections
            if (value is IEnumerable enumerable and not string)
            {
                int index = 0;
                foreach (object? item in enumerable)
                {
                    if (item != null && !IsSimpleType(item.GetType()) && !_validatedObjects.Contains(item))
                    {
                        string itemPath = $"{newPath}[{index}]";
                        var childContext = new ValidationStepContext(context.Options, _serviceProvider)
                        {
                            CurrentDepth = context.CurrentDepth + 1,
                            PropertyPath = itemPath
                        };

                        IList<IMessageResult> itemErrors = await ValidateObjectAsync(item, childContext, cancellationToken);
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

                IList<IMessageResult> nestedErrors = await ValidateObjectAsync(value, childContext, cancellationToken);
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
            foreach (ValidationResult result in validationResults)
            {
                string propertyName = result.MemberNames.Any()
                    ? string.Join(", ", result.MemberNames)
                    : "Unknown";

                string fullPath = context.Options.IncludePropertyPath
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
            Type validatorType = typeof(IValidator<>).MakeGenericType(instance.GetType());
            object? validator = _serviceProvider.GetService(validatorType);

            if (validator != null)
            {
                Type instanceType = instance.GetType();
                Type contextType = typeof(ValidationContext<>).MakeGenericType(instanceType);
                object? fluentContext = Activator.CreateInstance(contextType, instance);

                MethodInfo? validateMethod = validator.GetType()
                    .GetMethod("Validate", [contextType]);

                if (validateMethod != null && fluentContext != null)
                {
                    if (validateMethod.Invoke(validator, [fluentContext]) is FluentValidation.Results.ValidationResult result && !result.IsValid)
                    {
                        foreach (FluentValidation.Results.ValidationFailure? failure in result.Errors)
                        {
                            string fullPath = context.Options.IncludePropertyPath
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
        IList<IMessageResult> nestedErrors = ValidateNestedProperties(instance, context);
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
            foreach (ValidationResult result in validationResults)
            {
                string propertyName = result.MemberNames.Any()
                    ? string.Join(", ", result.MemberNames)
                    : "Unknown";

                string fullPath = context.Options.IncludePropertyPath
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
            Type validatorType = typeof(IValidator<>).MakeGenericType(instance.GetType());
            object? validator = _serviceProvider.GetService(validatorType);

            if (validator != null)
            {
                Type instanceType = instance.GetType();
                Type contextType = typeof(ValidationContext<>).MakeGenericType(instanceType);
                object? fluentContext = Activator.CreateInstance(contextType, instance);

                MethodInfo? validateMethod = validator.GetType()
                    .GetMethod("ValidateAsync", [contextType, typeof(CancellationToken)]);

                if (validateMethod != null && fluentContext != null)
                {
                    if (validateMethod.Invoke(validator, [fluentContext, cancellationToken]) is Task task)
                    {
                        await task;
                        PropertyInfo? resultProperty = task.GetType().GetProperty("Result");

                        if (resultProperty?.GetValue(task) is FluentValidation.Results.ValidationResult result && !result.IsValid)
                        {
                            foreach (FluentValidation.Results.ValidationFailure? failure in result.Errors)
                            {
                                string fullPath = context.Options.IncludePropertyPath
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
        IList<IMessageResult> nestedErrors = await ValidateNestedPropertiesAsync(instance, context, cancellationToken);
        errors.AddRange(nestedErrors);

        return errors;
    }

    private static bool IsSimpleType(Type type)
    {
        Type? underlyingType = Nullable.GetUnderlyingType(type);
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

