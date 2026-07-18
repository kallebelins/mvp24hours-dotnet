//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing.Contract;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Testing;

/// <summary>
/// Test harness for integration testing of RabbitMQ consumers and messaging.
/// </summary>
/// <remarks>
/// Creates a new test harness.
/// </remarks>
/// <param name="serviceProvider">The service provider.</param>
public class TestHarness(IServiceProvider serviceProvider) : ITestHarness
{
    private readonly InMemoryBus _bus = new(serviceProvider);
    private readonly ConcurrentDictionary<Type, object> _consumerHarnesses = new();
    private bool _isStarted;
    private bool _disposed;

    /// <summary>
    /// Creates a test harness with custom service configuration.
    /// </summary>
    /// <param name="configureServices">Action to configure services.</param>
    public static TestHarness Create(Action<IServiceCollection> configureServices)
    {
        var services = new ServiceCollection();
        configureServices(services);
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        return new TestHarness(serviceProvider);
    }

    /// <inheritdoc />
    public IInMemoryBus Bus => _bus;

    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; } = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <inheritdoc />
    public IReadOnlyList<IPublishedMessage> Published => _bus.PublishedMessages;

    /// <inheritdoc />
    public IReadOnlyList<IConsumedMessage> Consumed => _bus.ConsumedMessages;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isStarted)
        {
            return Task.CompletedTask;
        }

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
        string messageId = _bus.Publish(message, routingKey ?? typeof(TMessage).Name);
        return Task.FromResult(messageId);
    }

    /// <inheritdoc />
    public async Task<IConsumedMessage<TMessage>> PublishAndWaitAsync<TMessage>(
        TMessage message,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default) where TMessage : class
    {
        TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30);
        string messageId = await PublishAsync(message, cancellationToken: cancellationToken);

        // Consume the message
        ConsumeResult result = await _bus.ConsumeAsync(message, builder => builder.WithMessageId(messageId), cancellationToken);

        if (result.TimedOut)
        {
            throw new TimeoutException($"Message was not consumed within {effectiveTimeout.TotalSeconds} seconds.");
        }

        // Return the consumed message
        IConsumedMessage<TMessage>? consumed = _bus.GetConsumedMessages<TMessage>()
            .FirstOrDefault(m => m.MessageId == messageId) ?? throw new InvalidOperationException($"Message {messageId} was not found in consumed messages.");
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
        TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30);

        IRequestClient<TRequest, TResponse>? requestClient = ServiceProvider.GetService<IRequestClient<TRequest, TResponse>>();
        if (requestClient != null)
        {
            return await requestClient.GetResponseAsync(request, effectiveTimeout, cancellationToken);
        }

        // Simulate request-response through consumers
        string correlationId = Guid.NewGuid().ToString();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(effectiveTimeout);

        // Find request handler
        var handlers = ServiceProvider.GetServices<IRequestHandler<TRequest, TResponse>>().ToList();
        if (handlers.Count == 0)
        {
            throw new InvalidOperationException($"No request handler found for {typeof(TRequest).Name} -> {typeof(TResponse).Name}");
        }

        IRequestHandler<TRequest, TResponse> handler = handlers.First();

        TestConsumeContext<TRequest> context = new TestConsumeContextBuilder<TRequest>()
            .WithCorrelationId(correlationId)
            .WithServiceProvider(ServiceProvider)
            .Build(request);

        TResponse response = await handler.HandleAsync(context, cts.Token);

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
            _ => new ConsumerHarness<TConsumer>(ServiceProvider, _bus));
    }

    /// <inheritdoc />
    public async Task<IPublishedMessage<TMessage>> WaitForPublishAsync<TMessage>(
        TimeSpan? timeout = null,
        Func<TMessage, bool>? predicate = null,
        CancellationToken cancellationToken = default) where TMessage : class
    {
        TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30);
        DateTime deadline = DateTime.UtcNow.Add(effectiveTimeout);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<IPublishedMessage<TMessage>> messages = _bus.GetPublishedMessages<TMessage>();
            IPublishedMessage<TMessage>? match = predicate == null
                ? messages.FirstOrDefault()
                : messages.FirstOrDefault(m => predicate(m.Message));

            if (match != null)
            {
                return match;
            }

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
        TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30);
        DateTime deadline = DateTime.UtcNow.Add(effectiveTimeout);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<IConsumedMessage<TMessage>> messages = _bus.GetConsumedMessages<TMessage>();
            IConsumedMessage<TMessage>? match = predicate == null
                ? messages.FirstOrDefault()
                : messages.FirstOrDefault(m => predicate(m.Message));

            if (match != null)
            {
                return match;
            }

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
        {
            return;
        }

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
/// <remarks>
/// Creates a new consumer harness.
/// </remarks>
public class ConsumerHarness<TConsumer>(IServiceProvider serviceProvider, InMemoryBus bus) : IConsumerHarness<TConsumer> where TConsumer : class
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly InMemoryBus _bus = bus ?? throw new ArgumentNullException(nameof(bus));
    private readonly List<IConsumedMessage> _consumed = [];

    /// <inheritdoc />
    public TConsumer Consumer
    {
        get
        {
            field ??= _serviceProvider.GetRequiredService<TConsumer>();
            return field;
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
        ConsumeResult result = await _bus.ConsumeAsync(message, configureContext, cancellationToken);

        // Track consumed messages for this harness
        IReadOnlyList<IConsumedMessage<TMessage>> consumedMessages = _bus.GetConsumedMessages<TMessage>();
        foreach (IConsumedMessage<TMessage> consumed in consumedMessages)
        {
            if (!_consumed.Any(c => c.MessageId == consumed.MessageId))
            {
                _consumed.Add(consumed);
            }
        }

        return result;
    }
}

