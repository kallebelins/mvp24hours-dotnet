//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing.Contract;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Testing
{
    /// <summary>
    /// Implementation of a consumed message for verification in tests.
    /// </summary>
    public class ConsumedMessage : IConsumedMessage
    {
        /// <inheritdoc />
        public object Message { get; init; } = null!;

        /// <inheritdoc />
        public Type MessageType { get; init; } = null!;

        /// <inheritdoc />
        public string MessageId { get; init; } = string.Empty;

        /// <inheritdoc />
        public string? CorrelationId { get; init; }

        /// <inheritdoc />
        public string? CausationId { get; init; }

        /// <inheritdoc />
        public Type ConsumerType { get; init; } = null!;

        /// <inheritdoc />
        public string QueueName { get; init; } = string.Empty;

        /// <inheritdoc />
        public string Exchange { get; init; } = string.Empty;

        /// <inheritdoc />
        public string RoutingKey { get; init; } = string.Empty;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> Headers { get; init; } = new Dictionary<string, object>();

        /// <inheritdoc />
        public DateTimeOffset ConsumedAt { get; init; } = DateTimeOffset.UtcNow;

        /// <inheritdoc />
        public TimeSpan Duration { get; init; }

        /// <inheritdoc />
        public bool IsSuccess { get; init; }

        /// <inheritdoc />
        public Exception? Exception { get; init; }

        /// <inheritdoc />
        public int RedeliveryCount { get; init; }

        /// <inheritdoc />
        public bool Redelivered { get; init; }

        /// <summary>
        /// Creates a consumed message from raw data.
        /// </summary>
        public static ConsumedMessage Create<TMessage, TConsumer>(
            TMessage message,
            string messageId,
            Type consumerType,
            string queueName,
            string exchange,
            string routingKey,
            TimeSpan duration,
            bool isSuccess,
            Exception? exception = null,
            string? correlationId = null,
            string? causationId = null,
            IDictionary<string, object>? headers = null,
            int redeliveryCount = 0)
            where TMessage : class
            where TConsumer : class
        {
            return new ConsumedMessage
            {
                Message = message,
                MessageType = typeof(TMessage),
                MessageId = messageId,
                ConsumerType = consumerType,
                QueueName = queueName,
                Exchange = exchange,
                RoutingKey = routingKey,
                Duration = duration,
                IsSuccess = isSuccess,
                Exception = exception,
                CorrelationId = correlationId,
                CausationId = causationId,
                Headers = headers != null ? new Dictionary<string, object>(headers) : new Dictionary<string, object>(),
                RedeliveryCount = redeliveryCount,
                Redelivered = redeliveryCount > 0,
                ConsumedAt = DateTimeOffset.UtcNow
            };
        }
    }

    /// <summary>
    /// Implementation of a strongly-typed consumed message for verification in tests.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    public class ConsumedMessage<TMessage> : IConsumedMessage<TMessage> where TMessage : class
    {
        private readonly ConsumedMessage _inner;

        /// <summary>
        /// Creates a new typed consumed message.
        /// </summary>
        public ConsumedMessage(ConsumedMessage inner, TMessage message, IConsumeContext<TMessage> context)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public TMessage Message { get; }

        /// <inheritdoc />
        public IConsumeContext<TMessage> Context { get; }

        /// <inheritdoc />
        object IConsumedMessage.Message => Message;

        /// <inheritdoc />
        public Type MessageType => _inner.MessageType;

        /// <inheritdoc />
        public string MessageId => _inner.MessageId;

        /// <inheritdoc />
        public string? CorrelationId => _inner.CorrelationId;

        /// <inheritdoc />
        public string? CausationId => _inner.CausationId;

        /// <inheritdoc />
        public Type ConsumerType => _inner.ConsumerType;

        /// <inheritdoc />
        public string QueueName => _inner.QueueName;

        /// <inheritdoc />
        public string Exchange => _inner.Exchange;

        /// <inheritdoc />
        public string RoutingKey => _inner.RoutingKey;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> Headers => _inner.Headers;

        /// <inheritdoc />
        public DateTimeOffset ConsumedAt => _inner.ConsumedAt;

        /// <inheritdoc />
        public TimeSpan Duration => _inner.Duration;

        /// <inheritdoc />
        public bool IsSuccess => _inner.IsSuccess;

        /// <inheritdoc />
        public Exception? Exception => _inner.Exception;

        /// <inheritdoc />
        public int RedeliveryCount => _inner.RedeliveryCount;

        /// <inheritdoc />
        public bool Redelivered => _inner.Redelivered;

        /// <summary>
        /// Creates a typed consumed message.
        /// </summary>
        public static ConsumedMessage<TMessage> Create<TConsumer>(
            TMessage message,
            IConsumeContext<TMessage> context,
            Type consumerType,
            TimeSpan duration,
            bool isSuccess,
            Exception? exception = null)
            where TConsumer : class
        {
            var inner = ConsumedMessage.Create<TMessage, TConsumer>(
                message,
                context.MessageId,
                consumerType,
                context.QueueName,
                context.Exchange,
                context.RoutingKey,
                duration,
                isSuccess,
                exception,
                context.CorrelationId,
                context.CausationId,
                context.Headers as IDictionary<string, object>,
                context.RedeliveryCount);

            return new ConsumedMessage<TMessage>(inner, message, context);
        }
    }
}

