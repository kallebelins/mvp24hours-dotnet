//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing.Contract;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Testing
{
    /// <summary>
    /// In-memory implementation of the RabbitMQ client for unit testing.
    /// Tracks all published and consumed messages without requiring a real broker.
    /// </summary>
    public class InMemoryBus : IInMemoryBus
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentBag<IPublishedMessage> _publishedMessages = new();
        private readonly ConcurrentBag<IConsumedMessage> _consumedMessages = new();
        private readonly ConcurrentDictionary<Type, List<Type>> _consumerRegistry = new();
        private readonly object _lock = new();

        // Simulation state
        private TimeSpan? _simulatedTimeout;
        private Exception? _simulatedFailure;
        private TimeSpan? _simulatedDelay;

        /// <summary>
        /// Creates a new in-memory bus.
        /// </summary>
        /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
        public InMemoryBus(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <inheritdoc />
        public IReadOnlyList<IPublishedMessage> PublishedMessages => _publishedMessages.ToList();

        /// <inheritdoc />
        public IReadOnlyList<IConsumedMessage> ConsumedMessages => _consumedMessages.ToList();

        /// <inheritdoc />
        public IReadOnlyList<IPublishedMessage<TMessage>> GetPublishedMessages<TMessage>() where TMessage : class
        {
            return _publishedMessages
                .Where(m => m.MessageType == typeof(TMessage))
                .Select(m => new PublishedMessage<TMessage>((PublishedMessage)m, (TMessage)m.Message))
                .ToList();
        }

        /// <inheritdoc />
        public IReadOnlyList<IConsumedMessage<TMessage>> GetConsumedMessages<TMessage>() where TMessage : class
        {
            var result = new List<IConsumedMessage<TMessage>>();
            foreach (var m in _consumedMessages)
            {
                if (m is IConsumedMessage<TMessage> typed)
                {
                    result.Add(typed);
                }
            }
            return result;
        }

        /// <inheritdoc />
        public string Publish(object message, string routingKey, string? tokenDefault = null)
        {
            return PublishInternal(message, routingKey, tokenDefault, null, null, null);
        }

        /// <inheritdoc />
        public string Publish(object message, string routingKey, byte priority, string? tokenDefault = null)
        {
            return PublishInternal(message, routingKey, tokenDefault, priority, null, null);
        }

        /// <inheritdoc />
        public string Publish(object message, string routingKey, IDictionary<string, object> headers, string? tokenDefault = null)
        {
            return PublishInternal(message, routingKey, tokenDefault, null, headers, null);
        }

        /// <inheritdoc />
        public string PublishWithTtl(object message, string routingKey, int ttlMilliseconds, string? tokenDefault = null)
        {
            return PublishInternal(message, routingKey, tokenDefault, null, null, ttlMilliseconds);
        }

        /// <inheritdoc />
        public Task<string> PublishAsync(object message, string routingKey, string? tokenDefault = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Publish(message, routingKey, tokenDefault));
        }

        /// <inheritdoc />
        public IEnumerable<string> PublishBatch(IEnumerable<(object Message, string RoutingKey)> messages)
        {
            var results = new List<string>();
            foreach (var (message, routingKey) in messages)
            {
                var messageId = PublishInternal(message, routingKey, null, null, null, null, isBatch: true);
                results.Add(messageId);
            }
            return results;
        }

        /// <inheritdoc />
        public Task<IEnumerable<string>> PublishBatchAsync(IEnumerable<(object Message, string RoutingKey)> messages, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PublishBatch(messages));
        }

        private string PublishInternal(
            object message,
            string routingKey,
            string? tokenDefault,
            byte? priority,
            IDictionary<string, object>? headers,
            int? ttlMilliseconds,
            bool isBatch = false)
        {
            var messageId = tokenDefault ?? Guid.NewGuid().ToString();
            
            var publishedMessage = new PublishedMessage
            {
                Message = message,
                MessageType = message.GetType(),
                MessageId = messageId,
                RoutingKey = routingKey,
                Exchange = "in-memory-exchange",
                Headers = headers != null ? new Dictionary<string, object>(headers) : null,
                Priority = priority,
                TtlMilliseconds = ttlMilliseconds,
                IsBatch = isBatch,
                PublishedAt = DateTimeOffset.UtcNow
            };

            _publishedMessages.Add(publishedMessage);
            
            return messageId;
        }

        /// <inheritdoc />
        public async Task<ConsumeResult> ConsumeAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : class
        {
            return await ConsumeAsync(message, _ => { }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<ConsumeResult> ConsumeAsync<TMessage>(
            TMessage message,
            Action<TestConsumeContextBuilder<TMessage>> configureContext,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            var stopwatch = Stopwatch.StartNew();
            var messageId = Guid.NewGuid().ToString();

            try
            {
                // Check for simulated failures first
                if (_simulatedFailure != null)
                {
                    var failure = _simulatedFailure;
                    _simulatedFailure = null;
                    throw failure;
                }

                // Check for simulated timeout
                if (_simulatedTimeout.HasValue)
                {
                    var timeout = _simulatedTimeout.Value;
                    _simulatedTimeout = null;
                    await Task.Delay(timeout, cancellationToken);
                    throw new TimeoutException($"Simulated timeout after {timeout.TotalMilliseconds}ms");
                }

                // Check for simulated delay
                if (_simulatedDelay.HasValue)
                {
                    var delay = _simulatedDelay.Value;
                    _simulatedDelay = null;
                    await Task.Delay(delay, cancellationToken);
                }

                // Build context
                var builder = new TestConsumeContextBuilder<TMessage>()
                    .WithMessageId(messageId)
                    .WithServiceProvider(_serviceProvider);

                configureContext(builder);
                var context = builder.Build(message);

                // Find and invoke consumers
                var messageType = typeof(TMessage);
                var consumerInterface = typeof(IMessageConsumer<TMessage>);

                // Try to resolve consumers from DI
                var consumers = _serviceProvider.GetServices<IMessageConsumer<TMessage>>().ToList();

                if (consumers.Count == 0)
                {
                    // No consumers found, just record the message
                    stopwatch.Stop();
                    return ConsumeResult.Success(messageId, stopwatch.Elapsed);
                }

                foreach (var consumer in consumers)
                {
                    var consumerType = consumer.GetType();

                    try
                    {
                        await consumer.ConsumeAsync(context, cancellationToken);

                        var consumedMessage = ConsumedMessage<TMessage>.Create<IMessageConsumer<TMessage>>(
                            message,
                            context,
                            consumerType,
                            stopwatch.Elapsed,
                            isSuccess: true);

                        _consumedMessages.Add(consumedMessage);
                    }
                    catch (Exception ex)
                    {
                        var consumedMessage = ConsumedMessage<TMessage>.Create<IMessageConsumer<TMessage>>(
                            message,
                            context,
                            consumerType,
                            stopwatch.Elapsed,
                            isSuccess: false,
                            exception: ex);

                        _consumedMessages.Add(consumedMessage);
                        
                        stopwatch.Stop();
                        return ConsumeResult.Failure(messageId, ex, stopwatch.Elapsed);
                    }
                }

                stopwatch.Stop();
                return ConsumeResult.Success(messageId, stopwatch.Elapsed);
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                return ConsumeResult.Cancelled(messageId, stopwatch.Elapsed);
            }
            catch (TimeoutException)
            {
                stopwatch.Stop();
                return ConsumeResult.Timeout(messageId, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ConsumeResult.Failure(messageId, ex, stopwatch.Elapsed);
            }
        }

        /// <inheritdoc />
        public void Consume()
        {
            // No-op for in-memory bus - consumption is done via ConsumeAsync
        }

        /// <inheritdoc />
        public void Register<T>() where T : class, IMvpRabbitMQConsumer
        {
            Register(typeof(T));
        }

        /// <inheritdoc />
        public void Register(Type consumerType)
        {
            var interfaces = consumerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageConsumer<>));

            foreach (var iface in interfaces)
            {
                var messageType = iface.GetGenericArguments()[0];
                _consumerRegistry.AddOrUpdate(
                    messageType,
                    _ => new List<Type> { consumerType },
                    (_, list) => { list.Add(consumerType); return list; });
            }
        }

        /// <inheritdoc />
        public void Unregister<T>() where T : class, IMvpRabbitMQConsumer
        {
            Unregister(typeof(T));
        }

        /// <inheritdoc />
        public void Unregister(Type consumerType)
        {
            foreach (var kvp in _consumerRegistry)
            {
                kvp.Value.RemoveAll(t => t == consumerType);
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            ClearPublished();
            ClearConsumed();
        }

        /// <inheritdoc />
        public void ClearPublished()
        {
            lock (_lock)
            {
                while (_publishedMessages.TryTake(out _)) { }
            }
        }

        /// <inheritdoc />
        public void ClearConsumed()
        {
            lock (_lock)
            {
                while (_consumedMessages.TryTake(out _)) { }
            }
        }

        /// <inheritdoc />
        public bool WasPublished<TMessage>() where TMessage : class
        {
            return _publishedMessages.Any(m => m.MessageType == typeof(TMessage));
        }

        /// <inheritdoc />
        public bool WasPublished<TMessage>(Func<TMessage, bool> predicate) where TMessage : class
        {
            return _publishedMessages
                .Where(m => m.MessageType == typeof(TMessage))
                .Select(m => (TMessage)m.Message)
                .Any(predicate);
        }

        /// <inheritdoc />
        public bool WasConsumed<TMessage>() where TMessage : class
        {
            return _consumedMessages.Any(m => m.MessageType == typeof(TMessage));
        }

        /// <inheritdoc />
        public bool WasConsumed<TMessage>(Func<TMessage, bool> predicate) where TMessage : class
        {
            return _consumedMessages
                .Where(m => m.MessageType == typeof(TMessage))
                .Select(m => (TMessage)m.Message)
                .Any(predicate);
        }

        /// <inheritdoc />
        public int PublishedCount<TMessage>() where TMessage : class
        {
            return _publishedMessages.Count(m => m.MessageType == typeof(TMessage));
        }

        /// <inheritdoc />
        public int ConsumedCount<TMessage>() where TMessage : class
        {
            return _consumedMessages.Count(m => m.MessageType == typeof(TMessage));
        }

        /// <inheritdoc />
        public void SimulateTimeout(TimeSpan timeout)
        {
            _simulatedTimeout = timeout;
        }

        /// <inheritdoc />
        public void SimulateFailure(Exception exception)
        {
            _simulatedFailure = exception;
        }

        /// <inheritdoc />
        public void SimulateDelay(TimeSpan delay)
        {
            _simulatedDelay = delay;
        }

        /// <inheritdoc />
        public void ResetSimulations()
        {
            _simulatedTimeout = null;
            _simulatedFailure = null;
            _simulatedDelay = null;
        }
    }
}

