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
    /// Helper class for working with .NET 8+ TimeProvider in tests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides convenient factory methods for creating time providers
    /// and adapters for testing scenarios. For full FakeTimeProvider functionality,
    /// install the Microsoft.Extensions.TimeProvider.Testing NuGet package.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Using with Microsoft.Extensions.TimeProvider.Testing package:
    /// // Install-Package Microsoft.Extensions.TimeProvider.Testing
    /// 
    /// // In test setup:
    /// var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
    /// services.ReplaceTimeProvider(fakeTime);
    /// 
    /// // In test:
    /// fakeTime.Advance(TimeSpan.FromHours(1));
    /// 
    /// // Using MockClock adapter:
    /// var mockClock = MockClock.FromDate(2024, 1, 1);
    /// var timeProvider = FakeTimeProviderHelper.FromClock(mockClock);
    /// </code>
    /// </example>
    public static class FakeTimeProviderHelper
    {
        /// <summary>
        /// Creates a TimeProvider from an IClock instance.
        /// Useful for using existing TestClock/MockClock with TimeProvider-based code.
        /// </summary>
        /// <param name="clock">The IClock to wrap.</param>
        /// <returns>A TimeProvider that wraps the clock.</returns>
        public static TimeProvider FromClock(IClock clock)
        {
            return new ClockAdapter(clock);
        }

        /// <summary>
        /// Creates a TimeProviderAdapter that wraps the given TimeProvider.
        /// Useful for using FakeTimeProvider with IClock-based code.
        /// </summary>
        /// <param name="timeProvider">The TimeProvider to wrap.</param>
        /// <returns>An IClock that wraps the TimeProvider.</returns>
        public static IClock ToClock(TimeProvider timeProvider)
        {
            return new TimeProviderAdapter(timeProvider);
        }

        /// <summary>
        /// Creates a TimeProviderAdapter for the given TimeProvider with custom timezone.
        /// </summary>
        /// <param name="timeProvider">The TimeProvider to wrap.</param>
        /// <param name="localTimeZone">The timezone for local time conversions.</param>
        /// <returns>An IClock that wraps the TimeProvider.</returns>
        public static IClock ToClock(TimeProvider timeProvider, TimeZoneInfo localTimeZone)
        {
            return new TimeProviderAdapter(timeProvider, localTimeZone);
        }

        /// <summary>
        /// Creates a fixed-time TimeProvider for testing.
        /// The returned provider always returns the same time.
        /// </summary>
        /// <param name="fixedTime">The fixed time to return.</param>
        /// <returns>A TimeProvider that always returns the specified time.</returns>
        /// <remarks>
        /// Note: For more advanced scenarios (advancing time, etc.), use 
        /// FakeTimeProvider from Microsoft.Extensions.TimeProvider.Testing.
        /// </remarks>
        public static TimeProvider FixedAt(DateTimeOffset fixedTime)
        {
            return new FixedTimeProvider(fixedTime);
        }

        /// <summary>
        /// Creates a fixed-time TimeProvider for testing.
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="month">The month.</param>
        /// <param name="day">The day.</param>
        /// <param name="hour">The hour (default 0).</param>
        /// <param name="minute">The minute (default 0).</param>
        /// <param name="second">The second (default 0).</param>
        /// <returns>A TimeProvider that always returns the specified time.</returns>
        public static TimeProvider FixedAt(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
        {
            return FixedAt(new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero));
        }

        /// <summary>
        /// A simple TimeProvider that always returns a fixed time.
        /// For more advanced scenarios, use FakeTimeProvider from Microsoft.Extensions.TimeProvider.Testing.
        /// </summary>
        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _fixedTime;

            public FixedTimeProvider(DateTimeOffset fixedTime)
            {
                _fixedTime = fixedTime;
            }

            public override DateTimeOffset GetUtcNow() => _fixedTime;
        }
    }
}

