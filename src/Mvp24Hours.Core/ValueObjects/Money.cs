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
    /// Value Object representing a monetary amount with currency.
    /// Supports arithmetic operations and currency conversions.
    /// </summary>
    /// <example>
    /// <code>
    /// var price = Money.Create(99.99m, "USD");
    /// var tax = Money.Create(10m, "USD");
    /// var total = price + tax; // 109.99 USD
    /// 
    /// Console.WriteLine(total.ToString()); // "USD 109.99"
    /// Console.WriteLine(total.ToFormattedString()); // "$109.99" (culture-dependent)
    /// 
    /// // Brazilian Real
    /// var brl = Money.BRL(150.50m);
    /// Console.WriteLine(brl.ToFormattedString(new CultureInfo("pt-BR"))); // "R$ 150,50"
    /// </code>
    /// </example>
    public sealed class Money : BaseVO, IEquatable<Money>, IComparable<Money>
    {
        /// <summary>
        /// Gets the monetary amount.
        /// </summary>
        public decimal Amount { get; }

        /// <summary>
        /// Gets the currency code (ISO 4217).
        /// </summary>
        public string Currency { get; }

        private Money(decimal amount, string currency)
        {
            Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
            Currency = currency.ToUpperInvariant();
        }

        #region Factory Methods

        /// <summary>
        /// Creates a new Money instance.
        /// </summary>
        /// <param name="amount">The monetary amount.</param>
        /// <param name="currency">The currency code (ISO 4217).</param>
        /// <returns>A new Money instance.</returns>
        /// <exception cref="ArgumentException">Thrown when currency is invalid.</exception>
        public static Money Create(decimal amount, string currency)
        {
            Guard.Against.NullOrWhiteSpace(currency, nameof(currency));
            Guard.Against.LengthOutOfRange(currency, 3, 3, nameof(currency), "Currency must be a 3-letter ISO 4217 code.");

            return new Money(amount, currency);
        }

        /// <summary>
        /// Creates a zero amount in the specified currency.
        /// </summary>
        /// <param name="currency">The currency code.</param>
        /// <returns>A Money instance with zero amount.</returns>
        public static Money Zero(string currency) => Create(0, currency);

        /// <summary>
        /// Creates a Money instance in Brazilian Real (BRL).
        /// </summary>
        /// <param name="amount">The amount in BRL.</param>
        /// <returns>A new Money instance in BRL.</returns>
        public static Money BRL(decimal amount) => Create(amount, "BRL");

        /// <summary>
        /// Creates a Money instance in US Dollar (USD).
        /// </summary>
        /// <param name="amount">The amount in USD.</param>
        /// <returns>A new Money instance in USD.</returns>
        public static Money USD(decimal amount) => Create(amount, "USD");

        /// <summary>
        /// Creates a Money instance in Euro (EUR).
        /// </summary>
        /// <param name="amount">The amount in EUR.</param>
        /// <returns>A new Money instance in EUR.</returns>
        public static Money EUR(decimal amount) => Create(amount, "EUR");

        #endregion

        #region Arithmetic Operations

        /// <summary>
        /// Adds two Money instances.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when currencies don't match.</exception>
        public static Money operator +(Money left, Money right)
        {
            EnsureSameCurrency(left, right);
            return Create(left.Amount + right.Amount, left.Currency);
        }

        /// <summary>
        /// Subtracts one Money instance from another.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when currencies don't match.</exception>
        public static Money operator -(Money left, Money right)
        {
            EnsureSameCurrency(left, right);
            return Create(left.Amount - right.Amount, left.Currency);
        }

        /// <summary>
        /// Multiplies a Money instance by a scalar.
        /// </summary>
        public static Money operator *(Money money, decimal multiplier)
        {
            return Create(money.Amount * multiplier, money.Currency);
        }

        /// <summary>
        /// Multiplies a scalar by a Money instance.
        /// </summary>
        public static Money operator *(decimal multiplier, Money money)
        {
            return money * multiplier;
        }

        /// <summary>
        /// Divides a Money instance by a scalar.
        /// </summary>
        /// <exception cref="DivideByZeroException">Thrown when divisor is zero.</exception>
        public static Money operator /(Money money, decimal divisor)
        {
            if (divisor == 0)
            {
                throw new DivideByZeroException("Cannot divide money by zero.");
            }
            return Create(money.Amount / divisor, money.Currency);
        }

        /// <summary>
        /// Negates a Money instance.
        /// </summary>
        public static Money operator -(Money money)
        {
            return Create(-money.Amount, money.Currency);
        }

        #endregion

        #region Comparison Operators

        /// <summary>
        /// Checks if two Money instances are equal.
        /// </summary>
        public static bool operator ==(Money left, Money right)
        {
            if (left is null && right is null) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        /// <summary>
        /// Checks if two Money instances are not equal.
        /// </summary>
        public static bool operator !=(Money left, Money right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Checks if left Money is greater than right Money.
        /// </summary>
        public static bool operator >(Money left, Money right)
        {
            EnsureSameCurrency(left, right);
            return left.Amount > right.Amount;
        }

        /// <summary>
        /// Checks if left Money is less than right Money.
        /// </summary>
        public static bool operator <(Money left, Money right)
        {
            EnsureSameCurrency(left, right);
            return left.Amount < right.Amount;
        }

        /// <summary>
        /// Checks if left Money is greater than or equal to right Money.
        /// </summary>
        public static bool operator >=(Money left, Money right)
        {
            EnsureSameCurrency(left, right);
            return left.Amount >= right.Amount;
        }

        /// <summary>
        /// Checks if left Money is less than or equal to right Money.
        /// </summary>
        public static bool operator <=(Money left, Money right)
        {
            EnsureSameCurrency(left, right);
            return left.Amount <= right.Amount;
        }

        #endregion

        #region Instance Methods

        /// <summary>
        /// Returns the absolute value of this Money instance.
        /// </summary>
        public Money Abs() => Create(Math.Abs(Amount), Currency);

        /// <summary>
        /// Checks if the amount is zero.
        /// </summary>
        public bool IsZero => Amount == 0;

        /// <summary>
        /// Checks if the amount is positive.
        /// </summary>
        public bool IsPositive => Amount > 0;

        /// <summary>
        /// Checks if the amount is negative.
        /// </summary>
        public bool IsNegative => Amount < 0;

        /// <summary>
        /// Applies a percentage to this Money instance.
        /// </summary>
        /// <param name="percentage">The percentage (0-100).</param>
        /// <returns>The calculated percentage amount.</returns>
        public Money ApplyPercentage(decimal percentage)
        {
            return Create(Amount * (percentage / 100m), Currency);
        }

        /// <summary>
        /// Adds a percentage to this Money instance.
        /// </summary>
        /// <param name="percentage">The percentage to add (0-100).</param>
        /// <returns>The original amount plus the percentage.</returns>
        public Money AddPercentage(decimal percentage)
        {
            return this + ApplyPercentage(percentage);
        }

        /// <summary>
        /// Formats the money using the specified culture.
        /// </summary>
        /// <param name="culture">The culture for formatting.</param>
        /// <returns>A formatted string representation.</returns>
        public string ToFormattedString(CultureInfo? culture = null)
        {
            culture ??= CultureInfo.CurrentCulture;
            return Amount.ToString("C", culture);
        }

        #endregion

        #region Equality and Comparison

        private static void EnsureSameCurrency(Money left, Money right)
        {
            if (left is null || right is null)
            {
                throw new ArgumentNullException(left is null ? nameof(left) : nameof(right));
            }

            if (left.Currency != right.Currency)
            {
                throw new InvalidOperationException($"Cannot perform operation between different currencies: {left.Currency} and {right.Currency}.");
            }
        }

        /// <inheritdoc />
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }

        /// <inheritdoc />
        public bool Equals(Money? other)
        {
            if (other is null) return false;
            return Amount == other.Amount && Currency == other.Currency;
        }

        /// <inheritdoc />
        public int CompareTo(Money? other)
        {
            if (other is null) return 1;
            EnsureSameCurrency(this, other);
            return Amount.CompareTo(other.Amount);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => Equals(obj as Money);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (Amount.GetHashCode() * 397) ^ Currency.GetHashCode();
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{Currency} {Amount:F2}";

        #endregion
    }
}

