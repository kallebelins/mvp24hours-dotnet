//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using System.Text.Json;

namespace Mvp24Hours.Infrastructure.Cqrs.Messaging;

/// <summary>
/// Background service for processing incoming messages with deduplication.
/// Implements the Inbox Pattern for exactly-once processing semantics.
/// </summary>
/// <remarks>
/// <para>
/// This processor works with message consumers to ensure idempotent message handling.
/// It coordinates with <see cref="IInboxStore"/> to prevent duplicate processing.
/// </para>
/// <para>
/// <strong>How to use:</strong>
/// <list type="number">
/// <item>Register the processor in DI</item>
/// <item>Inject <see cref="IInboxProcessor"/> into your message consumer</item>
/// <item>Use <see cref="ProcessAsync{TMessage}"/> to process messages idempotently</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddHostedService&lt;InboxCleanupService&gt;();
/// services.AddScoped&lt;IInboxProcessor, InboxProcessor&gt;();
/// 
/// // In your consumer
/// public class OrderCreatedConsumer
/// {
///     private readonly IInboxProcessor _processor;
///     
///     public async Task HandleAsync(OrderCreatedEvent @event)
///     {
///         await _processor.ProcessAsync(@event, async (e, ct) =>
///         {
///             // Your processing logic here
///             await _inventoryService.ReserveItemsAsync(e.Items);
///         });
///     }
/// }
/// </code>
/// </example>
public interface IInboxProcessor
{
    /// <summary>
    /// Processes a message with deduplication.
    /// </summary>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="handler">The handler function to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the message was processed; false if it was a duplicate.</returns>
    Task<bool> ProcessAsync<TMessage>(
        TMessage message,
        Func<TMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : IIntegrationEvent;

    /// <summary>
    /// Checks if a message has already been processed.
    /// </summary>
    /// <param name="messageId">The message ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the message was already processed; otherwise, false.</returns>
    Task<bool> IsDuplicateAsync(Guid messageId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of <see cref="IInboxProcessor"/>.
/// </summary>
public sealed class InboxProcessor : IInboxProcessor
{
    private readonly IInboxStore _inboxStore;
    private readonly ILogger<InboxProcessor>? _logger;

    /// <summary>
    /// Creates a new instance of the InboxProcessor.
    /// </summary>
    /// <param name="inboxStore">The inbox store for deduplication.</param>
    /// <param name="logger">Optional logger.</param>
    public InboxProcessor(
        IInboxStore inboxStore,
        ILogger<InboxProcessor>? logger = null)
    {
        _inboxStore = inboxStore ?? throw new ArgumentNullException(nameof(inboxStore));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> ProcessAsync<TMessage>(
        TMessage message,
        Func<TMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : IIntegrationEvent
    {
        // Check for duplicate
        if (await _inboxStore.ExistsAsync(message.Id, cancellationToken))
        {
            _logger?.LogDebug(
                "[Inbox] Skipping duplicate message {MessageId} of type {MessageType}",
                message.Id,
                typeof(TMessage).Name);
            return false;
        }

        try
        {
            // Process the message
            await handler(message, cancellationToken);

            // Mark as processed
            await _inboxStore.MarkAsProcessedAsync(
                message.Id, 
                typeof(TMessage).Name, 
                cancellationToken);

            _logger?.LogDebug(
                "[Inbox] Successfully processed message {MessageId} of type {MessageType}",
                message.Id,
                typeof(TMessage).Name);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "[Inbox] Failed to process message {MessageId} of type {MessageType}",
                message.Id,
                typeof(TMessage).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> IsDuplicateAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return _inboxStore.ExistsAsync(messageId, cancellationToken);
    }
}

/// <summary>
/// Background service for cleaning up old inbox messages.
/// </summary>
/// <remarks>
/// <para>
/// <b>.NET 6+ PeriodicTimer:</b> This implementation uses PeriodicTimer instead of
/// Task.Delay for modern async/await patterns with proper cancellation support.
/// </para>
/// </remarks>
public sealed class InboxCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly InboxOutboxOptions _options;
    private readonly ILogger<InboxCleanupService>? _logger;

    /// <summary>
    /// Creates a new instance of the InboxCleanupService.
    /// </summary>
    public InboxCleanupService(
        IServiceProvider serviceProvider,
        IOptions<InboxOutboxOptions> options,
        ILogger<InboxCleanupService>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Use PeriodicTimer for modern async/await patterns
        using var timer = new PeriodicTimer(_options.CleanupInterval);
        
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var inboxStore = scope.ServiceProvider.GetService<IInboxStore>();

                    if (inboxStore != null)
                    {
                        var cutoffDate = DateTime.UtcNow.AddDays(-_options.InboxRetentionDays);
                        var deletedCount = await inboxStore.CleanupAsync(cutoffDate, stoppingToken);

                        if (deletedCount > 0)
                        {
                            _logger?.LogInformation(
                                "[Inbox] Cleaned up {Count} messages older than {CutoffDate}",
                                deletedCount,
                                cutoffDate);
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger?.LogError(ex, "[Inbox] Error during inbox cleanup");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger?.LogDebug("[Inbox] InboxCleanupService stopping gracefully");
        }
    }
}


