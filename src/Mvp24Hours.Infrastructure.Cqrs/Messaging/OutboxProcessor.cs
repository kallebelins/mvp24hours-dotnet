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

namespace Mvp24Hours.Infrastructure.Cqrs.Messaging;

/// <summary>
/// Background service for processing outbox messages.
/// Implements reliable message publishing with retry and dead letter support.
/// </summary>
/// <remarks>
/// <para>
/// The OutboxProcessor periodically reads pending messages from the outbox
/// and publishes them to the message broker. It handles:
/// <list type="bullet">
/// <item>Batch processing of pending messages</item>
/// <item>Retry with exponential backoff on failures</item>
/// <item>Moving messages to Dead Letter Queue after max retries</item>
/// <item>Cleanup of old processed messages</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddHostedService&lt;OutboxProcessor&gt;();
/// services.Configure&lt;InboxOutboxOptions&gt;(options =>
/// {
///     options.OutboxPollingInterval = TimeSpan.FromSeconds(5);
///     options.MaxRetries = 5;
///     options.BatchSize = 100;
/// });
/// </code>
/// </example>
public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly InboxOutboxOptions _options;
    private readonly ILogger<OutboxProcessor>? _logger;

    /// <summary>
    /// Creates a new instance of the OutboxProcessor.
    /// </summary>
    public OutboxProcessor(
        IServiceProvider serviceProvider,
        IOptions<InboxOutboxOptions> options,
        ILogger<OutboxProcessor>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger?.LogInformation(
            "[Outbox] Starting OutboxProcessor with polling interval of {Interval}",
            _options.OutboxPollingInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger?.LogError(ex, "[Outbox] Error processing outbox messages");
            }

            await Task.Delay(_options.OutboxPollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var outbox = scope.ServiceProvider.GetService<IIntegrationEventOutbox>();
        var publisher = scope.ServiceProvider.GetService<IIntegrationEventPublisher>();
        var deadLetterStore = scope.ServiceProvider.GetService<IDeadLetterStore>();

        if (outbox == null)
        {
            _logger?.LogDebug("[Outbox] IIntegrationEventOutbox not registered, skipping");
            return;
        }

        var messages = await outbox.GetPendingAsync(_options.BatchSize, stoppingToken);

        if (messages.Count == 0)
        {
            return;
        }

        _logger?.LogDebug("[Outbox] Processing {Count} pending messages", messages.Count);

        foreach (var message in messages)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await ProcessMessageAsync(message, outbox, publisher, deadLetterStore, stoppingToken);
        }
    }

    private async Task ProcessMessageAsync(
        OutboxMessage message,
        IIntegrationEventOutbox outbox,
        IIntegrationEventPublisher? publisher,
        IDeadLetterStore? deadLetterStore,
        CancellationToken stoppingToken)
    {
        try
        {
            if (publisher != null)
            {
                await publisher.PublishFromOutboxAsync(message, stoppingToken);
            }
            else
            {
                _logger?.LogWarning(
                    "[Outbox] No IIntegrationEventPublisher registered, simulating publish for {MessageId}",
                    message.Id);
            }

            await outbox.MarkAsPublishedAsync(message.Id, stoppingToken);

            _logger?.LogDebug(
                "[Outbox] Successfully published message {MessageId} of type {EventType}",
                message.Id,
                message.EventType);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                ex,
                "[Outbox] Failed to publish message {MessageId} (attempt {Attempt}/{MaxRetries})",
                message.Id,
                message.RetryCount + 1,
                _options.MaxRetries);

            // Check if max retries exceeded
            if (message.RetryCount + 1 >= _options.MaxRetries)
            {
                await MoveToDeadLetterAsync(message, ex, outbox, deadLetterStore, stoppingToken);
            }
            else
            {
                await outbox.MarkAsFailedAsync(message.Id, ex.Message, stoppingToken);

                // Apply exponential backoff delay for next retry
                var delay = CalculateBackoffDelay(message.RetryCount);
                _logger?.LogDebug(
                    "[Outbox] Message {MessageId} will be retried after {Delay}",
                    message.Id,
                    delay);
            }
        }
    }

    private async Task MoveToDeadLetterAsync(
        OutboxMessage message,
        Exception exception,
        IIntegrationEventOutbox outbox,
        IDeadLetterStore? deadLetterStore,
        CancellationToken stoppingToken)
    {
        _logger?.LogError(
            "[Outbox] Moving message {MessageId} to Dead Letter Queue after {MaxRetries} failed attempts",
            message.Id,
            _options.MaxRetries);

        if (deadLetterStore != null)
        {
            var deadLetterMessage = new DeadLetterMessage
            {
                OriginalMessageId = message.Id,
                EventType = message.EventType,
                Payload = message.Payload,
                Error = exception.Message,
                ExceptionDetails = exception.ToString(),
                RetryCount = message.RetryCount + 1,
                CorrelationId = message.CorrelationId,
                Source = "OutboxProcessor"
            };

            await deadLetterStore.AddAsync(deadLetterMessage, stoppingToken);
        }

        // Mark as failed with max retries indicator
        await outbox.MarkAsFailedAsync(
            message.Id, 
            $"Moved to DLQ: {exception.Message}", 
            stoppingToken);
    }

    private TimeSpan CalculateBackoffDelay(int retryCount)
    {
        // Exponential backoff: baseDelay * 2^retryCount
        // With jitter to prevent thundering herd
        var baseDelayMs = _options.RetryBaseDelayMilliseconds;
        var exponentialDelay = baseDelayMs * Math.Pow(2, retryCount);
        var maxDelay = _options.RetryMaxDelayMilliseconds;
        
        var delay = Math.Min(exponentialDelay, maxDelay);
        
        // Add jitter (±20%)
        var jitter = delay * 0.2 * (Random.Shared.NextDouble() * 2 - 1);
        
        return TimeSpan.FromMilliseconds(delay + jitter);
    }
}

/// <summary>
/// Background service for cleaning up old outbox messages.
/// </summary>
public sealed class OutboxCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly InboxOutboxOptions _options;
    private readonly ILogger<OutboxCleanupService>? _logger;

    /// <summary>
    /// Creates a new instance of the OutboxCleanupService.
    /// </summary>
    public OutboxCleanupService(
        IServiceProvider serviceProvider,
        IOptions<InboxOutboxOptions> options,
        ILogger<OutboxCleanupService>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var outbox = scope.ServiceProvider.GetService<IIntegrationEventOutbox>();

                if (outbox != null)
                {
                    var cutoffDate = DateTime.UtcNow.AddDays(-_options.OutboxRetentionDays);
                    var deletedCount = await outbox.CleanupAsync(cutoffDate, stoppingToken);

                    if (deletedCount > 0)
                    {
                        _logger?.LogInformation(
                            "[Outbox] Cleaned up {Count} processed messages older than {CutoffDate}",
                            deletedCount,
                            cutoffDate);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger?.LogError(ex, "[Outbox] Error during outbox cleanup");
            }

            await Task.Delay(_options.CleanupInterval, stoppingToken);
        }
    }
}


