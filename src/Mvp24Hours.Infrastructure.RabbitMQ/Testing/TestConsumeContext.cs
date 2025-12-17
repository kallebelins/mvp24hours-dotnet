//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Testing
{
    /// <summary>
    /// Test implementation of consume context for testing consumers.
    /// </summary>
    /// <typeparam name="TMessage">The type of the consumed message.</typeparam>
    public class TestConsumeContext<TMessage> : IConsumeContext<TMessage> where TMessage : class
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly List<object> _publishedMessages = new();
        private readonly List<object> _responses = new();

        /// <summary>
        /// Creates a new test consume context.
        /// </summary>
        public TestConsumeContext(
            TMessage message,
            IServiceProvider serviceProvider,
            string? messageId = null,
            string? correlationId = null,
            string? causationId = null,
            string? exchange = null,
            string? routingKey = null,
            string? queueName = null,
            IDictionary<string, object>? headers = null,
            int redeliveryCount = 0,
            DateTimeOffset? sentAt = null,
            CancellationToken cancellationToken = default)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            
            MessageId = messageId ?? Guid.NewGuid().ToString();
            CorrelationId = correlationId;
            CausationId = causationId;
            Exchange = exchange ?? "test-exchange";
            RoutingKey = routingKey ?? "test-routing-key";
            QueueName = queueName ?? "test-queue";
            Headers = headers != null 
                ? new Dictionary<string, object>(headers) 
                : new Dictionary<string, object>();
            RedeliveryCount = redeliveryCount;
            Redelivered = redeliveryCount > 0;
            SentAt = sentAt;
            ReceivedAt = DateTimeOffset.UtcNow;
            CancellationToken = cancellationToken;
            ConsumerTag = $"test-consumer-{Guid.NewGuid():N}";
            DeliveryTag = (ulong)Random.Shared.Next(1, int.MaxValue);
        }

        /// <inheritdoc />
        public TMessage Message { get; }

        /// <inheritdoc />
        public string MessageId { get; }

        /// <inheritdoc />
        public string? CorrelationId { get; }

        /// <inheritdoc />
        public string? CausationId { get; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> Headers { get; }

        /// <inheritdoc />
        public string Exchange { get; }

        /// <inheritdoc />
        public string RoutingKey { get; }

        /// <inheritdoc />
        public string QueueName { get; }

        /// <inheritdoc />
        public string ConsumerTag { get; }

        /// <inheritdoc />
        public ulong DeliveryTag { get; }

        /// <inheritdoc />
        public bool Redelivered { get; }

        /// <inheritdoc />
        public int RedeliveryCount { get; }

        /// <inheritdoc />
        public DateTimeOffset? SentAt { get; }

        /// <inheritdoc />
        public DateTimeOffset ReceivedAt { get; }

        /// <inheritdoc />
        public IServiceProvider ServiceProvider => _serviceProvider;

        /// <inheritdoc />
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// Gets the messages that were published during consumption.
        /// </summary>
        public IReadOnlyList<object> PublishedMessages => _publishedMessages;

        /// <summary>
        /// Gets the responses that were sent during consumption.
        /// </summary>
        public IReadOnlyList<object> Responses => _responses;

        /// <summary>
        /// Gets a typed list of published messages.
        /// </summary>
        public IReadOnlyList<T> GetPublishedMessages<T>() where T : class
        {
            var result = new List<T>();
            foreach (var msg in _publishedMessages)
            {
                if (msg is T typed)
                    result.Add(typed);
            }
            return result;
        }

        /// <summary>
        /// Gets a typed list of responses.
        /// </summary>
        public IReadOnlyList<T> GetResponses<T>() where T : class
        {
            var result = new List<T>();
            foreach (var resp in _responses)
            {
                if (resp is T typed)
                    result.Add(typed);
            }
            return result;
        }

        /// <inheritdoc />
        public T? GetHeader<T>(string key)
        {
            if (Headers.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                    return typedValue;

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return default;
                }
            }
            return default;
        }

        /// <inheritdoc />
        public Task PublishAsync<T>(T message, string? routingKey = null, CancellationToken cancellationToken = default) where T : class
        {
            _publishedMessages.Add(message);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task RespondAsync<T>(T response, CancellationToken cancellationToken = default) where T : class
        {
            _responses.Add(response);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Core.Contract.IServiceScope CreateScope()
        {
            return new TestServiceScope(_serviceProvider);
        }

        private class TestServiceScope : Core.Contract.IServiceScope
        {
            public TestServiceScope(IServiceProvider serviceProvider)
            {
                ServiceProvider = serviceProvider;
            }

            public IServiceProvider ServiceProvider { get; }

            public void Dispose()
            {
                // No-op for test scope
            }
        }
    }
}

