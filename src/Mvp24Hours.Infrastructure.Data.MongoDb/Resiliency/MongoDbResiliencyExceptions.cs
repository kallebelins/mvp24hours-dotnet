//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Resiliency
{
    /// <summary>
    /// Base exception for MongoDB resiliency-related errors.
    /// </summary>
    [Serializable]
    public class MongoDbResiliencyException : Exception
    {
        /// <summary>
        /// Gets the error code for this exception.
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbResiliencyException"/> class.
        /// </summary>
        public MongoDbResiliencyException()
            : this("A MongoDB resiliency error occurred.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbResiliencyException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public MongoDbResiliencyException(string message)
            : base(message)
        {
            ErrorCode = "MONGODB_RESILIENCY_ERROR";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbResiliencyException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="errorCode">The error code.</param>
        public MongoDbResiliencyException(string message, string errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbResiliencyException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public MongoDbResiliencyException(string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = "MONGODB_RESILIENCY_ERROR";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbResiliencyException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="innerException">The inner exception.</param>
        public MongoDbResiliencyException(string message, string errorCode, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Exception thrown when the circuit breaker is in open state and rejecting operations.
    /// </summary>
    [Serializable]
    public class MongoDbCircuitBreakerOpenException : MongoDbResiliencyException
    {
        /// <summary>
        /// Gets the remaining duration until the circuit breaker transitions to half-open state.
        /// </summary>
        public TimeSpan? RemainingDuration { get; }

        /// <summary>
        /// Gets the timestamp when the circuit breaker opened.
        /// </summary>
        public DateTimeOffset OpenedAt { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbCircuitBreakerOpenException"/> class.
        /// </summary>
        public MongoDbCircuitBreakerOpenException()
            : base("The circuit breaker is open. MongoDB operations are currently blocked.",
                   "MONGODB_CIRCUIT_BREAKER_OPEN")
        {
            OpenedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbCircuitBreakerOpenException"/> class.
        /// </summary>
        /// <param name="remainingDuration">The remaining duration until transition to half-open.</param>
        public MongoDbCircuitBreakerOpenException(TimeSpan remainingDuration)
            : base($"The circuit breaker is open. MongoDB operations are blocked for {remainingDuration.TotalSeconds:F1} more seconds.",
                   "MONGODB_CIRCUIT_BREAKER_OPEN")
        {
            RemainingDuration = remainingDuration;
            OpenedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbCircuitBreakerOpenException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="remainingDuration">The remaining duration until transition to half-open.</param>
        /// <param name="openedAt">When the circuit breaker opened.</param>
        public MongoDbCircuitBreakerOpenException(string message, TimeSpan remainingDuration, DateTimeOffset openedAt)
            : base(message, "MONGODB_CIRCUIT_BREAKER_OPEN")
        {
            RemainingDuration = remainingDuration;
            OpenedAt = openedAt;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbCircuitBreakerOpenException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public MongoDbCircuitBreakerOpenException(string message, Exception innerException)
            : base(message, "MONGODB_CIRCUIT_BREAKER_OPEN", innerException)
        {
            OpenedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Exception thrown when a MongoDB operation times out.
    /// </summary>
    [Serializable]
    public class MongoDbOperationTimeoutException : MongoDbResiliencyException
    {
        /// <summary>
        /// Gets the configured timeout duration.
        /// </summary>
        public TimeSpan Timeout { get; }

        /// <summary>
        /// Gets the type of operation that timed out.
        /// </summary>
        public string OperationType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbOperationTimeoutException"/> class.
        /// </summary>
        public MongoDbOperationTimeoutException()
            : base("The MongoDB operation timed out.", "MONGODB_OPERATION_TIMEOUT")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbOperationTimeoutException"/> class.
        /// </summary>
        /// <param name="timeout">The timeout duration.</param>
        public MongoDbOperationTimeoutException(TimeSpan timeout)
            : base($"The MongoDB operation timed out after {timeout.TotalSeconds:F1} seconds.",
                   "MONGODB_OPERATION_TIMEOUT")
        {
            Timeout = timeout;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbOperationTimeoutException"/> class.
        /// </summary>
        /// <param name="timeout">The timeout duration.</param>
        /// <param name="operationType">The type of operation that timed out.</param>
        public MongoDbOperationTimeoutException(TimeSpan timeout, string operationType)
            : base($"The MongoDB {operationType} operation timed out after {timeout.TotalSeconds:F1} seconds.",
                   "MONGODB_OPERATION_TIMEOUT")
        {
            Timeout = timeout;
            OperationType = operationType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbOperationTimeoutException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="timeout">The timeout duration.</param>
        /// <param name="innerException">The inner exception.</param>
        public MongoDbOperationTimeoutException(string message, TimeSpan timeout, Exception innerException)
            : base(message, "MONGODB_OPERATION_TIMEOUT", innerException)
        {
            Timeout = timeout;
        }
    }

    /// <summary>
    /// Exception thrown when all retry attempts have been exhausted.
    /// </summary>
    [Serializable]
    public class MongoDbRetryExhaustedException : MongoDbResiliencyException
    {
        /// <summary>
        /// Gets the number of retry attempts made.
        /// </summary>
        public int RetryCount { get; }

        /// <summary>
        /// Gets the total time spent on retries.
        /// </summary>
        public TimeSpan TotalRetryDuration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbRetryExhaustedException"/> class.
        /// </summary>
        public MongoDbRetryExhaustedException()
            : base("All retry attempts have been exhausted for the MongoDB operation.",
                   "MONGODB_RETRY_EXHAUSTED")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbRetryExhaustedException"/> class.
        /// </summary>
        /// <param name="retryCount">The number of retry attempts.</param>
        public MongoDbRetryExhaustedException(int retryCount)
            : base($"MongoDB operation failed after {retryCount} retry attempts.",
                   "MONGODB_RETRY_EXHAUSTED")
        {
            RetryCount = retryCount;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbRetryExhaustedException"/> class.
        /// </summary>
        /// <param name="retryCount">The number of retry attempts.</param>
        /// <param name="totalDuration">The total time spent on retries.</param>
        /// <param name="innerException">The inner exception from the last attempt.</param>
        public MongoDbRetryExhaustedException(int retryCount, TimeSpan totalDuration, Exception innerException)
            : base($"MongoDB operation failed after {retryCount} retry attempts over {totalDuration.TotalSeconds:F1} seconds.",
                   "MONGODB_RETRY_EXHAUSTED", innerException)
        {
            RetryCount = retryCount;
            TotalRetryDuration = totalDuration;
        }
    }

    /// <summary>
    /// Exception thrown when connection recovery fails.
    /// </summary>
    [Serializable]
    public class MongoDbConnectionRecoveryException : MongoDbResiliencyException
    {
        /// <summary>
        /// Gets the number of reconnection attempts made.
        /// </summary>
        public int ReconnectAttempts { get; }

        /// <summary>
        /// Gets the total time spent on reconnection attempts.
        /// </summary>
        public TimeSpan TotalReconnectDuration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbConnectionRecoveryException"/> class.
        /// </summary>
        public MongoDbConnectionRecoveryException()
            : base("Failed to recover MongoDB connection.", "MONGODB_CONNECTION_RECOVERY_FAILED")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbConnectionRecoveryException"/> class.
        /// </summary>
        /// <param name="reconnectAttempts">The number of reconnection attempts made.</param>
        public MongoDbConnectionRecoveryException(int reconnectAttempts)
            : base($"Failed to recover MongoDB connection after {reconnectAttempts} attempts.",
                   "MONGODB_CONNECTION_RECOVERY_FAILED")
        {
            ReconnectAttempts = reconnectAttempts;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbConnectionRecoveryException"/> class.
        /// </summary>
        /// <param name="reconnectAttempts">The number of reconnection attempts made.</param>
        /// <param name="totalDuration">The total time spent on reconnection.</param>
        /// <param name="innerException">The inner exception from the last attempt.</param>
        public MongoDbConnectionRecoveryException(int reconnectAttempts, TimeSpan totalDuration, Exception innerException)
            : base($"Failed to recover MongoDB connection after {reconnectAttempts} attempts over {totalDuration.TotalSeconds:F1} seconds.",
                   "MONGODB_CONNECTION_RECOVERY_FAILED", innerException)
        {
            ReconnectAttempts = reconnectAttempts;
            TotalReconnectDuration = totalDuration;
        }
    }

    /// <summary>
    /// Exception thrown when failover to a new primary fails.
    /// </summary>
    [Serializable]
    public class MongoDbFailoverException : MongoDbResiliencyException
    {
        /// <summary>
        /// Gets the server selection timeout that was exceeded.
        /// </summary>
        public TimeSpan ServerSelectionTimeout { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbFailoverException"/> class.
        /// </summary>
        public MongoDbFailoverException()
            : base("MongoDB replica set failover failed. No suitable server found.",
                   "MONGODB_FAILOVER_FAILED")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbFailoverException"/> class.
        /// </summary>
        /// <param name="serverSelectionTimeout">The timeout that was exceeded.</param>
        public MongoDbFailoverException(TimeSpan serverSelectionTimeout)
            : base($"MongoDB replica set failover failed. No suitable server found within {serverSelectionTimeout.TotalSeconds:F1} seconds.",
                   "MONGODB_FAILOVER_FAILED")
        {
            ServerSelectionTimeout = serverSelectionTimeout;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbFailoverException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public MongoDbFailoverException(string message, Exception innerException)
            : base(message, "MONGODB_FAILOVER_FAILED", innerException)
        {
        }
    }
}

