//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Linq;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Consumers
{
    /// <summary>
    /// Base class for declarative consumer configuration.
    /// Inherit from this class to configure a consumer.
    /// </summary>
    /// <typeparam name="TConsumer">The consumer type.</typeparam>
    public abstract class ConsumerDefinition<TConsumer> : IConsumerDefinition<TConsumer>
        where TConsumer : class
    {
        /// <summary>
        /// Creates a new consumer definition.
        /// </summary>
        protected ConsumerDefinition()
        {
            ConsumerType = typeof(TConsumer);
            
            // Auto-detect message type from IMessageConsumer<T> interface
            var consumerInterface = ConsumerType
                .GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && 
                    i.GetGenericTypeDefinition() == typeof(IMessageConsumer<>));

            MessageType = consumerInterface?.GetGenericArguments().FirstOrDefault() 
                ?? typeof(object);
        }

        /// <inheritdoc />
        public Type ConsumerType { get; }

        /// <inheritdoc />
        public Type MessageType { get; }

        /// <inheritdoc />
        public virtual string? QueueName { get; protected set; }

        /// <inheritdoc />
        public virtual string? Exchange { get; protected set; }

        /// <inheritdoc />
        public virtual string? RoutingKey { get; protected set; }

        /// <inheritdoc />
        public virtual ushort? PrefetchCount { get; protected set; }

        /// <inheritdoc />
        public virtual int? ConcurrentConsumers { get; protected set; }

        /// <inheritdoc />
        public virtual int? MaxRetryCount { get; protected set; }

        /// <inheritdoc />
        public virtual bool UseDeadLetterQueue { get; protected set; } = true;

        /// <summary>
        /// Configures the queue name for this consumer.
        /// </summary>
        protected void Queue(string queueName)
        {
            QueueName = queueName;
        }

        /// <summary>
        /// Configures the exchange for this consumer.
        /// </summary>
        protected void ExchangeName(string exchange)
        {
            Exchange = exchange;
        }

        /// <summary>
        /// Configures the routing key for this consumer.
        /// </summary>
        protected void Route(string routingKey)
        {
            RoutingKey = routingKey;
        }

        /// <summary>
        /// Configures the prefetch count for this consumer.
        /// </summary>
        protected void Prefetch(ushort count)
        {
            PrefetchCount = count;
        }

        /// <summary>
        /// Configures the number of concurrent consumers.
        /// </summary>
        protected void Concurrent(int count)
        {
            ConcurrentConsumers = count;
        }

        /// <summary>
        /// Configures the maximum retry count.
        /// </summary>
        protected void Retry(int maxCount)
        {
            MaxRetryCount = maxCount;
        }

        /// <summary>
        /// Disables the dead letter queue for this consumer.
        /// </summary>
        protected void NoDeadLetter()
        {
            UseDeadLetterQueue = false;
        }
    }
}

