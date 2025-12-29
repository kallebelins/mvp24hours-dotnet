//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Testing.Observability
{
    /// <summary>
    /// Represents a recorded activity (span) captured by FakeActivityListener.
    /// </summary>
    public sealed class RecordedActivity
    {
        /// <summary>
        /// Gets the activity operation name.
        /// </summary>
        public string OperationName { get; }

        /// <summary>
        /// Gets the source name.
        /// </summary>
        public string SourceName { get; }

        /// <summary>
        /// Gets the activity ID.
        /// </summary>
        public string? Id { get; }

        /// <summary>
        /// Gets the parent activity ID.
        /// </summary>
        public string? ParentId { get; }

        /// <summary>
        /// Gets the trace ID.
        /// </summary>
        public string? TraceId { get; }

        /// <summary>
        /// Gets the span ID.
        /// </summary>
        public string? SpanId { get; }

        /// <summary>
        /// Gets the activity kind.
        /// </summary>
        public ActivityKind Kind { get; }

        /// <summary>
        /// Gets the activity status.
        /// </summary>
        public ActivityStatusCode Status { get; }

        /// <summary>
        /// Gets the status description.
        /// </summary>
        public string? StatusDescription { get; }

        /// <summary>
        /// Gets the start time.
        /// </summary>
        public DateTimeOffset StartTime { get; }

        /// <summary>
        /// Gets the duration.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Gets the tags associated with this activity.
        /// </summary>
        public IReadOnlyDictionary<string, string?> Tags { get; }

        /// <summary>
        /// Gets the events associated with this activity.
        /// </summary>
        public IReadOnlyList<ActivityEvent> Events { get; }

        /// <summary>
        /// Gets the links associated with this activity.
        /// </summary>
        public IReadOnlyList<ActivityLink> Links { get; }

        /// <summary>
        /// Gets the baggage items.
        /// </summary>
        public IReadOnlyDictionary<string, string?> Baggage { get; }

        /// <summary>
        /// Indicates if this activity had an error status.
        /// </summary>
        public bool HasError => Status == ActivityStatusCode.Error;

        /// <summary>
        /// Creates a new recorded activity from an Activity.
        /// </summary>
        public RecordedActivity(Activity activity)
        {
            OperationName = activity.OperationName;
            SourceName = activity.Source.Name;
            Id = activity.Id;
            ParentId = activity.ParentId;
            TraceId = activity.TraceId.ToString();
            SpanId = activity.SpanId.ToString();
            Kind = activity.Kind;
            Status = activity.Status;
            StatusDescription = activity.StatusDescription;
            StartTime = activity.StartTimeUtc;
            Duration = activity.Duration;
            Tags = activity.Tags.ToDictionary(t => t.Key, t => t.Value);
            Events = activity.Events.ToList().AsReadOnly();
            Links = activity.Links.ToList().AsReadOnly();
            Baggage = activity.Baggage.ToDictionary(b => b.Key, b => b.Value);
        }

        /// <summary>
        /// Gets a tag value.
        /// </summary>
        /// <param name="key">The tag key.</param>
        /// <returns>The tag value, or null if not found.</returns>
        public string? GetTag(string key)
        {
            return Tags.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// Checks if a tag exists.
        /// </summary>
        /// <param name="key">The tag key.</param>
        /// <returns>True if the tag exists.</returns>
        public bool HasTag(string key)
        {
            return Tags.ContainsKey(key);
        }

        /// <summary>
        /// Checks if an event with the specified name exists.
        /// </summary>
        /// <param name="eventName">The event name.</param>
        /// <returns>True if the event exists.</returns>
        public bool HasEvent(string eventName)
        {
            return Events.Any(e => e.Name == eventName);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[{Kind}] {SourceName}/{OperationName} ({Duration.TotalMilliseconds:F2}ms) - {Status}";
        }
    }

    /// <summary>
    /// A fake ActivityListener for capturing distributed tracing spans in tests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// FakeActivityListener provides a way to capture and verify OpenTelemetry-style
    /// spans (Activities) in unit and integration tests. It automatically subscribes
    /// to ActivitySources and records all activities for later assertions.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create and start listening
    /// using var listener = new FakeActivityListener("Mvp24Hours.*");
    /// 
    /// // Run code that creates activities
    /// await service.ProcessAsync();
    /// 
    /// // Verify spans
    /// Assert.True(listener.HasActivity("Mvp24Hours.Pipe.Pipeline"));
    /// Assert.Equal(5, listener.GetActivities("Mvp24Hours.Pipe.Operation").Count);
    /// </code>
    /// </example>
    public sealed class FakeActivityListener : IDisposable
    {
        private readonly ActivityListener _listener;
        private readonly ConcurrentBag<RecordedActivity> _activities = new();
        private readonly string? _sourceNameFilter;
        private bool _disposed;

        /// <summary>
        /// Gets all recorded activities.
        /// </summary>
        public IReadOnlyList<RecordedActivity> Activities => _activities.ToList().AsReadOnly();

        /// <summary>
        /// Gets the total count of recorded activities.
        /// </summary>
        public int ActivityCount => _activities.Count;

        /// <summary>
        /// Event raised when a new activity is recorded.
        /// </summary>
        public event EventHandler<RecordedActivity>? ActivityRecorded;

        /// <summary>
        /// Creates a new FakeActivityListener that listens to all ActivitySources.
        /// </summary>
        public FakeActivityListener() : this(null)
        {
        }

        /// <summary>
        /// Creates a new FakeActivityListener with an optional source name filter.
        /// </summary>
        /// <param name="sourceNameFilter">
        /// Optional filter for source names. Supports wildcard (*) at the end.
        /// Examples: "Mvp24Hours.*", "Mvp24Hours.Pipe", null (all sources).
        /// </param>
        public FakeActivityListener(string? sourceNameFilter)
        {
            _sourceNameFilter = sourceNameFilter;

            _listener = new ActivityListener
            {
                ShouldListenTo = ShouldListenTo,
                Sample = SampleAllData,
                ActivityStarted = OnActivityStarted,
                ActivityStopped = OnActivityStopped
            };

            ActivitySource.AddActivityListener(_listener);
        }

        private bool ShouldListenTo(ActivitySource source)
        {
            if (string.IsNullOrEmpty(_sourceNameFilter))
                return true;

            if (_sourceNameFilter.EndsWith("*"))
            {
                var prefix = _sourceNameFilter[..^1];
                return source.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }

            return source.Name.Equals(_sourceNameFilter, StringComparison.OrdinalIgnoreCase);
        }

        private static ActivitySamplingResult SampleAllData(ref ActivityCreationOptions<ActivityContext> options)
        {
            return ActivitySamplingResult.AllDataAndRecorded;
        }

        private void OnActivityStarted(Activity activity)
        {
            // We record on stop to capture duration and final status
        }

        private void OnActivityStopped(Activity activity)
        {
            var recorded = new RecordedActivity(activity);
            _activities.Add(recorded);
            ActivityRecorded?.Invoke(this, recorded);
        }

        /// <summary>
        /// Gets all activities with the specified operation name.
        /// </summary>
        /// <param name="operationName">The operation name to filter by.</param>
        /// <returns>A list of matching activities.</returns>
        public IReadOnlyList<RecordedActivity> GetActivities(string operationName)
        {
            return _activities
                .Where(a => a.OperationName.Equals(operationName, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Gets all activities from the specified source.
        /// </summary>
        /// <param name="sourceName">The source name to filter by.</param>
        /// <returns>A list of matching activities.</returns>
        public IReadOnlyList<RecordedActivity> GetActivitiesFromSource(string sourceName)
        {
            return _activities
                .Where(a => a.SourceName.Equals(sourceName, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Gets all activities matching a predicate.
        /// </summary>
        /// <param name="predicate">The predicate to filter activities.</param>
        /// <returns>A list of matching activities.</returns>
        public IReadOnlyList<RecordedActivity> GetActivities(Func<RecordedActivity, bool> predicate)
        {
            return _activities.Where(predicate).ToList().AsReadOnly();
        }

        /// <summary>
        /// Checks if any activity was recorded with the specified operation name.
        /// </summary>
        /// <param name="operationName">The operation name to check for.</param>
        /// <returns>True if a matching activity was found.</returns>
        public bool HasActivity(string operationName)
        {
            return _activities.Any(a => a.OperationName.Equals(operationName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if any activity was recorded from the specified source.
        /// </summary>
        /// <param name="sourceName">The source name to check for.</param>
        /// <returns>True if a matching activity was found.</returns>
        public bool HasActivityFromSource(string sourceName)
        {
            return _activities.Any(a => a.SourceName.Equals(sourceName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if any activity with an error status was recorded.
        /// </summary>
        /// <returns>True if an error activity was found.</returns>
        public bool HasErrors()
        {
            return _activities.Any(a => a.HasError);
        }

        /// <summary>
        /// Gets all activities with error status.
        /// </summary>
        /// <returns>A list of error activities.</returns>
        public IReadOnlyList<RecordedActivity> GetErrorActivities()
        {
            return _activities.Where(a => a.HasError).ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets the count of activities by operation name.
        /// </summary>
        /// <returns>A dictionary of operation name to count.</returns>
        public IReadOnlyDictionary<string, int> GetActivityCountsByOperation()
        {
            return _activities
                .GroupBy(a => a.OperationName)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Gets the count of activities by source name.
        /// </summary>
        /// <returns>A dictionary of source name to count.</returns>
        public IReadOnlyDictionary<string, int> GetActivityCountsBySource()
        {
            return _activities
                .GroupBy(a => a.SourceName)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Gets the average duration for activities with the specified operation name.
        /// </summary>
        /// <param name="operationName">The operation name.</param>
        /// <returns>The average duration, or null if no matching activities.</returns>
        public TimeSpan? GetAverageDuration(string operationName)
        {
            var matching = _activities.Where(a => a.OperationName.Equals(operationName, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!matching.Any())
                return null;

            return TimeSpan.FromTicks((long)matching.Average(a => a.Duration.Ticks));
        }

        /// <summary>
        /// Clears all recorded activities.
        /// </summary>
        public void Clear()
        {
            _activities.Clear();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _listener.Dispose();
                _disposed = true;
            }
        }
    }
}

