//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentValidation;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Contract.Validation;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic.Validation
{
    /// <summary>
    /// Default implementation of IValidationService that supports FluentValidation and DataAnnotations.
    /// </summary>
    /// <typeparam name="T">The type to validate.</typeparam>
    public class ValidationService<T> : IValidationService<T>, ICascadeValidator<T> where T : class
    {
        private readonly IEnumerable<IValidator<T>> _fluentValidators;
        private readonly IServiceProvider? _serviceProvider;
        private readonly ILogger<ValidationService<T>>? _logger;
        private readonly ValidationServiceOptions _options;

        /// <summary>
        /// Creates a new instance of ValidationService.
        /// </summary>
        /// <param name="fluentValidators">Collection of FluentValidation validators.</param>
        /// <param name="serviceProvider">Service provider for resolving nested validators.</param>
        /// <param name="logger">Logger for validation operations.</param>
        /// <param name="options">Service options.</param>
        public ValidationService(
            IEnumerable<IValidator<T>>? fluentValidators = null,
            IServiceProvider? serviceProvider = null,
            ILogger<ValidationService<T>>? logger = null,
            ValidationServiceOptions? options = null)
        {
            _fluentValidators = fluentValidators ?? Enumerable.Empty<IValidator<T>>();
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options ?? new ValidationServiceOptions();
        }

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
                var fluentErrors = ValidateWithFluentValidation(instance, options);
                errors.AddRange(fluentErrors);

                if (options.StopOnFirstError && errors.Any())
                {
                    return ValidationServiceResult.Failure(errors);
                }
            }

            // DataAnnotations
            if (_options.UseDataAnnotations)
            {
                var annotationErrors = ValidateWithDataAnnotations(instance, options);
                errors.AddRange(annotationErrors);

                if (options.StopOnFirstError && errors.Any())
                {
                    return ValidationServiceResult.Failure(errors);
                }
            }

            // Cascade validation for nested objects
            if (options.ValidateNestedObjects && _options.UseCascadeValidation)
            {
                var nestedErrors = ValidateNestedObjects(instance, options, 0, string.Empty);
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
                var fluentErrors = await ValidateWithFluentValidationAsync(instance, options, cancellationToken);
                errors.AddRange(fluentErrors);

                if (options.StopOnFirstError && errors.Any())
                {
                    return ValidationServiceResult.Failure(errors);
                }
            }

            // DataAnnotations (sync, no async version)
            if (_options.UseDataAnnotations)
            {
                var annotationErrors = ValidateWithDataAnnotations(instance, options);
                errors.AddRange(annotationErrors);

                if (options.StopOnFirstError && errors.Any())
                {
                    return ValidationServiceResult.Failure(errors);
                }
            }

            // Cascade validation for nested objects
            if (options.ValidateNestedObjects && _options.UseCascadeValidation)
            {
                var nestedErrors = await ValidateNestedObjectsAsync(instance, options, 0, string.Empty, cancellationToken);
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
            var index = 0;

            foreach (var instance in instances)
            {
                var result = Validate(instance);
                if (!result.IsValid)
                {
                    foreach (var error in result.Errors)
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
            var index = 0;

            foreach (var instance in instances)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await ValidateAsync(instance, cancellationToken);
                if (!result.IsValid)
                {
                    foreach (var error in result.Errors)
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
            var result = Validate(instance);
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
            var result = await ValidateAsync(instance, cancellationToken);
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

            foreach (var validator in _fluentValidators)
            {
                var result = validator.Validate(context);
                if (!result.IsValid)
                {
                    foreach (var failure in result.Errors)
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

            foreach (var validator in _fluentValidators)
            {
                var result = await validator.ValidateAsync(context, cancellationToken);
                if (!result.IsValid)
                {
                    foreach (var failure in result.Errors)
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
                foreach (var result in validationResults)
                {
                    var propertyName = result.MemberNames.Any()
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

            var type = instance.GetType();
            var properties = type.GetProperties()
                .Where(p => p.CanRead && !IsSimpleType(p.PropertyType));

            foreach (var property in properties)
            {
                var value = property.GetValue(instance);
                if (value == null)
                {
                    continue;
                }

                var newPath = options.IncludePropertyPath
                    ? (string.IsNullOrEmpty(propertyPath) ? property.Name : $"{propertyPath}.{property.Name}")
                    : property.Name;

                // Check for ValidateNested attribute
                var validateAttr = property.GetCustomAttributes(typeof(ValidateNestedAttribute), true)
                    .FirstOrDefault() as ValidateNestedAttribute;

                var shouldValidate = validateAttr != null ||
                                    typeof(IHasNestedValidation).IsAssignableFrom(property.PropertyType);

                if (!shouldValidate && !_options.ValidateAllNestedObjects)
                {
                    continue;
                }

                var maxDepth = validateAttr?.MaxDepth ?? options.MaxValidationDepth;
                if (currentDepth >= maxDepth)
                {
                    continue;
                }

                // Handle collections
                if (value is System.Collections.IEnumerable enumerable && !(value is string))
                {
                    var index = 0;
                    foreach (var item in enumerable)
                    {
                        if (item != null && !IsSimpleType(item.GetType()))
                        {
                            var itemPath = $"{newPath}[{index}]";
                            var itemErrors = ValidateObjectDynamic(item, options, currentDepth + 1, itemPath);
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
                    var nestedErrors = ValidateObjectDynamic(value, options, currentDepth + 1, newPath);
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

            var type = instance.GetType();
            var properties = type.GetProperties()
                .Where(p => p.CanRead && !IsSimpleType(p.PropertyType));

            foreach (var property in properties)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var value = property.GetValue(instance);
                if (value == null)
                {
                    continue;
                }

                var newPath = options.IncludePropertyPath
                    ? (string.IsNullOrEmpty(propertyPath) ? property.Name : $"{propertyPath}.{property.Name}")
                    : property.Name;

                // Check for ValidateNested attribute
                var validateAttr = property.GetCustomAttributes(typeof(ValidateNestedAttribute), true)
                    .FirstOrDefault() as ValidateNestedAttribute;

                var shouldValidate = validateAttr != null ||
                                    typeof(IHasNestedValidation).IsAssignableFrom(property.PropertyType);

                if (!shouldValidate && !_options.ValidateAllNestedObjects)
                {
                    continue;
                }

                var maxDepth = validateAttr?.MaxDepth ?? options.MaxValidationDepth;
                if (currentDepth >= maxDepth)
                {
                    continue;
                }

                // Handle collections
                if (value is System.Collections.IEnumerable enumerable && !(value is string))
                {
                    var index = 0;
                    foreach (var item in enumerable)
                    {
                        if (item != null && !IsSimpleType(item.GetType()))
                        {
                            var itemPath = $"{newPath}[{index}]";
                            var itemErrors = await ValidateObjectDynamicAsync(item, options, currentDepth + 1, itemPath, cancellationToken);
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
                    var nestedErrors = await ValidateObjectDynamicAsync(value, options, currentDepth + 1, newPath, cancellationToken);
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
                    foreach (var result in validationResults)
                    {
                        var propertyName = result.MemberNames.Any()
                            ? string.Join(", ", result.MemberNames)
                            : "Unknown";

                        var fullPath = options.IncludePropertyPath
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
                var validatorType = typeof(IValidator<>).MakeGenericType(instance.GetType());
                var validator = _serviceProvider.GetService(validatorType);

                if (validator != null)
                {
                    var validateMethod = validatorType.GetMethod("Validate", new[] { instance.GetType() });
                    if (validateMethod != null)
                    {
                        var result = validateMethod.Invoke(validator, new[] { instance }) as FluentValidation.Results.ValidationResult;
                        if (result != null && !result.IsValid)
                        {
                            foreach (var failure in result.Errors)
                            {
                                var fullPath = options.IncludePropertyPath
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
            var nestedErrors = ValidateNestedObjects(instance, options, depth, path);
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
                    foreach (var result in validationResults)
                    {
                        var propertyName = result.MemberNames.Any()
                            ? string.Join(", ", result.MemberNames)
                            : "Unknown";

                        var fullPath = options.IncludePropertyPath
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
                var validatorType = typeof(IValidator<>).MakeGenericType(instance.GetType());
                var validator = _serviceProvider.GetService(validatorType);

                if (validator != null)
                {
                    // Use reflection to call ValidateAsync
                    var validateMethod = validatorType.GetMethod("ValidateAsync",
                        new[] { instance.GetType(), typeof(CancellationToken) });

                    if (validateMethod != null)
                    {
                        var task = validateMethod.Invoke(validator, new object[] { instance, cancellationToken }) as Task;
                        if (task != null)
                        {
                            await task;
                            var resultProperty = task.GetType().GetProperty("Result");
                            var result = resultProperty?.GetValue(task) as FluentValidation.Results.ValidationResult;

                            if (result != null && !result.IsValid)
                            {
                                foreach (var failure in result.Errors)
                                {
                                    var fullPath = options.IncludePropertyPath
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
            var nestedErrors = await ValidateNestedObjectsAsync(instance, options, depth, path, cancellationToken);
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
}

