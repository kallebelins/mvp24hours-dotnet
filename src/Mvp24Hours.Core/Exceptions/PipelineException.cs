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
    /// Exception thrown when pipeline operations fail.
    /// </summary>
    /// <remarks>
    /// This exception is used to signal errors that occur during pipeline execution,
    /// including operation failures, validation errors in the pipeline, or issues
    /// with pipeline configuration. It can include information about which operation
    /// in the pipeline failed and the state of the pipeline message.
    /// </remarks>
    /// <example>
    /// <code>
    /// try
    /// {
    ///     await pipeline.ExecuteAsync(message);
    /// }
    /// catch (Exception ex)
    /// {
    ///     throw new PipelineException(
    ///         "Pipeline execution failed at operation ValidateCustomer", 
    ///         "PIPELINE_OPERATION_FAILED",
    ///         ex,
    ///         new Dictionary&lt;string, object&gt; 
    ///         { 
    ///             ["Operation"] = "ValidateCustomer",
    ///             ["MessageId"] = message.Id
    ///         }
    ///     );
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public class PipelineException : Mvp24HoursException
    {
        /// <summary>
        /// Gets the name of the pipeline operation that failed.
        /// </summary>
        public string? OperationName { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineException"/> class.
        /// </summary>
        public PipelineException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public PipelineException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public PipelineException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineException"/> class with a specified error message,
        /// error code, and optional context information.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="errorCode">A unique code identifying the pipeline error type.</param>
        /// <param name="context">Additional context information about the error.</param>
        public PipelineException(string message, string errorCode, IDictionary<string, object>? context = null)
            : base(message, errorCode, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineException"/> class with a specified error message,
        /// error code, operation name, and optional context information.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="errorCode">A unique code identifying the pipeline error type.</param>
        /// <param name="operationName">The name of the operation that failed.</param>
        /// <param name="context">Additional context information about the error.</param>
        public PipelineException(string message, string errorCode, string operationName, IDictionary<string, object>? context = null)
            : base(message, errorCode, context)
        {
            OperationName = operationName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineException"/> class with a specified error message,
        /// error code, inner exception, and optional context information.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="errorCode">A unique code identifying the pipeline error type.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <param name="context">Additional context information about the error.</param>
        public PipelineException(string message, string errorCode, Exception innerException, IDictionary<string, object>? context = null)
            : base(message, errorCode, innerException, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information.</param>
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
        protected PipelineException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

