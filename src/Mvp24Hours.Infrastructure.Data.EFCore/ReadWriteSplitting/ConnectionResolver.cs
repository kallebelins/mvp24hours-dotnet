//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mvp24Hours.Infrastructure.Data.EFCore.ReadWriteSplitting;

/// <summary>
/// Default implementation of connection resolver for read/write splitting.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConnectionResolver"/> class.
/// </remarks>
public class ConnectionResolver(
    IReplicaSelector replicaSelector,
    IOptions<ReadWriteOptions> options,
    ILogger<ConnectionResolver> logger) : IConnectionResolver
{
    private readonly IReplicaSelector _replicaSelector = replicaSelector ?? throw new ArgumentNullException(nameof(replicaSelector));
    private readonly ReadWriteOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<ConnectionResolver> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private DateTime? _lastWriteTime;

    /// <inheritdoc/>
    public bool IsReadForcedToPrimary { get => field || IsWithinReadAfterWriteWindow(); private set; }

    /// <inheritdoc/>
    public async Task<string> GetReadConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        // Check if we should read from primary
        if (IsReadForcedToPrimary)
        {
            if (_options.LogReplicaSelection)
            {
                _logger.LogDebug("Reading from primary due to read-after-write consistency");
            }
            return _replicaSelector.GetPrimaryConnectionString();
        }

        // Check if any replicas are configured
        if (_options.ReplicaConnectionStrings.Count == 0)
        {
            _logger.LogDebug("No replicas configured, using primary for read");
            return _replicaSelector.GetPrimaryConnectionString();
        }

        // Select a replica
        string? replica = await _replicaSelector.SelectReplicaAsync(cancellationToken);

        if (replica == null)
        {
            _logger.LogWarning("No available replicas, falling back to primary");
            return _replicaSelector.GetPrimaryConnectionString();
        }

        return replica;
    }

    /// <inheritdoc/>
    public string GetWriteConnectionString()
    {
        return _replicaSelector.GetPrimaryConnectionString();
    }

    /// <inheritdoc/>
    public void ForceReadFromPrimary()
    {
        IsReadForcedToPrimary = true;
        _logger.LogDebug("Reads forced to primary");
    }

    /// <inheritdoc/>
    public void ResetReadFromPrimary()
    {
        IsReadForcedToPrimary = false;
        _lastWriteTime = null;
        _logger.LogDebug("Read from primary reset");
    }

    /// <inheritdoc/>
    public void NotifyWritePerformed()
    {
        if (_options.EnableReadAfterWriteConsistency)
        {
            _lastWriteTime = DateTime.UtcNow;

            if (_options.LogReplicaSelection)
            {
                _logger.LogDebug(
                    "Write performed, reads will use primary for {Duration}ms",
                    _options.ReadAfterWriteWindow.TotalMilliseconds);
            }
        }
    }

    private bool IsWithinReadAfterWriteWindow()
    {
        if (!_options.EnableReadAfterWriteConsistency || !_lastWriteTime.HasValue)
        {
            return false;
        }

        return DateTime.UtcNow - _lastWriteTime.Value < _options.ReadAfterWriteWindow;
    }
}

