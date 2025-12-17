//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Transactional.Contract;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Transactional
{
    /// <summary>
    /// Implementation of transactional consume context with support for outbox-based publishing.
    /// </summary>
    /// <typeparam name="TMessage">The type of the consumed message.</typeparam>
    public class TransactionalConsumeContext<TMessage> : ITransactionalConsumeContext<TMessage>
        where TMessage : class
    {
        private readonly IConsumeContext<TMessage> _innerContext;
        private readonly ITransactionalBus _transactionalBus;

        /// <summary>
        /// Creates a new transactional consume context wrapping an existing consume context.
        /// </summary>
        /// <param name="innerContext">The underlying consume context.</param>
        /// <param name="transactionalBus">The transactional bus for outbox publishing.</param>
        public TransactionalConsumeContext(
            IConsumeContext<TMessage> innerContext,
            ITransactionalBus transactionalBus)
        {
            _innerContext = innerContext ?? throw new ArgumentNullException(nameof(innerContext));
            _transactionalBus = transactionalBus ?? throw new ArgumentNullException(nameof(transactionalBus));
        }

        /// <inheritdoc />
        public ITransactionalBus TransactionalBus => _transactionalBus;

        #region IConsumeContext<TMessage> delegation

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
        public Task PublishAsync<T>(T message, string? routingKey = null, CancellationToken cancellationToken = default)
            where T : class
            => _innerContext.PublishAsync(message, routingKey, cancellationToken);

        /// <inheritdoc />
        public Task RespondAsync<T>(T response, CancellationToken cancellationToken = default)
            where T : class
            => _innerContext.RespondAsync(response, cancellationToken);

        /// <inheritdoc />
        public Core.Contract.IServiceScope CreateScope() => _innerContext.CreateScope();

        #endregion

        #region Transactional publishing

        /// <inheritdoc />
        public async Task<Guid> PublishWithinTransactionAsync<T>(
            T message,
            string? routingKey = null,
            CancellationToken cancellationToken = default)
            where T : class
        {
            var headers = CreateCorrelationHeaders();
            return await _transactionalBus.PublishAsync(message, headers, routingKey, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<Guid> PublishWithinTransactionAsync<T>(
            T message,
            IDictionary<string, object> headers,
            string? routingKey = null,
            CancellationToken cancellationToken = default)
            where T : class
        {
            // Merge correlation headers with custom headers
            var mergedHeaders = CreateCorrelationHeaders();
            foreach (var header in headers)
            {
                mergedHeaders[header.Key] = header.Value;
            }

            return await _transactionalBus.PublishAsync(message, mergedHeaders, routingKey, cancellationToken);
        }

        private Dictionary<string, object> CreateCorrelationHeaders()
        {
            var headers = new Dictionary<string, object>();

            // Propagate correlation ID
            if (!string.IsNullOrEmpty(CorrelationId))
            {
                headers["x-correlation-id"] = CorrelationId;
            }

            // Set causation ID to the current message ID
            headers["x-causation-id"] = MessageId;

            return headers;
        }

        #endregion
    }

    /// <summary>
    /// Factory for creating transactional consume contexts.
    /// </summary>
    public interface ITransactionalConsumeContextFactory
    {
        /// <summary>
        /// Creates a transactional consume context from an existing consume context.
        /// </summary>
        /// <typeparam name="TMessage">The type of the consumed message.</typeparam>
        /// <param name="context">The underlying consume context.</param>
        /// <returns>A transactional consume context.</returns>
        ITransactionalConsumeContext<TMessage> Create<TMessage>(IConsumeContext<TMessage> context)
            where TMessage : class;
    }

    /// <summary>
    /// Default implementation of the transactional consume context factory.
    /// </summary>
    public class TransactionalConsumeContextFactory : ITransactionalConsumeContextFactory
    {
        private readonly ITransactionalBus _transactionalBus;

        /// <summary>
        /// Creates a new instance of the factory.
        /// </summary>
        /// <param name="transactionalBus">The transactional bus to use for publishing.</param>
        public TransactionalConsumeContextFactory(ITransactionalBus transactionalBus)
        {
            _transactionalBus = transactionalBus ?? throw new ArgumentNullException(nameof(transactionalBus));
        }

        /// <inheritdoc />
        public ITransactionalConsumeContext<TMessage> Create<TMessage>(IConsumeContext<TMessage> context)
            where TMessage : class
        {
            return new TransactionalConsumeContext<TMessage>(context, _transactionalBus);
        }
    }
}

