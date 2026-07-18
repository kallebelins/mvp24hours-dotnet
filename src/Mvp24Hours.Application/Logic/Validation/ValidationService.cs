//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Contract.Validation;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Application.Logic.Validation;

/// <summary>
/// Default implementation of IValidationService that supports FluentValidation and DataAnnotations.
/// </summary>
/// <typeparam name="T">The type to validate.</typeparam>
/// <remarks>
/// Creates a new instance of ValidationService.
/// </remarks>
/// <param name="fluentValidators">Collection of FluentValidation validators.</param>
/// <param name="serviceProvider">Service provider for resolving nested validators.</param>
/// <param name="logger">Logger for validation operations.</param>
/// <param name="options">Service options.</param>
public class ValidationService<T>(
    IEnumerable<IValidator<T>>? fluentValidators = null,
    IServiceProvider? serviceProvider = null,
    ILogger<ValidationService<T>>? logger = null,
    ValidationServiceOptions? options = null) : IValidationService<T>, ICascadeValidator<T> where T : class
{
    private readonly IEnumerable<IValidator<T>> _fluentValidators = fluentValidators ?? [];
    private readonly IServiceProvider? _serviceProvider = serviceProvider;
    private readonly ILogger<ValidationService<T>>? _logger = logger;
    private readonly ValidationServiceOptions _options = options ?? new ValidationServiceOptions();

    #region IValidationService<T> Implementation

    /// <inheritdoc/>
    public ValidationServiceResult Validate(T instance)
    {
        return Validate(instance, ValidationOptions.Default);
    }

    /// <inheritdoc/>
    public ValidationServiceResult Validate(T instance, ValidationOptions options)
    {
        _logger?.LogDebug("application-validationservice-validate");

        if (instance == null)
        {
            return ValidationServiceResult.Failure("instance", "Instance cannot be null.");
        }

        var errors = new List<IMessageResult>();

        // FluentValidation
        if (_options.UseFluentValidation && _fluentValidators.Any())
        {
            IList<IMessageResult> fluentErrors = ValidateWithFluentValidation(instance, options);
            errors.AddRange(fluentErrors);

            if (options.StopOnFirstError && errors.Any())
            {
                return ValidationServiceResult.Failure(errors);
            }
        }

        // DataAnnotations
        if (_options.UseDataAnnotations)
        {
            IList<IMessageResult> annotationErrors = ValidateWithDataAnnotations(instance, options);
            errors.AddRange(annotationErrors);

            if (options.StopOnFirstError && errors.Any())
            {
                return ValidationServiceResult.Failure(errors);
            }
        }

        // Cascade validation for nested objects
        if (options.ValidateNestedObjects && _options.UseCascadeValidation)
        {
            IList<IMessageResult> nestedErrors = ValidateNestedObjects(instance, options, 0, string.Empty);
            errors.AddRange(nestedErrors);
        }

        if (errors.Any())
        {
            _logger?.LogDebug("Validation failed for type {TypeName} with {ErrorCount} error(s)",
                typeof(T).Name, errors.Count);
            return ValidationServiceResult.Failure(errors);
        }

        return ValidationServiceResult.Success();
    }

    /// <inheritdoc/>
    public async Task<ValidationServiceResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
    {
        return await ValidateAsync(instance, ValidationOptions.Default, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ValidationServiceResult> ValidateAsync(T instance, ValidationOptions options, CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("application-validationservice-validateasync");

        if (instance == null)
        {
            return ValidationServiceResult.Failure("instance", "Instance cannot be null.");
        }

        var errors = new List<IMessageResult>();

        // FluentValidation async
        if (_options.UseFluentValidation && _fluentValidators.Any())
        {
            IList<IMessageResult> fluentErrors = await ValidateWithFluentValidationAsync(instance, options, cancellationToken);
            errors.AddRange(fluentErrors);

            if (options.StopOnFirstError && errors.Any())
            {
                return ValidationServiceResult.Failure(errors);
            }
        }

        // DataAnnotations (sync, no async version)
        if (_options.UseDataAnnotations)
        {
            IList<IMessageResult> annotationErrors = ValidateWithDataAnnotations(instance, options);
            errors.AddRange(annotationErrors);

            if (options.StopOnFirstError && errors.Any())
            {
                return ValidationServiceResult.Failure(errors);
            }
        }

        // Cascade validation for nested objects
        if (options.ValidateNestedObjects && _options.UseCascadeValidation)
        {
            IList<IMessageResult> nestedErrors = await ValidateNestedObjectsAsync(instance, options, 0, string.Empty, cancellationToken);
            errors.AddRange(nestedErrors);
        }

        if (errors.Any())
        {
            _logger?.LogDebug("Validation failed for type {TypeName} with {ErrorCount} error(s)",
                typeof(T).Name, errors.Count);
            return ValidationServiceResult.Failure(errors);
        }

        return ValidationServiceResult.Success();
    }

    /// <inheritdoc/>
    public ValidationServiceResult ValidateMany(IEnumerable<T> instances)
    {
        _logger?.LogDebug("application-validationservice-validatemany");

        if (instances == null)
        {
            return ValidationServiceResult.Failure("instances", "Collection cannot be null.");
        }

        var errors = new List<IMessageResult>();
        int index = 0;

        foreach (T instance in instances)
        {
            ValidationServiceResult result = Validate(instance);
            if (!result.IsValid)
            {
                foreach (IMessageResult error in result.Errors)
                {
                    errors.Add(new MessageResult(
                        $"[{index}].{error.Key}",
                        error.Message ?? string.Empty,
                        Core.Enums.MessageType.Error));
                }
            }
            index++;
        }

        return errors.Any()
            ? ValidationServiceResult.Failure(errors)
            : ValidationServiceResult.Success();
    }

    /// <inheritdoc/>
    public async Task<ValidationServiceResult> ValidateManyAsync(IEnumerable<T> instances, CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("application-validationservice-validatemanyasync");

        if (instances == null)
        {
            return ValidationServiceResult.Failure("instances", "Collection cannot be null.");
        }

        var errors = new List<IMessageResult>();
        int index = 0;

        foreach (T instance in instances)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidationServiceResult result = await ValidateAsync(instance, cancellationToken);
            if (!result.IsValid)
            {
                foreach (IMessageResult error in result.Errors)
                {
                    errors.Add(new MessageResult(
                        $"[{index}].{error.Key}",
                        error.Message ?? string.Empty,
                        Core.Enums.MessageType.Error));
                }
            }
            index++;
        }

        return errors.Any()
            ? ValidationServiceResult.Failure(errors)
            : ValidationServiceResult.Success();
    }

    /// <inheritdoc/>
    public void ValidateAndThrow(T instance)
    {
        ValidationServiceResult result = Validate(instance);
        if (!result.IsValid)
        {
            throw new Core.Exceptions.ValidationException(
                $"Validation failed for {typeof(T).Name}",
                "VALIDATION_ERROR",
                result.Errors);
        }
    }

    /// <inheritdoc/>
    public async Task ValidateAndThrowAsync(T instance, CancellationToken cancellationToken = default)
    {
        ValidationServiceResult result = await ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new Core.Exceptions.ValidationException(
                $"Validation failed for {typeof(T).Name}",
                "VALIDATION_ERROR",
                result.Errors);
        }
    }

    #endregion

    #region ICascadeValidator<T> Implementation

    /// <inheritdoc/>
    public ValidationServiceResult ValidateWithNested(T instance)
    {
        return ValidateWithNested(instance, ValidationOptions.WithCascadeValidation);
    }

    /// <inheritdoc/>
    public ValidationServiceResult ValidateWithNested(T instance, ValidationOptions options)
    {
        options.ValidateNestedObjects = true;
        return Validate(instance, options);
    }

    /// <inheritdoc/>
    public async Task<ValidationServiceResult> ValidateWithNestedAsync(T instance, CancellationToken cancellationToken = default)
    {
        return await ValidateWithNestedAsync(instance, ValidationOptions.WithCascadeValidation, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ValidationServiceResult> ValidateWithNestedAsync(T instance, ValidationOptions options, CancellationToken cancellationToken = default)
    {
        options.ValidateNestedObjects = true;
        return await ValidateAsync(instance, options, cancellationToken);
    }

    #endregion

    #region Private Methods

    private IList<IMessageResult> ValidateWithFluentValidation(T instance, ValidationOptions options)
    {
        var errors = new List<IMessageResult>();
        var context = new ValidationContext<T>(instance);

        foreach (IValidator<T> validator in _fluentValidators)
        {
            FluentValidation.Results.ValidationResult result = validator.Validate(context);
            if (!result.IsValid)
            {
                foreach (ValidationFailure? failure in result.Errors)
                {
                    errors.Add(new MessageResult(
                        failure.PropertyName ?? failure.ErrorCode,
                        failure.ErrorMessage,
                        Core.Enums.MessageType.Error));

                    if (options.StopOnFirstError)
                    {
                        return errors;
                    }
                }
            }
        }

        return errors;
    }

    private async Task<IList<IMessageResult>> ValidateWithFluentValidationAsync(
        T instance,
        ValidationOptions options,
        CancellationToken cancellationToken)
    {
        var errors = new List<IMessageResult>();
        var context = new ValidationContext<T>(instance);

        foreach (IValidator<T> validator in _fluentValidators)
        {
            FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(context, cancellationToken);
            if (!result.IsValid)
            {
                foreach (ValidationFailure? failure in result.Errors)
                {
                    errors.Add(new MessageResult(
                        failure.PropertyName ?? failure.ErrorCode,
                        failure.ErrorMessage,
                        Core.Enums.MessageType.Error));

                    if (options.StopOnFirstError)
                    {
                        return errors;
                    }
                }
            }
        }

        return errors;
    }

    private IList<IMessageResult> ValidateWithDataAnnotations(T instance, ValidationOptions options)
    {
        var errors = new List<IMessageResult>();
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(instance, null, null);

        if (!Validator.TryValidateObject(instance, validationContext, validationResults, true))
        {
            foreach (System.ComponentModel.DataAnnotations.ValidationResult result in validationResults)
            {
                string propertyName = result.MemberNames.Any()
                    ? string.Join(", ", result.MemberNames)
                    : "Unknown";

                errors.Add(new MessageResult(
                    propertyName,
                    result.ErrorMessage ?? "Validation failed",
                    Core.Enums.MessageType.Error));

                if (options.StopOnFirstError)
                {
                    return errors;
                }
            }
        }

        return errors;
    }

    private IList<IMessageResult> ValidateNestedObjects(
        object instance,
        ValidationOptions options,
        int currentDepth,
        string propertyPath)
    {
        var errors = new List<IMessageResult>();

        if (currentDepth >= options.MaxValidationDepth)
        {
            return errors;
        }

        Type type = instance.GetType();
        IEnumerable<PropertyInfo> properties = type.GetProperties()
            .Where(p => p.CanRead && !IsSimpleType(p.PropertyType));

        foreach (PropertyInfo? property in properties)
        {
            object? value = property.GetValue(instance);
            if (value == null)
            {
                continue;
            }

            string newPath = options.IncludePropertyPath
                ? (string.IsNullOrEmpty(propertyPath) ? property.Name : $"{propertyPath}.{property.Name}")
                : property.Name;

            // Check for ValidateNested attribute
            var validateAttr = property.GetCustomAttributes(typeof(ValidateNestedAttribute), true)
                .FirstOrDefault() as ValidateNestedAttribute;

            bool shouldValidate = validateAttr != null ||
                                typeof(IHasNestedValidation).IsAssignableFrom(property.PropertyType);

            if (!shouldValidate && !_options.ValidateAllNestedObjects)
            {
                continue;
            }

            int maxDepth = validateAttr?.MaxDepth ?? options.MaxValidationDepth;
            if (currentDepth >= maxDepth)
            {
                continue;
            }

            // Handle collections
            if (value is System.Collections.IEnumerable enumerable and not string)
            {
                int index = 0;
                foreach (object? item in enumerable)
                {
                    if (item != null && !IsSimpleType(item.GetType()))
                    {
                        string itemPath = $"{newPath}[{index}]";
                        IList<IMessageResult> itemErrors = ValidateObjectDynamic(item, options, currentDepth + 1, itemPath);
                        errors.AddRange(itemErrors);

                        if (options.StopOnFirstError && errors.Any())
                        {
                            return errors;
                        }
                    }
                    index++;
                }
            }
            else
            {
                IList<IMessageResult> nestedErrors = ValidateObjectDynamic(value, options, currentDepth + 1, newPath);
                errors.AddRange(nestedErrors);

                if (options.StopOnFirstError && errors.Any())
                {
                    return errors;
                }
            }
        }

        return errors;
    }

    private async Task<IList<IMessageResult>> ValidateNestedObjectsAsync(
        object instance,
        ValidationOptions options,
        int currentDepth,
        string propertyPath,
        CancellationToken cancellationToken)
    {
        var errors = new List<IMessageResult>();

        if (currentDepth >= options.MaxValidationDepth)
        {
            return errors;
        }

        Type type = instance.GetType();
        IEnumerable<PropertyInfo> properties = type.GetProperties()
            .Where(p => p.CanRead && !IsSimpleType(p.PropertyType));

        foreach (PropertyInfo? property in properties)
        {
            cancellationToken.ThrowIfCancellationRequested();

            object? value = property.GetValue(instance);
            if (value == null)
            {
                continue;
            }

            string newPath = options.IncludePropertyPath
                ? (string.IsNullOrEmpty(propertyPath) ? property.Name : $"{propertyPath}.{property.Name}")
                : property.Name;

            // Check for ValidateNested attribute
            var validateAttr = property.GetCustomAttributes(typeof(ValidateNestedAttribute), true)
                .FirstOrDefault() as ValidateNestedAttribute;

            bool shouldValidate = validateAttr != null ||
                                typeof(IHasNestedValidation).IsAssignableFrom(property.PropertyType);

            if (!shouldValidate && !_options.ValidateAllNestedObjects)
            {
                continue;
            }

            int maxDepth = validateAttr?.MaxDepth ?? options.MaxValidationDepth;
            if (currentDepth >= maxDepth)
            {
                continue;
            }

            // Handle collections
            if (value is System.Collections.IEnumerable enumerable and not string)
            {
                int index = 0;
                foreach (object? item in enumerable)
                {
                    if (item != null && !IsSimpleType(item.GetType()))
                    {
                        string itemPath = $"{newPath}[{index}]";
                        IList<IMessageResult> itemErrors = await ValidateObjectDynamicAsync(item, options, currentDepth + 1, itemPath, cancellationToken);
                        errors.AddRange(itemErrors);

                        if (options.StopOnFirstError && errors.Any())
                        {
                            return errors;
                        }
                    }
                    index++;
                }
            }
            else
            {
                IList<IMessageResult> nestedErrors = await ValidateObjectDynamicAsync(value, options, currentDepth + 1, newPath, cancellationToken);
                errors.AddRange(nestedErrors);

                if (options.StopOnFirstError && errors.Any())
                {
                    return errors;
                }
            }
        }

        return errors;
    }

    private IList<IMessageResult> ValidateObjectDynamic(
        object instance,
        ValidationOptions options,
        int depth,
        string path)
    {
        var errors = new List<IMessageResult>();

        // Validate with DataAnnotations
        if (_options.UseDataAnnotations)
        {
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(instance, null, null);

            if (!Validator.TryValidateObject(instance, validationContext, validationResults, true))
            {
                foreach (System.ComponentModel.DataAnnotations.ValidationResult result in validationResults)
                {
                    string propertyName = result.MemberNames.Any()
                        ? string.Join(", ", result.MemberNames)
                        : "Unknown";

                    string fullPath = options.IncludePropertyPath
                        ? $"{path}.{propertyName}"
                        : propertyName;

                    errors.Add(new MessageResult(
                        fullPath,
                        result.ErrorMessage ?? "Validation failed",
                        Core.Enums.MessageType.Error));
                }
            }
        }

        // Try to resolve FluentValidator from DI
        if (_options.UseFluentValidation && _serviceProvider != null)
        {
            Type validatorType = typeof(IValidator<>).MakeGenericType(instance.GetType());
            object? validator = _serviceProvider.GetService(validatorType);

            if (validator != null)
            {
                MethodInfo? validateMethod = validatorType.GetMethod("Validate", [instance.GetType()]);
                if (validateMethod != null)
                {
                    if (validateMethod.Invoke(validator, [instance]) is FluentValidation.Results.ValidationResult result && !result.IsValid)
                    {
                        foreach (ValidationFailure? failure in result.Errors)
                        {
                            string fullPath = options.IncludePropertyPath
                                ? $"{path}.{failure.PropertyName}"
                                : failure.PropertyName;

                            errors.Add(new MessageResult(
                                fullPath,
                                failure.ErrorMessage,
                                Core.Enums.MessageType.Error));
                        }
                    }
                }
            }
        }

        // Continue validating nested objects
        IList<IMessageResult> nestedErrors = ValidateNestedObjects(instance, options, depth, path);
        errors.AddRange(nestedErrors);

        return errors;
    }

    private async Task<IList<IMessageResult>> ValidateObjectDynamicAsync(
        object instance,
        ValidationOptions options,
        int depth,
        string path,
        CancellationToken cancellationToken)
    {
        var errors = new List<IMessageResult>();

        // Validate with DataAnnotations (sync)
        if (_options.UseDataAnnotations)
        {
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(instance, null, null);

            if (!Validator.TryValidateObject(instance, validationContext, validationResults, true))
            {
                foreach (System.ComponentModel.DataAnnotations.ValidationResult result in validationResults)
                {
                    string propertyName = result.MemberNames.Any()
                        ? string.Join(", ", result.MemberNames)
                        : "Unknown";

                    string fullPath = options.IncludePropertyPath
                        ? $"{path}.{propertyName}"
                        : propertyName;

                    errors.Add(new MessageResult(
                        fullPath,
                        result.ErrorMessage ?? "Validation failed",
                        Core.Enums.MessageType.Error));
                }
            }
        }

        // Try to resolve FluentValidator from DI
        if (_options.UseFluentValidation && _serviceProvider != null)
        {
            Type validatorType = typeof(IValidator<>).MakeGenericType(instance.GetType());
            object? validator = _serviceProvider.GetService(validatorType);

            if (validator != null)
            {
                // Use reflection to call ValidateAsync
                MethodInfo? validateMethod = validatorType.GetMethod("ValidateAsync",
                    [instance.GetType(), typeof(CancellationToken)]);

                if (validateMethod != null)
                {
                    if (validateMethod.Invoke(validator, [instance, cancellationToken]) is Task task)
                    {
                        await task;
                        PropertyInfo? resultProperty = task.GetType().GetProperty("Result");

                        if (resultProperty?.GetValue(task) is FluentValidation.Results.ValidationResult result && !result.IsValid)
                        {
                            foreach (ValidationFailure? failure in result.Errors)
                            {
                                string fullPath = options.IncludePropertyPath
                                    ? $"{path}.{failure.PropertyName}"
                                    : failure.PropertyName;

                                errors.Add(new MessageResult(
                                    fullPath,
                                    failure.ErrorMessage,
                                    Core.Enums.MessageType.Error));
                            }
                        }
                    }
                }
            }
        }

        // Continue validating nested objects
        IList<IMessageResult> nestedErrors = await ValidateNestedObjectsAsync(instance, options, depth, path, cancellationToken);
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

    #endregion
}

/// <summary>
/// Configuration options for ValidationService.
/// </summary>
public class ValidationServiceOptions
{
    /// <summary>
    /// Gets or sets whether to use FluentValidation. Default is true.
    /// </summary>
    public bool UseFluentValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use DataAnnotations. Default is true.
    /// </summary>
    public bool UseDataAnnotations { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use cascade validation. Default is true.
    /// </summary>
    public bool UseCascadeValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to validate all nested objects even without ValidateNested attribute.
    /// Default is false.
    /// </summary>
    public bool ValidateAllNestedObjects { get; set; } = false;
}

