//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Pipeline
{
    /// <summary>
    /// Implementation of publish filter context with message metadata and filter operations.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message being published.</typeparam>
    public class PublishFilterContext<TMessage> : IPublishFilterContext<TMessage> where TMessage : class
    {
        private string? _correlationId;
        private string? _causationId;

        /// <summary>
        /// Creates a new publish filter context.
        /// </summary>
        /// <param name="message">The message being published.</param>
        /// <param name="exchange">The exchange name.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="messageId">Optional message ID. Generated if not provided.</param>
        /// <param name="correlationId">Optional correlation ID.</param>
        /// <param name="causationId">Optional causation ID.</param>
        /// <param name="headers">Optional initial headers.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public PublishFilterContext(
            TMessage message,
            string exchange,
            string routingKey,
            IServiceProvider serviceProvider,
            string? messageId = null,
            string? correlationId = null,
            string? causationId = null,
            IDictionary<string, object>? headers = null,
            CancellationToken cancellationToken = default)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Exchange = exchange ?? string.Empty;
            RoutingKey = routingKey ?? string.Empty;
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            MessageId = messageId ?? Guid.NewGuid().ToString();
            _correlationId = correlationId;
            _causationId = causationId;
            Headers = headers ?? new Dictionary<string, object>();
            CancellationToken = cancellationToken;
            PublishedAt = DateTimeOffset.UtcNow;
            Items = new Dictionary<string, object?>();
        }

        /// <inheritdoc />
        public TMessage Message { get; }

        /// <inheritdoc />
        public string MessageId { get; }

        /// <inheritdoc />
        public string? CorrelationId => _correlationId;

        /// <inheritdoc />
        public string? CausationId => _causationId;

        /// <inheritdoc />
        public IDictionary<string, object> Headers { get; }

        /// <inheritdoc />
        public string Exchange { get; }

        /// <inheritdoc />
        public string RoutingKey { get; set; }

        /// <inheritdoc />
        public byte? Priority { get; set; }

        /// <inheritdoc />
        public int? TtlMilliseconds { get; set; }

        /// <inheritdoc />
        public DateTimeOffset PublishedAt { get; }

        /// <inheritdoc />
        public IServiceProvider ServiceProvider { get; }

        /// <inheritdoc />
        public CancellationToken CancellationToken { get; }

        /// <inheritdoc />
        public IDictionary<string, object?> Items { get; }

        /// <inheritdoc />
        public bool ShouldSkipRemainingFilters { get; private set; }

        /// <inheritdoc />
        public bool ShouldCancelPublish { get; private set; }

        /// <inheritdoc />
        public string? CancellationReason { get; private set; }

        /// <inheritdoc />
        public Exception? Exception { get; private set; }

        /// <inheritdoc />
        public void SkipRemainingFilters()
        {
            ShouldSkipRemainingFilters = true;
        }

        /// <inheritdoc />
        public void CancelPublish(string reason)
        {
            ShouldCancelPublish = true;
            CancellationReason = reason;
        }

        /// <inheritdoc />
        public void SetException(Exception exception)
        {
            Exception = exception;
        }

        /// <inheritdoc />
        public void SetCorrelationId(string correlationId)
        {
            _correlationId = correlationId;
            Headers["x-correlation-id"] = correlationId;
        }

        /// <inheritdoc />
        public void SetCausationId(string causationId)
        {
            _causationId = causationId;
            Headers["x-causation-id"] = causationId;
        }

        /// <summary>
        /// Resets the skip flag.
        /// </summary>
        public void ResetSkip()
        {
            ShouldSkipRemainingFilters = false;
        }

        /// <summary>
        /// Resets the cancel flag.
        /// </summary>
        public void ResetCancel()
        {
            ShouldCancelPublish = false;
            CancellationReason = null;
        }

        /// <summary>
        /// Resets the exception.
        /// </summary>
        public void ResetException()
        {
            Exception = null;
        }
    }
}

