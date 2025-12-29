//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Infrastructure.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Mvp24Hours.Core.Extensions.Options;

/// <summary>
/// Base class for options validators with fluent validation API.
/// </summary>
/// <typeparam name="TOptions">The type of options to validate.</typeparam>
/// <remarks>
/// <para>
/// Provides a base implementation with common validation patterns and a fluent API
/// for building validation rules. Also integrates with Data Annotations validation.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// public class MyOptionsValidator : OptionsValidatorBase&lt;MyOptions&gt;
/// {
///     protected override void ConfigureValidation(OptionsValidationContext&lt;MyOptions&gt; context, MyOptions options)
///     {
///         context.ValidateProperty(nameof(options.ConnectionString), options.ConnectionString)
///             .NotNullOrEmpty("ConnectionString is required");
///             
///         context.ValidateProperty(nameof(options.Timeout), options.Timeout)
///             .Positive("Timeout must be positive");
///             
///         context.ValidateProperty(nameof(options.Port), options.Port)
///             .InRange(1, 65535, "Port must be between 1 and 65535");
///     }
/// }
/// </code>
/// </remarks>
public abstract class OptionsValidatorBase<TOptions> :
    IOptionsValidator<TOptions>,
    IValidateOptions<TOptions>
    where TOptions : class
{
    /// <summary>
    /// Gets or sets whether to include Data Annotations validation.
    /// Default is <c>true</c>.
    /// </summary>
    protected virtual bool IncludeDataAnnotations => true;

    /// <summary>
    /// Validates the specified options instance.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <returns>The validation result.</returns>
    public OptionsValidationResult Validate(TOptions options)
    {
        if (options == null)
        {
            return OptionsValidationResult.Fail($"{typeof(TOptions).Name} cannot be null.");
        }

        var errors = new List<string>();

        // First, validate with Data Annotations if enabled
        if (IncludeDataAnnotations)
        {
            var dataAnnotationsResult = ValidateDataAnnotations(options);
            if (!dataAnnotationsResult.Succeeded)
            {
                errors.AddRange(dataAnnotationsResult.Failures);
            }
        }

        // Then, run custom validation
        var context = new OptionsValidationContext<TOptions>();
        ConfigureValidation(context, options);

        if (context.HasErrors)
        {
            errors.AddRange(context.Errors);
        }

        return errors.Count == 0
            ? OptionsValidationResult.Success()
            : OptionsValidationResult.Fail(errors);
    }

    /// <summary>
    /// Configures the validation rules for the options.
    /// Override this method to add custom validation logic.
    /// </summary>
    /// <param name="context">The validation context for adding errors.</param>
    /// <param name="options">The options being validated.</param>
    protected abstract void ConfigureValidation(
        OptionsValidationContext<TOptions> context,
        TOptions options);

    /// <summary>
    /// Validates the options using Data Annotations.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <returns>The validation result.</returns>
    protected OptionsValidationResult ValidateDataAnnotations(TOptions options)
    {
        var validationContext = new ValidationContext(options);
        var results = new List<ValidationResult>();

        if (Validator.TryValidateObject(options, validationContext, results, validateAllProperties: true))
        {
            return OptionsValidationResult.Success();
        }

        var errors = new List<string>();
        foreach (var result in results)
        {
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                errors.Add(result.ErrorMessage);
            }
        }

        return OptionsValidationResult.Fail(errors);
    }

    /// <summary>
    /// Creates a validation error for a property.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>The formatted error message.</returns>
    protected static string PropertyError(string propertyName, string errorMessage)
    {
        return $"{typeof(TOptions).Name}.{propertyName}: {errorMessage}";
    }

    #region IValidateOptions<TOptions> Implementation

    /// <summary>
    /// Validates the options for integration with the Options framework.
    /// </summary>
    ValidateOptionsResult IValidateOptions<TOptions>.Validate(string? name, TOptions options)
    {
        var result = Validate(options);

        return result.Succeeded
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(result.Failures);
    }

    #endregion
}

/// <summary>
/// Simplified options validator base that only requires implementing
/// a validation predicate and failure message.
/// </summary>
/// <typeparam name="TOptions">The type of options to validate.</typeparam>
/// <remarks>
/// <para>
/// Use this when you have simple validation requirements that can be
/// expressed as a single predicate.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class ConnectionStringValidator : SimpleOptionsValidatorBase&lt;DatabaseOptions&gt;
/// {
///     protected override bool IsValid(DatabaseOptions options)
///         => !string.IsNullOrEmpty(options.ConnectionString);
///     
///     protected override string FailureMessage
///         => "ConnectionString is required for DatabaseOptions.";
/// }
/// </code>
/// </example>
public abstract class SimpleOptionsValidatorBase<TOptions> :
    IOptionsValidator<TOptions>,
    IValidateOptions<TOptions>
    where TOptions : class
{
    /// <summary>
    /// Gets the failure message when validation fails.
    /// </summary>
    protected abstract string FailureMessage { get; }

    /// <summary>
    /// Determines whether the options are valid.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <returns><c>true</c> if valid; otherwise, <c>false</c>.</returns>
    protected abstract bool IsValid(TOptions options);

    /// <summary>
    /// Validates the specified options instance.
    /// </summary>
    public OptionsValidationResult Validate(TOptions options)
    {
        if (options == null)
        {
            return OptionsValidationResult.Fail($"{typeof(TOptions).Name} cannot be null.");
        }

        return IsValid(options)
            ? OptionsValidationResult.Success()
            : OptionsValidationResult.Fail(FailureMessage);
    }

    /// <summary>
    /// Validates the options for integration with the Options framework.
    /// </summary>
    ValidateOptionsResult IValidateOptions<TOptions>.Validate(string? name, TOptions options)
    {
        var result = Validate(options);

        return result.Succeeded
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(result.Failures);
    }
}

/// <summary>
/// Composite validator that combines multiple validators for the same options type.
/// </summary>
/// <typeparam name="TOptions">The type of options to validate.</typeparam>
public sealed class CompositeOptionsValidator<TOptions> :
    IOptionsValidator<TOptions>,
    IValidateOptions<TOptions>
    where TOptions : class
{
    private readonly IEnumerable<IOptionsValidator<TOptions>> _validators;

    /// <summary>
    /// Initializes a new instance of the composite validator.
    /// </summary>
    /// <param name="validators">The validators to combine.</param>
    public CompositeOptionsValidator(IEnumerable<IOptionsValidator<TOptions>> validators)
    {
        _validators = validators ?? throw new ArgumentNullException(nameof(validators));
    }

    /// <summary>
    /// Validates the options using all registered validators.
    /// </summary>
    public OptionsValidationResult Validate(TOptions options)
    {
        if (options == null)
        {
            return OptionsValidationResult.Fail($"{typeof(TOptions).Name} cannot be null.");
        }

        var errors = new List<string>();

        foreach (var validator in _validators)
        {
            var result = validator.Validate(options);
            if (!result.Succeeded)
            {
                errors.AddRange(result.Failures);
            }
        }

        return errors.Count == 0
            ? OptionsValidationResult.Success()
            : OptionsValidationResult.Fail(errors);
    }

    /// <summary>
    /// Validates the options for integration with the Options framework.
    /// </summary>
    ValidateOptionsResult IValidateOptions<TOptions>.Validate(string? name, TOptions options)
    {
        var result = Validate(options);

        return result.Succeeded
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(result.Failures);
    }
}

/// <summary>
/// Inline options validator using a delegate.
/// </summary>
/// <typeparam name="TOptions">The type of options to validate.</typeparam>
public sealed class DelegateOptionsValidator<TOptions> :
    IOptionsValidator<TOptions>,
    IValidateOptions<TOptions>
    where TOptions : class
{
    private readonly Func<TOptions, OptionsValidationResult> _validateFunc;

    /// <summary>
    /// Initializes a new instance of the delegate validator.
    /// </summary>
    /// <param name="validateFunc">The validation function.</param>
    public DelegateOptionsValidator(Func<TOptions, OptionsValidationResult> validateFunc)
    {
        _validateFunc = validateFunc ?? throw new ArgumentNullException(nameof(validateFunc));
    }

    /// <summary>
    /// Creates a validator from a predicate and failure message.
    /// </summary>
    /// <param name="predicate">The validation predicate.</param>
    /// <param name="failureMessage">The failure message when the predicate returns false.</param>
    /// <returns>A new delegate validator.</returns>
    public static DelegateOptionsValidator<TOptions> Create(
        Func<TOptions, bool> predicate,
        string failureMessage)
    {
        return new DelegateOptionsValidator<TOptions>(options =>
            predicate(options)
                ? OptionsValidationResult.Success()
                : OptionsValidationResult.Fail(failureMessage));
    }

    /// <summary>
    /// Validates the options using the delegate.
    /// </summary>
    public OptionsValidationResult Validate(TOptions options)
    {
        if (options == null)
        {
            return OptionsValidationResult.Fail($"{typeof(TOptions).Name} cannot be null.");
        }

        return _validateFunc(options);
    }

    /// <summary>
    /// Validates the options for integration with the Options framework.
    /// </summary>
    ValidateOptionsResult IValidateOptions<TOptions>.Validate(string? name, TOptions options)
    {
        var result = Validate(options);

        return result.Succeeded
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(result.Failures);
    }
}

