//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Core.Exceptions;
using System;
using System.Net;

namespace Mvp24Hours.WebAPI.Configuration
{
    /// <summary>
    /// Configuration options for the legacy exception middleware.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class configures the behavior of <see cref="Mvp24Hours.WebAPI.Middlewares.ExceptionMiddleware"/>.
    /// For RFC 7807 compliant responses, consider using <see cref="ProblemDetailsOptions"/> instead.
    /// </para>
    /// </remarks>
    public class ExceptionOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionOptions"/> class with default mappings.
        /// </summary>
        public ExceptionOptions()
        {
            StatusCodeHandle = GetDefaultStatusCode;
        }

        /// <summary>
        /// Gets or sets whether to include stack trace in error responses.
        /// </summary>
        /// <remarks>
        /// Should be set to false in production environments.
        /// </remarks>
        public bool TraceMiddleware { get; set; }

        /// <summary>
        /// Gets or sets the function that determines the HTTP status code for an exception.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default mapping handles common Mvp24Hours exceptions:
        /// <list type="bullet">
        /// <item><see cref="NotFoundException"/> → 404</item>
        /// <item><see cref="ValidationException"/> → 400</item>
        /// <item><see cref="UnauthorizedException"/> → 401</item>
        /// <item><see cref="ForbiddenException"/> → 403</item>
        /// <item><see cref="ConflictException"/> → 409</item>
        /// <item><see cref="DomainException"/> → 422</item>
        /// <item><see cref="BusinessException"/> → 422</item>
        /// <item>Other exceptions → 500</item>
        /// </list>
        /// </para>
        /// </remarks>
        public Func<Exception, int> StatusCodeHandle { get; set; }

        /// <summary>
        /// Gets the default HTTP status code for common exception types.
        /// </summary>
        /// <param name="exception">The exception to map.</param>
        /// <returns>The HTTP status code.</returns>
        public static int GetDefaultStatusCode(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception), "Exception cannot be null.");
            }

            return exception switch
            {
                // Mvp24Hours Core Exceptions
                NotFoundException => (int)HttpStatusCode.NotFound,
                ValidationException => (int)HttpStatusCode.BadRequest,
                UnauthorizedException => (int)HttpStatusCode.Unauthorized,
                ForbiddenException => (int)HttpStatusCode.Forbidden,
                ConflictException => (int)HttpStatusCode.Conflict,
                DomainException => (int)HttpStatusCode.UnprocessableEntity,
                BusinessException => (int)HttpStatusCode.UnprocessableEntity,
                DataException => (int)HttpStatusCode.InternalServerError,
                ConfigurationException => (int)HttpStatusCode.InternalServerError,
                PipelineException => (int)HttpStatusCode.InternalServerError,

                // .NET Framework Exceptions (most specific first)
                ArgumentNullException => (int)HttpStatusCode.BadRequest,
                ArgumentOutOfRangeException => (int)HttpStatusCode.BadRequest,
                ArgumentException => (int)HttpStatusCode.BadRequest,
                InvalidOperationException => (int)HttpStatusCode.Conflict,
                UnauthorizedAccessException => (int)HttpStatusCode.Forbidden,
                TimeoutException => (int)HttpStatusCode.RequestTimeout,
                OperationCanceledException => 499, // Client Closed Request
                NotImplementedException => (int)HttpStatusCode.NotImplemented,
                NotSupportedException => (int)HttpStatusCode.NotImplemented,

                // Default
                _ => (int)HttpStatusCode.InternalServerError
            };
        }

        /// <summary>
        /// Creates exception options with default Mvp24Hours mappings enabled.
        /// </summary>
        /// <returns>A new ExceptionOptions instance with default mappings.</returns>
        public static ExceptionOptions WithDefaultMappings()
        {
            return new ExceptionOptions
            {
                StatusCodeHandle = GetDefaultStatusCode
            };
        }

        /// <summary>
        /// Creates exception options that always return 500 Internal Server Error.
        /// </summary>
        /// <returns>A new ExceptionOptions instance with all exceptions mapped to 500.</returns>
        public static ExceptionOptions WithInternalServerErrorOnly()
        {
            return new ExceptionOptions
            {
                StatusCodeHandle = _ => (int)HttpStatusCode.InternalServerError
            };
        }
    }
}
