//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Consumers
{
    /// <summary>
    /// Result for a single message in a batch, indicating whether it should be acked or nacked.
    /// </summary>
    public class BatchMessageResult : IBatchMessageResult
    {
        private BatchMessageResult(ulong deliveryTag, bool success, bool requeue, string? errorMessage)
        {
            DeliveryTag = deliveryTag;
            Success = success;
            Requeue = requeue;
            ErrorMessage = errorMessage;
        }

        /// <inheritdoc />
        public ulong DeliveryTag { get; }

        /// <inheritdoc />
        public bool Success { get; }

        /// <inheritdoc />
        public bool Requeue { get; }

        /// <inheritdoc />
        public string? ErrorMessage { get; }

        /// <summary>
        /// Creates an acknowledgment result for a successfully processed message.
        /// </summary>
        /// <param name="deliveryTag">The delivery tag of the message.</param>
        /// <returns>A successful batch message result.</returns>
        public static BatchMessageResult Ack(ulong deliveryTag)
            => new(deliveryTag, success: true, requeue: false, errorMessage: null);

        /// <summary>
        /// Creates a negative acknowledgment result for a failed message.
        /// </summary>
        /// <param name="deliveryTag">The delivery tag of the message.</param>
        /// <param name="requeue">Whether to requeue the message. Default is true.</param>
        /// <param name="errorMessage">Optional error message describing the failure.</param>
        /// <returns>A failed batch message result.</returns>
        public static BatchMessageResult Nack(ulong deliveryTag, bool requeue = true, string? errorMessage = null)
            => new(deliveryTag, success: false, requeue: requeue, errorMessage: errorMessage);

        /// <summary>
        /// Creates an acknowledgment result from a batch message item.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="item">The batch message item.</param>
        /// <returns>A successful batch message result.</returns>
        public static BatchMessageResult AckFrom<TMessage>(IBatchMessageItem<TMessage> item) where TMessage : class
            => Ack(item.DeliveryTag);

        /// <summary>
        /// Creates a negative acknowledgment result from a batch message item.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="item">The batch message item.</param>
        /// <param name="requeue">Whether to requeue the message.</param>
        /// <param name="errorMessage">Optional error message.</param>
        /// <returns>A failed batch message result.</returns>
        public static BatchMessageResult NackFrom<TMessage>(
            IBatchMessageItem<TMessage> item, 
            bool requeue = true, 
            string? errorMessage = null) where TMessage : class
            => Nack(item.DeliveryTag, requeue, errorMessage);

        /// <summary>
        /// Creates acknowledgment results for all messages in a batch.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="context">The batch consume context.</param>
        /// <returns>A collection of successful batch message results.</returns>
        public static IEnumerable<IBatchMessageResult> AckAll<TMessage>(
            IBatchConsumeContext<TMessage> context) where TMessage : class
            => context.Messages.Select(m => Ack(m.DeliveryTag));

        /// <summary>
        /// Creates negative acknowledgment results for all messages in a batch.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="context">The batch consume context.</param>
        /// <param name="requeue">Whether to requeue the messages.</param>
        /// <param name="errorMessage">Optional error message.</param>
        /// <returns>A collection of failed batch message results.</returns>
        public static IEnumerable<IBatchMessageResult> NackAll<TMessage>(
            IBatchConsumeContext<TMessage> context,
            bool requeue = true,
            string? errorMessage = null) where TMessage : class
            => context.Messages.Select(m => Nack(m.DeliveryTag, requeue, errorMessage));
    }

    /// <summary>
    /// Extensions for working with batch message results.
    /// </summary>
    public static class BatchMessageResultExtensions
    {
        /// <summary>
        /// Gets the count of successful results.
        /// </summary>
        public static int SuccessCount(this IEnumerable<IBatchMessageResult> results)
            => results.Count(r => r.Success);

        /// <summary>
        /// Gets the count of failed results.
        /// </summary>
        public static int FailureCount(this IEnumerable<IBatchMessageResult> results)
            => results.Count(r => !r.Success);

        /// <summary>
        /// Gets whether all results are successful.
        /// </summary>
        public static bool AllSucceeded(this IEnumerable<IBatchMessageResult> results)
            => results.All(r => r.Success);

        /// <summary>
        /// Gets whether any result failed.
        /// </summary>
        public static bool AnyFailed(this IEnumerable<IBatchMessageResult> results)
            => results.Any(r => !r.Success);

        /// <summary>
        /// Gets only the successful results.
        /// </summary>
        public static IEnumerable<IBatchMessageResult> Successes(this IEnumerable<IBatchMessageResult> results)
            => results.Where(r => r.Success);

        /// <summary>
        /// Gets only the failed results.
        /// </summary>
        public static IEnumerable<IBatchMessageResult> Failures(this IEnumerable<IBatchMessageResult> results)
            => results.Where(r => !r.Success);

        /// <summary>
        /// Gets results that should be requeued.
        /// </summary>
        public static IEnumerable<IBatchMessageResult> ToRequeue(this IEnumerable<IBatchMessageResult> results)
            => results.Where(r => !r.Success && r.Requeue);

        /// <summary>
        /// Gets results that should go to dead letter queue.
        /// </summary>
        public static IEnumerable<IBatchMessageResult> ToDeadLetter(this IEnumerable<IBatchMessageResult> results)
            => results.Where(r => !r.Success && !r.Requeue);
    }
}

