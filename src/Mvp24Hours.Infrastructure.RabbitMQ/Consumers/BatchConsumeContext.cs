//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Consumers
{
    /// <summary>
    /// Implementation of batch consume context for processing multiple messages together.
    /// </summary>
    /// <typeparam name="TMessage">The type of the consumed messages.</typeparam>
    public class BatchConsumeContext<TMessage> : IBatchConsumeContext<TMessage> where TMessage : class
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMvpRabbitMQClient? _rabbitMQClient;
        private readonly List<IBatchMessageItem<TMessage>> _messages;

        /// <summary>
        /// Creates a new batch consume context.
        /// </summary>
        /// <param name="messages">The list of messages in the batch.</param>
        /// <param name="serviceProvider">The service provider for DI.</param>
        /// <param name="rabbitMQClient">The RabbitMQ client for publishing.</param>
        /// <param name="queueName">The queue name.</param>
        /// <param name="exchange">The exchange name.</param>
        /// <param name="consumerTag">The consumer tag.</param>
        /// <param name="batchCreatedAt">When the batch was started.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public BatchConsumeContext(
            IEnumerable<IBatchMessageItem<TMessage>> messages,
            IServiceProvider serviceProvider,
            IMvpRabbitMQClient? rabbitMQClient = null,
            string? queueName = null,
            string? exchange = null,
            string? consumerTag = null,
            DateTimeOffset? batchCreatedAt = null,
            CancellationToken cancellationToken = default)
        {
            _messages = new List<IBatchMessageItem<TMessage>>(messages ?? throw new ArgumentNullException(nameof(messages)));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _rabbitMQClient = rabbitMQClient;
            QueueName = queueName ?? string.Empty;
            Exchange = exchange ?? string.Empty;
            ConsumerTag = consumerTag ?? string.Empty;
            BatchCreatedAt = batchCreatedAt ?? DateTimeOffset.UtcNow;
            BatchCompletedAt = DateTimeOffset.UtcNow;
            CancellationToken = cancellationToken;
            BatchId = Guid.NewGuid().ToString("N");

            // Try to get a common correlation ID from the first message
            CorrelationId = _messages.Count > 0 ? _messages[0].CorrelationId : null;
        }

        /// <inheritdoc />
        public IReadOnlyList<IBatchMessageItem<TMessage>> Messages => _messages;

        /// <inheritdoc />
        public int BatchSize => _messages.Count;

        /// <inheritdoc />
        public string BatchId { get; }

        /// <inheritdoc />
        public string? CorrelationId { get; }

        /// <inheritdoc />
        public string Exchange { get; }

        /// <inheritdoc />
        public string QueueName { get; }

        /// <inheritdoc />
        public string ConsumerTag { get; }

        /// <inheritdoc />
        public DateTimeOffset BatchCreatedAt { get; }

        /// <inheritdoc />
        public DateTimeOffset BatchCompletedAt { get; }

        /// <inheritdoc />
        public TimeSpan BatchAge => BatchCompletedAt - BatchCreatedAt;

        /// <inheritdoc />
        public IServiceProvider ServiceProvider => _serviceProvider;

        /// <inheritdoc />
        public CancellationToken CancellationToken { get; }

        /// <inheritdoc />
        public Core.Contract.IServiceScope CreateScope()
        {
            var scope = _serviceProvider.CreateScope();
            return new ServiceScopeWrapper(scope);
        }

        /// <inheritdoc />
        public Task PublishAsync<T>(T message, string? routingKey = null, CancellationToken cancellationToken = default) where T : class
        {
            if (_rabbitMQClient == null)
                throw new InvalidOperationException("RabbitMQ client is not available in this context.");

            var headers = new Dictionary<string, object>
            {
                ["x-batch-id"] = BatchId
            };

            // Propagate correlation ID if available
            if (!string.IsNullOrEmpty(CorrelationId))
            {
                headers["x-correlation-id"] = CorrelationId;
            }

            _rabbitMQClient.Publish(message, routingKey ?? string.Empty, headers);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task PublishBatchAsync<T>(IEnumerable<T> messages, string? routingKey = null, CancellationToken cancellationToken = default) where T : class
        {
            if (_rabbitMQClient == null)
                throw new InvalidOperationException("RabbitMQ client is not available in this context.");

            var headers = new Dictionary<string, object>
            {
                ["x-batch-id"] = BatchId
            };

            // Propagate correlation ID if available
            if (!string.IsNullOrEmpty(CorrelationId))
            {
                headers["x-correlation-id"] = CorrelationId;
            }

            foreach (var message in messages)
            {
                _rabbitMQClient.Publish(message, routingKey ?? string.Empty, headers);
            }

            return Task.CompletedTask;
        }

        private class ServiceScopeWrapper : Core.Contract.IServiceScope
        {
            private readonly Microsoft.Extensions.DependencyInjection.IServiceScope _scope;

            public ServiceScopeWrapper(Microsoft.Extensions.DependencyInjection.IServiceScope scope)
            {
                _scope = scope;
            }

            public IServiceProvider ServiceProvider => _scope.ServiceProvider;

            public void Dispose()
            {
                _scope.Dispose();
            }
        }
    }
}

