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
    /// Production implementation of IClock using the real system time.
    /// </summary>
    /// <remarks>
    /// Use this implementation in production. For tests, use <see cref="TestClock"/>.
    /// </remarks>
    public sealed class SystemClock : IClock
    {
        /// <summary>
        /// Singleton instance of the system clock.
        /// </summary>
        public static readonly SystemClock Instance = new();

        /// <inheritdoc />
        public DateTime UtcNow => DateTime.UtcNow;

        /// <inheritdoc />
        public DateTime Now => DateTime.Now;

        /// <inheritdoc />
        public DateTime UtcToday => DateTime.UtcNow.Date;

        /// <inheritdoc />
        public DateTime Today => DateTime.Today;

        /// <inheritdoc />
        public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;

        /// <inheritdoc />
        public DateTimeOffset NowOffset => DateTimeOffset.Now;
    }
}

