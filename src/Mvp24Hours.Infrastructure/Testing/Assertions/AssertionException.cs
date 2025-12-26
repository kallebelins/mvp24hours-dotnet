//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using System;

namespace Mvp24Hours.Infrastructure.Testing.Assertions
{
    /// <summary>
    /// Exception thrown when a test assertion fails.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exception is used by the assertion helpers in the Testing namespace
    /// when a condition is not met during testing.
    /// </para>
    /// </remarks>
    public class AssertionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssertionException"/> class.
        /// </summary>
        public AssertionException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssertionException"/> class with a message.
        /// </summary>
        /// <param name="message">The assertion failure message.</param>
        public AssertionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssertionException"/> class with a message and inner exception.
        /// </summary>
        /// <param name="message">The assertion failure message.</param>
        /// <param name="innerException">The inner exception.</param>
        public AssertionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}

