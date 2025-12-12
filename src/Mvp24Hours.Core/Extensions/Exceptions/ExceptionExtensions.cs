//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Core.ValueObjects.Logic;
using System;
using System.Net;

namespace Mvp24Hours.Core.Extensions.Exceptions
{
    /// <summary>
    /// Extension methods for converting exceptions to BusinessResult and HTTP status codes.
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Converts an exception to a failure BusinessResult.
        /// </summary>
        /// <typeparam name="T">The expected result type.</typeparam>
        /// <param name="exception">The exception to convert.</param>
        /// <returns>A failure BusinessResult with the exception message.</returns>
        /// <example>
        /// <code>
        /// try
        /// {
        ///     // Some operation
        /// }
        /// catch (Exception ex)
        /// {
        ///     return ex.ToBusinessResult&lt;Order&gt;();
        /// }
        /// </code>
        /// </example>
        public static IBusinessResult<T> ToBusinessResult<T>(this Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            var errorCode = exception.GetErrorCode();
            return BusinessResult.Failure<T>(exception.Message, errorCode);
        }

        /// <summary>
        /// Converts an exception to a failure BusinessResult with a custom message.
        /// </summary>
        /// <typeparam name="T">The expected result type.</typeparam>
        /// <param name="exception">The exception to convert.</param>
        /// <param name="message">Custom error message.</param>
        /// <returns>A failure BusinessResult with the custom message.</returns>
        public static IBusinessResult<T> ToBusinessResult<T>(this Exception exception, string message)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            var errorCode = exception.GetErrorCode();
            return BusinessResult.Failure<T>(message ?? exception.Message, errorCode);
        }

        /// <summary>
        /// Gets the error code associated with an exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>The error code string.</returns>
        public static string GetErrorCode(this Exception exception)
        {
            return exception switch
            {
                NotFoundException => "NOT_FOUND",
                ConflictException e => e.ErrorCode ?? "CONFLICT",
                UnauthorizedException e => e.ErrorCode ?? "UNAUTHORIZED",
                ForbiddenException e => e.ErrorCode ?? "FORBIDDEN",
                DomainException e => e.ErrorCode ?? "DOMAIN_ERROR",
                ValidationException => "VALIDATION_ERROR",
                BusinessException e => e.ErrorCode ?? "BUSINESS_ERROR",
                ConfigurationException e => e.ErrorCode ?? "CONFIGURATION_ERROR",
                DataException e => e.ErrorCode ?? "DATA_ERROR",
                PipelineException e => e.ErrorCode ?? "PIPELINE_ERROR",
                Mvp24HoursException e => e.ErrorCode ?? "MVP24_ERROR",
                ArgumentNullException => "ARGUMENT_NULL",
                ArgumentException => "ARGUMENT_INVALID",
                InvalidOperationException => "INVALID_OPERATION",
                TimeoutException => "TIMEOUT",
                OperationCanceledException => "OPERATION_CANCELLED",
                _ => "INTERNAL_ERROR"
            };
        }

        /// <summary>
        /// Maps an exception to an HTTP status code.
        /// </summary>
        /// <param name="exception">The exception to map.</param>
        /// <returns>The corresponding HTTP status code.</returns>
        /// <example>
        /// <code>
        /// catch (Exception ex)
        /// {
        ///     return StatusCode((int)ex.ToHttpStatusCode(), new { error = ex.Message });
        /// }
        /// </code>
        /// </example>
        public static HttpStatusCode ToHttpStatusCode(this Exception exception)
        {
            return exception switch
            {
                NotFoundException => HttpStatusCode.NotFound,
                ConflictException => HttpStatusCode.Conflict,
                UnauthorizedException => HttpStatusCode.Unauthorized,
                ForbiddenException => HttpStatusCode.Forbidden,
                ValidationException => HttpStatusCode.BadRequest,
                DomainException => HttpStatusCode.UnprocessableEntity,
                BusinessException => HttpStatusCode.BadRequest,
                ConfigurationException => HttpStatusCode.InternalServerError,
                DataException => HttpStatusCode.InternalServerError,
                PipelineException => HttpStatusCode.InternalServerError,
                ArgumentNullException => HttpStatusCode.BadRequest,
                ArgumentException => HttpStatusCode.BadRequest,
                InvalidOperationException => HttpStatusCode.BadRequest,
                TimeoutException => HttpStatusCode.RequestTimeout,
                OperationCanceledException => HttpStatusCode.BadRequest,
                HttpStatusCodeException e => e.StatusCode,
                _ => HttpStatusCode.InternalServerError
            };
        }

        /// <summary>
        /// Maps an exception to an HTTP status code integer.
        /// </summary>
        /// <param name="exception">The exception to map.</param>
        /// <returns>The corresponding HTTP status code as integer.</returns>
        public static int ToHttpStatusCodeInt(this Exception exception)
        {
            return (int)exception.ToHttpStatusCode();
        }

        /// <summary>
        /// Checks if the exception represents a client error (4xx).
        /// </summary>
        /// <param name="exception">The exception to check.</param>
        /// <returns>True if the exception maps to a 4xx status code.</returns>
        public static bool IsClientError(this Exception exception)
        {
            var statusCode = (int)exception.ToHttpStatusCode();
            return statusCode >= 400 && statusCode < 500;
        }

        /// <summary>
        /// Checks if the exception represents a server error (5xx).
        /// </summary>
        /// <param name="exception">The exception to check.</param>
        /// <returns>True if the exception maps to a 5xx status code.</returns>
        public static bool IsServerError(this Exception exception)
        {
            var statusCode = (int)exception.ToHttpStatusCode();
            return statusCode >= 500;
        }

        /// <summary>
        /// Gets a user-friendly error message from the exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="includeDetails">Whether to include detailed information (for development).</param>
        /// <returns>A user-friendly error message.</returns>
        public static string ToUserFriendlyMessage(this Exception exception, bool includeDetails = false)
        {
            var baseMessage = exception switch
            {
                NotFoundException e => e.Message,
                ConflictException e => e.Message,
                UnauthorizedException => "Authentication required. Please log in.",
                ForbiddenException => "You don't have permission to perform this action.",
                ValidationException e => $"Validation failed: {e.Message}",
                DomainException e => e.Message,
                BusinessException e => e.Message,
                TimeoutException => "The operation timed out. Please try again.",
                OperationCanceledException => "The operation was cancelled.",
                _ => "An unexpected error occurred. Please try again later."
            };

            if (includeDetails && exception is not (UnauthorizedException or ForbiddenException))
            {
                return $"{baseMessage} [{exception.GetType().Name}]";
            }

            return baseMessage;
        }
    }
}

