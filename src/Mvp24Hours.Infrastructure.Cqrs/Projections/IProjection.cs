//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace Mvp24Hours.Infrastructure.Cqrs.Projections;

/// <summary>
/// Marker interface for read model projections.
/// A projection is a denormalized view of data optimized for querying.
/// </summary>
/// <remarks>
/// <para>
/// <strong>CQRS Pattern:</strong>
/// Projections are the "Query" side of CQRS. They maintain read-optimized views
/// of data that are updated in response to domain events.
/// </para>
/// <para>
/// <strong>Key Characteristics:</strong>
/// <list type="bullet">
/// <item>Optimized for specific query patterns</item>
/// <item>Denormalized for fast reads</item>
/// <item>Eventually consistent with the event store</item>
/// <item>Can be rebuilt from events at any time</item>
/// </list>
/// </para>
/// </remarks>
public interface IProjection
{
    /// <summary>
    /// Gets the unique name of this projection.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the current position (global event position) of this projection.
    /// </summary>
    long Position { get; }

    /// <summary>
    /// Gets the status of this projection.
    /// </summary>
    ProjectionStatus Status { get; }
}

/// <summary>
/// Interface for projections that can be reset and rebuilt.
/// </summary>
public interface IResettableProjection : IProjection
{
    /// <summary>
    /// Resets the projection to its initial state.
    /// All projected data should be cleared.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ResetAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for projections that track their own position.
/// </summary>
public interface IPositionTrackedProjection : IProjection
{
    /// <summary>
    /// Updates the projection's position after processing events.
    /// </summary>
    /// <param name="position">The new position.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdatePositionAsync(long position, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the status of a projection.
/// </summary>
public enum ProjectionStatus
{
    /// <summary>
    /// The projection has not been started.
    /// </summary>
    NotStarted,

    /// <summary>
    /// The projection is currently catching up with events.
    /// </summary>
    CatchingUp,

    /// <summary>
    /// The projection is up to date and processing live events.
    /// </summary>
    Live,

    /// <summary>
    /// The projection has been stopped.
    /// </summary>
    Stopped,

    /// <summary>
    /// The projection encountered an error and is in a faulted state.
    /// </summary>
    Faulted,

    /// <summary>
    /// The projection is being rebuilt.
    /// </summary>
    Rebuilding
}

/// <summary>
/// Base class for projections with common functionality.
/// </summary>
public abstract class ProjectionBase : IResettableProjection, IPositionTrackedProjection
{
    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public long Position { get; protected set; }

    /// <inheritdoc />
    public ProjectionStatus Status { get; protected set; } = ProjectionStatus.NotStarted;

    /// <summary>
    /// Gets or sets the last error that occurred during projection.
    /// </summary>
    public Exception? LastError { get; protected set; }

    /// <summary>
    /// Gets or sets the timestamp of the last update.
    /// </summary>
    public DateTime? LastUpdatedAt { get; protected set; }

    /// <inheritdoc />
    public virtual Task ResetAsync(CancellationToken cancellationToken = default)
    {
        Position = 0;
        Status = ProjectionStatus.NotStarted;
        LastError = null;
        LastUpdatedAt = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task UpdatePositionAsync(long position, CancellationToken cancellationToken = default)
    {
        Position = position;
        LastUpdatedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the projection status to Live.
    /// </summary>
    public void SetLive()
    {
        Status = ProjectionStatus.Live;
    }

    /// <summary>
    /// Sets the projection status to CatchingUp.
    /// </summary>
    public void SetCatchingUp()
    {
        Status = ProjectionStatus.CatchingUp;
    }

    /// <summary>
    /// Sets the projection to faulted state with an error.
    /// </summary>
    /// <param name="error">The error that caused the fault.</param>
    public void SetFaulted(Exception error)
    {
        Status = ProjectionStatus.Faulted;
        LastError = error;
    }

    /// <summary>
    /// Sets the projection status to Rebuilding.
    /// </summary>
    public void SetRebuilding()
    {
        Status = ProjectionStatus.Rebuilding;
    }

    /// <summary>
    /// Sets the projection status to Stopped.
    /// </summary>
    public void SetStopped()
    {
        Status = ProjectionStatus.Stopped;
    }
}

/// <summary>
/// Information about a projection's state for monitoring.
/// </summary>
public class ProjectionInfo
{
    /// <summary>
    /// Gets or sets the projection name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current position.
    /// </summary>
    public long Position { get; set; }

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public ProjectionStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the last error message.
    /// </summary>
    public string? LastErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime? LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the event types this projection handles.
    /// </summary>
    public IReadOnlyList<string> HandledEventTypes { get; set; } = Array.Empty<string>();
}


