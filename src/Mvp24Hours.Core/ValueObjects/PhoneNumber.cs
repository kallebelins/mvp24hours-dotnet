//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Mvp24Hours.Core.ValueObjects
{
    /// <summary>
    /// Value Object representing a phone number with country code and area code.
    /// </summary>
    /// <example>
    /// <code>
    /// var phone = PhoneNumber.Create("+55", "11", "999887766");
    /// Console.WriteLine(phone.ToString()); // +55 (11) 99988-7766
    /// Console.WriteLine(phone.FullNumber); // 5511999887766
    /// 
    /// // Brazilian phone with default country code
    /// var brPhone = PhoneNumber.CreateBrazilian("11", "999887766");
    /// </code>
    /// </example>
    public sealed class PhoneNumber : BaseVO, IEquatable<PhoneNumber>, IComparable<PhoneNumber>
    {
        private static readonly Regex OnlyDigitsRegex = new(@"[^\d]", RegexOptions.Compiled);
        private static readonly Regex CountryCodeRegex = new(@"^\+?\d{1,3}$", RegexOptions.Compiled);

        /// <summary>
        /// Gets the country code (e.g., "+55" for Brazil, "+1" for USA).
        /// </summary>
        public string CountryCode { get; }

        /// <summary>
        /// Gets the area code (DDD in Brazil).
        /// </summary>
        public string AreaCode { get; }

        /// <summary>
        /// Gets the local number without area code.
        /// </summary>
        public string Number { get; }

        /// <summary>
        /// Gets the full number with country code, area code, and number (digits only).
        /// </summary>
        public string FullNumber => $"{OnlyDigitsRegex.Replace(CountryCode, "")}{AreaCode}{Number}";

        private PhoneNumber(string countryCode, string areaCode, string number)
        {
            CountryCode = countryCode.StartsWith("+") ? countryCode : $"+{countryCode}";
            AreaCode = areaCode;
            Number = number;
        }

        /// <summary>
        /// Creates a new PhoneNumber instance.
        /// </summary>
        /// <param name="countryCode">The country code (with or without +).</param>
        /// <param name="areaCode">The area code.</param>
        /// <param name="number">The local number.</param>
        /// <returns>A valid PhoneNumber instance.</returns>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
        public static PhoneNumber Create(string countryCode, string areaCode, string number)
        {
            Guard.Against.NullOrWhiteSpace(countryCode, nameof(countryCode));
            Guard.Against.NullOrWhiteSpace(areaCode, nameof(areaCode));
            Guard.Against.NullOrWhiteSpace(number, nameof(number));

            // Normalize country code
            var normalizedCountryCode = countryCode.TrimStart('+');
            if (!CountryCodeRegex.IsMatch(normalizedCountryCode) && !CountryCodeRegex.IsMatch($"+{normalizedCountryCode}"))
            {
                throw new ArgumentException($"'{countryCode}' is not a valid country code.", nameof(countryCode));
            }

            var cleanAreaCode = OnlyDigitsRegex.Replace(areaCode, "");
            var cleanNumber = OnlyDigitsRegex.Replace(number, "");

            if (string.IsNullOrEmpty(cleanAreaCode))
            {
                throw new ArgumentException("Area code must contain at least one digit.", nameof(areaCode));
            }

            if (cleanNumber.Length < 6 || cleanNumber.Length > 11)
            {
                throw new ArgumentException("Phone number must be between 6 and 11 digits.", nameof(number));
            }

            return new PhoneNumber(normalizedCountryCode, cleanAreaCode, cleanNumber);
        }

        /// <summary>
        /// Creates a Brazilian phone number (+55).
        /// </summary>
        /// <param name="areaCode">The DDD (2 digits).</param>
        /// <param name="number">The phone number (8-9 digits).</param>
        /// <returns>A new PhoneNumber instance for Brazil.</returns>
        public static PhoneNumber CreateBrazilian(string areaCode, string number)
        {
            return Create("55", areaCode, number);
        }

        /// <summary>
        /// Creates a US/Canada phone number (+1).
        /// </summary>
        /// <param name="areaCode">The area code (3 digits).</param>
        /// <param name="number">The phone number (7 digits).</param>
        /// <returns>A new PhoneNumber instance for US/Canada.</returns>
        public static PhoneNumber CreateUSA(string areaCode, string number)
        {
            return Create("1", areaCode, number);
        }

        /// <summary>
        /// Tries to parse a string into a PhoneNumber instance.
        /// </summary>
        /// <param name="countryCode">The country code.</param>
        /// <param name="areaCode">The area code.</param>
        /// <param name="number">The phone number.</param>
        /// <param name="result">The resulting PhoneNumber if successful.</param>
        /// <returns>True if parsing was successful; otherwise, false.</returns>
        public static bool TryParse(string countryCode, string areaCode, string number, out PhoneNumber result)
        {
            result = null!;

            try
            {
                result = Create(countryCode, areaCode, number);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Formats the phone number in a human-readable format.
        /// </summary>
        /// <returns>Formatted phone number.</returns>
        public string ToFormattedString()
        {
            // Brazilian format
            if (OnlyDigitsRegex.Replace(CountryCode, "") == "55" && Number.Length >= 8)
            {
                if (Number.Length == 9)
                {
                    return $"{CountryCode} ({AreaCode}) {Number.Substring(0, 5)}-{Number.Substring(5)}";
                }
                return $"{CountryCode} ({AreaCode}) {Number.Substring(0, 4)}-{Number.Substring(4)}";
            }

            // Generic format
            return $"{CountryCode} ({AreaCode}) {Number}";
        }

        /// <inheritdoc />
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return FullNumber;
        }

        /// <inheritdoc />
        public bool Equals(PhoneNumber other)
        {
            if (other is null) return false;
            return FullNumber == other.FullNumber;
        }

        /// <inheritdoc />
        public int CompareTo(PhoneNumber other)
        {
            if (other is null) return 1;
            return string.Compare(FullNumber, other.FullNumber, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override string ToString() => ToFormattedString();

        /// <summary>
        /// Implicit conversion from PhoneNumber to string (returns full number).
        /// </summary>
        public static implicit operator string(PhoneNumber phone) => phone?.FullNumber ?? string.Empty;
    }
}

