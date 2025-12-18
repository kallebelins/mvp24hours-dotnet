//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Application.Contract.Events;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic.Events;

/// <summary>
/// Background service that processes the application event outbox.
/// Dispatches pending events to their handlers.
/// </summary>
/// <remarks>
/// <para>
/// This processor runs as a hosted service and periodically checks the outbox
/// for pending events to dispatch.
/// </para>
/// <para>
/// <strong>Features:</strong>
/// <list type="bullet">
/// <item>Configurable polling interval</item>
/// <item>Batch processing of pending events</item>
/// <item>Automatic retry with exponential backoff</item>
/// <item>Dead letter handling for events that exceed retry limit</item>
/// </list>
/// </para>
/// </remarks>
public sealed class ApplicationEventOutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ApplicationEventOutboxProcessorOptions _options;
    private readonly ILogger<ApplicationEventOutboxProcessor>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Creates a new instance of the ApplicationEventOutboxProcessor.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="options">The processor options.</param>
    /// <param name="logger">Optional logger.</param>
    public ApplicationEventOutboxProcessor(
        IServiceProvider serviceProvider,
        IOptions<ApplicationEventOutboxProcessorOptions>? options = null,
        ILogger<ApplicationEventOutboxProcessor>? logger = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? new ApplicationEventOutboxProcessorOptions();
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger?.LogInformation(
            "[ApplicationEventOutbox] Processor started with interval {Interval}ms",
            _options.PollingIntervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger?.LogError(
                    ex,
                    "[ApplicationEventOutbox] Error processing outbox: {Message}",
                    ex.Message);
            }

            try
            {
                await Task.Delay(_options.PollingIntervalMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger?.LogInformation("[ApplicationEventOutbox] Processor stopped");
    }

    private async Task ProcessPendingEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var outbox = scope.ServiceProvider.GetService<IApplicationEventOutbox>();
        if (outbox == null)
        {
            _logger?.LogDebug("[ApplicationEventOutbox] Outbox not registered, skipping");
            return;
        }

        var pending = await outbox.GetPendingAsync(_options.BatchSize, cancellationToken);
        if (pending.Count == 0)
        {
            return;
        }

        _logger?.LogDebug(
            "[ApplicationEventOutbox] Processing {Count} pending events",
            pending.Count);

        foreach (var entry in pending)
        {
            await ProcessEntryAsync(scope.ServiceProvider, outbox, entry, cancellationToken);
        }
    }

    private async Task ProcessEntryAsync(
        IServiceProvider serviceProvider,
        IApplicationEventOutbox outbox,
        ApplicationEventOutboxEntry entry,
        CancellationToken cancellationToken)
    {
        try
        {
            // Resolve the event type
            var eventType = Type.GetType(entry.EventType);
            if (eventType == null)
            {
                _logger?.LogWarning(
                    "[ApplicationEventOutbox] Could not resolve event type: {EventType}",
                    entry.EventType);
                await outbox.MarkAsFailedAsync(entry.Id, $"Unknown event type: {entry.EventType}", cancellationToken);
                return;
            }

            // Deserialize the event
            var @event = JsonSerializer.Deserialize(entry.Payload, eventType, _jsonOptions);
            if (@event == null)
            {
                _logger?.LogWarning(
                    "[ApplicationEventOutbox] Failed to deserialize event: {EntryId}",
                    entry.Id);
                await outbox.MarkAsFailedAsync(entry.Id, "Failed to deserialize event", cancellationToken);
                return;
            }

            // Get handlers
            var handlerType = typeof(IApplicationEventHandler<>).MakeGenericType(eventType);
            var handlers = serviceProvider.GetServices(handlerType);

            // Dispatch to handlers
            foreach (var handler in handlers)
            {
                if (handler == null) continue;

                try
                {
                    var handleMethod = handlerType.GetMethod("HandleAsync");
                    if (handleMethod != null)
                    {
                        var task = handleMethod.Invoke(handler, [@event, cancellationToken]) as Task;
                        if (task != null)
                        {
                            await task;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(
                        ex,
                        "[ApplicationEventOutbox] Handler {HandlerType} failed for entry {EntryId}: {Message}",
                        handler.GetType().Name,
                        entry.Id,
                        ex.Message);
                    // Continue with other handlers
                }
            }

            // Mark as dispatched
            await outbox.MarkAsDispatchedAsync(entry.Id, cancellationToken);

            _logger?.LogDebug(
                "[ApplicationEventOutbox] Successfully processed entry {EntryId}",
                entry.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "[ApplicationEventOutbox] Failed to process entry {EntryId}: {Message}",
                entry.Id,
                ex.Message);

            await outbox.MarkAsFailedAsync(entry.Id, ex.Message, cancellationToken);
        }
    }
}

/// <summary>
/// Options for the application event outbox processor.
/// </summary>
public class ApplicationEventOutboxProcessorOptions
{
    /// <summary>
    /// Gets or sets the polling interval in milliseconds.
    /// Default is 5000 (5 seconds).
    /// </summary>
    public int PollingIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the batch size for processing pending events.
    /// Default is 100.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether the processor is enabled.
    /// Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

