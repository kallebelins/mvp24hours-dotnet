//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;

namespace Mvp24Hours.Core.Contract.Infrastructure.Options;

/// <summary>
/// Interface for strongly-typed options validation.
/// Provides a fluent validation API for configuration options.
/// </summary>
/// <typeparam name="TOptions">The type of options to validate.</typeparam>
/// <remarks>
/// <para>
/// This interface provides a simplified validation contract that can be used
/// alongside or instead of <see cref="Microsoft.Extensions.Options.IValidateOptions{TOptions}"/>.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// public class MyOptionsValidator : IOptionsValidator&lt;MyOptions&gt;
/// {
///     public OptionsValidationResult Validate(MyOptions options)
///     {
///         var errors = new List&lt;string&gt;();
///         
///         if (string.IsNullOrEmpty(options.ConnectionString))
///             errors.Add("ConnectionString is required.");
///             
///         return errors.Count == 0
///             ? OptionsValidationResult.Success()
///             : OptionsValidationResult.Fail(errors);
///     }
/// }
/// </code>
/// </remarks>
public interface IOptionsValidator<in TOptions> where TOptions : class
{
    /// <summary>
    /// Validates the specified options instance.
    /// </summary>
    /// <param name="options">The options instance to validate.</param>
    /// <returns>The validation result containing any errors.</returns>
    OptionsValidationResult Validate(TOptions options);
}

/// <summary>
/// Represents the result of options validation.
/// </summary>
public sealed class OptionsValidationResult
{
    /// <summary>
    /// Gets whether the validation succeeded.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets the validation failure messages.
    /// </summary>
    public IReadOnlyList<string> Failures { get; }

    /// <summary>
    /// Gets a single failure message combining all failures.
    /// </summary>
    public string? FailureMessage => Failures.Count > 0 ? string.Join("; ", Failures) : null;

    private OptionsValidationResult(bool succeeded, IReadOnlyList<string>? failures = null)
    {
        Succeeded = succeeded;
        Failures = failures ?? [];
    }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static OptionsValidationResult Success() => new(true);

    /// <summary>
    /// Creates a failed validation result with a single failure message.
    /// </summary>
    /// <param name="failureMessage">The failure message.</param>
    public static OptionsValidationResult Fail(string failureMessage)
        => new(false, new[] { failureMessage });

    /// <summary>
    /// Creates a failed validation result with multiple failure messages.
    /// </summary>
    /// <param name="failures">The failure messages.</param>
    public static OptionsValidationResult Fail(IReadOnlyList<string> failures)
        => new(false, failures);

    /// <summary>
    /// Creates a failed validation result with multiple failure messages.
    /// </summary>
    /// <param name="failures">The failure messages.</param>
    public static OptionsValidationResult Fail(IEnumerable<string> failures)
        => new(false, new List<string>(failures));
}

