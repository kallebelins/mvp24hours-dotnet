//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract
{
    /// <summary>
    /// Context for publish filters, providing access to message data and filter-specific operations.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message being published.</typeparam>
    public interface IPublishFilterContext<out TMessage> where TMessage : class
    {
        /// <summary>
        /// Gets the message being published.
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
        /// Gets the exchange name the message will be published to.
        /// </summary>
        string Exchange { get; }

        /// <summary>
        /// Gets or sets the routing key.
        /// </summary>
        string RoutingKey { get; set; }

        /// <summary>
        /// Gets or sets the message priority (0-255).
        /// </summary>
        byte? Priority { get; set; }

        /// <summary>
        /// Gets or sets the message TTL in milliseconds.
        /// </summary>
        int? TtlMilliseconds { get; set; }

        /// <summary>
        /// Gets the timestamp when the message is being published.
        /// </summary>
        DateTimeOffset PublishedAt { get; }

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
        /// Gets whether the message publishing should be cancelled.
        /// </summary>
        bool ShouldCancelPublish { get; }

        /// <summary>
        /// Cancels the message publishing.
        /// </summary>
        /// <param name="reason">Reason for cancellation.</param>
        void CancelPublish(string reason);

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

