//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Mvp24Hours.Core.Contract.Infrastructure;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Infrastructure.Clock
{
    /// <summary>
    /// Adapter that wraps an <see cref="IClock"/> to implement <see cref="TimeProvider"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This adapter enables using existing IClock implementations with code that expects TimeProvider.
    /// Useful for gradual migration from IClock to TimeProvider.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Wrap existing IClock as TimeProvider
    /// IClock clock = new TestClock(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    /// TimeProvider timeProvider = new ClockAdapter(clock);
    /// 
    /// // Use with code expecting TimeProvider
    /// var time = timeProvider.GetUtcNow();
    /// </code>
    /// </example>
    public sealed class ClockAdapter : TimeProvider
    {
        private readonly IClock _clock;
        private readonly TimeZoneInfo _localTimeZone;

        /// <summary>
        /// Creates a new adapter wrapping the specified IClock.
        /// </summary>
        /// <param name="clock">The IClock to wrap.</param>
        public ClockAdapter(IClock clock)
            : this(clock, TimeZoneInfo.Local)
        {
        }

        /// <summary>
        /// Creates a new adapter wrapping the specified IClock with custom timezone.
        /// </summary>
        /// <param name="clock">The IClock to wrap.</param>
        /// <param name="localTimeZone">The timezone for local time conversions.</param>
        public ClockAdapter(IClock clock, TimeZoneInfo localTimeZone)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _localTimeZone = localTimeZone ?? throw new ArgumentNullException(nameof(localTimeZone));
        }

        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => _clock.UtcNowOffset;

        /// <inheritdoc />
        public override TimeZoneInfo LocalTimeZone => _localTimeZone;

        /// <summary>
        /// Gets the underlying IClock.
        /// </summary>
        public IClock Clock => _clock;

        /// <inheritdoc />
        public override long GetTimestamp() => Stopwatch.GetTimestamp();

        /// <inheritdoc />
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            return new SystemTimer(callback, state, dueTime, period);
        }

        /// <summary>
        /// Internal timer implementation that wraps System.Threading.Timer.
        /// </summary>
        private sealed class SystemTimer : ITimer
        {
            private readonly Timer _timer;
            private bool _disposed;

            public SystemTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
            {
                _timer = new Timer(callback, state, dueTime, period);
            }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                if (_disposed) return false;
                return _timer.Change(dueTime, period);
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _timer.Dispose();
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}

