//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Testing.Contract;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Testing
{
    /// <summary>
    /// Implementation of a published message for tracking in tests.
    /// </summary>
    public class PublishedMessage : IPublishedMessage
    {
        /// <inheritdoc />
        public object Message { get; init; } = null!;

        /// <inheritdoc />
        public Type MessageType { get; init; } = null!;

        /// <inheritdoc />
        public string MessageId { get; init; } = string.Empty;

        /// <inheritdoc />
        public string RoutingKey { get; init; } = string.Empty;

        /// <inheritdoc />
        public string? Exchange { get; init; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object>? Headers { get; init; }

        /// <inheritdoc />
        public byte? Priority { get; init; }

        /// <inheritdoc />
        public int? TtlMilliseconds { get; init; }

        /// <inheritdoc />
        public DateTimeOffset PublishedAt { get; init; } = DateTimeOffset.UtcNow;

        /// <inheritdoc />
        public bool IsBatch { get; init; }

        /// <inheritdoc />
        public string? CorrelationId { get; init; }

        /// <summary>
        /// Creates a published message from raw data.
        /// </summary>
        public static PublishedMessage Create<TMessage>(
            TMessage message,
            string messageId,
            string routingKey,
            string? exchange = null,
            IDictionary<string, object>? headers = null,
            byte? priority = null,
            int? ttlMilliseconds = null,
            string? correlationId = null,
            bool isBatch = false) where TMessage : class
        {
            return new PublishedMessage
            {
                Message = message,
                MessageType = typeof(TMessage),
                MessageId = messageId,
                RoutingKey = routingKey,
                Exchange = exchange,
                Headers = headers != null ? new Dictionary<string, object>(headers) : null,
                Priority = priority,
                TtlMilliseconds = ttlMilliseconds,
                CorrelationId = correlationId,
                IsBatch = isBatch,
                PublishedAt = DateTimeOffset.UtcNow
            };
        }
    }

    /// <summary>
    /// Implementation of a strongly-typed published message for tracking in tests.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    public class PublishedMessage<TMessage> : IPublishedMessage<TMessage> where TMessage : class
    {
        private readonly PublishedMessage _inner;

        /// <summary>
        /// Creates a new typed published message.
        /// </summary>
        public PublishedMessage(PublishedMessage inner, TMessage message)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        /// <inheritdoc />
        public TMessage Message { get; }

        /// <inheritdoc />
        object IPublishedMessage.Message => Message;

        /// <inheritdoc />
        public Type MessageType => _inner.MessageType;

        /// <inheritdoc />
        public string MessageId => _inner.MessageId;

        /// <inheritdoc />
        public string RoutingKey => _inner.RoutingKey;

        /// <inheritdoc />
        public string? Exchange => _inner.Exchange;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object>? Headers => _inner.Headers;

        /// <inheritdoc />
        public byte? Priority => _inner.Priority;

        /// <inheritdoc />
        public int? TtlMilliseconds => _inner.TtlMilliseconds;

        /// <inheritdoc />
        public DateTimeOffset PublishedAt => _inner.PublishedAt;

        /// <inheritdoc />
        public bool IsBatch => _inner.IsBatch;

        /// <inheritdoc />
        public string? CorrelationId => _inner.CorrelationId;

        /// <summary>
        /// Creates a typed published message from raw data.
        /// </summary>
        public static PublishedMessage<TMessage> Create(
            TMessage message,
            string messageId,
            string routingKey,
            string? exchange = null,
            IDictionary<string, object>? headers = null,
            byte? priority = null,
            int? ttlMilliseconds = null,
            string? correlationId = null,
            bool isBatch = false)
        {
            var inner = PublishedMessage.Create(
                message, messageId, routingKey, exchange,
                headers, priority, ttlMilliseconds, correlationId, isBatch);

            return new PublishedMessage<TMessage>(inner, message);
        }
    }
}

