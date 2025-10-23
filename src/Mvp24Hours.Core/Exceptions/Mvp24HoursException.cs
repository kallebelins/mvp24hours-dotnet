//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Mvp24Hours.Core.Exceptions
{
    /// <summary>
    /// Base exception for all Mvp24Hours library exceptions.
    /// Provides additional context and structured error information.
    /// </summary>
    /// <remarks>
    /// This exception serves as the base class for all custom exceptions in the library.
    /// It provides additional properties to capture context-specific information that can
    /// be useful for logging, monitoring, and troubleshooting.
    /// </remarks>
    /// <example>
    /// <code>
    /// try 
    /// {
    ///     // Some operation
    /// }
    /// catch (Mvp24HoursException ex)
    /// {
    ///     // Handle library-specific errors
    ///     logger.LogError(ex, "Error code: {ErrorCode}, Context: {Context}", ex.ErrorCode, ex.Context);
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public class Mvp24HoursException : Exception
    {
        /// <summary>
        /// Gets the error code associated with this exception.
        /// </summary>
        /// <remarks>
        /// Error codes can be used for programmatic error handling and localization.
        /// </remarks>
        public string? ErrorCode { get; init; }

        /// <summary>
        /// Gets additional context information about the error.
        /// </summary>
        /// <remarks>
        /// Context can include relevant data like entity IDs, operation names, or any other
        /// information that helps understand the circumstances of the error.
        /// </remarks>
        public IDictionary<string, object>? Context { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Mvp24HoursException"/> class.
        /// </summary>
        public Mvp24HoursException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Mvp24HoursException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public Mvp24HoursException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Mvp24HoursException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public Mvp24HoursException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Mvp24HoursException"/> class with a specified error message,
        /// error code, and optional context information.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="errorCode">A unique code identifying the error type.</param>
        /// <param name="context">Additional context information about the error.</param>
        public Mvp24HoursException(string message, string errorCode, IDictionary<string, object>? context = null)
            : base(message)
        {
            ErrorCode = errorCode;
            Context = context;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Mvp24HoursException"/> class with a specified error message,
        /// error code, inner exception, and optional context information.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="errorCode">A unique code identifying the error type.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <param name="context">Additional context information about the error.</param>
        public Mvp24HoursException(string message, string errorCode, Exception innerException, IDictionary<string, object>? context = null)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            Context = context;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Mvp24HoursException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
        protected Mvp24HoursException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

