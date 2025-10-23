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
    /// Exception thrown when business rule violations occur.
    /// </summary>
    /// <remarks>
    /// This exception is used to signal violations of business rules or constraints
    /// that are not related to validation or data access. Examples include insufficient
    /// balance for a transaction, attempting to perform an operation in an invalid state,
    /// or violating domain-specific business logic.
    /// </remarks>
    /// <example>
    /// <code>
    /// if (account.Balance &lt; transaction.Amount)
    /// {
    ///     throw new BusinessException(
    ///         "Insufficient balance for transaction", 
    ///         "INSUFFICIENT_BALANCE",
    ///         new Dictionary&lt;string, object&gt; 
    ///         { 
    ///             ["AccountId"] = account.Id,
    ///             ["Balance"] = account.Balance,
    ///             ["Amount"] = transaction.Amount
    ///         }
    ///     );
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public class BusinessException : Mvp24HoursException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessException"/> class.
        /// </summary>
        public BusinessException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public BusinessException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public BusinessException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessException"/> class with a specified error message,
        /// error code, and optional context information.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="errorCode">A unique code identifying the business rule violation.</param>
        /// <param name="context">Additional context information about the error.</param>
        public BusinessException(string message, string errorCode, IDictionary<string, object>? context = null)
            : base(message, errorCode, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessException"/> class with a specified error message,
        /// error code, inner exception, and optional context information.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="errorCode">A unique code identifying the business rule violation.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <param name="context">Additional context information about the error.</param>
        public BusinessException(string message, string errorCode, Exception innerException, IDictionary<string, object>? context = null)
            : base(message, errorCode, innerException, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information.</param>
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
        protected BusinessException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

