//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mvp24Hours.Core.ValueObjects
{
    /// <summary>
    /// Value Object representing a valid Brazilian CPF (Cadastro de Pessoas Físicas).
    /// </summary>
    /// <example>
    /// <code>
    /// var cpf = Cpf.Create("123.456.789-09");
    /// Console.WriteLine(cpf.Value); // 12345678909 (unformatted)
    /// Console.WriteLine(cpf.Formatted); // 123.456.789-09
    /// 
    /// // Using TryParse
    /// if (Cpf.TryParse("12345678909", out var result))
    /// {
    ///     Console.WriteLine(result.Formatted);
    /// }
    /// </code>
    /// </example>
    public sealed class Cpf : BaseVO, IEquatable<Cpf>, IComparable<Cpf>
    {
        private static readonly Regex OnlyDigitsRegex = new(@"[^\d]", RegexOptions.Compiled);

        /// <summary>
        /// Gets the CPF value without formatting (11 digits only).
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets the CPF with standard formatting (XXX.XXX.XXX-XX).
        /// </summary>
        public string Formatted => FormatCpf(Value);

        private Cpf(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new CPF instance.
        /// </summary>
        /// <param name="cpf">The CPF string (with or without formatting).</param>
        /// <returns>A valid CPF instance.</returns>
        /// <exception cref="ArgumentException">Thrown when CPF is invalid.</exception>
        public static Cpf Create(string cpf)
        {
            Guard.Against.NullOrWhiteSpace(cpf, nameof(cpf));

            var digitsOnly = OnlyDigitsRegex.Replace(cpf, "");

            if (!IsValid(digitsOnly))
            {
                throw new ArgumentException($"'{cpf}' is not a valid CPF.", nameof(cpf));
            }

            return new Cpf(digitsOnly);
        }

        /// <summary>
        /// Tries to parse a string into a CPF instance.
        /// </summary>
        /// <param name="cpf">The CPF string.</param>
        /// <param name="result">The resulting CPF if successful.</param>
        /// <returns>True if parsing was successful; otherwise, false.</returns>
        public static bool TryParse(string cpf, out Cpf result)
        {
            result = null!;

            if (string.IsNullOrWhiteSpace(cpf))
            {
                return false;
            }

            var digitsOnly = OnlyDigitsRegex.Replace(cpf, "");

            if (!IsValid(digitsOnly))
            {
                return false;
            }

            result = new Cpf(digitsOnly);
            return true;
        }

        /// <summary>
        /// Checks if a string is a valid CPF.
        /// </summary>
        /// <param name="cpf">The CPF to validate (digits only).</param>
        /// <returns>True if valid; otherwise, false.</returns>
        public static bool IsValid(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
            {
                return false;
            }

            var digitsOnly = OnlyDigitsRegex.Replace(cpf, "");

            if (digitsOnly.Length != 11)
            {
                return false;
            }

            // Check for known invalid CPFs (all same digits)
            if (digitsOnly.All(c => c == digitsOnly[0]))
            {
                return false;
            }

            return ValidateCheckDigits(digitsOnly);
        }

        private static bool ValidateCheckDigits(string cpf)
        {
            // First check digit
            var sum = 0;
            for (var i = 0; i < 9; i++)
            {
                sum += (cpf[i] - '0') * (10 - i);
            }
            var remainder = sum % 11;
            var firstCheckDigit = remainder < 2 ? 0 : 11 - remainder;

            if (cpf[9] - '0' != firstCheckDigit)
            {
                return false;
            }

            // Second check digit
            sum = 0;
            for (var i = 0; i < 10; i++)
            {
                sum += (cpf[i] - '0') * (11 - i);
            }
            remainder = sum % 11;
            var secondCheckDigit = remainder < 2 ? 0 : 11 - remainder;

            return cpf[10] - '0' == secondCheckDigit;
        }

        private static string FormatCpf(string cpf)
        {
            if (cpf.Length != 11) return cpf;
            return $"{cpf.Substring(0, 3)}.{cpf.Substring(3, 3)}.{cpf.Substring(6, 3)}-{cpf.Substring(9, 2)}";
        }

        /// <inheritdoc />
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        /// <inheritdoc />
        public bool Equals(Cpf other)
        {
            if (other is null) return false;
            return Value == other.Value;
        }

        /// <inheritdoc />
        public int CompareTo(Cpf other)
        {
            if (other is null) return 1;
            return string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override string ToString() => Formatted;

        /// <summary>
        /// Implicit conversion from CPF to string (returns formatted).
        /// </summary>
        public static implicit operator string(Cpf cpf) => cpf?.Formatted ?? string.Empty;

        /// <summary>
        /// Explicit conversion from string to CPF.
        /// </summary>
        public static explicit operator Cpf(string cpf) => Create(cpf);
    }
}

