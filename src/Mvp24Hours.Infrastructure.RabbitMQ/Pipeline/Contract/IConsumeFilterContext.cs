//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract
{
    /// <summary>
    /// Context for consume filters, providing access to message data and filter-specific operations.
    /// </summary>
    /// <typeparam name="TMessage">The type of the consumed message.</typeparam>
    public interface IConsumeFilterContext<out TMessage> where TMessage : class
    {
        /// <summary>
        /// Gets the consumed message.
        /// </summary>
        TMessage Message { get; }

        /// <summary>
        /// Gets the unique message identifier.
        /// </summary>
        string MessageId { get; }

        /// <summary>
        /// Gets the correlation identifier for distributed tracing.
        /// </summary>
        string? CorrelationId { get; }

        /// <summary>
        /// Gets the causation identifier linking to the parent operation.
        /// </summary>
        string? CausationId { get; }

        /// <summary>
        /// Gets the message headers.
        /// </summary>
        IReadOnlyDictionary<string, object> Headers { get; }

        /// <summary>
        /// Gets the exchange name the message was received from.
        /// </summary>
        string Exchange { get; }

        /// <summary>
        /// Gets the routing key used.
        /// </summary>
        string RoutingKey { get; }

        /// <summary>
        /// Gets the queue name the message was consumed from.
        /// </summary>
        string QueueName { get; }

        /// <summary>
        /// Gets the consumer tag.
        /// </summary>
        string ConsumerTag { get; }

        /// <summary>
        /// Gets the delivery tag.
        /// </summary>
        ulong DeliveryTag { get; }

        /// <summary>
        /// Gets whether this message is a redelivery.
        /// </summary>
        bool Redelivered { get; }

        /// <summary>
        /// Gets the redelivery count.
        /// </summary>
        int RedeliveryCount { get; }

        /// <summary>
        /// Gets the timestamp when the message was originally sent.
        /// </summary>
        DateTimeOffset? SentAt { get; }

        /// <summary>
        /// Gets the timestamp when the message was received.
        /// </summary>
        DateTimeOffset ReceivedAt { get; }

        /// <summary>
        /// Gets the service provider for resolving dependencies.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Gets the cancellation token.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Gets or sets additional items that can be shared between filters.
        /// </summary>
        IDictionary<string, object?> Items { get; }

        /// <summary>
        /// Gets the underlying consume context.
        /// </summary>
        IConsumeContext<TMessage> ConsumeContext { get; }

        /// <summary>
        /// Gets a header value by key.
        /// </summary>
        /// <typeparam name="T">The expected type of the header value.</typeparam>
        /// <param name="key">The header key.</param>
        /// <returns>The header value or default if not found.</returns>
        T? GetHeader<T>(string key);

        /// <summary>
        /// Gets whether the filter pipeline should short-circuit (skip remaining filters).
        /// </summary>
        bool ShouldSkipRemainingFilters { get; }

        /// <summary>
        /// Sets the filter pipeline to skip remaining filters.
        /// </summary>
        void SkipRemainingFilters();

        /// <summary>
        /// Gets whether the message should be retried.
        /// </summary>
        bool ShouldRetry { get; }

        /// <summary>
        /// Sets the message to be retried.
        /// </summary>
        /// <param name="retryDelay">Optional delay before retry.</param>
        void SetRetry(TimeSpan? retryDelay = null);

        /// <summary>
        /// Gets the retry delay if set.
        /// </summary>
        TimeSpan? RetryDelay { get; }

        /// <summary>
        /// Gets whether the message should be sent to dead letter queue.
        /// </summary>
        bool ShouldSendToDeadLetter { get; }

        /// <summary>
        /// Sets the message to be sent to dead letter queue.
        /// </summary>
        /// <param name="reason">Reason for dead lettering.</param>
        void SendToDeadLetter(string reason);

        /// <summary>
        /// Gets the dead letter reason if set.
        /// </summary>
        string? DeadLetterReason { get; }

        /// <summary>
        /// Gets the exception that occurred during processing, if any.
        /// </summary>
        Exception? Exception { get; }

        /// <summary>
        /// Sets an exception that occurred during processing.
        /// </summary>
        /// <param name="exception">The exception.</param>
        void SetException(Exception exception);

        /// <summary>
        /// Publishes a message to an exchange from within a filter.
        /// </summary>
        /// <typeparam name="T">The type of message to publish.</typeparam>
        /// <param name="message">The message to publish.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task PublishAsync<T>(T message, string? routingKey = null, CancellationToken cancellationToken = default) where T : class;
    }
}

