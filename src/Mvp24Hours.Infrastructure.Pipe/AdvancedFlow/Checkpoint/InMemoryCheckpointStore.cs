//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.Checkpoint
{
    /// <summary>
    /// In-memory implementation of checkpoint store for development and testing.
    /// </summary>
    public sealed class InMemoryCheckpointStore : ICheckpointStore
    {
        private readonly ConcurrentDictionary<string, PipelineCheckpoint> _checkpoints = new();
        private readonly ConcurrentDictionary<string, List<string>> _executionCheckpoints = new();

        /// <inheritdoc/>
        public Task SaveCheckpointAsync(PipelineCheckpoint checkpoint, CancellationToken cancellationToken = default)
        {
            if (checkpoint == null)
                throw new ArgumentNullException(nameof(checkpoint));

            _checkpoints[checkpoint.CheckpointId] = checkpoint;

            _executionCheckpoints.AddOrUpdate(
                checkpoint.PipelineExecutionId,
                _ => [checkpoint.CheckpointId],
                (_, list) =>
                {
                    if (!list.Contains(checkpoint.CheckpointId))
                    {
                        list.Add(checkpoint.CheckpointId);
                    }
                    return list;
                });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<PipelineCheckpoint?> GetCheckpointAsync(string checkpointId, CancellationToken cancellationToken = default)
        {
            _checkpoints.TryGetValue(checkpointId, out var checkpoint);
            return Task.FromResult(checkpoint);
        }

        /// <inheritdoc/>
        public Task<PipelineCheckpoint?> GetLatestCheckpointAsync(string pipelineExecutionId, CancellationToken cancellationToken = default)
        {
            if (!_executionCheckpoints.TryGetValue(pipelineExecutionId, out var checkpointIds))
            {
                return Task.FromResult<PipelineCheckpoint?>(null);
            }

            var latestCheckpoint = checkpointIds
                .Select(id => _checkpoints.GetValueOrDefault(id))
                .Where(c => c != null)
                .OrderByDescending(c => c!.CreatedAt)
                .FirstOrDefault();

            return Task.FromResult(latestCheckpoint);
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<PipelineCheckpoint>> GetCheckpointsAsync(string pipelineExecutionId, CancellationToken cancellationToken = default)
        {
            if (!_executionCheckpoints.TryGetValue(pipelineExecutionId, out var checkpointIds))
            {
                return Task.FromResult<IReadOnlyList<PipelineCheckpoint>>([]);
            }

            var checkpoints = checkpointIds
                .Select(id => _checkpoints.GetValueOrDefault(id))
                .Where(c => c != null)
                .OrderBy(c => c!.CreatedAt)
                .ToList()!;

            return Task.FromResult<IReadOnlyList<PipelineCheckpoint>>(checkpoints!);
        }

        /// <inheritdoc/>
        public Task UpdateCheckpointStatusAsync(string checkpointId, CheckpointStatus status, string? errorMessage = null, CancellationToken cancellationToken = default)
        {
            if (_checkpoints.TryGetValue(checkpointId, out var existing))
            {
                var updated = new PipelineCheckpoint
                {
                    CheckpointId = existing.CheckpointId,
                    PipelineExecutionId = existing.PipelineExecutionId,
                    PipelineName = existing.PipelineName,
                    StepIndex = existing.StepIndex,
                    StepId = existing.StepId,
                    StepName = existing.StepName,
                    StateData = existing.StateData,
                    StateTypeName = existing.StateTypeName,
                    CreatedAt = existing.CreatedAt,
                    Metadata = existing.Metadata,
                    Status = status,
                    ErrorMessage = errorMessage ?? existing.ErrorMessage,
                    CorrelationId = existing.CorrelationId
                };

                _checkpoints[checkpointId] = updated;
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteCheckpointAsync(string checkpointId, CancellationToken cancellationToken = default)
        {
            if (_checkpoints.TryRemove(checkpointId, out var checkpoint))
            {
                if (_executionCheckpoints.TryGetValue(checkpoint.PipelineExecutionId, out var list))
                {
                    list.Remove(checkpointId);
                }
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteCheckpointsAsync(string pipelineExecutionId, CancellationToken cancellationToken = default)
        {
            if (_executionCheckpoints.TryRemove(pipelineExecutionId, out var checkpointIds))
            {
                foreach (var id in checkpointIds)
                {
                    _checkpoints.TryRemove(id, out _);
                }
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<PipelineCheckpoint>> GetResumableCheckpointsAsync(string? pipelineName = null, CancellationToken cancellationToken = default)
        {
            var resumable = _checkpoints.Values
                .Where(c => c.Status == CheckpointStatus.Paused || c.Status == CheckpointStatus.Failed)
                .Where(c => pipelineName == null || c.PipelineName == pipelineName)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            return Task.FromResult<IReadOnlyList<PipelineCheckpoint>>(resumable);
        }

        /// <inheritdoc/>
        public Task<int> CleanupExpiredCheckpointsAsync(TimeSpan expirationTime, CancellationToken cancellationToken = default)
        {
            var cutoff = DateTime.UtcNow - expirationTime;
            var expiredIds = _checkpoints.Values
                .Where(c => c.CreatedAt < cutoff)
                .Select(c => c.CheckpointId)
                .ToList();

            foreach (var id in expiredIds)
            {
                DeleteCheckpointAsync(id, cancellationToken);
            }

            return Task.FromResult(expiredIds.Count);
        }

        /// <summary>
        /// Gets the total number of checkpoints stored.
        /// </summary>
        public int Count => _checkpoints.Count;

        /// <summary>
        /// Clears all checkpoints.
        /// </summary>
        public void Clear()
        {
            _checkpoints.Clear();
            _executionCheckpoints.Clear();
        }
    }
}

