//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Testing
{
    /// <summary>
    /// Represents the result of a consume operation in tests.
    /// </summary>
    public class ConsumeResult
    {
        /// <summary>
        /// Gets whether the consumption was successful.
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// Gets the exception if the consumption failed.
        /// </summary>
        public Exception? Exception { get; init; }

        /// <summary>
        /// Gets the duration of the consume operation.
        /// </summary>
        public TimeSpan Duration { get; init; }

        /// <summary>
        /// Gets the message ID.
        /// </summary>
        public string MessageId { get; init; } = string.Empty;

        /// <summary>
        /// Gets whether the message was acknowledged.
        /// </summary>
        public bool WasAcknowledged { get; init; }

        /// <summary>
        /// Gets whether the message was rejected.
        /// </summary>
        public bool WasRejected { get; init; }

        /// <summary>
        /// Gets whether the message was requeued.
        /// </summary>
        public bool WasRequeued { get; init; }

        /// <summary>
        /// Gets whether a timeout occurred.
        /// </summary>
        public bool TimedOut { get; init; }

        /// <summary>
        /// Gets whether the operation was cancelled.
        /// </summary>
        public bool WasCancelled { get; init; }

        /// <summary>
        /// Creates a successful consume result.
        /// </summary>
        public static ConsumeResult Success(string messageId, TimeSpan duration) => new()
        {
            IsSuccess = true,
            MessageId = messageId,
            Duration = duration,
            WasAcknowledged = true
        };

        /// <summary>
        /// Creates a failed consume result.
        /// </summary>
        public static ConsumeResult Failure(string messageId, Exception exception, TimeSpan duration, bool requeued = false) => new()
        {
            IsSuccess = false,
            MessageId = messageId,
            Exception = exception,
            Duration = duration,
            WasRejected = !requeued,
            WasRequeued = requeued
        };

        /// <summary>
        /// Creates a timeout consume result.
        /// </summary>
        public static ConsumeResult Timeout(string messageId, TimeSpan duration) => new()
        {
            IsSuccess = false,
            MessageId = messageId,
            Duration = duration,
            TimedOut = true,
            Exception = new TimeoutException($"Consume operation timed out after {duration.TotalMilliseconds}ms")
        };

        /// <summary>
        /// Creates a cancelled consume result.
        /// </summary>
        public static ConsumeResult Cancelled(string messageId, TimeSpan duration) => new()
        {
            IsSuccess = false,
            MessageId = messageId,
            Duration = duration,
            WasCancelled = true,
            Exception = new OperationCanceledException("Consume operation was cancelled")
        };
    }
}

