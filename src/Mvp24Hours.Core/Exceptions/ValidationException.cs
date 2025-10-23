//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Mvp24Hours.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when entity or data validation fails.
    /// </summary>
    /// <remarks>
    /// This exception is typically thrown when FluentValidation rules or DataAnnotations
    /// validations fail. It can include detailed information about which validation rules
    /// were violated and for which properties.
    /// </remarks>
    /// <example>
    /// <code>
    /// var validationErrors = new List&lt;IMessageResult&gt; 
    /// { 
    ///     new MessageResult("Email", "Email is required"),
    ///     new MessageResult("Name", "Name must be at least 3 characters")
    /// };
    /// throw new ValidationException("Validation failed", validationErrors);
    /// </code>
    /// </example>
    [Serializable]
    public class ValidationException : Mvp24HoursException
    {
        /// <summary>
        /// Gets the collection of validation errors.
        /// </summary>
        /// <remarks>
        /// Each message contains information about a specific validation failure,
        /// including the property name and the error message.
        /// </remarks>
        public IList<IMessageResult>? ValidationErrors { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class.
        /// </summary>
        public ValidationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ValidationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class with validation errors.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="validationErrors">The collection of validation errors.</param>
        public ValidationException(string message, IList<IMessageResult> validationErrors)
            : base(message)
        {
            ValidationErrors = validationErrors;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified error message,
        /// error code, and optional context information.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="errorCode">A unique code identifying the validation error type.</param>
        /// <param name="context">Additional context information about the error.</param>
        public ValidationException(string message, string errorCode, IDictionary<string, object>? context = null)
            : base(message, errorCode, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class with validation errors,
        /// error code, and optional context information.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="errorCode">A unique code identifying the validation error type.</param>
        /// <param name="validationErrors">The collection of validation errors.</param>
        /// <param name="context">Additional context information about the error.</param>
        public ValidationException(string message, string errorCode, IList<IMessageResult> validationErrors, IDictionary<string, object>? context = null)
            : base(message, errorCode, context)
        {
            ValidationErrors = validationErrors;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information.</param>
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
        protected ValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

