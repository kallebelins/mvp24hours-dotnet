//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract
{
    /// <summary>
    /// Filter interface for message sending pipeline.
    /// Filters are executed in order during message sending (direct queue).
    /// </summary>
    public interface ISendFilter
    {
        /// <summary>
        /// Executes the filter logic during message sending.
        /// </summary>
        /// <typeparam name="TMessage">The type of message being sent.</typeparam>
        /// <param name="context">The send filter context containing message and metadata.</param>
        /// <param name="next">Delegate to call the next filter in the pipeline.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendAsync<TMessage>(
            ISendFilterContext<TMessage> context,
            SendFilterDelegate<TMessage> next,
            CancellationToken cancellationToken = default) where TMessage : class;
    }

    /// <summary>
    /// Strongly-typed send filter for specific message types.
    /// </summary>
    /// <typeparam name="TMessage">The type of message being sent.</typeparam>
    public interface ISendFilter<TMessage> where TMessage : class
    {
        /// <summary>
        /// Executes the filter logic during message sending.
        /// </summary>
        /// <param name="context">The send filter context containing message and metadata.</param>
        /// <param name="next">Delegate to call the next filter in the pipeline.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendAsync(
            ISendFilterContext<TMessage> context,
            SendFilterDelegate<TMessage> next,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Delegate representing the next filter in the send pipeline.
    /// </summary>
    /// <typeparam name="TMessage">The type of message being sent.</typeparam>
    /// <param name="context">The send filter context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public delegate Task SendFilterDelegate<TMessage>(
        ISendFilterContext<TMessage> context,
        CancellationToken cancellationToken = default) where TMessage : class;

    /// <summary>
    /// Context for send filters, providing access to message data and filter-specific operations.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message being sent.</typeparam>
    public interface ISendFilterContext<out TMessage> where TMessage : class
    {
        /// <summary>
        /// Gets the message being sent.
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
        /// Gets the message headers (mutable for adding headers in filters).
        /// </summary>
        IDictionary<string, object> Headers { get; }

        /// <summary>
        /// Gets the destination queue name.
        /// </summary>
        string DestinationQueue { get; }

        /// <summary>
        /// Gets or sets the message priority (0-255).
        /// </summary>
        byte? Priority { get; set; }

        /// <summary>
        /// Gets or sets the message TTL in milliseconds.
        /// </summary>
        int? TtlMilliseconds { get; set; }

        /// <summary>
        /// Gets the timestamp when the message is being sent.
        /// </summary>
        DateTimeOffset SentAt { get; }

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
        /// Gets whether the filter pipeline should short-circuit (skip remaining filters).
        /// </summary>
        bool ShouldSkipRemainingFilters { get; }

        /// <summary>
        /// Sets the filter pipeline to skip remaining filters.
        /// </summary>
        void SkipRemainingFilters();

        /// <summary>
        /// Gets whether the message sending should be cancelled.
        /// </summary>
        bool ShouldCancelSend { get; }

        /// <summary>
        /// Cancels the message sending.
        /// </summary>
        /// <param name="reason">Reason for cancellation.</param>
        void CancelSend(string reason);

        /// <summary>
        /// Gets the cancellation reason if set.
        /// </summary>
        string? CancellationReason { get; }

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
        /// Sets the correlation ID.
        /// </summary>
        /// <param name="correlationId">The correlation ID.</param>
        void SetCorrelationId(string correlationId);

        /// <summary>
        /// Sets the causation ID.
        /// </summary>
        /// <param name="causationId">The causation ID.</param>
        void SetCausationId(string causationId);
    }
}

