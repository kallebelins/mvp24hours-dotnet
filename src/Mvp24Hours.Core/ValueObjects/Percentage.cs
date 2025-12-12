//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Mvp24Hours.Core.ValueObjects
{
    /// <summary>
    /// Value Object representing a percentage value.
    /// Supports both 0-100 and 0-1 representations.
    /// </summary>
    /// <example>
    /// <code>
    /// var discount = Percentage.FromPercent(15); // 15%
    /// var tax = Percentage.FromDecimal(0.10m);   // 10%
    /// 
    /// Console.WriteLine(discount.Value);       // 15
    /// Console.WriteLine(discount.AsDecimal);   // 0.15
    /// 
    /// var price = 100m;
    /// var discountedPrice = discount.ApplyTo(price); // 85
    /// var addedTax = tax.AddTo(price);               // 110
    /// </code>
    /// </example>
    public sealed class Percentage : BaseVO, IEquatable<Percentage>, IComparable<Percentage>
    {
        /// <summary>
        /// Gets the percentage value (0-100 scale).
        /// </summary>
        public decimal Value { get; }

        /// <summary>
        /// Gets the percentage as a decimal (0-1 scale).
        /// </summary>
        public decimal AsDecimal => Value / 100m;

        /// <summary>
        /// Gets the percentage as a fraction (0-1 scale).
        /// </summary>
        public decimal AsFraction => AsDecimal;

        /// <summary>
        /// Checks if this percentage represents zero.
        /// </summary>
        public bool IsZero => Value == 0;

        /// <summary>
        /// Checks if this percentage represents 100%.
        /// </summary>
        public bool IsFull => Value == 100;

        private Percentage(decimal value)
        {
            Value = Math.Round(value, 4, MidpointRounding.AwayFromZero);
        }

        #region Factory Methods

        /// <summary>
        /// Creates a Percentage from a 0-100 scale value.
        /// </summary>
        /// <param name="percent">The percentage value (0-100).</param>
        /// <returns>A new Percentage instance.</returns>
        public static Percentage FromPercent(decimal percent)
        {
            return new Percentage(percent);
        }

        /// <summary>
        /// Creates a Percentage from a 0-1 scale value.
        /// </summary>
        /// <param name="decimalValue">The decimal value (0-1).</param>
        /// <returns>A new Percentage instance.</returns>
        public static Percentage FromDecimal(decimal decimalValue)
        {
            return new Percentage(decimalValue * 100m);
        }

        /// <summary>
        /// Creates a Percentage from a ratio of two values.
        /// </summary>
        /// <param name="part">The part value.</param>
        /// <param name="whole">The whole value.</param>
        /// <returns>A new Percentage instance.</returns>
        /// <exception cref="DivideByZeroException">Thrown when whole is zero.</exception>
        public static Percentage FromRatio(decimal part, decimal whole)
        {
            if (whole == 0)
            {
                throw new DivideByZeroException("Cannot calculate percentage with zero as the whole.");
            }
            return new Percentage((part / whole) * 100m);
        }

        /// <summary>
        /// Creates a zero percentage.
        /// </summary>
        public static Percentage Zero => new(0);

        /// <summary>
        /// Creates a 100% percentage.
        /// </summary>
        public static Percentage Full => new(100);

        /// <summary>
        /// Tries to parse a string into a Percentage.
        /// </summary>
        /// <param name="value">The string value (e.g., "15", "15%", "0.15").</param>
        /// <param name="result">The resulting Percentage if successful.</param>
        /// <returns>True if parsing was successful; otherwise, false.</returns>
        public static bool TryParse(string value, out Percentage result)
        {
            result = null!;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            value = value.Trim().TrimEnd('%');

            if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var numericValue))
            {
                return false;
            }

            // If value is between 0 and 1, treat as decimal
            if (numericValue > 0 && numericValue < 1)
            {
                result = FromDecimal(numericValue);
            }
            else
            {
                result = FromPercent(numericValue);
            }

            return true;
        }

        #endregion

        #region Operations

        /// <summary>
        /// Calculates this percentage of a value.
        /// </summary>
        /// <param name="value">The value to calculate percentage of.</param>
        /// <returns>The percentage amount.</returns>
        public decimal Of(decimal value)
        {
            return value * AsDecimal;
        }

        /// <summary>
        /// Applies this percentage as a discount to a value.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <returns>The value after applying the percentage as a discount.</returns>
        public decimal ApplyTo(decimal value)
        {
            return value * (1m - AsDecimal);
        }

        /// <summary>
        /// Adds this percentage to a value.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <returns>The value plus this percentage.</returns>
        public decimal AddTo(decimal value)
        {
            return value * (1m + AsDecimal);
        }

        /// <summary>
        /// Returns the complement (100% - this percentage).
        /// </summary>
        public Percentage Complement => new(100m - Value);

        #endregion

        #region Arithmetic Operators

        public static Percentage operator +(Percentage left, Percentage right)
        {
            return new Percentage(left.Value + right.Value);
        }

        public static Percentage operator -(Percentage left, Percentage right)
        {
            return new Percentage(left.Value - right.Value);
        }

        public static Percentage operator *(Percentage left, decimal multiplier)
        {
            return new Percentage(left.Value * multiplier);
        }

        public static Percentage operator /(Percentage left, decimal divisor)
        {
            Guard.Against.Condition(divisor == 0, nameof(divisor), "Cannot divide by zero.");
            return new Percentage(left.Value / divisor);
        }

        #endregion

        #region Comparison Operators

        public static bool operator >(Percentage left, Percentage right)
        {
            return left.Value > right.Value;
        }

        public static bool operator <(Percentage left, Percentage right)
        {
            return left.Value < right.Value;
        }

        public static bool operator >=(Percentage left, Percentage right)
        {
            return left.Value >= right.Value;
        }

        public static bool operator <=(Percentage left, Percentage right)
        {
            return left.Value <= right.Value;
        }

        #endregion

        #region Equality and Comparison

        /// <inheritdoc />
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        /// <inheritdoc />
        public bool Equals(Percentage? other)
        {
            if (other is null) return false;
            return Value == other.Value;
        }

        /// <inheritdoc />
        public int CompareTo(Percentage? other)
        {
            if (other is null) return 1;
            return Value.CompareTo(other.Value);
        }

        /// <inheritdoc />
        public override string ToString() => $"{Value}%";

        /// <summary>
        /// Formats the percentage with specified decimal places.
        /// </summary>
        /// <param name="decimalPlaces">Number of decimal places.</param>
        /// <returns>Formatted percentage string.</returns>
        public string ToString(int decimalPlaces)
        {
            return $"{Math.Round(Value, decimalPlaces)}%";
        }

        #endregion

        #region Conversions

        /// <summary>
        /// Implicit conversion from Percentage to decimal (returns 0-100 value).
        /// </summary>
        public static implicit operator decimal(Percentage percentage) => percentage?.Value ?? 0;

        /// <summary>
        /// Explicit conversion from decimal to Percentage.
        /// </summary>
        public static explicit operator Percentage(decimal value) => FromPercent(value);

        #endregion
    }
}

