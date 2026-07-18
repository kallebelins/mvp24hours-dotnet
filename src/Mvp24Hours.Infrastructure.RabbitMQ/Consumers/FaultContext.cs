//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Consumers;

/// <summary>
/// Implementation of fault context for handling faulted messages.
/// </summary>
/// <typeparam name="TMessage">The type of the faulted message.</typeparam>
/// <remarks>
/// Creates a new fault context.
/// </remarks>
public class FaultContext<TMessage>(
    TMessage message,
    Exception exception,
    string messageId,
    string? correlationId,
    string exchange,
    string routingKey,
    string queueName,
    int retryCount,
    IServiceProvider serviceProvider) : IFaultContext<TMessage> where TMessage : class
{

    /// <inheritdoc />
    public TMessage Message { get; } = message ?? throw new ArgumentNullException(nameof(message));

    /// <inheritdoc />
    public Exception Exception { get; } = exception ?? throw new ArgumentNullException(nameof(exception));

    /// <inheritdoc />
    public int RetryCount { get; } = retryCount;

    /// <inheritdoc />
    public string MessageId { get; } = messageId ?? throw new ArgumentNullException(nameof(messageId));

    /// <inheritdoc />
    public string? CorrelationId { get; } = correlationId;

    /// <inheritdoc />
    public string Exchange { get; } = exchange ?? string.Empty;

    /// <inheritdoc />
    public string RoutingKey { get; } = routingKey ?? string.Empty;

    /// <inheritdoc />
    public string QueueName { get; } = queueName ?? string.Empty;

    /// <inheritdoc />
    public DateTimeOffset FaultedAt { get; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; } = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <summary>
    /// Creates a fault context from a consume context.
    /// </summary>
    public static FaultContext<TMessage> FromConsumeContext(
        IConsumeContext<TMessage> context,
        Exception exception)
    {
        return new FaultContext<TMessage>(
            context.Message,
            exception,
            context.MessageId,
            context.CorrelationId,
            context.Exchange,
            context.RoutingKey,
            context.QueueName,
            context.RedeliveryCount,
            context.ServiceProvider);
    }
}

