//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Filters;

/// <summary>
/// Consume filter that propagates correlation and causation IDs for distributed tracing.
/// Stores correlation context for use in downstream operations.
/// </summary>
/// <remarks>
/// Creates a new correlation consume filter.
/// </remarks>
/// <param name="logger">Optional logger instance.</param>
public class CorrelationConsumeFilter(ILogger<CorrelationConsumeFilter>? logger = null) : IConsumeFilter
{
    private readonly ILogger<CorrelationConsumeFilter>? _logger = logger;
    private static readonly AsyncLocal<CorrelationContext?> _currentContext = new();

    /// <summary>
    /// Gets the current correlation context.
    /// </summary>
    public static CorrelationContext? Current => _currentContext.Value;

    /// <inheritdoc />
    public async Task ConsumeAsync<TMessage>(
        IConsumeFilterContext<TMessage> context,
        ConsumeFilterDelegate<TMessage> next,
        CancellationToken cancellationToken = default) where TMessage : class
    {
        // Extract or generate correlation ID
        string correlationId = context.CorrelationId ?? context.MessageId;
        string? causationId = context.CausationId;
        string messageId = context.MessageId;

        // Create correlation context
        var correlationContext = new CorrelationContext(
            correlationId,
            causationId,
            messageId);

        // Store in AsyncLocal for downstream access
        CorrelationContext? previousContext = _currentContext.Value;
        _currentContext.Value = correlationContext;

        // Store in Items for filter chain access
        context.Items["CorrelationId"] = correlationId;
        context.Items["CausationId"] = causationId;
        context.Items["MessageId"] = messageId;
        context.Items["CorrelationContext"] = correlationContext;

        LogCorrelationStarted(correlationId, causationId, messageId);

        try
        {
            await next(context, cancellationToken);
        }
        finally
        {
            // Restore previous context
            _currentContext.Value = previousContext;

            LogCorrelationEnded(correlationId);
        }
    }

    private void LogCorrelationStarted(string correlationId, string? causationId, string messageId)
    {
        _logger?.LogDebug(
            "Correlation context started. CorrelationId={CorrelationId}, CausationId={CausationId}, MessageId={MessageId}",
            correlationId, causationId, messageId);
    }

    private void LogCorrelationEnded(string correlationId)
    {
        _logger?.LogDebug(
            "Correlation context ended. CorrelationId={CorrelationId}",
            correlationId);
    }
}

/// <summary>
/// Publish filter that propagates correlation and causation IDs.
/// </summary>
/// <remarks>
/// Creates a new correlation publish filter.
/// </remarks>
/// <param name="logger">Optional logger instance.</param>
public class CorrelationPublishFilter(ILogger<CorrelationPublishFilter>? logger = null) : IPublishFilter
{
    private readonly ILogger<CorrelationPublishFilter>? _logger = logger;

    /// <inheritdoc />
    public async Task PublishAsync<TMessage>(
        IPublishFilterContext<TMessage> context,
        PublishFilterDelegate<TMessage> next,
        CancellationToken cancellationToken = default) where TMessage : class
    {
        // Get current correlation context
        CorrelationContext? currentContext = CorrelationConsumeFilter.Current;

        // Set correlation ID (preserve existing or use from context or generate new)
        string correlationId = context.CorrelationId
            ?? currentContext?.CorrelationId
            ?? Guid.NewGuid().ToString();

        context.SetCorrelationId(correlationId);

        // Set causation ID to the current message ID (the message that caused this publish)
        string? causationId = currentContext?.MessageId ?? context.CausationId;
        if (!string.IsNullOrEmpty(causationId))
        {
            context.SetCausationId(causationId);
        }

        LogCorrelationPropagated(correlationId, causationId, context.MessageId);

        await next(context, cancellationToken);
    }

    private void LogCorrelationPropagated(string correlationId, string? causationId, string messageId)
    {
        _logger?.LogDebug(
            "Correlation propagated to publish. CorrelationId={CorrelationId}, CausationId={CausationId}, MessageId={MessageId}",
            correlationId, causationId, messageId);
    }
}

/// <summary>
/// Send filter that propagates correlation and causation IDs.
/// </summary>
/// <remarks>
/// Creates a new correlation send filter.
/// </remarks>
/// <param name="logger">Optional logger instance.</param>
public class CorrelationSendFilter(ILogger<CorrelationSendFilter>? logger = null) : ISendFilter
{
    private readonly ILogger<CorrelationSendFilter>? _logger = logger;

    /// <inheritdoc />
    public async Task SendAsync<TMessage>(
        ISendFilterContext<TMessage> context,
        SendFilterDelegate<TMessage> next,
        CancellationToken cancellationToken = default) where TMessage : class
    {
        // Get current correlation context
        CorrelationContext? currentContext = CorrelationConsumeFilter.Current;

        // Set correlation ID (preserve existing or use from context or generate new)
        string correlationId = context.CorrelationId
            ?? currentContext?.CorrelationId
            ?? Guid.NewGuid().ToString();

        context.SetCorrelationId(correlationId);

        // Set causation ID to the current message ID (the message that caused this send)
        string? causationId = currentContext?.MessageId ?? context.CausationId;
        if (!string.IsNullOrEmpty(causationId))
        {
            context.SetCausationId(causationId);
        }

        LogCorrelationPropagated(correlationId, causationId, context.MessageId);

        await next(context, cancellationToken);
    }

    private void LogCorrelationPropagated(string correlationId, string? causationId, string messageId)
    {
        _logger?.LogDebug(
            "Correlation propagated to send. CorrelationId={CorrelationId}, CausationId={CausationId}, MessageId={MessageId}",
            correlationId, causationId, messageId);
    }
}

/// <summary>
/// Represents the correlation context for distributed tracing.
/// </summary>
/// <remarks>
/// Creates a new correlation context.
/// </remarks>
/// <param name="correlationId">The correlation ID.</param>
/// <param name="causationId">The causation ID.</param>
/// <param name="messageId">The message ID.</param>
public class CorrelationContext(string correlationId, string? causationId, string messageId)
{

    /// <summary>
    /// Gets the correlation ID for distributed tracing.
    /// </summary>
    public string CorrelationId { get; } = correlationId ?? throw new ArgumentNullException(nameof(correlationId));

    /// <summary>
    /// Gets the causation ID linking to the parent operation.
    /// </summary>
    public string? CausationId { get; } = causationId;

    /// <summary>
    /// Gets the current message ID.
    /// </summary>
    public string MessageId { get; } = messageId ?? throw new ArgumentNullException(nameof(messageId));

    /// <summary>
    /// Gets when this correlation context was created.
    /// </summary>
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;
}

