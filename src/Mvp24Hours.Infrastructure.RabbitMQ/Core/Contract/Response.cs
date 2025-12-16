//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract
{
    /// <summary>
    /// Wrapper for request/response pattern responses.
    /// </summary>
    /// <typeparam name="T">The type of the response message.</typeparam>
    public class Response<T> where T : class
    {
        /// <summary>
        /// Gets the response message.
        /// </summary>
        public T? Message { get; init; }

        /// <summary>
        /// Gets whether the request was successful.
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// Gets the error message if the request failed.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Gets the exception if one occurred.
        /// </summary>
        public Exception? Exception { get; init; }

        /// <summary>
        /// Gets the response status.
        /// </summary>
        public ResponseStatus Status { get; init; }

        /// <summary>
        /// Gets the correlation ID.
        /// </summary>
        public string? CorrelationId { get; init; }

        /// <summary>
        /// Gets the timestamp when the response was received.
        /// </summary>
        public DateTimeOffset ReceivedAt { get; init; }

        /// <summary>
        /// Gets the time elapsed from sending the request to receiving the response.
        /// </summary>
        public TimeSpan? Elapsed { get; init; }

        /// <summary>
        /// Creates a successful response.
        /// </summary>
        public static Response<T> Success(T message, string? correlationId = null, TimeSpan? elapsed = null)
        {
            return new Response<T>
            {
                Message = message,
                IsSuccess = true,
                Status = ResponseStatus.Success,
                CorrelationId = correlationId,
                ReceivedAt = DateTimeOffset.UtcNow,
                Elapsed = elapsed
            };
        }

        /// <summary>
        /// Creates a timeout response.
        /// </summary>
        public static Response<T> Timeout(string? correlationId = null, TimeSpan? elapsed = null)
        {
            return new Response<T>
            {
                IsSuccess = false,
                Status = ResponseStatus.Timeout,
                ErrorMessage = "Request timed out waiting for response.",
                CorrelationId = correlationId,
                ReceivedAt = DateTimeOffset.UtcNow,
                Elapsed = elapsed
            };
        }

        /// <summary>
        /// Creates a failed response.
        /// </summary>
        public static Response<T> Failure(string errorMessage, Exception? exception = null, string? correlationId = null, TimeSpan? elapsed = null)
        {
            return new Response<T>
            {
                IsSuccess = false,
                Status = ResponseStatus.Failed,
                ErrorMessage = errorMessage,
                Exception = exception,
                CorrelationId = correlationId,
                ReceivedAt = DateTimeOffset.UtcNow,
                Elapsed = elapsed
            };
        }

        /// <summary>
        /// Creates a cancelled response.
        /// </summary>
        public static Response<T> Cancelled(string? correlationId = null, TimeSpan? elapsed = null)
        {
            return new Response<T>
            {
                IsSuccess = false,
                Status = ResponseStatus.Cancelled,
                ErrorMessage = "Request was cancelled.",
                CorrelationId = correlationId,
                ReceivedAt = DateTimeOffset.UtcNow,
                Elapsed = elapsed
            };
        }
    }

    /// <summary>
    /// Response status enumeration.
    /// </summary>
    public enum ResponseStatus
    {
        /// <summary>
        /// Request was successful and response received.
        /// </summary>
        Success,

        /// <summary>
        /// Request timed out waiting for response.
        /// </summary>
        Timeout,

        /// <summary>
        /// Request failed with an error.
        /// </summary>
        Failed,

        /// <summary>
        /// Request was cancelled.
        /// </summary>
        Cancelled
    }
}

