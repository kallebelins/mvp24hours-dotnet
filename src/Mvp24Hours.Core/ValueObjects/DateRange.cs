//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Helpers;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Core.ValueObjects
{
    /// <summary>
    /// Value Object representing a date/time range with start and end dates.
    /// </summary>
    /// <example>
    /// <code>
    /// var range = DateRange.Create(
    ///     DateTime.Today,
    ///     DateTime.Today.AddDays(30)
    /// );
    /// 
    /// Console.WriteLine(range.Duration.TotalDays); // 30
    /// Console.WriteLine(range.Contains(DateTime.Today.AddDays(15))); // true
    /// 
    /// var otherRange = DateRange.Create(
    ///     DateTime.Today.AddDays(25),
    ///     DateTime.Today.AddDays(35)
    /// );
    /// Console.WriteLine(range.Overlaps(otherRange)); // true
    /// </code>
    /// </example>
    public sealed class DateRange : BaseVO, IEquatable<DateRange>, IComparable<DateRange>
    {
        /// <summary>
        /// Gets the start date/time of the range.
        /// </summary>
        public DateTime Start { get; }

        /// <summary>
        /// Gets the end date/time of the range.
        /// </summary>
        public DateTime End { get; }

        /// <summary>
        /// Gets the duration of the range.
        /// </summary>
        public TimeSpan Duration => End - Start;

        /// <summary>
        /// Gets the total number of days in the range.
        /// </summary>
        public double TotalDays => Duration.TotalDays;

        /// <summary>
        /// Gets the total number of hours in the range.
        /// </summary>
        public double TotalHours => Duration.TotalHours;

        private DateRange(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// Creates a new DateRange instance.
        /// </summary>
        /// <param name="start">The start date/time.</param>
        /// <param name="end">The end date/time.</param>
        /// <returns>A new DateRange instance.</returns>
        /// <exception cref="ArgumentException">Thrown when end is before start.</exception>
        public static DateRange Create(DateTime start, DateTime end)
        {
            if (end < start)
            {
                throw new ArgumentException("End date must be greater than or equal to start date.", nameof(end));
            }

            return new DateRange(start, end);
        }

        /// <summary>
        /// Creates a DateRange from a start date with a specific duration.
        /// </summary>
        /// <param name="start">The start date/time.</param>
        /// <param name="duration">The duration of the range.</param>
        /// <returns>A new DateRange instance.</returns>
        public static DateRange FromDuration(DateTime start, TimeSpan duration)
        {
            Guard.Against.Condition(duration < TimeSpan.Zero, nameof(duration), "Duration cannot be negative.");
            return new DateRange(start, start.Add(duration));
        }

        /// <summary>
        /// Creates a DateRange for a single day.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>A DateRange spanning the entire day.</returns>
        public static DateRange ForDay(DateTime date)
        {
            var start = date.Date;
            var end = start.AddDays(1).AddTicks(-1);
            return new DateRange(start, end);
        }

        /// <summary>
        /// Creates a DateRange for the current month.
        /// </summary>
        /// <returns>A DateRange spanning the current month.</returns>
        public static DateRange CurrentMonth()
        {
            var now = DateTime.Now;
            var start = new DateTime(now.Year, now.Month, 1);
            var end = start.AddMonths(1).AddTicks(-1);
            return new DateRange(start, end);
        }

        /// <summary>
        /// Creates a DateRange for the current year.
        /// </summary>
        /// <returns>A DateRange spanning the current year.</returns>
        public static DateRange CurrentYear()
        {
            var now = DateTime.Now;
            var start = new DateTime(now.Year, 1, 1);
            var end = start.AddYears(1).AddTicks(-1);
            return new DateRange(start, end);
        }

        /// <summary>
        /// Checks if a date/time is within this range.
        /// </summary>
        /// <param name="dateTime">The date/time to check.</param>
        /// <returns>True if the date/time is within the range; otherwise, false.</returns>
        public bool Contains(DateTime dateTime)
        {
            return dateTime >= Start && dateTime <= End;
        }

        /// <summary>
        /// Checks if another DateRange is completely within this range.
        /// </summary>
        /// <param name="other">The other DateRange to check.</param>
        /// <returns>True if the other range is within this range; otherwise, false.</returns>
        public bool Contains(DateRange other)
        {
            Guard.Against.Null(other, nameof(other));
            return other.Start >= Start && other.End <= End;
        }

        /// <summary>
        /// Checks if this range overlaps with another range.
        /// </summary>
        /// <param name="other">The other DateRange to check.</param>
        /// <returns>True if the ranges overlap; otherwise, false.</returns>
        public bool Overlaps(DateRange other)
        {
            Guard.Against.Null(other, nameof(other));
            return Start <= other.End && End >= other.Start;
        }

        /// <summary>
        /// Gets the intersection of this range with another range.
        /// </summary>
        /// <param name="other">The other DateRange.</param>
        /// <returns>The intersection, or null if no overlap.</returns>
        public DateRange GetIntersection(DateRange other)
        {
            Guard.Against.Null(other, nameof(other));

            if (!Overlaps(other))
            {
                return null;
            }

            var intersectStart = Start > other.Start ? Start : other.Start;
            var intersectEnd = End < other.End ? End : other.End;

            return new DateRange(intersectStart, intersectEnd);
        }

        /// <summary>
        /// Gets the union of this range with another range.
        /// </summary>
        /// <param name="other">The other DateRange.</param>
        /// <returns>A new DateRange spanning both ranges.</returns>
        public DateRange GetUnion(DateRange other)
        {
            Guard.Against.Null(other, nameof(other));

            var unionStart = Start < other.Start ? Start : other.Start;
            var unionEnd = End > other.End ? End : other.End;

            return new DateRange(unionStart, unionEnd);
        }

        /// <summary>
        /// Extends the range by the specified duration.
        /// </summary>
        /// <param name="duration">The duration to extend by.</param>
        /// <returns>A new extended DateRange.</returns>
        public DateRange Extend(TimeSpan duration)
        {
            return new DateRange(Start, End.Add(duration));
        }

        /// <summary>
        /// Shifts the entire range by the specified duration.
        /// </summary>
        /// <param name="duration">The duration to shift by.</param>
        /// <returns>A new shifted DateRange.</returns>
        public DateRange Shift(TimeSpan duration)
        {
            return new DateRange(Start.Add(duration), End.Add(duration));
        }

        /// <inheritdoc />
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Start;
            yield return End;
        }

        /// <inheritdoc />
        public bool Equals(DateRange other)
        {
            if (other is null) return false;
            return Start == other.Start && End == other.End;
        }

        /// <inheritdoc />
        public int CompareTo(DateRange other)
        {
            if (other is null) return 1;

            var startComparison = Start.CompareTo(other.Start);
            if (startComparison != 0) return startComparison;

            return End.CompareTo(other.End);
        }

        /// <inheritdoc />
        public override string ToString() => $"{Start:yyyy-MM-dd HH:mm} - {End:yyyy-MM-dd HH:mm}";
    }
}

