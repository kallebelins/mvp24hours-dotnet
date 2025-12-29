//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Mvp24Hours.Core.Contract.Infrastructure.Options;

/// <summary>
/// Context for building validation rules in a fluent manner.
/// </summary>
/// <typeparam name="TOptions">The type of options being validated.</typeparam>
/// <remarks>
/// <para>
/// Provides a fluent API for validating options properties with common validation rules.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// var context = new OptionsValidationContext&lt;MyOptions&gt;();
/// context.ValidateProperty("ConnectionString", options.ConnectionString)
///     .NotNullOrEmpty()
///     .MaxLength(500);
/// context.ValidateProperty("Port", options.Port)
///     .InRange(1, 65535);
/// return context.ToResult();
/// </code>
/// </remarks>
public sealed class OptionsValidationContext<TOptions> where TOptions : class
{
    private readonly List<string> _errors = [];
    private readonly string _optionsName;

    /// <summary>
    /// Initializes a new instance of the validation context.
    /// </summary>
    /// <param name="optionsName">Optional name for the options (for error messages).</param>
    public OptionsValidationContext(string? optionsName = null)
    {
        _optionsName = optionsName ?? typeof(TOptions).Name;
    }

    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors => _errors;

    /// <summary>
    /// Gets whether the validation has any errors.
    /// </summary>
    public bool HasErrors => _errors.Count > 0;

    /// <summary>
    /// Adds a validation error.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    public void AddError(string errorMessage)
    {
        _errors.Add(errorMessage);
    }

    /// <summary>
    /// Adds a validation error with property name formatting.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="errorMessage">The error message.</param>
    public void AddPropertyError(string propertyName, string errorMessage)
    {
        _errors.Add($"{_optionsName}.{propertyName}: {errorMessage}");
    }

    /// <summary>
    /// Starts validation for a string property.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <returns>A fluent validator for the property.</returns>
    public StringPropertyValidator ValidateProperty(string propertyName, string? value)
    {
        return new StringPropertyValidator(this, propertyName, value);
    }

    /// <summary>
    /// Starts validation for a numeric property.
    /// </summary>
    /// <typeparam name="T">The numeric type.</typeparam>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <returns>A fluent validator for the property.</returns>
    public NumericPropertyValidator<T> ValidateProperty<T>(string propertyName, T value)
        where T : struct, IComparable<T>
    {
        return new NumericPropertyValidator<T>(this, propertyName, value);
    }

    /// <summary>
    /// Starts validation for a nullable numeric property.
    /// </summary>
    /// <typeparam name="T">The numeric type.</typeparam>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <returns>A fluent validator for the property.</returns>
    public NullableNumericPropertyValidator<T> ValidateProperty<T>(string propertyName, T? value)
        where T : struct, IComparable<T>
    {
        return new NullableNumericPropertyValidator<T>(this, propertyName, value);
    }

    /// <summary>
    /// Starts validation for a TimeSpan property.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <returns>A fluent validator for the property.</returns>
    public TimeSpanPropertyValidator ValidateTimeSpan(string propertyName, TimeSpan value)
    {
        return new TimeSpanPropertyValidator(this, propertyName, value);
    }

    /// <summary>
    /// Starts validation for a Uri property.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <returns>A fluent validator for the property.</returns>
    public UriPropertyValidator ValidateUri(string propertyName, Uri? value)
    {
        return new UriPropertyValidator(this, propertyName, value);
    }

    /// <summary>
    /// Validates that at least one of the specified conditions is true.
    /// </summary>
    /// <param name="errorMessage">The error message if no conditions are true.</param>
    /// <param name="conditions">The conditions to evaluate.</param>
    /// <returns>This context for chaining.</returns>
    public OptionsValidationContext<TOptions> AtLeastOne(string errorMessage, params bool[] conditions)
    {
        foreach (var condition in conditions)
        {
            if (condition) return this;
        }
        AddError($"{_optionsName}: {errorMessage}");
        return this;
    }

    /// <summary>
    /// Validates that exactly one of the specified conditions is true.
    /// </summary>
    /// <param name="errorMessage">The error message if not exactly one condition is true.</param>
    /// <param name="conditions">The conditions to evaluate.</param>
    /// <returns>This context for chaining.</returns>
    public OptionsValidationContext<TOptions> ExactlyOne(string errorMessage, params bool[] conditions)
    {
        int count = 0;
        foreach (var condition in conditions)
        {
            if (condition) count++;
        }
        if (count != 1)
        {
            AddError($"{_optionsName}: {errorMessage}");
        }
        return this;
    }

    /// <summary>
    /// Validates a custom condition.
    /// </summary>
    /// <param name="condition">The condition to check.</param>
    /// <param name="errorMessage">The error message if the condition is false.</param>
    /// <returns>This context for chaining.</returns>
    public OptionsValidationContext<TOptions> When(bool condition, string errorMessage)
    {
        if (!condition)
        {
            AddError(errorMessage);
        }
        return this;
    }

    /// <summary>
    /// Converts the validation context to a validation result.
    /// </summary>
    /// <returns>The validation result.</returns>
    public OptionsValidationResult ToResult()
    {
        return HasErrors
            ? OptionsValidationResult.Fail(_errors)
            : OptionsValidationResult.Success();
    }
}

/// <summary>
/// Fluent validator for string properties.
/// </summary>
public sealed class StringPropertyValidator
{
    private readonly OptionsValidationContext<object> _context;
    private readonly string _propertyName;
    private readonly string? _value;

    internal StringPropertyValidator(object context, string propertyName, string? value)
    {
        _context = (OptionsValidationContext<object>)context;
        _propertyName = propertyName;
        _value = value;
    }

    /// <summary>
    /// Validates that the value is not null or empty.
    /// </summary>
    public StringPropertyValidator NotNullOrEmpty(string? errorMessage = null)
    {
        if (string.IsNullOrEmpty(_value))
        {
            _context.AddPropertyError(_propertyName,
                errorMessage ?? "Value is required and cannot be empty.");
        }
        return this;
    }

    /// <summary>
    /// Validates that the value is not null, empty, or whitespace.
    /// </summary>
    public StringPropertyValidator NotNullOrWhiteSpace(string? errorMessage = null)
    {
        if (string.IsNullOrWhiteSpace(_value))
        {
            _context.AddPropertyError(_propertyName,
                errorMessage ?? "Value is required and cannot be empty or whitespace.");
        }
        return this;
    }

    /// <summary>
    /// Validates that the value has a maximum length.
    /// </summary>
    public StringPropertyValidator MaxLength(int maxLength, string? errorMessage = null)
    {
        if (_value != null && _value.Length > maxLength)
        {
            _context.AddPropertyError(_propertyName,
                errorMessage ?? $"Value cannot exceed {maxLength} characters.");
        }
        return this;
    }

    /// <summary>
    /// Validates that the value has a minimum length.
    /// </summary>
    public StringPropertyValidator MinLength(int minLength, string? errorMessage = null)
    {
        if (_value != null && _value.Length < minLength)
        {
            _context.AddPropertyError(_propertyName,
                errorMessage ?? $"Value must be at least {minLength} characters.");
        }
        return this;
    }

    /// <summary>
    /// Validates that the value matches a regex pattern.
    /// </summary>
    public StringPropertyValidator Matches(string pattern, string? errorMessage = null)
    {
        if (_value != null && !Regex.IsMatch(_value, pattern))
        {
            _context.AddPropertyError(_propertyName,
                errorMessage ?? $"Value does not match the required pattern: {pattern}");
        }
        return this;
    }

    /// <summary>
    /// Validates that the value is a valid email address.
    /// </summary>
    public StringPropertyValidator IsEmail(string? errorMessage = null)
    {
        if (!string.IsNullOrEmpty(_value))
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(_value);
                if (addr.Address != _value)
                {
                    _context.AddPropertyError(_propertyName,
                        errorMessage ?? "Value must be a valid email address.");
                }
            }
            catch
            {
                _context.AddPropertyError(_propertyName,
                    errorMessage ?? "Value must be a valid email address.");
            }
        }
        return this;
    }

    /// <summary>
    /// Validates that the value is a valid URI.
    /// </summary>
    public StringPropertyValidator IsUri(UriKind kind = UriKind.Absolute, string? errorMessage = null)
    {
        if (!string.IsNullOrEmpty(_value) && !Uri.TryCreate(_value, kind, out _))
        {
            _context.AddPropertyError(_propertyName,
                errorMessage ?? $"Value must be a valid {kind} URI.");
        }
        return this;
    }

    /// <summary>
    /// Validates a custom condition.
    /// </summary>
    public StringPropertyValidator Must(Func<string?, bool> predicate, string errorMessage)
    {
        if (!predicate(_value))
        {
            _context.AddPropertyError(_propertyName, errorMessage);
        }
        return this;
    }

    /// <summary>
    /// Conditionally validates the property.
    /// </summary>
    public StringPropertyValidator When(bool condition, Action<StringPropertyValidator> configure)
    {
        if (condition)
        {
            configure(this);
        }
        return this;
    }
}

/// <summary>
/// Fluent validator for numeric properties.
/// </summary>
/// <typeparam name="T">The numeric type.</typeparam>
public sealed class NumericPropertyValidator<T> where T : struct, IComparable<T>
{
    private readonly OptionsValidationContext<object> _context;
    private readonly string _propertyName;
    private readonly T _value;

    internal NumericPropertyValidator(object context, string propertyName, T value)
    {
        _context = (OptionsValidationContext<object>)context;
        _propertyName = propertyName;
        _value = value;
    }

    /// <summary>
    /// Validates that the value is greater than the specified minimum.
    /// </summary>
    public NumericPropertyValidator<T> GreaterThan(T minimum, string? errorMessage = null)
    {
        if (_value.CompareTo(minimum) <= 0)
        {
            _context.AddPropertyError(_propertyName,
                errorMessage ?? $"Value must be greater than {minimum}.");
        }
        return this;
    }

    /// <summary>
    /// Validates that the value is greater than or equal to the specified minimum.
    /// </summary>
    public NumericPropertyValidator<T> GreaterThanOrEqualTo(T minimum, string? errorMessage = null)
    {
        if (_value.CompareTo(minimum) < 0)
        {
            _context.AddPropertyError(_propertyName,
                errorMessage ?? $"Value must be greater than or equal to {minimum}.");
        }
        return this;
    }

    /// <summary>
    /// Validates that the value is less than the specified maximum.
    /// </summary>
    public NumericPropertyValidator<T> LessThan(T maximum, string? errorMessage = null)
    {
        if (_value.CompareTo(maximum) >= 0)
        {
            _context.AddPropertyError(_propertyName,
                errorMessage ?? $"Value must be less than {maximum}.");
        }
        return this;
    }

    /// <summary>
    /// Validates that the value is less than or equal to the specified maximum.
    /// </summary>
    public NumericPropertyValidator<T> LessThanOrEqualTo(T maximum, string? errorMessage = null)
    {
        if (_value.CompareTo(maximum) > 0)
        {
            _context.AddPropertyError(_propertyName,
                errorMessage ?? $"Value must be less than or equal to {maximum}.");
        }
        return this;
    }

    /// <summary>
    /// Validates that the value is within the specified range (inclusive).
    /// </summary>
    public NumericPropertyValidator<T> InRange(T minimum, T maximum, string? errorMessage = null)
    {
        if (_value.CompareTo(minimum) < 0 || _value.CompareTo(maximum) > 0)
        {
            _context.AddPropertyError(_propertyName,
                errorMessage ?? $"Value must be between {minimum} and {maximum}.");
        }
        return this;
    }

    /// <summary>
    /// Validates a custom condition.
    /// </summary>
    public NumericPropertyValidator<T> Must(Func<T, bool> predicate, string errorMessage)
    {
        if (!predicate(_value))
        {
            _context.AddPropertyError(_propertyName, errorMessage);
        }
        return this;
    }
}

/// <summary>
/// Fluent validator for nullable numeric properties.
/// </summary>
/// <typeparam name="T">The numeric type.</typeparam>
public sealed class NullableNumericPropertyValidator<T> where T : struct, IComparable<T>
{
    private readonly OptionsValidationContext<object> _context;
    private readonly string _propertyName;
    private readonly T? _value;

    internal NullableNumericPropertyValidator(object context, string propertyName, T? value)
    {
        _context = (OptionsValidationContext<object>)context;
        _propertyName = propertyName;
        _value = value;
    }

    /// <summary>
    /// Validates that the value is not null.
    /// </summary>
    public NullableNumericPropertyValidator<T> NotNull(string? errorMessage = null)
    {
        if (!_value.HasValue)
        {
            _context.AddPropertyError(_propertyName,
                errorMessage ?? "Value is required.");
        }
        return this;
    }

    /// <summary>
    /// Validates that the value, if present, is within the specified range (inclusive).
    /// </summary>
    public NullableNumericPropertyValidator<T> InRangeIfPresent(T minimum, T maximum, string? errorMessage = null)
    {
        if (_value.HasValue)
        {
            if (_value.Value.CompareTo(minimum) < 0 || _value.Value.CompareTo(maximum) > 0)
            {
                _context.AddPropertyError(_propertyName,
                    errorMessage ?? $"Value must be between {minimum} and {maximum}.");
            }
        }
        return this;
    }
}

/// <summary>
/// Fluent validator for TimeSpan properties.
/// </summary>
public sealed class TimeSpanPropertyValidator
{
    private readonly OptionsValidationContext<object> _context;
    private readonly string _propertyName;
    private readonly TimeSpan _value;

    internal TimeSpanPropertyValidator(object context, string propertyName, TimeSpan value)
    {
        _context = (OptionsValidationContext<object>)context;
        _propertyName = propertyName;
        _value = value;
    }

    /// <summary>
    /// Validates that the value is positive (greater than zero).
    /// </summary>
    public TimeSpanPropertyValidator Positive(string? errorMessage = null)
    {
        if (_value <= TimeSpan.Zero)
        {
            _context.AddPropertyError(_propertyName,
                errorMessage ?? "Value must be greater than zero.");
        }
        return this;
    }

    /// <summary>
    /// Validates that the value is not negative.
    /// </summary>
    public TimeSpanPropertyValidator NotNegative(string? errorMessage = null)
    {
        if (_value < TimeSpan.Zero)
        {
            _context.AddPropertyError(_propertyName,
                errorMessage ?? "Value cannot be negative.");
        }
        return this;
    }

    /// <summary>
    /// Validates that the value does not exceed the maximum.
    /// </summary>
    public TimeSpanPropertyValidator MaxValue(TimeSpan maximum, string? errorMessage = null)
    {
        if (_value > maximum)
        {
            _context.AddPropertyError(_propertyName,
                errorMessage ?? $"Value cannot exceed {maximum}.");
        }
        return this;
    }

    /// <summary>
    /// Validates that the value is at least the minimum.
    /// </summary>
    public TimeSpanPropertyValidator MinValue(TimeSpan minimum, string? errorMessage = null)
    {
        if (_value < minimum)
        {
            _context.AddPropertyError(_propertyName,
                errorMessage ?? $"Value must be at least {minimum}.");
        }
        return this;
    }
}

/// <summary>
/// Fluent validator for Uri properties.
/// </summary>
public sealed class UriPropertyValidator
{
    private readonly OptionsValidationContext<object> _context;
    private readonly string _propertyName;
    private readonly Uri? _value;

    internal UriPropertyValidator(object context, string propertyName, Uri? value)
    {
        _context = (OptionsValidationContext<object>)context;
        _propertyName = propertyName;
        _value = value;
    }

    /// <summary>
    /// Validates that the URI is not null.
    /// </summary>
    public UriPropertyValidator NotNull(string? errorMessage = null)
    {
        if (_value == null)
        {
            _context.AddPropertyError(_propertyName,
                errorMessage ?? "URI is required.");
        }
        return this;
    }

    /// <summary>
    /// Validates that the URI is absolute.
    /// </summary>
    public UriPropertyValidator IsAbsolute(string? errorMessage = null)
    {
        if (_value != null && !_value.IsAbsoluteUri)
        {
            _context.AddPropertyError(_propertyName,
                errorMessage ?? "URI must be absolute.");
        }
        return this;
    }

    /// <summary>
    /// Validates that the URI uses HTTPS.
    /// </summary>
    public UriPropertyValidator IsHttps(string? errorMessage = null)
    {
        if (_value != null && _value.Scheme != Uri.UriSchemeHttps)
        {
            _context.AddPropertyError(_propertyName,
                errorMessage ?? "URI must use HTTPS.");
        }
        return this;
    }

    /// <summary>
    /// Validates that the URI uses one of the allowed schemes.
    /// </summary>
    public UriPropertyValidator HasScheme(string[] allowedSchemes, string? errorMessage = null)
    {
        if (_value != null)
        {
            var hasValidScheme = false;
            foreach (var scheme in allowedSchemes)
            {
                if (string.Equals(_value.Scheme, scheme, StringComparison.OrdinalIgnoreCase))
                {
                    hasValidScheme = true;
                    break;
                }
            }
            if (!hasValidScheme)
            {
                _context.AddPropertyError(_propertyName,
                    errorMessage ?? $"URI must use one of the allowed schemes: {string.Join(", ", allowedSchemes)}");
            }
        }
        return this;
    }
}

