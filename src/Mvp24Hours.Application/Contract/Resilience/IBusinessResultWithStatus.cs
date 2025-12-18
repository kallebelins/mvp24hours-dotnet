//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System.Collections.Generic;

namespace Mvp24Hours.Application.Contract.Resilience
{
    /// <summary>
    /// Extended business result with status codes and error/warning distinction.
    /// Supports structured error codes for internationalization.
    /// </summary>
    /// <typeparam name="T">The type of the result data.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface extends <see cref="IBusinessResult{T}"/> with:
    /// <list type="bullet">
    /// <item><c>StatusCode</c>: A standardized status code for the result.</item>
    /// <item><c>HasWarnings</c>: Indicates if warnings are present.</item>
    /// <item><c>Errors</c>: Collection of error messages only.</item>
    /// <item><c>Warnings</c>: Collection of warning messages only.</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = BusinessResultWithStatus.Failure&lt;Order&gt;(
    ///     ResultStatusCode.NotFound,
    ///     "Order not found",
    ///     "ORDER.NOT_FOUND"
    /// );
    /// 
    /// // Check status code
    /// switch (result.StatusCode)
    /// {
    ///     case ResultStatusCode.Success:
    ///         return Ok(result.Data);
    ///     case ResultStatusCode.NotFound:
    ///         return NotFound(result);
    ///     case ResultStatusCode.ValidationFailed:
    ///         return BadRequest(result);
    ///     default:
    ///         return StatusCode(500, result);
    /// }
    /// </code>
    /// </example>
    public interface IBusinessResultWithStatus<T> : IBusinessResult<T>
    {
        /// <summary>
        /// Gets the status code of the result.
        /// </summary>
        ResultStatusCode StatusCode { get; }

        /// <summary>
        /// Gets whether the result has warning messages.
        /// </summary>
        bool HasWarnings { get; }

        /// <summary>
        /// Gets the error messages only.
        /// </summary>
        IReadOnlyCollection<IResultMessage> Errors { get; }

        /// <summary>
        /// Gets the warning messages only.
        /// </summary>
        IReadOnlyCollection<IResultMessage> Warnings { get; }

        /// <summary>
        /// Gets the info messages only.
        /// </summary>
        IReadOnlyCollection<IResultMessage> Infos { get; }

        /// <summary>
        /// Gets all extended messages with severity information.
        /// </summary>
        IReadOnlyCollection<IResultMessage> ExtendedMessages { get; }

        /// <summary>
        /// Gets the primary error code (from the first error message).
        /// Useful for i18n and programmatic error handling.
        /// </summary>
        string? PrimaryErrorCode { get; }

        /// <summary>
        /// Checks if the result has a specific error code.
        /// </summary>
        /// <param name="errorCode">The error code to check.</param>
        /// <returns>True if the error code exists in the messages.</returns>
        bool HasErrorCode(string errorCode);

        /// <summary>
        /// Checks if the result has an error for a specific property.
        /// </summary>
        /// <param name="propertyName">The property name to check.</param>
        /// <returns>True if there's an error for the property.</returns>
        bool HasPropertyError(string propertyName);
    }
}

