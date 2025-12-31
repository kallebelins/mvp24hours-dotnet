//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Mvp24Hours.Infrastructure.CronJob.Context
{
    /// <summary>
    /// Default implementation of <see cref="ICronJobContext"/> providing execution metadata.
    /// </summary>
    public sealed class CronJobContext : ICronJobContext
    {
        private readonly Stopwatch _stopwatch;
        private readonly TimeSpan? _timeout;
        private readonly ConcurrentDictionary<string, object?> _properties;

        /// <summary>
        /// Creates a new CronJob execution context.
        /// </summary>
        /// <param name="jobName">Name of the CronJob.</param>
        /// <param name="cronExpression">CRON expression used for scheduling.</param>
        /// <param name="timeZone">Timezone for CRON expression evaluation.</param>
        /// <param name="cancellationToken">Cancellation token for this execution.</param>
        /// <param name="executionCount">Total execution count for this job instance.</param>
        /// <param name="maxAttempts">Maximum number of attempts (1 + max retries).</param>
        /// <param name="timeout">Optional execution timeout.</param>
        /// <param name="scheduledTime">Scheduled time based on CRON expression.</param>
        /// <param name="parentJobId">Parent job ID for dependency execution.</param>
        /// <param name="correlationId">Correlation ID for distributed tracing.</param>
        public CronJobContext(
            string jobName,
            string? cronExpression,
            TimeZoneInfo? timeZone,
            CancellationToken cancellationToken,
            long executionCount,
            int maxAttempts = 1,
            TimeSpan? timeout = null,
            DateTimeOffset? scheduledTime = null,
            Guid? parentJobId = null,
            string? correlationId = null)
        {
            JobId = Guid.NewGuid();
            JobName = jobName ?? throw new ArgumentNullException(nameof(jobName));
            CronExpression = cronExpression;
            TimeZone = timeZone;
            CancellationToken = cancellationToken;
            ExecutionCount = executionCount;
            MaxAttempts = Math.Max(1, maxAttempts);
            ScheduledTime = scheduledTime;
            ParentJobId = parentJobId;
            CorrelationId = correlationId ?? Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
            
            StartTime = DateTimeOffset.UtcNow;
            CurrentAttempt = 1;
            _timeout = timeout;
            _stopwatch = Stopwatch.StartNew();
            _properties = new ConcurrentDictionary<string, object?>();
        }

        /// <inheritdoc />
        public Guid JobId { get; }

        /// <inheritdoc />
        public string JobName { get; }

        /// <inheritdoc />
        public DateTimeOffset StartTime { get; }

        /// <inheritdoc />
        public DateTimeOffset? ScheduledTime { get; }

        /// <inheritdoc />
        public int CurrentAttempt { get; private set; }

        /// <inheritdoc />
        public int MaxAttempts { get; }

        /// <inheritdoc />
        public bool IsRetry => CurrentAttempt > 1;

        /// <inheritdoc />
        public long ExecutionCount { get; }

        /// <inheritdoc />
        public CancellationToken CancellationToken { get; }

        /// <inheritdoc />
        public string? CronExpression { get; }

        /// <inheritdoc />
        public TimeZoneInfo? TimeZone { get; }

        /// <inheritdoc />
        public string? CorrelationId { get; }

        /// <inheritdoc />
        public Guid? ParentJobId { get; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object?> Properties => _properties;

        /// <inheritdoc />
        public TimeSpan Elapsed => _stopwatch.Elapsed;

        /// <inheritdoc />
        public bool IsTimedOut => _timeout.HasValue && Elapsed > _timeout.Value;

        /// <inheritdoc />
        public T? GetProperty<T>(string key, T? defaultValue = default)
        {
            if (_properties.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// Sets a custom property value.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="value">The property value.</param>
        /// <returns>This context for chaining.</returns>
        public CronJobContext SetProperty(string key, object? value)
        {
            _properties[key] = value;
            return this;
        }

        /// <summary>
        /// Increments the attempt counter for retry scenarios.
        /// </summary>
        internal void IncrementAttempt()
        {
            if (CurrentAttempt < MaxAttempts)
            {
                CurrentAttempt++;
            }
        }

        /// <summary>
        /// Creates a copy of this context for a retry attempt.
        /// </summary>
        /// <returns>A new context with incremented attempt.</returns>
        internal CronJobContext CreateRetryContext()
        {
            var retryContext = new CronJobContext(
                JobName,
                CronExpression,
                TimeZone,
                CancellationToken,
                ExecutionCount,
                MaxAttempts,
                _timeout,
                ScheduledTime,
                ParentJobId,
                CorrelationId)
            {
                CurrentAttempt = CurrentAttempt + 1
            };

            // Copy properties
            foreach (var property in _properties)
            {
                retryContext._properties[property.Key] = property.Value;
            }

            return retryContext;
        }
    }
}

