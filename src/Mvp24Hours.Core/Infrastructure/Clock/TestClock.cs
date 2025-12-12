//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure;
using System;

namespace Mvp24Hours.Core.Infrastructure.Clock
{
    /// <summary>
    /// Test implementation of IClock that allows time manipulation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this implementation in unit tests to:
    /// - Set a specific point in time
    /// - Advance or rewind time
    /// - Test time-dependent behavior deterministically
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a clock set to a specific time
    /// var clock = new TestClock(new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc));
    /// 
    /// // Test time-sensitive code
    /// var service = new ExpirationService(clock);
    /// 
    /// // Advance time to test expiration
    /// clock.AdvanceBy(TimeSpan.FromHours(24));
    /// Assert.True(service.IsExpired(item));
    /// 
    /// // Reset to original time
    /// clock.Reset();
    /// Assert.False(service.IsExpired(item));
    /// </code>
    /// </example>
    public sealed class TestClock : IClock
    {
        private readonly DateTime _initialTime;
        private DateTime _currentUtcTime;
        private readonly TimeZoneInfo _timeZone;

        /// <summary>
        /// Creates a test clock set to the current UTC time.
        /// </summary>
        public TestClock() : this(DateTime.UtcNow)
        {
        }

        /// <summary>
        /// Creates a test clock set to a specific UTC time.
        /// </summary>
        /// <param name="initialUtcTime">The initial UTC time.</param>
        public TestClock(DateTime initialUtcTime) : this(initialUtcTime, TimeZoneInfo.Local)
        {
        }

        /// <summary>
        /// Creates a test clock set to a specific UTC time with a specific timezone.
        /// </summary>
        /// <param name="initialUtcTime">The initial UTC time.</param>
        /// <param name="timeZone">The timezone for local time conversions.</param>
        public TestClock(DateTime initialUtcTime, TimeZoneInfo timeZone)
        {
            _initialTime = DateTime.SpecifyKind(initialUtcTime, DateTimeKind.Utc);
            _currentUtcTime = _initialTime;
            _timeZone = timeZone ?? TimeZoneInfo.Local;
        }

        /// <inheritdoc />
        public DateTime UtcNow => _currentUtcTime;

        /// <inheritdoc />
        public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(_currentUtcTime, _timeZone);

        /// <inheritdoc />
        public DateTime UtcToday => _currentUtcTime.Date;

        /// <inheritdoc />
        public DateTime Today => TimeZoneInfo.ConvertTimeFromUtc(_currentUtcTime, _timeZone).Date;

        /// <inheritdoc />
        public DateTimeOffset UtcNowOffset => new DateTimeOffset(_currentUtcTime, TimeSpan.Zero);

        /// <inheritdoc />
        public DateTimeOffset NowOffset => TimeZoneInfo.ConvertTime(UtcNowOffset, _timeZone);

        /// <summary>
        /// Sets the clock to a specific UTC time.
        /// </summary>
        /// <param name="utcTime">The UTC time to set.</param>
        public void SetUtcNow(DateTime utcTime)
        {
            _currentUtcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
        }

        /// <summary>
        /// Advances the clock by the specified duration.
        /// </summary>
        /// <param name="duration">The duration to advance by.</param>
        public void AdvanceBy(TimeSpan duration)
        {
            _currentUtcTime = _currentUtcTime.Add(duration);
        }

        /// <summary>
        /// Rewinds the clock by the specified duration.
        /// </summary>
        /// <param name="duration">The duration to rewind by.</param>
        public void RewindBy(TimeSpan duration)
        {
            _currentUtcTime = _currentUtcTime.Subtract(duration);
        }

        /// <summary>
        /// Advances the clock by the specified number of seconds.
        /// </summary>
        public void AdvanceSeconds(double seconds) => AdvanceBy(TimeSpan.FromSeconds(seconds));

        /// <summary>
        /// Advances the clock by the specified number of minutes.
        /// </summary>
        public void AdvanceMinutes(double minutes) => AdvanceBy(TimeSpan.FromMinutes(minutes));

        /// <summary>
        /// Advances the clock by the specified number of hours.
        /// </summary>
        public void AdvanceHours(double hours) => AdvanceBy(TimeSpan.FromHours(hours));

        /// <summary>
        /// Advances the clock by the specified number of days.
        /// </summary>
        public void AdvanceDays(double days) => AdvanceBy(TimeSpan.FromDays(days));

        /// <summary>
        /// Resets the clock to its initial time.
        /// </summary>
        public void Reset()
        {
            _currentUtcTime = _initialTime;
        }

        /// <summary>
        /// Freezes time at the current moment (returns the same time until unfrozen or changed).
        /// Note: This clock is already effectively frozen since it doesn't advance automatically.
        /// </summary>
        public void Freeze()
        {
            // No-op - this clock is already frozen by design
        }
    }
}

