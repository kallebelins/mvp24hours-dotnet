//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace Mvp24Hours.Application.Contract.Resilience
{
    /// <summary>
    /// Extended message result with severity and structured error codes.
    /// Supports internationalization through error codes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface extends <see cref="IMessageResult"/> with additional properties
    /// for enhanced error handling:
    /// <list type="bullet">
    /// <item><c>Severity</c>: Distinguishes between errors and warnings.</item>
    /// <item><c>ErrorCode</c>: Structured code for i18n and programmatic handling.</item>
    /// <item><c>PropertyName</c>: The property associated with the message (for validation).</item>
    /// <item><c>AttemptedValue</c>: The value that caused the error.</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var message = new ResultMessage(
    ///     severity: MessageSeverity.Error,
    ///     errorCode: "VALIDATION.EMAIL.INVALID",
    ///     message: "The email format is invalid",
    ///     propertyName: "Email",
    ///     attemptedValue: "invalid-email"
    /// );
    /// </code>
    /// </example>
    public interface IResultMessage : IMessageResult
    {
        /// <summary>
        /// Gets the severity of the message.
        /// </summary>
        MessageSeverity Severity { get; }

        /// <summary>
        /// Gets the structured error code for internationalization.
        /// Format: CATEGORY.SUBCATEGORY.ERROR (e.g., "VALIDATION.EMAIL.INVALID")
        /// </summary>
        string? ErrorCode { get; }

        /// <summary>
        /// Gets the property name associated with this message (typically for validation errors).
        /// </summary>
        string? PropertyName { get; }

        /// <summary>
        /// Gets the value that was attempted when the error occurred.
        /// </summary>
        object? AttemptedValue { get; }

        /// <summary>
        /// Gets additional metadata associated with the message.
        /// </summary>
        IDictionary<string, object?>? Metadata { get; }
    }
}

