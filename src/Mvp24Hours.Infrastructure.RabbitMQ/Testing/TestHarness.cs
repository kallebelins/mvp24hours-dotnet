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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Testing
{
    /// <summary>
    /// Test harness for integration testing of RabbitMQ consumers and messaging.
    /// </summary>
    public class TestHarness : ITestHarness
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly InMemoryBus _bus;
        private readonly ConcurrentDictionary<Type, object> _consumerHarnesses = new();
        private bool _isStarted;
        private bool _disposed;

        /// <summary>
        /// Creates a new test harness.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public TestHarness(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _bus = new InMemoryBus(serviceProvider);
        }

        /// <summary>
        /// Creates a test harness with custom service configuration.
        /// </summary>
        /// <param name="configureServices">Action to configure services.</param>
        public static TestHarness Create(Action<IServiceCollection> configureServices)
        {
            var services = new ServiceCollection();
            configureServices(services);
            var serviceProvider = services.BuildServiceProvider();
            return new TestHarness(serviceProvider);
        }

        /// <inheritdoc />
        public IInMemoryBus Bus => _bus;

        /// <inheritdoc />
        public IServiceProvider ServiceProvider => _serviceProvider;

        /// <inheritdoc />
        public IReadOnlyList<IPublishedMessage> Published => _bus.PublishedMessages;

        /// <inheritdoc />
        public IReadOnlyList<IConsumedMessage> Consumed => _bus.ConsumedMessages;

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_isStarted)
                return Task.CompletedTask;

            _isStarted = true;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            _isStarted = false;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<string> PublishAsync<TMessage>(
            TMessage message,
            string? routingKey = null,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            var messageId = _bus.Publish(message, routingKey ?? typeof(TMessage).Name);
            return Task.FromResult(messageId);
        }

        /// <inheritdoc />
        public async Task<IConsumedMessage<TMessage>> PublishAndWaitAsync<TMessage>(
            TMessage message,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30);
            var messageId = await PublishAsync(message, cancellationToken: cancellationToken);

            // Consume the message
            var result = await _bus.ConsumeAsync(message, builder =>
            {
                builder.WithMessageId(messageId);
            }, cancellationToken);

            if (result.TimedOut)
            {
                throw new TimeoutException($"Message was not consumed within {effectiveTimeout.TotalSeconds} seconds.");
            }

            // Return the consumed message
            var consumed = _bus.GetConsumedMessages<TMessage>()
                .FirstOrDefault(m => m.MessageId == messageId);

            if (consumed == null)
            {
                throw new InvalidOperationException($"Message {messageId} was not found in consumed messages.");
            }

            return consumed;
        }

        /// <inheritdoc />
        public async Task<Response<TResponse>> RequestAsync<TRequest, TResponse>(
            TRequest request,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class
        {
            var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30);
            
            var requestClient = _serviceProvider.GetService<IRequestClient<TRequest, TResponse>>();
            if (requestClient != null)
            {
                return await requestClient.GetResponseAsync(request, effectiveTimeout, cancellationToken);
            }

            // Simulate request-response through consumers
            var correlationId = Guid.NewGuid().ToString();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(effectiveTimeout);

            // Find request handler
            var handlers = _serviceProvider.GetServices<IRequestHandler<TRequest, TResponse>>().ToList();
            if (handlers.Count == 0)
            {
                throw new InvalidOperationException($"No request handler found for {typeof(TRequest).Name} -> {typeof(TResponse).Name}");
            }

            var handler = handlers.First();
            
            var context = new TestConsumeContextBuilder<TRequest>()
                .WithCorrelationId(correlationId)
                .WithServiceProvider(_serviceProvider)
                .Build(request);

            var response = await handler.HandleAsync(context, cts.Token);
            
            return new Response<TResponse>
            {
                IsSuccess = true,
                Message = response
            };
        }

        /// <inheritdoc />
        public IConsumerHarness<TConsumer> GetConsumerHarness<TConsumer>() where TConsumer : class
        {
            return (IConsumerHarness<TConsumer>)_consumerHarnesses.GetOrAdd(
                typeof(TConsumer),
                _ => new ConsumerHarness<TConsumer>(_serviceProvider, _bus));
        }

        /// <inheritdoc />
        public async Task<IPublishedMessage<TMessage>> WaitForPublishAsync<TMessage>(
            TimeSpan? timeout = null,
            Func<TMessage, bool>? predicate = null,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30);
            var deadline = DateTime.UtcNow.Add(effectiveTimeout);

            while (DateTime.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var messages = _bus.GetPublishedMessages<TMessage>();
                var match = predicate == null
                    ? messages.FirstOrDefault()
                    : messages.FirstOrDefault(m => predicate(m.Message));

                if (match != null)
                    return match;

                await Task.Delay(50, cancellationToken);
            }

            throw new TimeoutException($"No message of type {typeof(TMessage).Name} was published within {effectiveTimeout.TotalSeconds} seconds.");
        }

        /// <inheritdoc />
        public async Task<IConsumedMessage<TMessage>> WaitForConsumeAsync<TMessage>(
            TimeSpan? timeout = null,
            Func<TMessage, bool>? predicate = null,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30);
            var deadline = DateTime.UtcNow.Add(effectiveTimeout);

            while (DateTime.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var messages = _bus.GetConsumedMessages<TMessage>();
                var match = predicate == null
                    ? messages.FirstOrDefault()
                    : messages.FirstOrDefault(m => predicate(m.Message));

                if (match != null)
                    return match;

                await Task.Delay(50, cancellationToken);
            }

            throw new TimeoutException($"No message of type {typeof(TMessage).Name} was consumed within {effectiveTimeout.TotalSeconds} seconds.");
        }

        /// <inheritdoc />
        public void Reset()
        {
            _bus.Clear();
            _bus.ResetSimulations();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the test harness.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _consumerHarnesses.Clear();
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Consumer-specific test harness.
    /// </summary>
    /// <typeparam name="TConsumer">The consumer type.</typeparam>
    public class ConsumerHarness<TConsumer> : IConsumerHarness<TConsumer> where TConsumer : class
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly InMemoryBus _bus;
        private readonly List<IConsumedMessage> _consumed = new();
        private TConsumer? _consumer;

        /// <summary>
        /// Creates a new consumer harness.
        /// </summary>
        public ConsumerHarness(IServiceProvider serviceProvider, InMemoryBus bus)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        /// <inheritdoc />
        public TConsumer Consumer
        {
            get
            {
                _consumer ??= _serviceProvider.GetRequiredService<TConsumer>();
                return _consumer;
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<IConsumedMessage> Consumed => _consumed;

        /// <inheritdoc />
        public async Task<ConsumeResult> ConsumeAsync<TMessage>(
            TMessage message,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            return await ConsumeAsync(message, _ => { }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<ConsumeResult> ConsumeAsync<TMessage>(
            TMessage message,
            Action<TestConsumeContextBuilder<TMessage>> configureContext,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            var result = await _bus.ConsumeAsync(message, configureContext, cancellationToken);
            
            // Track consumed messages for this harness
            var consumedMessages = _bus.GetConsumedMessages<TMessage>();
            foreach (var consumed in consumedMessages)
            {
                if (!_consumed.Any(c => c.MessageId == consumed.MessageId))
                {
                    _consumed.Add(consumed);
                }
            }

            return result;
        }
    }
}

