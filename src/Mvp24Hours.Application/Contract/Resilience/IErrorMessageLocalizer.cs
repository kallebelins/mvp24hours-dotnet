//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Globalization;

namespace Mvp24Hours.Application.Contract.Resilience
{
    /// <summary>
    /// Provides localized error messages based on error codes.
    /// Enables internationalization (i18n) of error messages.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implement this interface to provide localized error messages.
    /// The default implementation returns the error code or a fallback message.
    /// </para>
    /// <para>
    /// Integration with ASP.NET Core localization:
    /// <code>
    /// services.AddSingleton&lt;IErrorMessageLocalizer, ResourceBasedErrorMessageLocalizer&gt;();
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class ResourceBasedErrorMessageLocalizer : IErrorMessageLocalizer
    /// {
    ///     private readonly IStringLocalizer&lt;ErrorMessages&gt; _localizer;
    ///     
    ///     public string GetMessage(string errorCode, params object[] args)
    ///     {
    ///         return _localizer[errorCode, args];
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IErrorMessageLocalizer
    {
        /// <summary>
        /// Gets a localized error message for the specified error code.
        /// </summary>
        /// <param name="errorCode">The error code (e.g., "VALIDATION.REQUIRED").</param>
        /// <param name="args">Optional format arguments for the message.</param>
        /// <returns>The localized error message.</returns>
        string GetMessage(string errorCode, params object[] args);

        /// <summary>
        /// Gets a localized error message for the specified error code and culture.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="culture">The culture for localization.</param>
        /// <param name="args">Optional format arguments for the message.</param>
        /// <returns>The localized error message.</returns>
        string GetMessage(string errorCode, CultureInfo culture, params object[] args);

        /// <summary>
        /// Checks if a localized message exists for the error code.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <returns>True if a localized message exists.</returns>
        bool HasMessage(string errorCode);

        /// <summary>
        /// Gets a localized error message for a property validation error.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="propertyName">The name of the property with the error.</param>
        /// <param name="args">Optional format arguments.</param>
        /// <returns>The localized error message with property name included.</returns>
        string GetPropertyMessage(string errorCode, string propertyName, params object[] args);
    }
}

