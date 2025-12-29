//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when a rate limit is exceeded.
    /// </summary>
    public class RateLimitExceededException : Exception
    {
        /// <summary>
        /// Default error code for rate limit exceeded.
        /// </summary>
        public const string DefaultErrorCode = "RATE_LIMIT_EXCEEDED";

        /// <summary>
        /// Gets the error code for this exception.
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Gets the rate limiter key that was exceeded.
        /// </summary>
        public string RateLimiterKey { get; }

        /// <summary>
        /// Gets the time after which the operation can be retried.
        /// </summary>
        public TimeSpan? RetryAfter { get; }

        /// <summary>
        /// Gets the permit limit configured for this rate limiter.
        /// </summary>
        public int? PermitLimit { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="RateLimitExceededException"/>.
        /// </summary>
        public RateLimitExceededException()
            : this("Rate limit exceeded. Please try again later.")
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RateLimitExceededException"/> with a message.
        /// </summary>
        public RateLimitExceededException(string message)
            : this(message, "default", null, null, DefaultErrorCode)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RateLimitExceededException"/> with details.
        /// </summary>
        public RateLimitExceededException(
            string message,
            string rateLimiterKey,
            TimeSpan? retryAfter = null,
            int? permitLimit = null,
            string errorCode = DefaultErrorCode)
            : base(message)
        {
            RateLimiterKey = rateLimiterKey;
            RetryAfter = retryAfter;
            PermitLimit = permitLimit;
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RateLimitExceededException"/> with an inner exception.
        /// </summary>
        public RateLimitExceededException(
            string message,
            string rateLimiterKey,
            Exception innerException,
            TimeSpan? retryAfter = null,
            int? permitLimit = null,
            string errorCode = DefaultErrorCode)
            : base(message, innerException)
        {
            RateLimiterKey = rateLimiterKey;
            RetryAfter = retryAfter;
            PermitLimit = permitLimit;
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Creates a rate limit exceeded exception for a specific key.
        /// </summary>
        public static RateLimitExceededException ForKey(
            string key,
            TimeSpan? retryAfter = null,
            int? permitLimit = null)
        {
            var retryMessage = retryAfter.HasValue
                ? $" Retry after {retryAfter.Value.TotalSeconds:F0} seconds."
                : string.Empty;

            return new RateLimitExceededException(
                $"Rate limit exceeded for '{key}'.{retryMessage}",
                key,
                retryAfter,
                permitLimit);
        }
    }
}

