//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Contract;
using System;
using System.Linq;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Saga
{
    /// <summary>
    /// Base class for saga consumer definitions.
    /// Provides declarative configuration for saga consumers.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <typeparam name="TConsumer">The type of saga consumer.</typeparam>
    public abstract class SagaConsumerDefinition<TData, TMessage, TConsumer> : IConsumerDefinition<SagaMessageConsumerAdapter<TData, TMessage, TConsumer>>
        where TData : class, new()
        where TMessage : class
        where TConsumer : class, ISagaConsumer<TData, TMessage>
    {
        /// <summary>
        /// Creates a new saga consumer definition.
        /// </summary>
        protected SagaConsumerDefinition()
        {
            ConsumerType = typeof(SagaMessageConsumerAdapter<TData, TMessage, TConsumer>);
            MessageType = typeof(TMessage);
        }

        /// <inheritdoc />
        public Type ConsumerType { get; }

        /// <inheritdoc />
        public Type MessageType { get; }

        /// <summary>
        /// Gets or sets the queue name for this saga consumer.
        /// Default: saga.{SagaTypeName}.{MessageTypeName}
        /// </summary>
        public virtual string? QueueName { get; protected set; }

        /// <summary>
        /// Gets or sets the exchange name.
        /// </summary>
        public virtual string? Exchange { get; protected set; }

        /// <summary>
        /// Gets or sets the routing key.
        /// </summary>
        public virtual string? RoutingKey { get; protected set; }

        /// <summary>
        /// Gets or sets the prefetch count for this consumer.
        /// </summary>
        public virtual ushort? PrefetchCount { get; protected set; }

        /// <summary>
        /// Gets or sets the concurrent message limit.
        /// </summary>
        public virtual int ConcurrentMessageLimit { get; protected set; } = 1;

        /// <inheritdoc />
        public virtual int? ConcurrentConsumers { get; protected set; }

        /// <inheritdoc />
        public virtual int? MaxRetryCount { get; protected set; }

        /// <inheritdoc />
        public virtual bool UseDeadLetterQueue { get; protected set; } = true;

        /// <summary>
        /// Gets the default queue name for the saga consumer.
        /// </summary>
        protected virtual string GetDefaultQueueName()
        {
            var sagaName = typeof(TData).Name.Replace("Data", "").Replace("SagaData", "Saga");
            var messageName = typeof(TMessage).Name;
            return $"saga.{sagaName}.{messageName}".ToLowerInvariant();
        }

        /// <summary>
        /// Gets the actual queue name to use.
        /// </summary>
        public string GetQueueName()
        {
            return QueueName ?? GetDefaultQueueName();
        }
    }

    /// <summary>
    /// Base class for saga state machine consumer definitions.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <typeparam name="TMachine">The type of saga state machine.</typeparam>
    public abstract class SagaStateMachineConsumerDefinition<TData, TMessage, TMachine> : IConsumerDefinition<SagaStateMachineConsumer<TData, TMessage, TMachine>>
        where TData : class, new()
        where TMessage : class
        where TMachine : SagaStateMachine<TData>
    {
        /// <summary>
        /// Creates a new saga state machine consumer definition.
        /// </summary>
        protected SagaStateMachineConsumerDefinition()
        {
            ConsumerType = typeof(SagaStateMachineConsumer<TData, TMessage, TMachine>);
            MessageType = typeof(TMessage);
        }

        /// <inheritdoc />
        public Type ConsumerType { get; }

        /// <inheritdoc />
        public Type MessageType { get; }

        /// <summary>
        /// Gets or sets the queue name for this consumer.
        /// </summary>
        public virtual string? QueueName { get; protected set; }

        /// <summary>
        /// Gets or sets the exchange name.
        /// </summary>
        public virtual string? Exchange { get; protected set; }

        /// <summary>
        /// Gets or sets the routing key.
        /// </summary>
        public virtual string? RoutingKey { get; protected set; }

        /// <summary>
        /// Gets or sets the prefetch count.
        /// </summary>
        public virtual ushort? PrefetchCount { get; protected set; }

        /// <summary>
        /// Gets or sets the concurrent message limit.
        /// </summary>
        public virtual int ConcurrentMessageLimit { get; protected set; } = 1;

        /// <inheritdoc />
        public virtual int? ConcurrentConsumers { get; protected set; }

        /// <inheritdoc />
        public virtual int? MaxRetryCount { get; protected set; }

        /// <inheritdoc />
        public virtual bool UseDeadLetterQueue { get; protected set; } = true;

        /// <summary>
        /// Gets the default queue name.
        /// </summary>
        protected virtual string GetDefaultQueueName()
        {
            var machineName = typeof(TMachine).Name.Replace("StateMachine", "").Replace("Saga", "");
            var messageName = typeof(TMessage).Name;
            return $"saga.{machineName}.{messageName}".ToLowerInvariant();
        }

        /// <summary>
        /// Gets the actual queue name.
        /// </summary>
        public string GetQueueName()
        {
            return QueueName ?? GetDefaultQueueName();
        }
    }
}

