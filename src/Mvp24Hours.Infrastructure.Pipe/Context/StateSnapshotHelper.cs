//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Mvp24Hours.Infrastructure.Pipe.Context
{
    /// <summary>
    /// Helper class for capturing and managing pipeline state snapshots.
    /// Provides utilities for debugging, auditing, and analyzing pipeline execution.
    /// </summary>
    public static class StateSnapshotHelper
    {
        /// <summary>
        /// Default JSON serializer options for state serialization.
        /// </summary>
        private static readonly JsonSerializerOptions DefaultJsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Captures a snapshot of the pipeline message state.
        /// </summary>
        /// <param name="message">The pipeline message to capture.</param>
        /// <param name="operationName">The name of the current operation.</param>
        /// <param name="includeContents">Whether to include message contents in the snapshot.</param>
        /// <returns>A state object representing the message state.</returns>
        public static object CaptureMessageState(
            IPipelineMessage message,
            string operationName,
            bool includeContents = false)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var state = new
            {
                OperationName = operationName,
                Token = message.Token,
                IsFaulty = message.IsFaulty,
                IsLocked = message.IsLocked,
                MessageCount = message.Messages?.Count ?? 0,
                Messages = message.Messages?.Select(m => new
                {
                    m.Key,
                    m.Message,
                    m.Type
                }).ToList(),
                ContentCount = message.GetContentAll()?.Count ?? 0,
                Contents = includeContents ? GetContentsSummary(message) : null,
                CapturedAt = DateTime.UtcNow
            };

            return state;
        }

        /// <summary>
        /// Creates a comparison between two snapshots.
        /// </summary>
        /// <param name="before">The snapshot taken before an operation.</param>
        /// <param name="after">The snapshot taken after an operation.</param>
        /// <returns>An object describing the differences.</returns>
        public static object CompareSnapshots(
            PipelineStateSnapshot before,
            PipelineStateSnapshot after)
        {
            if (before == null || after == null)
            {
                throw new ArgumentNullException(before == null ? nameof(before) : nameof(after));
            }

            return new
            {
                Before = new
                {
                    before.OperationName,
                    before.CapturedAt,
                    before.SequenceNumber
                },
                After = new
                {
                    after.OperationName,
                    after.CapturedAt,
                    after.SequenceNumber
                },
                Duration = after.CapturedAt - before.CapturedAt,
                MetadataChanges = GetMetadataChanges(before.Metadata, after.Metadata)
            };
        }

        /// <summary>
        /// Serializes a snapshot to JSON for logging or storage.
        /// </summary>
        /// <param name="snapshot">The snapshot to serialize.</param>
        /// <param name="options">Optional JSON serializer options.</param>
        /// <returns>A JSON string representation of the snapshot.</returns>
        public static string SerializeSnapshot(
            PipelineStateSnapshot snapshot,
            JsonSerializerOptions? options = null)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            return JsonSerializer.Serialize(snapshot, options ?? DefaultJsonOptions);
        }

        /// <summary>
        /// Creates a timeline summary of all snapshots.
        /// </summary>
        /// <param name="snapshots">The list of snapshots.</param>
        /// <returns>A timeline summary object.</returns>
        public static object CreateTimeline(IReadOnlyList<PipelineStateSnapshot> snapshots)
        {
            if (snapshots == null || snapshots.Count == 0)
            {
                return new { Operations = Array.Empty<object>(), TotalDuration = TimeSpan.Zero };
            }

            var operations = new List<object>();
            for (int i = 0; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                var duration = i < snapshots.Count - 1
                    ? snapshots[i + 1].CapturedAt - snapshot.CapturedAt
                    : TimeSpan.Zero;

                operations.Add(new
                {
                    Index = i,
                    snapshot.SequenceNumber,
                    snapshot.OperationName,
                    snapshot.CapturedAt,
                    Duration = duration,
                    snapshot.Description,
                    HasError = snapshot.Description?.StartsWith("Error:") == true
                });
            }

            return new
            {
                Operations = operations,
                TotalDuration = snapshots[^1].CapturedAt - snapshots[0].CapturedAt,
                SnapshotCount = snapshots.Count,
                CorrelationId = snapshots[0].CorrelationId
            };
        }

        /// <summary>
        /// Filters snapshots by operation name.
        /// </summary>
        /// <param name="snapshots">The list of snapshots.</param>
        /// <param name="operationNamePattern">The operation name pattern (supports wildcards *).</param>
        /// <returns>Filtered list of snapshots.</returns>
        public static IEnumerable<PipelineStateSnapshot> FilterByOperation(
            IReadOnlyList<PipelineStateSnapshot> snapshots,
            string operationNamePattern)
        {
            if (snapshots == null)
            {
                yield break;
            }

            if (string.IsNullOrEmpty(operationNamePattern))
            {
                foreach (var snapshot in snapshots)
                {
                    yield return snapshot;
                }
                yield break;
            }

            // Simple wildcard matching
            var pattern = operationNamePattern
                .Replace("*", ".*")
                .Replace("?", ".");

            var regex = new System.Text.RegularExpressions.Regex(
                $"^{pattern}$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (var snapshot in snapshots)
            {
                if (regex.IsMatch(snapshot.OperationName))
                {
                    yield return snapshot;
                }
            }
        }

        /// <summary>
        /// Gets snapshots that contain errors.
        /// </summary>
        /// <param name="snapshots">The list of snapshots.</param>
        /// <returns>Snapshots that indicate errors.</returns>
        public static IEnumerable<PipelineStateSnapshot> GetErrorSnapshots(
            IReadOnlyList<PipelineStateSnapshot> snapshots)
        {
            if (snapshots == null)
            {
                yield break;
            }

            foreach (var snapshot in snapshots)
            {
                if (snapshot.Description?.StartsWith("Error:") == true ||
                    snapshot.OperationName.EndsWith(".Error") ||
                    snapshot.OperationName.EndsWith(".Failed"))
                {
                    yield return snapshot;
                }
            }
        }

        /// <summary>
        /// Gets a summary of contents from a pipeline message.
        /// </summary>
        private static List<object>? GetContentsSummary(IPipelineMessage message)
        {
            var contents = message.GetContentAll();
            if (contents == null || contents.Count == 0)
            {
                return null;
            }

            var summary = new List<object>();
            foreach (var content in contents)
            {
                if (content == null) continue;

                summary.Add(new
                {
                    Type = content.GetType().Name,
                    FullTypeName = content.GetType().FullName,
                    IsCollection = content is System.Collections.IEnumerable && content is not string,
                    StringPreview = content.ToString()?[..Math.Min(100, content.ToString()?.Length ?? 0)]
                });
            }

            return summary;
        }

        /// <summary>
        /// Compares two metadata dictionaries and returns the changes.
        /// </summary>
        private static object? GetMetadataChanges(
            IReadOnlyDictionary<string, object>? before,
            IReadOnlyDictionary<string, object>? after)
        {
            if (before == null && after == null)
            {
                return null;
            }

            before ??= new Dictionary<string, object>();
            after ??= new Dictionary<string, object>();

            var added = after.Keys.Except(before.Keys).ToList();
            var removed = before.Keys.Except(after.Keys).ToList();
            var modified = before.Keys.Intersect(after.Keys)
                .Where(k => !Equals(before[k], after[k]))
                .ToList();

            if (added.Count == 0 && removed.Count == 0 && modified.Count == 0)
            {
                return null;
            }

            return new
            {
                Added = added,
                Removed = removed,
                Modified = modified
            };
        }
    }
}

