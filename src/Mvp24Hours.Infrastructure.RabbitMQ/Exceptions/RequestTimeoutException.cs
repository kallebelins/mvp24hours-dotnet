//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Exceptions
{
    /// <summary>
    /// Exception thrown when a request times out waiting for a response.
    /// </summary>
    public class RequestTimeoutException : TimeoutException
    {
        /// <summary>
        /// Gets the correlation ID of the request that timed out.
        /// </summary>
        public string? CorrelationId { get; }

        /// <summary>
        /// Gets the request type.
        /// </summary>
        public Type? RequestType { get; }

        /// <summary>
        /// Gets the expected response type.
        /// </summary>
        public Type? ResponseType { get; }

        /// <summary>
        /// Gets the timeout duration.
        /// </summary>
        public TimeSpan Timeout { get; }

        /// <summary>
        /// Creates a new request timeout exception.
        /// </summary>
        public RequestTimeoutException()
            : base("Request timed out waiting for response.")
        {
        }

        /// <summary>
        /// Creates a new request timeout exception with a message.
        /// </summary>
        /// <param name="message">The error message.</param>
        public RequestTimeoutException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new request timeout exception with a message and inner exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public RequestTimeoutException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates a new request timeout exception with full details.
        /// </summary>
        /// <param name="requestType">The request type.</param>
        /// <param name="responseType">The expected response type.</param>
        /// <param name="timeout">The timeout duration.</param>
        /// <param name="correlationId">The correlation ID.</param>
        public RequestTimeoutException(Type requestType, Type responseType, TimeSpan timeout, string? correlationId = null)
            : base($"Request of type '{requestType.Name}' timed out after {timeout.TotalMilliseconds}ms waiting for response of type '{responseType.Name}'.")
        {
            RequestType = requestType;
            ResponseType = responseType;
            Timeout = timeout;
            CorrelationId = correlationId;
        }
    }
}

