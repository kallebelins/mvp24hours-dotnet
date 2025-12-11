//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mvp24Hours.Infrastructure.Cqrs.Projections;

/// <summary>
/// Hosted service for running projections in background.
/// Automatically starts and stops projections with the application.
/// </summary>
/// <remarks>
/// <para>
/// This hosted service ensures that projections:
/// <list type="bullet">
/// <item>Start when the application starts</item>
/// <item>Catch up with historical events</item>
/// <item>Process new events as they arrive</item>
/// <item>Stop gracefully when the application stops</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register the hosted service
/// services.AddHostedService&lt;ProjectionHostedService&gt;();
/// 
/// // Or use the extension method
/// services.AddProjectionHostedService();
/// </code>
/// </example>
public class ProjectionHostedService : BackgroundService
{
    private readonly IProjectionManager _projectionManager;
    private readonly ILogger<ProjectionHostedService> _logger;

    /// <summary>
    /// Initializes a new instance of the projection hosted service.
    /// </summary>
    /// <param name="projectionManager">The projection manager.</param>
    /// <param name="logger">The logger.</param>
    public ProjectionHostedService(
        IProjectionManager projectionManager,
        ILogger<ProjectionHostedService> logger)
    {
        _projectionManager = projectionManager ?? throw new ArgumentNullException(nameof(projectionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Projection hosted service starting");

        try
        {
            await _projectionManager.StartAsync(stoppingToken);

            // Keep alive until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Projection hosted service stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Projection hosted service failed");
            throw;
        }
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Projection hosted service stopping");
        await _projectionManager.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Interface for projection rebuild operations.
/// </summary>
public interface IProjectionRebuildService
{
    /// <summary>
    /// Schedules a projection rebuild.
    /// </summary>
    /// <param name="projectionName">The projection name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ScheduleRebuildAsync(string projectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a scheduled rebuild.
    /// </summary>
    /// <param name="projectionName">The projection name.</param>
    /// <returns>The rebuild status.</returns>
    RebuildStatus? GetRebuildStatus(string projectionName);
}

/// <summary>
/// Status of a projection rebuild operation.
/// </summary>
public record RebuildStatus
{
    /// <summary>
    /// Gets the projection name.
    /// </summary>
    public string ProjectionName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the current state of the rebuild.
    /// </summary>
    public RebuildState State { get; init; }

    /// <summary>
    /// Gets the percentage of completion.
    /// </summary>
    public int ProgressPercentage { get; init; }

    /// <summary>
    /// Gets the number of events processed.
    /// </summary>
    public long EventsProcessed { get; init; }

    /// <summary>
    /// Gets the total number of events to process.
    /// </summary>
    public long TotalEvents { get; init; }

    /// <summary>
    /// Gets the start time of the rebuild.
    /// </summary>
    public DateTime? StartedAt { get; init; }

    /// <summary>
    /// Gets the end time of the rebuild.
    /// </summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// Gets any error that occurred.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// State of a rebuild operation.
/// </summary>
public enum RebuildState
{
    /// <summary>
    /// Rebuild is queued.
    /// </summary>
    Queued,

    /// <summary>
    /// Rebuild is in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Rebuild completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Rebuild failed with an error.
    /// </summary>
    Failed,

    /// <summary>
    /// Rebuild was cancelled.
    /// </summary>
    Cancelled
}

/// <summary>
/// Service for scheduling and tracking projection rebuilds.
/// </summary>
public class ProjectionRebuildService : IProjectionRebuildService
{
    private readonly IProjectionManager _projectionManager;
    private readonly ILogger<ProjectionRebuildService> _logger;
    private readonly Dictionary<string, RebuildStatus> _rebuildStatuses = new();
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the rebuild service.
    /// </summary>
    public ProjectionRebuildService(
        IProjectionManager projectionManager,
        ILogger<ProjectionRebuildService> logger)
    {
        _projectionManager = projectionManager ?? throw new ArgumentNullException(nameof(projectionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task ScheduleRebuildAsync(string projectionName, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _rebuildStatuses[projectionName] = new RebuildStatus
            {
                ProjectionName = projectionName,
                State = RebuildState.InProgress,
                StartedAt = DateTime.UtcNow
            };
        }

        try
        {
            _logger.LogInformation("Starting scheduled rebuild for projection {Name}", projectionName);
            
            await _projectionManager.RebuildAsync(projectionName, cancellationToken);

            lock (_lock)
            {
                _rebuildStatuses[projectionName] = new RebuildStatus
                {
                    ProjectionName = projectionName,
                    State = RebuildState.Completed,
                    StartedAt = _rebuildStatuses[projectionName].StartedAt,
                    CompletedAt = DateTime.UtcNow,
                    ProgressPercentage = 100
                };
            }
        }
        catch (OperationCanceledException)
        {
            lock (_lock)
            {
                _rebuildStatuses[projectionName] = _rebuildStatuses[projectionName] with
                {
                    State = RebuildState.Cancelled,
                    CompletedAt = DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rebuild failed for projection {Name}", projectionName);
            
            lock (_lock)
            {
                _rebuildStatuses[projectionName] = _rebuildStatuses[projectionName] with
                {
                    State = RebuildState.Failed,
                    CompletedAt = DateTime.UtcNow,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    /// <inheritdoc />
    public RebuildStatus? GetRebuildStatus(string projectionName)
    {
        lock (_lock)
        {
            _rebuildStatuses.TryGetValue(projectionName, out var status);
            return status;
        }
    }
}


