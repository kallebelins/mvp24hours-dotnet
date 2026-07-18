//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Pipeline;

/// <summary>
/// Implementation of send filter context with message metadata and filter operations.
/// </summary>
/// <typeparam name="TMessage">The type of the message being sent.</typeparam>
/// <remarks>
/// Creates a new send filter context.
/// </remarks>
/// <param name="message">The message being sent.</param>
/// <param name="destinationQueue">The destination queue name.</param>
/// <param name="serviceProvider">The service provider.</param>
/// <param name="messageId">Optional message ID. Generated if not provided.</param>
/// <param name="correlationId">Optional correlation ID.</param>
/// <param name="causationId">Optional causation ID.</param>
/// <param name="headers">Optional initial headers.</param>
/// <param name="cancellationToken">Cancellation token.</param>
public class SendFilterContext<TMessage>(
    TMessage message,
    string destinationQueue,
    IServiceProvider serviceProvider,
    string? messageId = null,
    string? correlationId = null,
    string? causationId = null,
    IDictionary<string, object>? headers = null,
    CancellationToken cancellationToken = default) : ISendFilterContext<TMessage> where TMessage : class
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
    public string DestinationQueue { get; } = destinationQueue ?? throw new ArgumentNullException(nameof(destinationQueue));

    /// <inheritdoc />
    public byte? Priority { get; set; }

    /// <inheritdoc />
    public int? TtlMilliseconds { get; set; }

    /// <inheritdoc />
    public DateTimeOffset SentAt { get; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; } = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <inheritdoc />
    public CancellationToken CancellationToken { get; } = cancellationToken;

    /// <inheritdoc />
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();

    /// <inheritdoc />
    public bool ShouldSkipRemainingFilters { get; private set; }

    /// <inheritdoc />
    public bool ShouldCancelSend { get; private set; }

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
    public void CancelSend(string reason)
    {
        ShouldCancelSend = true;
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
        ShouldCancelSend = false;
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

