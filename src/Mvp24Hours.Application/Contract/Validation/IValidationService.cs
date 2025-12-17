//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Contract.Validation
{
    /// <summary>
    /// Injectable validation service interface for entity/DTO validation.
    /// Provides a centralized, testable validation mechanism that can be injected via DI.
    /// </summary>
    /// <typeparam name="T">The type to validate.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface abstracts validation logic and supports multiple validation strategies:
    /// <list type="bullet">
    /// <item>FluentValidation validators</item>
    /// <item>DataAnnotation validation</item>
    /// <item>Custom validation rules</item>
    /// <item>Cascade validation for nested objects</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Usage example:</strong>
    /// <code>
    /// public class CustomerService
    /// {
    ///     private readonly IValidationService&lt;CustomerDto&gt; _validationService;
    ///     
    ///     public CustomerService(IValidationService&lt;CustomerDto&gt; validationService)
    ///     {
    ///         _validationService = validationService;
    ///     }
    ///     
    ///     public IBusinessResult&lt;int&gt; Create(CustomerDto dto)
    ///     {
    ///         var result = _validationService.Validate(dto);
    ///         if (!result.IsValid)
    ///         {
    ///             return result.Errors.ToBusiness&lt;int&gt;();
    ///         }
    ///         // ... continue with creation
    ///     }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public interface IValidationService<T> where T : class
    {
        /// <summary>
        /// Validates the specified instance synchronously.
        /// </summary>
        /// <param name="instance">The instance to validate.</param>
        /// <returns>The validation result containing errors if validation failed.</returns>
        ValidationServiceResult Validate(T instance);

        /// <summary>
        /// Validates the specified instance synchronously with additional options.
        /// </summary>
        /// <param name="instance">The instance to validate.</param>
        /// <param name="options">Validation options for customizing behavior.</param>
        /// <returns>The validation result containing errors if validation failed.</returns>
        ValidationServiceResult Validate(T instance, ValidationOptions options);

        /// <summary>
        /// Validates the specified instance asynchronously.
        /// </summary>
        /// <param name="instance">The instance to validate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The validation result containing errors if validation failed.</returns>
        Task<ValidationServiceResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates the specified instance asynchronously with additional options.
        /// </summary>
        /// <param name="instance">The instance to validate.</param>
        /// <param name="options">Validation options for customizing behavior.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The validation result containing errors if validation failed.</returns>
        Task<ValidationServiceResult> ValidateAsync(T instance, ValidationOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a collection of instances synchronously.
        /// </summary>
        /// <param name="instances">The collection of instances to validate.</param>
        /// <returns>The validation result containing errors for all failed validations.</returns>
        ValidationServiceResult ValidateMany(IEnumerable<T> instances);

        /// <summary>
        /// Validates a collection of instances asynchronously.
        /// </summary>
        /// <param name="instances">The collection of instances to validate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The validation result containing errors for all failed validations.</returns>
        Task<ValidationServiceResult> ValidateManyAsync(IEnumerable<T> instances, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates and throws a ValidationException if validation fails.
        /// </summary>
        /// <param name="instance">The instance to validate.</param>
        /// <exception cref="Core.Exceptions.ValidationException">Thrown when validation fails.</exception>
        void ValidateAndThrow(T instance);

        /// <summary>
        /// Validates and throws a ValidationException if validation fails.
        /// </summary>
        /// <param name="instance">The instance to validate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="Core.Exceptions.ValidationException">Thrown when validation fails.</exception>
        Task ValidateAndThrowAsync(T instance, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents the result of a validation operation.
    /// </summary>
    public class ValidationServiceResult
    {
        /// <summary>
        /// Gets or sets whether the validation passed.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the collection of validation errors.
        /// </summary>
        public IList<IMessageResult> Errors { get; set; } = new List<IMessageResult>();

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        /// <returns>A successful validation result.</returns>
        public static ValidationServiceResult Success()
        {
            return new ValidationServiceResult { IsValid = true };
        }

        /// <summary>
        /// Creates a failed validation result with errors.
        /// </summary>
        /// <param name="errors">The validation errors.</param>
        /// <returns>A failed validation result.</returns>
        public static ValidationServiceResult Failure(IList<IMessageResult> errors)
        {
            return new ValidationServiceResult
            {
                IsValid = false,
                Errors = errors ?? new List<IMessageResult>()
            };
        }

        /// <summary>
        /// Creates a failed validation result with a single error.
        /// </summary>
        /// <param name="propertyName">The property name that failed validation.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>A failed validation result.</returns>
        public static ValidationServiceResult Failure(string propertyName, string errorMessage)
        {
            return new ValidationServiceResult
            {
                IsValid = false,
                Errors = new List<IMessageResult>
                {
                    new Core.ValueObjects.Logic.MessageResult(
                        propertyName,
                        errorMessage,
                        Core.Enums.MessageType.Error)
                }
            };
        }
    }

    /// <summary>
    /// Options for customizing validation behavior.
    /// </summary>
    public class ValidationOptions
    {
        /// <summary>
        /// Gets or sets whether to validate nested/child objects.
        /// Default is true.
        /// </summary>
        public bool ValidateNestedObjects { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include property path for nested validation errors.
        /// Default is true.
        /// </summary>
        public bool IncludePropertyPath { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum depth for nested validation.
        /// Default is 10 to prevent infinite loops.
        /// </summary>
        public int MaxValidationDepth { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to stop on first error.
        /// Default is false (collect all errors).
        /// </summary>
        public bool StopOnFirstError { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to throw exception on validation failure.
        /// Default is false.
        /// </summary>
        public bool ThrowOnValidationFailure { get; set; } = false;

        /// <summary>
        /// Gets or sets rulesets to include in validation.
        /// When null or empty, all rules are included.
        /// </summary>
        public string[]? RuleSets { get; set; }

        /// <summary>
        /// Gets the default validation options.
        /// </summary>
        public static ValidationOptions Default => new ValidationOptions();

        /// <summary>
        /// Gets options for cascade validation with nested objects.
        /// </summary>
        public static ValidationOptions WithCascadeValidation => new ValidationOptions
        {
            ValidateNestedObjects = true,
            IncludePropertyPath = true,
            MaxValidationDepth = 10
        };

        /// <summary>
        /// Gets options for fast validation (stop on first error).
        /// </summary>
        public static ValidationOptions FastValidation => new ValidationOptions
        {
            StopOnFirstError = true
        };
    }
}

