//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Saga.Contract
{
    /// <summary>
    /// Strongly-typed saga consumer interface for consuming messages that drive saga progression.
    /// Combines message consumption with saga lifecycle management.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    /// <typeparam name="TMessage">The type of message that triggers saga transitions.</typeparam>
    /// <remarks>
    /// <para>
    /// Saga consumers are specialized consumers that:
    /// <list type="bullet">
    /// <item>Automatically correlate messages to saga instances</item>
    /// <item>Load/save saga state before/after processing</item>
    /// <item>Handle saga lifecycle (start, continue, complete)</item>
    /// <item>Support compensation on failure</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class OrderSagaConsumer : ISagaConsumer&lt;OrderSagaData, OrderCreatedEvent&gt;
    /// {
    ///     public async Task ConsumeAsync(ISagaConsumeContext&lt;OrderSagaData, OrderCreatedEvent&gt; context, CancellationToken cancellationToken)
    ///     {
    ///         // Start or continue the saga
    ///         context.SagaData.OrderId = context.Message.OrderId;
    ///         
    ///         // Transition to next state
    ///         await context.TransitionToAsync(OrderSagaState.PaymentPending, cancellationToken);
    ///         
    ///         // Publish next command
    ///         await context.PublishAsync(new ProcessPaymentCommand { OrderId = context.Message.OrderId }, cancellationToken);
    ///     }
    ///     
    ///     public Guid GetCorrelationId(OrderCreatedEvent message) => message.OrderId;
    /// }
    /// </code>
    /// </example>
    public interface ISagaConsumer<TData, in TMessage>
        where TData : class, new()
        where TMessage : class
    {
        /// <summary>
        /// Handles the consumption of a message that drives saga progression.
        /// </summary>
        /// <param name="context">The saga consume context containing the message, saga data, and operations.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ConsumeAsync(ISagaConsumeContext<TData, TMessage> context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Extracts the correlation ID from a message to locate the corresponding saga instance.
        /// </summary>
        /// <param name="message">The message to extract correlation ID from.</param>
        /// <returns>The correlation ID (typically the saga ID or business identifier).</returns>
        Guid GetCorrelationId(TMessage message);

        /// <summary>
        /// Determines whether this message should start a new saga instance.
        /// </summary>
        /// <param name="message">The message to evaluate.</param>
        /// <returns>True if a new saga should be created, false to only process existing sagas.</returns>
        bool CanStartSaga(TMessage message) => false;

        /// <summary>
        /// Called when the saga instance is not found and CanStartSaga returns false.
        /// Override to customize behavior (e.g., dead letter, log warning).
        /// </summary>
        /// <param name="context">The consume context.</param>
        /// <param name="correlationId">The correlation ID that was not found.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task OnSagaNotFoundAsync(IConsumeContext<TMessage> context, Guid correlationId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

