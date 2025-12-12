//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.Contract.Infrastructure
{
    /// <summary>
    /// Abstraction for system time, enabling testability and time manipulation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Using IClock instead of DateTime.Now/UtcNow directly allows you to:
    /// - Write deterministic tests that don't depend on actual time
    /// - Test time-sensitive code (expiration, scheduling, etc.)
    /// - Simulate different timezones and time scenarios
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In production code
    /// public class OrderService
    /// {
    ///     private readonly IClock _clock;
    ///     
    ///     public OrderService(IClock clock)
    ///     {
    ///         _clock = clock;
    ///     }
    ///     
    ///     public bool IsOrderExpired(Order order)
    ///     {
    ///         return _clock.UtcNow > order.ExpirationDate;
    ///     }
    /// }
    /// 
    /// // In tests
    /// var testClock = new TestClock(new DateTime(2024, 1, 1));
    /// var service = new OrderService(testClock);
    /// testClock.AdvanceBy(TimeSpan.FromHours(24));
    /// Assert.True(service.IsOrderExpired(order));
    /// </code>
    /// </example>
    public interface IClock
    {
        /// <summary>
        /// Gets the current UTC date and time.
        /// </summary>
        DateTime UtcNow { get; }

        /// <summary>
        /// Gets the current local date and time.
        /// </summary>
        DateTime Now { get; }

        /// <summary>
        /// Gets the current UTC date (time part is 00:00:00).
        /// </summary>
        DateTime UtcToday { get; }

        /// <summary>
        /// Gets the current local date (time part is 00:00:00).
        /// </summary>
        DateTime Today { get; }

        /// <summary>
        /// Gets the current UTC date and time with timezone information.
        /// </summary>
        DateTimeOffset UtcNowOffset { get; }

        /// <summary>
        /// Gets the current local date and time with timezone information.
        /// </summary>
        DateTimeOffset NowOffset { get; }
    }
}

