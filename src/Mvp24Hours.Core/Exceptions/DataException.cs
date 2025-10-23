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
    /// Exception thrown when data access or persistence operations fail.
    /// </summary>
    /// <remarks>
    /// This exception is typically thrown when operations involving repositories,
    /// unit of work, or direct database access encounter errors. It can represent
    /// connection failures, constraint violations, timeout errors, or other data-related issues.
    /// </remarks>
    /// <example>
    /// <code>
    /// if (entity == null)
    /// {
    ///     throw new DataException(
    ///         "Entity not found", 
    ///         "DATA_NOT_FOUND",
    ///         new Dictionary&lt;string, object&gt; { ["EntityId"] = id }
    ///     );
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public class DataException : Mvp24HoursException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataException"/> class.
        /// </summary>
        public DataException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DataException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DataException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataException"/> class with a specified error message,
        /// error code, and optional context information.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="errorCode">A unique code identifying the data error type.</param>
        /// <param name="context">Additional context information about the error.</param>
        public DataException(string message, string errorCode, IDictionary<string, object>? context = null)
            : base(message, errorCode, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataException"/> class with a specified error message,
        /// error code, inner exception, and optional context information.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="errorCode">A unique code identifying the data error type.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <param name="context">Additional context information about the error.</param>
        public DataException(string message, string errorCode, Exception innerException, IDictionary<string, object>? context = null)
            : base(message, errorCode, innerException, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information.</param>
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
        protected DataException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

