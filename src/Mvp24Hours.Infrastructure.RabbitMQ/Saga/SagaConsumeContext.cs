//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Contract;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Saga
{
    /// <summary>
    /// Implementation of saga consume context with saga-specific operations.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    /// <typeparam name="TMessage">The type of the consumed message.</typeparam>
    public class SagaConsumeContext<TData, TMessage> : ISagaConsumeContext<TData, TMessage>
        where TData : class, new()
        where TMessage : class
    {
        private readonly IConsumeContext<TMessage> _innerContext;
        private readonly SagaInstance<TData> _instance;
        private readonly IMessageScheduler? _scheduler;
        private readonly IMvpRabbitMQClient? _rabbitMQClient;
        private readonly ISagaRepository<TData>? _repository;

        /// <summary>
        /// Creates a new saga consume context.
        /// </summary>
        public SagaConsumeContext(
            IConsumeContext<TMessage> innerContext,
            SagaInstance<TData> instance,
            bool isNew,
            IMessageScheduler? scheduler = null,
            IMvpRabbitMQClient? rabbitMQClient = null,
            ISagaRepository<TData>? repository = null)
        {
            _innerContext = innerContext ?? throw new ArgumentNullException(nameof(innerContext));
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
            IsNew = isNew;
            _scheduler = scheduler;
            _rabbitMQClient = rabbitMQClient;
            _repository = repository;
        }

        #region ISagaConsumeContext Members

        /// <inheritdoc />
        public Guid SagaId => _instance.CorrelationId;

        /// <inheritdoc />
        public TData SagaData => _instance.Data;

        /// <inheritdoc />
        public string CurrentState => _instance.CurrentState;

        /// <inheritdoc />
        public bool IsNew { get; }

        /// <inheritdoc />
        public bool IsCompleted => _instance.IsCompleted;

        /// <inheritdoc />
        public bool IsFaulted => _instance.IsFaulted;

        /// <inheritdoc />
        public DateTime CreatedAt => _instance.CreatedAt;

        /// <inheritdoc />
        public DateTime LastUpdatedAt => _instance.LastUpdatedAt;

        /// <inheritdoc />
        public Task TransitionToAsync(string newState, CancellationToken cancellationToken = default)
        {
            _instance.TransitionTo(newState, $"Message: {typeof(TMessage).Name}");
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task TransitionToAsync<TState>(TState newState, CancellationToken cancellationToken = default) where TState : Enum
        {
            return TransitionToAsync(newState.ToString(), cancellationToken);
        }

        /// <inheritdoc />
        public Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            _instance.Complete();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task FaultAsync(string errorMessage, CancellationToken cancellationToken = default)
        {
            _instance.Fault(errorMessage);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<Guid> ScheduleTimeoutAsync<TTimeout>(
            TimeSpan delay,
            TTimeout timeoutMessage,
            CancellationToken cancellationToken = default) where TTimeout : class
        {
            if (_scheduler == null)
            {
                throw new InvalidOperationException("Message scheduler is not available in this context.");
            }

            // Add saga correlation to timeout message headers
            var routingKey = $"saga.{typeof(TData).Name}.timeout";
            var timeoutId = await _scheduler.ScheduleMessageAsync(
                delay,
                timeoutMessage,
                routingKey,
                cancellationToken);

            _instance.ScheduledTimeouts.Add(timeoutId);
            return timeoutId;
        }

        /// <inheritdoc />
        public async Task<bool> CancelTimeoutAsync(Guid timeoutId, CancellationToken cancellationToken = default)
        {
            if (_scheduler == null)
            {
                throw new InvalidOperationException("Message scheduler is not available in this context.");
            }

            var cancelled = await _scheduler.CancelScheduledMessageAsync(timeoutId, cancellationToken);
            if (cancelled)
            {
                _instance.ScheduledTimeouts.Remove(timeoutId);
            }
            return cancelled;
        }

        /// <inheritdoc />
        public Task RaiseEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
        {
            return PublishAsync(@event, null, cancellationToken);
        }

        /// <inheritdoc />
        public void SetMetadata(string key, string value)
        {
            _instance.Metadata[key] = value;
        }

        /// <inheritdoc />
        public string? GetMetadata(string key)
        {
            return _instance.Metadata.TryGetValue(key, out var value) ? value : null;
        }

        #endregion

        #region IConsumeContext Delegation

        /// <inheritdoc />
        public TMessage Message => _innerContext.Message;

        /// <inheritdoc />
        public string MessageId => _innerContext.MessageId;

        /// <inheritdoc />
        public string? CorrelationId => _innerContext.CorrelationId;

        /// <inheritdoc />
        public string? CausationId => _innerContext.CausationId;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> Headers => _innerContext.Headers;

        /// <inheritdoc />
        public string Exchange => _innerContext.Exchange;

        /// <inheritdoc />
        public string RoutingKey => _innerContext.RoutingKey;

        /// <inheritdoc />
        public string QueueName => _innerContext.QueueName;

        /// <inheritdoc />
        public string ConsumerTag => _innerContext.ConsumerTag;

        /// <inheritdoc />
        public ulong DeliveryTag => _innerContext.DeliveryTag;

        /// <inheritdoc />
        public bool Redelivered => _innerContext.Redelivered;

        /// <inheritdoc />
        public int RedeliveryCount => _innerContext.RedeliveryCount;

        /// <inheritdoc />
        public DateTimeOffset? SentAt => _innerContext.SentAt;

        /// <inheritdoc />
        public DateTimeOffset ReceivedAt => _innerContext.ReceivedAt;

        /// <inheritdoc />
        public IServiceProvider ServiceProvider => _innerContext.ServiceProvider;

        /// <inheritdoc />
        public CancellationToken CancellationToken => _innerContext.CancellationToken;

        /// <inheritdoc />
        public T? GetHeader<T>(string key) => _innerContext.GetHeader<T>(key);

        /// <inheritdoc />
        public Task PublishAsync<T>(T message, string? routingKey = null, CancellationToken cancellationToken = default) where T : class
        {
            return _innerContext.PublishAsync(message, routingKey, cancellationToken);
        }

        /// <inheritdoc />
        public Task RespondAsync<T>(T response, CancellationToken cancellationToken = default) where T : class
        {
            return _innerContext.RespondAsync(response, cancellationToken);
        }

        /// <inheritdoc />
        public Core.Contract.IServiceScope CreateScope()
        {
            return _innerContext.CreateScope();
        }

        #endregion

        /// <summary>
        /// Gets the underlying saga instance.
        /// </summary>
        public SagaInstance<TData> GetSagaInstance() => _instance;
    }
}

