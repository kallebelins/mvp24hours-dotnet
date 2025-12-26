//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Core.Infrastructure.Clock;
using System;

namespace Mvp24Hours.Infrastructure.Testing
{
    /// <summary>
    /// Mock clock implementation for testing time-dependent code.
    /// This is an alias for <see cref="TestClock"/> with additional convenience methods.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MockClock provides a simple way to control time in tests:
    /// - Freeze time at a specific moment
    /// - Advance or rewind time
    /// - Test time-sensitive logic deterministically
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a mock clock starting at a specific time
    /// var clock = new MockClock(new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc));
    /// 
    /// // Use in service under test
    /// var service = new ExpirationService(clock);
    /// 
    /// // Verify initial state
    /// Assert.False(service.IsExpired(token));
    /// 
    /// // Advance time by 1 hour
    /// clock.AdvanceBy(TimeSpan.FromHours(1));
    /// 
    /// // Verify expired state
    /// Assert.True(service.IsExpired(token));
    /// </code>
    /// </example>
    public sealed class MockClock : IClock
    {
        private readonly TestClock _testClock;

        /// <summary>
        /// Creates a mock clock set to the current UTC time.
        /// </summary>
        public MockClock()
        {
            _testClock = new TestClock();
        }

        /// <summary>
        /// Creates a mock clock set to the specified UTC time.
        /// </summary>
        /// <param name="initialUtcTime">The initial UTC time.</param>
        public MockClock(DateTime initialUtcTime)
        {
            _testClock = new TestClock(initialUtcTime);
        }

        /// <summary>
        /// Creates a mock clock set to the specified UTC time with timezone.
        /// </summary>
        /// <param name="initialUtcTime">The initial UTC time.</param>
        /// <param name="timeZone">The timezone for local time conversions.</param>
        public MockClock(DateTime initialUtcTime, TimeZoneInfo timeZone)
        {
            _testClock = new TestClock(initialUtcTime, timeZone);
        }

        /// <inheritdoc />
        public DateTime UtcNow => _testClock.UtcNow;

        /// <inheritdoc />
        public DateTime Now => _testClock.Now;

        /// <inheritdoc />
        public DateTime UtcToday => _testClock.UtcToday;

        /// <inheritdoc />
        public DateTime Today => _testClock.Today;

        /// <inheritdoc />
        public DateTimeOffset UtcNowOffset => _testClock.UtcNowOffset;

        /// <inheritdoc />
        public DateTimeOffset NowOffset => _testClock.NowOffset;

        /// <summary>
        /// Sets the clock to a specific UTC time.
        /// </summary>
        /// <param name="utcTime">The UTC time to set.</param>
        public void SetUtcNow(DateTime utcTime)
        {
            _testClock.SetUtcNow(utcTime);
        }

        /// <summary>
        /// Advances the clock by the specified duration.
        /// </summary>
        /// <param name="duration">The duration to advance.</param>
        public void AdvanceBy(TimeSpan duration)
        {
            _testClock.AdvanceBy(duration);
        }

        /// <summary>
        /// Rewinds the clock by the specified duration.
        /// </summary>
        /// <param name="duration">The duration to rewind.</param>
        public void RewindBy(TimeSpan duration)
        {
            _testClock.RewindBy(duration);
        }

        /// <summary>
        /// Advances the clock by the specified number of seconds.
        /// </summary>
        /// <param name="seconds">The number of seconds to advance.</param>
        public void AdvanceSeconds(double seconds)
        {
            _testClock.AdvanceSeconds(seconds);
        }

        /// <summary>
        /// Advances the clock by the specified number of minutes.
        /// </summary>
        /// <param name="minutes">The number of minutes to advance.</param>
        public void AdvanceMinutes(double minutes)
        {
            _testClock.AdvanceMinutes(minutes);
        }

        /// <summary>
        /// Advances the clock by the specified number of hours.
        /// </summary>
        /// <param name="hours">The number of hours to advance.</param>
        public void AdvanceHours(double hours)
        {
            _testClock.AdvanceHours(hours);
        }

        /// <summary>
        /// Advances the clock by the specified number of days.
        /// </summary>
        /// <param name="days">The number of days to advance.</param>
        public void AdvanceDays(double days)
        {
            _testClock.AdvanceDays(days);
        }

        /// <summary>
        /// Resets the clock to its initial time.
        /// </summary>
        public void Reset()
        {
            _testClock.Reset();
        }

        /// <summary>
        /// Creates a MockClock set to the beginning of the specified year.
        /// </summary>
        /// <param name="year">The year.</param>
        public static MockClock FromYear(int year)
        {
            return new MockClock(new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        /// <summary>
        /// Creates a MockClock set to the specified date.
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="month">The month.</param>
        /// <param name="day">The day.</param>
        public static MockClock FromDate(int year, int month, int day)
        {
            return new MockClock(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc));
        }

        /// <summary>
        /// Creates a MockClock set to the specified date and time.
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="month">The month.</param>
        /// <param name="day">The day.</param>
        /// <param name="hour">The hour.</param>
        /// <param name="minute">The minute.</param>
        /// <param name="second">The second.</param>
        public static MockClock FromDateTime(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
        {
            return new MockClock(new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc));
        }

        /// <summary>
        /// Creates a MockClock set to the current system time (frozen at creation).
        /// </summary>
        public static MockClock FromNow()
        {
            return new MockClock(DateTime.UtcNow);
        }
    }
}

