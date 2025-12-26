//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Resilience.Exceptions
{
    /// <summary>
    /// Exception thrown when a bulkhead is full and cannot accept more operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exception is thrown when attempting to execute an operation while the
    /// bulkhead has reached its maximum concurrency limit and cannot accept more operations.
    /// </para>
    /// </remarks>
    public class BulkheadRejectedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BulkheadRejectedException"/> class.
        /// </summary>
        public BulkheadRejectedException()
            : base("Bulkhead is full. Operation cannot be executed.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkheadRejectedException"/> class
        /// with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public BulkheadRejectedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkheadRejectedException"/> class
        /// with a specified error message and a reference to the inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public BulkheadRejectedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}

