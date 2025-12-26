//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Resilience.Exceptions
{
    /// <summary>
    /// Exception thrown when a circuit breaker is open and cannot execute operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exception is thrown when attempting to execute an operation while the
    /// circuit breaker is in the Open state. The circuit breaker opens when too
    /// many failures occur, preventing further requests from being executed.
    /// </para>
    /// </remarks>
    public class CircuitBreakerOpenException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class.
        /// </summary>
        public CircuitBreakerOpenException()
            : base("Circuit breaker is open. Operation cannot be executed.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class
        /// with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CircuitBreakerOpenException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class
        /// with a specified error message and a reference to the inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public CircuitBreakerOpenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}

