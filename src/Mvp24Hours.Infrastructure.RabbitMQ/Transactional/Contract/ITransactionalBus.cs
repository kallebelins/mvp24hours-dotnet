//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Transactional.Contract
{
    /// <summary>
    /// Interface for transactional message bus operations.
    /// Ensures messages are only published after the database transaction commits successfully.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Transactional Messaging Pattern:</strong>
    /// </para>
    /// <para>
    /// This interface implements the Outbox Pattern for reliable message publishing.
    /// Messages are stored in an outbox table within the same database transaction
    /// as the business data, then published asynchronously by a background process.
    /// </para>
    /// <para>
    /// <strong>Benefits:</strong>
    /// <list type="bullet">
    /// <item>Guaranteed delivery - messages won't be lost if the broker is unavailable</item>
    /// <item>Atomic consistency - data and messages are committed together</item>
    /// <item>At-least-once delivery semantics</item>
    /// <item>No distributed transactions required</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Usage Flow:</strong>
    /// <list type="number">
    /// <item>Begin database transaction</item>
    /// <item>Perform business operations</item>
    /// <item>Call <see cref="PublishAsync{TMessage}"/> to stage messages</item>
    /// <item>Commit transaction - messages are stored in outbox</item>
    /// <item>Background process publishes messages to RabbitMQ</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class OrderService
    /// {
    ///     private readonly ITransactionalBus _bus;
    ///     private readonly IUnitOfWork _unitOfWork;
    ///     
    ///     public async Task CreateOrderAsync(CreateOrderCommand command)
    ///     {
    ///         var order = new Order(command.CustomerId, command.Items);
    ///         
    ///         _unitOfWork.GetRepository&lt;Order&gt;().Add(order);
    ///         
    ///         // Message is staged, not sent yet
    ///         await _bus.PublishAsync(new OrderCreatedEvent
    ///         {
    ///             OrderId = order.Id,
    ///             CustomerId = command.CustomerId
    ///         });
    ///         
    ///         // On commit, message is stored in outbox
    ///         await _unitOfWork.SaveChangesAsync();
    ///         
    ///         // Background process will publish to RabbitMQ
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface ITransactionalBus
    {
        /// <summary>
        /// Stages a message to be published after the current transaction commits.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message to publish.</typeparam>
        /// <param name="message">The message to publish.</param>
        /// <param name="routingKey">Optional routing key. If not specified, derived from message type.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The outbox message ID for tracking.</returns>
        /// <remarks>
        /// <para>
        /// The message is not published immediately. Instead, it's stored in the outbox table
        /// and will be published by the <see cref="IOutboxPublisher"/> background service
        /// after the transaction commits.
        /// </para>
        /// </remarks>
        Task<Guid> PublishAsync<TMessage>(
            TMessage message,
            string? routingKey = null,
            CancellationToken cancellationToken = default)
            where TMessage : class;

        /// <summary>
        /// Stages a message with custom headers to be published after the current transaction commits.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message to publish.</typeparam>
        /// <param name="message">The message to publish.</param>
        /// <param name="headers">Custom headers to include with the message.</param>
        /// <param name="routingKey">Optional routing key. If not specified, derived from message type.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The outbox message ID for tracking.</returns>
        Task<Guid> PublishAsync<TMessage>(
            TMessage message,
            IDictionary<string, object> headers,
            string? routingKey = null,
            CancellationToken cancellationToken = default)
            where TMessage : class;

        /// <summary>
        /// Stages multiple messages to be published after the current transaction commits.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages to publish.</typeparam>
        /// <param name="messages">The messages to publish.</param>
        /// <param name="routingKey">Optional routing key. If not specified, derived from message type.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The outbox message IDs for tracking.</returns>
        Task<IReadOnlyList<Guid>> PublishBatchAsync<TMessage>(
            IEnumerable<TMessage> messages,
            string? routingKey = null,
            CancellationToken cancellationToken = default)
            where TMessage : class;

        /// <summary>
        /// Gets the current count of pending (staged but not yet published) messages.
        /// </summary>
        /// <returns>Number of pending messages in the current transaction scope.</returns>
        int GetPendingCount();

        /// <summary>
        /// Clears all staged messages in the current transaction scope.
        /// </summary>
        /// <remarks>
        /// Use this method to discard staged messages before they're committed.
        /// Typically called when rolling back a transaction.
        /// </remarks>
        void ClearPending();
    }

    /// <summary>
    /// Interface for the background service that publishes outbox messages to RabbitMQ.
    /// </summary>
    public interface IOutboxPublisher
    {
        /// <summary>
        /// Publishes all pending messages from the outbox.
        /// </summary>
        /// <param name="batchSize">Maximum number of messages to process in one batch.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of messages successfully published.</returns>
        Task<int> PublishPendingAsync(int batchSize = 100, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current status of the outbox publisher.
        /// </summary>
        OutboxPublisherStatus GetStatus();
    }

    /// <summary>
    /// Status information for the outbox publisher.
    /// </summary>
    public class OutboxPublisherStatus
    {
        /// <summary>
        /// Whether the publisher is currently running.
        /// </summary>
        public bool IsRunning { get; init; }

        /// <summary>
        /// Total messages published since startup.
        /// </summary>
        public long TotalPublished { get; init; }

        /// <summary>
        /// Total failed publish attempts since startup.
        /// </summary>
        public long TotalFailed { get; init; }

        /// <summary>
        /// Current pending message count.
        /// </summary>
        public int PendingCount { get; init; }

        /// <summary>
        /// Last time a message was successfully published.
        /// </summary>
        public DateTimeOffset? LastPublishedAt { get; init; }

        /// <summary>
        /// Last error encountered during publishing.
        /// </summary>
        public string? LastError { get; init; }

        /// <summary>
        /// Last time an error occurred.
        /// </summary>
        public DateTimeOffset? LastErrorAt { get; init; }
    }
}

