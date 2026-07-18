//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Pipeline;

/// <summary>
/// Implementation of consume filter context with message metadata and filter operations.
/// </summary>
/// <typeparam name="TMessage">The type of the consumed message.</typeparam>
/// <remarks>
/// Creates a new consume filter context.
/// </remarks>
/// <param name="consumeContext">The underlying consume context.</param>
/// <param name="cancellationToken">Cancellation token.</param>
public class ConsumeFilterContext<TMessage>(
    IConsumeContext<TMessage> consumeContext,
    CancellationToken cancellationToken = default) : IConsumeFilterContext<TMessage> where TMessage : class
{

    /// <inheritdoc />
    public TMessage Message => ConsumeContext.Message;

    /// <inheritdoc />
    public string MessageId => ConsumeContext.MessageId;

    /// <inheritdoc />
    public string? CorrelationId => ConsumeContext.CorrelationId;

    /// <inheritdoc />
    public string? CausationId => ConsumeContext.CausationId;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Headers => ConsumeContext.Headers;

    /// <inheritdoc />
    public string Exchange => ConsumeContext.Exchange;

    /// <inheritdoc />
    public string RoutingKey => ConsumeContext.RoutingKey;

    /// <inheritdoc />
    public string QueueName => ConsumeContext.QueueName;

    /// <inheritdoc />
    public string ConsumerTag => ConsumeContext.ConsumerTag;

    /// <inheritdoc />
    public ulong DeliveryTag => ConsumeContext.DeliveryTag;

    /// <inheritdoc />
    public bool Redelivered => ConsumeContext.Redelivered;

    /// <inheritdoc />
    public int RedeliveryCount => ConsumeContext.RedeliveryCount;

    /// <inheritdoc />
    public DateTimeOffset? SentAt => ConsumeContext.SentAt;

    /// <inheritdoc />
    public DateTimeOffset ReceivedAt => ConsumeContext.ReceivedAt;

    /// <inheritdoc />
    public IServiceProvider ServiceProvider => ConsumeContext.ServiceProvider;

    /// <inheritdoc />
    public CancellationToken CancellationToken { get; } = cancellationToken;

    /// <inheritdoc />
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();

    /// <inheritdoc />
    public IConsumeContext<TMessage> ConsumeContext { get; } = consumeContext ?? throw new ArgumentNullException(nameof(consumeContext));

    /// <inheritdoc />
    public bool ShouldSkipRemainingFilters { get; private set; }

    /// <inheritdoc />
    public bool ShouldRetry { get; private set; }

    /// <inheritdoc />
    public TimeSpan? RetryDelay { get; private set; }

    /// <inheritdoc />
    public bool ShouldSendToDeadLetter { get; private set; }

    /// <inheritdoc />
    public string? DeadLetterReason { get; private set; }

    /// <inheritdoc />
    public Exception? Exception { get; private set; }

    /// <inheritdoc />
    public T? GetHeader<T>(string key)
    {
        return ConsumeContext.GetHeader<T>(key);
    }

    /// <inheritdoc />
    public void SkipRemainingFilters()
    {
        ShouldSkipRemainingFilters = true;
    }

    /// <inheritdoc />
    public void SetRetry(TimeSpan? retryDelay = null)
    {
        ShouldRetry = true;
        RetryDelay = retryDelay;
    }

    /// <inheritdoc />
    public void SendToDeadLetter(string reason)
    {
        ShouldSendToDeadLetter = true;
        DeadLetterReason = reason;
    }

    /// <inheritdoc />
    public void SetException(Exception exception)
    {
        Exception = exception;
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(T message, string? routingKey = null, CancellationToken cancellationToken = default) where T : class
    {
        return ConsumeContext.PublishAsync(message, routingKey, cancellationToken);
    }

    /// <summary>
    /// Resets the retry flag.
    /// </summary>
    public void ResetRetry()
    {
        ShouldRetry = false;
        RetryDelay = null;
    }

    /// <summary>
    /// Resets the dead letter flag.
    /// </summary>
    public void ResetDeadLetter()
    {
        ShouldSendToDeadLetter = false;
        DeadLetterReason = null;
    }

    /// <summary>
    /// Resets the skip flag.
    /// </summary>
    public void ResetSkip()
    {
        ShouldSkipRemainingFilters = false;
    }

    /// <summary>
    /// Resets the exception.
    /// </summary>
    public void ResetException()
    {
        Exception = null;
    }
}

