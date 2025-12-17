//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Mvp24Hours.WebAPI.Exceptions
{
    /// <summary>
    /// Defines the contract for mapping exceptions to ProblemDetails responses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations of this interface are responsible for converting exceptions
    /// into standardized ProblemDetails responses following RFC 7807.
    /// </para>
    /// <para>
    /// The mapper determines:
    /// <list type="bullet">
    /// <item>The HTTP status code for the response</item>
    /// <item>The problem type URI</item>
    /// <item>The title and detail of the error</item>
    /// <item>Any additional extension members</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IExceptionToProblemDetailsMapper
    {
        /// <summary>
        /// Maps an exception to a ProblemDetails response.
        /// </summary>
        /// <param name="exception">The exception to map.</param>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <returns>A ProblemDetails instance representing the error.</returns>
        ProblemDetails Map(Exception exception, HttpContext context);

        /// <summary>
        /// Gets the HTTP status code for an exception.
        /// </summary>
        /// <param name="exception">The exception to get the status code for.</param>
        /// <returns>The HTTP status code.</returns>
        int GetStatusCode(Exception exception);

        /// <summary>
        /// Determines whether the mapper can handle the specified exception type.
        /// </summary>
        /// <param name="exception">The exception to check.</param>
        /// <returns>True if the mapper can handle the exception; otherwise, false.</returns>
        bool CanHandle(Exception exception);
    }
}

