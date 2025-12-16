//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Consumers
{
    /// <summary>
    /// Implementation of fault context for handling faulted messages.
    /// </summary>
    /// <typeparam name="TMessage">The type of the faulted message.</typeparam>
    public class FaultContext<TMessage> : IFaultContext<TMessage> where TMessage : class
    {
        /// <summary>
        /// Creates a new fault context.
        /// </summary>
        public FaultContext(
            TMessage message,
            Exception exception,
            string messageId,
            string? correlationId,
            string exchange,
            string routingKey,
            string queueName,
            int retryCount,
            IServiceProvider serviceProvider)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            MessageId = messageId ?? throw new ArgumentNullException(nameof(messageId));
            CorrelationId = correlationId;
            Exchange = exchange ?? string.Empty;
            RoutingKey = routingKey ?? string.Empty;
            QueueName = queueName ?? string.Empty;
            RetryCount = retryCount;
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            FaultedAt = DateTimeOffset.UtcNow;
        }

        /// <inheritdoc />
        public TMessage Message { get; }

        /// <inheritdoc />
        public Exception Exception { get; }

        /// <inheritdoc />
        public int RetryCount { get; }

        /// <inheritdoc />
        public string MessageId { get; }

        /// <inheritdoc />
        public string? CorrelationId { get; }

        /// <inheritdoc />
        public string Exchange { get; }

        /// <inheritdoc />
        public string RoutingKey { get; }

        /// <inheritdoc />
        public string QueueName { get; }

        /// <inheritdoc />
        public DateTimeOffset FaultedAt { get; }

        /// <inheritdoc />
        public IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Creates a fault context from a consume context.
        /// </summary>
        public static FaultContext<TMessage> FromConsumeContext(
            IConsumeContext<TMessage> context,
            Exception exception)
        {
            return new FaultContext<TMessage>(
                context.Message,
                exception,
                context.MessageId,
                context.CorrelationId,
                context.Exchange,
                context.RoutingKey,
                context.QueueName,
                context.RedeliveryCount,
                context.ServiceProvider);
        }
    }
}

