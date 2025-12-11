//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mvp24Hours.Infrastructure.Cqrs.Saga;

/// <summary>
/// Background service that processes saga timeouts, retries, and cleanup.
/// </summary>
/// <remarks>
/// <para>
/// This service runs in the background and periodically:
/// <list type="bullet">
/// <item>Processes timed out sagas</item>
/// <item>Retries suspended sagas that are ready</item>
/// <item>Cleans up expired saga states</item>
/// </list>
/// </para>
/// </remarks>
public class SagaHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SagaHostedService> _logger;
    private readonly SagaHostedServiceOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaHostedService"/> class.
    /// </summary>
    public SagaHostedService(
        IServiceProvider serviceProvider,
        ILogger<SagaHostedService> logger,
        SagaHostedServiceOptions? options = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new SagaHostedServiceOptions();
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Saga background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in saga background processing");
            }

            await Task.Delay(_options.ProcessingInterval, stoppingToken);
        }

        _logger.LogInformation("Saga background service stopped");
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<ISagaOrchestrator>();

        // Process timeouts
        if (_options.ProcessTimeouts)
        {
            var timedOut = await orchestrator.ProcessTimeoutsAsync(cancellationToken);
            if (timedOut > 0)
            {
                _logger.LogInformation("Processed {Count} timed out sagas", timedOut);
            }
        }

        // Process retries
        if (_options.ProcessRetries)
        {
            var retried = await orchestrator.ProcessRetryQueueAsync(cancellationToken);
            if (retried > 0)
            {
                _logger.LogInformation("Processed {Count} saga retries", retried);
            }
        }

        // Cleanup expired states
        if (_options.CleanupExpired)
        {
            var cleaned = await orchestrator.CleanupAsync(
                DateTime.UtcNow.Subtract(_options.CleanupOlderThan),
                cancellationToken);
            if (cleaned > 0)
            {
                _logger.LogDebug("Cleaned up {Count} expired saga states", cleaned);
            }
        }
    }
}

/// <summary>
/// Options for the saga background service.
/// </summary>
public class SagaHostedServiceOptions
{
    /// <summary>
    /// Gets or sets the interval between processing cycles.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan ProcessingInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets whether to process timed out sagas.
    /// Default is true.
    /// </summary>
    public bool ProcessTimeouts { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to process saga retries.
    /// Default is true.
    /// </summary>
    public bool ProcessRetries { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to cleanup expired saga states.
    /// Default is true.
    /// </summary>
    public bool CleanupExpired { get; set; } = true;

    /// <summary>
    /// Gets or sets the age threshold for cleanup.
    /// States older than this will be cleaned up.
    /// Default is 7 days.
    /// </summary>
    public TimeSpan CleanupOlderThan { get; set; } = TimeSpan.FromDays(7);
}

