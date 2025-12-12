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
    /// Value Object representing a valid Brazilian CNPJ (Cadastro Nacional da Pessoa Jurídica).
    /// </summary>
    /// <example>
    /// <code>
    /// var cnpj = Cnpj.Create("11.222.333/0001-81");
    /// Console.WriteLine(cnpj.Value); // 11222333000181 (unformatted)
    /// Console.WriteLine(cnpj.Formatted); // 11.222.333/0001-81
    /// 
    /// // Using TryParse
    /// if (Cnpj.TryParse("11222333000181", out var result))
    /// {
    ///     Console.WriteLine(result.Formatted);
    /// }
    /// </code>
    /// </example>
    public sealed class Cnpj : BaseVO, IEquatable<Cnpj>, IComparable<Cnpj>
    {
        private static readonly Regex OnlyDigitsRegex = new(@"[^\d]", RegexOptions.Compiled);

        /// <summary>
        /// Gets the CNPJ value without formatting (14 digits only).
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets the CNPJ with standard formatting (XX.XXX.XXX/XXXX-XX).
        /// </summary>
        public string Formatted => FormatCnpj(Value);

        private Cnpj(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new CNPJ instance.
        /// </summary>
        /// <param name="cnpj">The CNPJ string (with or without formatting).</param>
        /// <returns>A valid CNPJ instance.</returns>
        /// <exception cref="ArgumentException">Thrown when CNPJ is invalid.</exception>
        public static Cnpj Create(string cnpj)
        {
            Guard.Against.NullOrWhiteSpace(cnpj, nameof(cnpj));

            var digitsOnly = OnlyDigitsRegex.Replace(cnpj, "");

            if (!IsValid(digitsOnly))
            {
                throw new ArgumentException($"'{cnpj}' is not a valid CNPJ.", nameof(cnpj));
            }

            return new Cnpj(digitsOnly);
        }

        /// <summary>
        /// Tries to parse a string into a CNPJ instance.
        /// </summary>
        /// <param name="cnpj">The CNPJ string.</param>
        /// <param name="result">The resulting CNPJ if successful.</param>
        /// <returns>True if parsing was successful; otherwise, false.</returns>
        public static bool TryParse(string cnpj, out Cnpj result)
        {
            result = null!;

            if (string.IsNullOrWhiteSpace(cnpj))
            {
                return false;
            }

            var digitsOnly = OnlyDigitsRegex.Replace(cnpj, "");

            if (!IsValid(digitsOnly))
            {
                return false;
            }

            result = new Cnpj(digitsOnly);
            return true;
        }

        /// <summary>
        /// Checks if a string is a valid CNPJ.
        /// </summary>
        /// <param name="cnpj">The CNPJ to validate.</param>
        /// <returns>True if valid; otherwise, false.</returns>
        public static bool IsValid(string cnpj)
        {
            if (string.IsNullOrWhiteSpace(cnpj))
            {
                return false;
            }

            var digitsOnly = OnlyDigitsRegex.Replace(cnpj, "");

            if (digitsOnly.Length != 14)
            {
                return false;
            }

            // Check for known invalid CNPJs (all same digits)
            if (digitsOnly.All(c => c == digitsOnly[0]))
            {
                return false;
            }

            return ValidateCheckDigits(digitsOnly);
        }

        private static bool ValidateCheckDigits(string cnpj)
        {
            // First check digit
            int[] weights1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            var sum = 0;
            for (var i = 0; i < 12; i++)
            {
                sum += (cnpj[i] - '0') * weights1[i];
            }
            var remainder = sum % 11;
            var firstCheckDigit = remainder < 2 ? 0 : 11 - remainder;

            if (cnpj[12] - '0' != firstCheckDigit)
            {
                return false;
            }

            // Second check digit
            int[] weights2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            sum = 0;
            for (var i = 0; i < 13; i++)
            {
                sum += (cnpj[i] - '0') * weights2[i];
            }
            remainder = sum % 11;
            var secondCheckDigit = remainder < 2 ? 0 : 11 - remainder;

            return cnpj[13] - '0' == secondCheckDigit;
        }

        private static string FormatCnpj(string cnpj)
        {
            if (cnpj.Length != 14) return cnpj;
            return $"{cnpj.Substring(0, 2)}.{cnpj.Substring(2, 3)}.{cnpj.Substring(5, 3)}/{cnpj.Substring(8, 4)}-{cnpj.Substring(12, 2)}";
        }

        /// <inheritdoc />
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        /// <inheritdoc />
        public bool Equals(Cnpj other)
        {
            if (other is null) return false;
            return Value == other.Value;
        }

        /// <inheritdoc />
        public int CompareTo(Cnpj other)
        {
            if (other is null) return 1;
            return string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override string ToString() => Formatted;

        /// <summary>
        /// Implicit conversion from CNPJ to string (returns formatted).
        /// </summary>
        public static implicit operator string(Cnpj cnpj) => cnpj?.Formatted ?? string.Empty;

        /// <summary>
        /// Explicit conversion from string to CNPJ.
        /// </summary>
        public static explicit operator Cnpj(string cnpj) => Create(cnpj);
    }
}

