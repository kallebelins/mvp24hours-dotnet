//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Pipeline;

/// <summary>
/// Implementation of publish filter context with message metadata and filter operations.
/// </summary>
/// <typeparam name="TMessage">The type of the message being published.</typeparam>
/// <remarks>
/// Creates a new publish filter context.
/// </remarks>
/// <param name="message">The message being published.</param>
/// <param name="exchange">The exchange name.</param>
/// <param name="routingKey">The routing key.</param>
/// <param name="serviceProvider">The service provider.</param>
/// <param name="messageId">Optional message ID. Generated if not provided.</param>
/// <param name="correlationId">Optional correlation ID.</param>
/// <param name="causationId">Optional causation ID.</param>
/// <param name="headers">Optional initial headers.</param>
/// <param name="cancellationToken">Cancellation token.</param>
public class PublishFilterContext<TMessage>(
    TMessage message,
    string exchange,
    string routingKey,
    IServiceProvider serviceProvider,
    string? messageId = null,
    string? correlationId = null,
    string? causationId = null,
    IDictionary<string, object>? headers = null,
    CancellationToken cancellationToken = default) : IPublishFilterContext<TMessage> where TMessage : class
{

    /// <inheritdoc />
    public TMessage Message { get; } = message ?? throw new ArgumentNullException(nameof(message));

    /// <inheritdoc />
    public string MessageId { get; } = messageId ?? Guid.NewGuid().ToString();

    /// <inheritdoc />
    public string? CorrelationId { get; private set; } = correlationId;

    /// <inheritdoc />
    public string? CausationId { get; private set; } = causationId;

    /// <inheritdoc />
    public IDictionary<string, object> Headers { get; } = headers ?? new Dictionary<string, object>();

    /// <inheritdoc />
    public string Exchange { get; } = exchange ?? string.Empty;

    /// <inheritdoc />
    public string RoutingKey { get; set; } = routingKey ?? string.Empty;

    /// <inheritdoc />
    public byte? Priority { get; set; }

    /// <inheritdoc />
    public int? TtlMilliseconds { get; set; }

    /// <inheritdoc />
    public DateTimeOffset PublishedAt { get; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; } = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <inheritdoc />
    public CancellationToken CancellationToken { get; } = cancellationToken;

    /// <inheritdoc />
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();

    /// <inheritdoc />
    public bool ShouldSkipRemainingFilters { get; private set; }

    /// <inheritdoc />
    public bool ShouldCancelPublish { get; private set; }

    /// <inheritdoc />
    public string? CancellationReason { get; private set; }

    /// <inheritdoc />
    public Exception? Exception { get; private set; }

    /// <inheritdoc />
    public void SkipRemainingFilters()
    {
        ShouldSkipRemainingFilters = true;
    }

    /// <inheritdoc />
    public void CancelPublish(string reason)
    {
        ShouldCancelPublish = true;
        CancellationReason = reason;
    }

    /// <inheritdoc />
    public void SetException(Exception exception)
    {
        Exception = exception;
    }

    /// <inheritdoc />
    public void SetCorrelationId(string correlationId)
    {
        CorrelationId = correlationId;
        Headers["x-correlation-id"] = correlationId;
    }

    /// <inheritdoc />
    public void SetCausationId(string causationId)
    {
        CausationId = causationId;
        Headers["x-causation-id"] = causationId;
    }

    /// <summary>
    /// Resets the skip flag.
    /// </summary>
    public void ResetSkip()
    {
        ShouldSkipRemainingFilters = false;
    }

    /// <summary>
    /// Resets the cancel flag.
    /// </summary>
    public void ResetCancel()
    {
        ShouldCancelPublish = false;
        CancellationReason = null;
    }

    /// <summary>
    /// Resets the exception.
    /// </summary>
    public void ResetException()
    {
        Exception = null;
    }
}

