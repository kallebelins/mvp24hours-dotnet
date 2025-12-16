//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract
{
    /// <summary>
    /// Consumer for handling faulted messages of a specific type.
    /// Implement this interface to handle messages that failed processing.
    /// </summary>
    /// <typeparam name="TMessage">The type of the faulted message.</typeparam>
    public interface IFaultConsumer<in TMessage> where TMessage : class
    {
        /// <summary>
        /// Handles a faulted message.
        /// </summary>
        /// <param name="context">The fault context containing the message and exception details.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task HandleFaultAsync(IFaultContext<TMessage> context, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Context for faulted message handling.
    /// </summary>
    /// <typeparam name="TMessage">The type of the faulted message.</typeparam>
    public interface IFaultContext<out TMessage> where TMessage : class
    {
        /// <summary>
        /// Gets the original message that caused the fault.
        /// </summary>
        TMessage Message { get; }

        /// <summary>
        /// Gets the exception that caused the fault.
        /// </summary>
        Exception Exception { get; }

        /// <summary>
        /// Gets the number of times this message has been retried.
        /// </summary>
        int RetryCount { get; }

        /// <summary>
        /// Gets the message ID.
        /// </summary>
        string MessageId { get; }

        /// <summary>
        /// Gets the correlation ID.
        /// </summary>
        string? CorrelationId { get; }

        /// <summary>
        /// Gets the exchange the message was received from.
        /// </summary>
        string Exchange { get; }

        /// <summary>
        /// Gets the routing key.
        /// </summary>
        string RoutingKey { get; }

        /// <summary>
        /// Gets the queue name.
        /// </summary>
        string QueueName { get; }

        /// <summary>
        /// Gets the timestamp when the fault occurred.
        /// </summary>
        DateTimeOffset FaultedAt { get; }

        /// <summary>
        /// Gets the service provider for resolving dependencies.
        /// </summary>
        IServiceProvider ServiceProvider { get; }
    }
}

