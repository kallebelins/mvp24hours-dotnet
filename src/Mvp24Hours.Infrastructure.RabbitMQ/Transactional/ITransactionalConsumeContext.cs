//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Transactional.Contract;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Transactional
{
    /// <summary>
    /// Extended consume context that supports transactional publishing via the outbox pattern.
    /// </summary>
    /// <typeparam name="TMessage">The type of the consumed message.</typeparam>
    /// <remarks>
    /// <para>
    /// <strong>Transactional Publishing:</strong>
    /// </para>
    /// <para>
    /// When processing a message, you may need to publish follow-up messages as part of
    /// the same logical transaction. Using <see cref="PublishWithinTransactionAsync{T}"/>
    /// ensures that:
    /// </para>
    /// <list type="bullet">
    /// <item>Messages are only published if the database transaction commits</item>
    /// <item>Messages are not lost if the broker is temporarily unavailable</item>
    /// <item>Message publishing is atomic with database changes</item>
    /// </list>
    /// <para>
    /// <strong>Usage Pattern:</strong>
    /// </para>
    /// <code>
    /// public async Task ConsumeAsync(ITransactionalConsumeContext&lt;OrderCreatedEvent&gt; context)
    /// {
    ///     // Process the message
    ///     var order = await _repository.GetByIdAsync(context.Message.OrderId);
    ///     order.Process();
    ///     
    ///     // Publish follow-up event within the same transaction
    ///     await context.PublishWithinTransactionAsync(new OrderProcessedEvent
    ///     {
    ///         OrderId = order.Id
    ///     });
    ///     
    ///     // Save changes - this commits both the order changes and the outbox message
    ///     await _unitOfWork.SaveChangesAsync();
    /// }
    /// </code>
    /// </remarks>
    public interface ITransactionalConsumeContext<out TMessage> : IConsumeContext<TMessage>
        where TMessage : class
    {
        /// <summary>
        /// Gets the transactional bus for this context.
        /// </summary>
        ITransactionalBus TransactionalBus { get; }

        /// <summary>
        /// Publishes a message using the transactional outbox pattern.
        /// The message is staged and will be published after the database transaction commits.
        /// </summary>
        /// <typeparam name="T">The type of message to publish.</typeparam>
        /// <param name="message">The message to publish.</param>
        /// <param name="routingKey">Optional routing key. If not specified, derived from message type.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The outbox message ID for tracking.</returns>
        Task<Guid> PublishWithinTransactionAsync<T>(
            T message,
            string? routingKey = null,
            CancellationToken cancellationToken = default)
            where T : class;

        /// <summary>
        /// Publishes a message with custom headers using the transactional outbox pattern.
        /// </summary>
        /// <typeparam name="T">The type of message to publish.</typeparam>
        /// <param name="message">The message to publish.</param>
        /// <param name="headers">Custom headers to include with the message.</param>
        /// <param name="routingKey">Optional routing key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The outbox message ID for tracking.</returns>
        Task<Guid> PublishWithinTransactionAsync<T>(
            T message,
            IDictionary<string, object> headers,
            string? routingKey = null,
            CancellationToken cancellationToken = default)
            where T : class;
    }
}

