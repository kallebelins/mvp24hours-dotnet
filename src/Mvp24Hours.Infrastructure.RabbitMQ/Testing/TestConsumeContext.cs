//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Testing;

/// <summary>
/// Test implementation of consume context for testing consumers.
/// </summary>
/// <typeparam name="TMessage">The type of the consumed message.</typeparam>
/// <remarks>
/// Creates a new test consume context.
/// </remarks>
public class TestConsumeContext<TMessage>(
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
    CancellationToken cancellationToken = default) : IConsumeContext<TMessage> where TMessage : class
{
    private readonly List<object> _publishedMessages = [];
    private readonly List<object> _responses = [];

    /// <inheritdoc />
    public TMessage Message { get; } = message ?? throw new ArgumentNullException(nameof(message));

    /// <inheritdoc />
    public string MessageId { get; } = messageId ?? Guid.NewGuid().ToString();

    /// <inheritdoc />
    public string? CorrelationId { get; } = correlationId;

    /// <inheritdoc />
    public string? CausationId { get; } = causationId;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Headers { get; } = headers != null
            ? new Dictionary<string, object>(headers)
            : [];

    /// <inheritdoc />
    public string Exchange { get; } = exchange ?? "test-exchange";

    /// <inheritdoc />
    public string RoutingKey { get; } = routingKey ?? "test-routing-key";

    /// <inheritdoc />
    public string QueueName { get; } = queueName ?? "test-queue";

    /// <inheritdoc />
    public string ConsumerTag { get; } = $"test-consumer-{Guid.NewGuid():N}";

    /// <inheritdoc />
    public ulong DeliveryTag { get; } = (ulong)Random.Shared.Next(1, int.MaxValue);

    /// <inheritdoc />
    public bool Redelivered { get; } = redeliveryCount > 0;

    /// <inheritdoc />
    public int RedeliveryCount { get; } = redeliveryCount;

    /// <inheritdoc />
    public DateTimeOffset? SentAt { get; } = sentAt;

    /// <inheritdoc />
    public DateTimeOffset ReceivedAt { get; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; } = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <inheritdoc />
    public CancellationToken CancellationToken { get; } = cancellationToken;

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
        foreach (object msg in _publishedMessages)
        {
            if (msg is T typed)
            {
                result.Add(typed);
            }
        }
        return result;
    }

    /// <summary>
    /// Gets a typed list of responses.
    /// </summary>
    public IReadOnlyList<T> GetResponses<T>() where T : class
    {
        var result = new List<T>();
        foreach (object resp in _responses)
        {
            if (resp is T typed)
            {
                result.Add(typed);
            }
        }
        return result;
    }

    /// <inheritdoc />
    public T? GetHeader<T>(string key)
    {
        if (Headers.TryGetValue(key, out object? value))
        {
            if (value is T typedValue)
            {
                return typedValue;
            }

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
        return new TestServiceScope(ServiceProvider);
    }

    private class TestServiceScope(IServiceProvider serviceProvider) : Core.Contract.IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;

        public void Dispose()
        {
            // No-op for test scope
        }
    }
}

