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
    /// Value Object representing a valid email address.
    /// </summary>
    /// <example>
    /// <code>
    /// var email = Email.Create("user@example.com");
    /// Console.WriteLine(email.Value); // user@example.com
    /// Console.WriteLine(email.LocalPart); // user
    /// Console.WriteLine(email.Domain); // example.com
    /// 
    /// // Using TryParse for safe parsing
    /// if (Email.TryParse("invalid", out var result))
    /// {
    ///     // Won't reach here
    /// }
    /// 
    /// // Implicit conversion from string
    /// Email email2 = "another@example.com";
    /// </code>
    /// </example>
    public sealed class Email : BaseVO, IEquatable<Email>, IComparable<Email>
    {
        /// <summary>
        /// RFC 5322 compliant email regex pattern (simplified).
        /// </summary>
        private const string EmailPattern = @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$";
        private static readonly Regex EmailRegex = new(EmailPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Gets the full email address.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets the local part (before @) of the email address.
        /// </summary>
        public string LocalPart { get; }

        /// <summary>
        /// Gets the domain part (after @) of the email address.
        /// </summary>
        public string Domain { get; }

        private Email(string value)
        {
            Value = value.ToLowerInvariant();
            var parts = Value.Split('@');
            LocalPart = parts[0];
            Domain = parts[1];
        }

        /// <summary>
        /// Creates a new Email instance.
        /// </summary>
        /// <param name="email">The email address string.</param>
        /// <returns>A valid Email instance.</returns>
        /// <exception cref="ArgumentException">Thrown when email is invalid.</exception>
        public static Email Create(string email)
        {
            Guard.Against.NullOrWhiteSpace(email, nameof(email));

            email = email.Trim();

            if (!IsValid(email))
            {
                throw new ArgumentException($"'{email}' is not a valid email address.", nameof(email));
            }

            return new Email(email);
        }

        /// <summary>
        /// Tries to parse a string into an Email instance.
        /// </summary>
        /// <param name="email">The email address string.</param>
        /// <param name="result">The resulting Email if successful.</param>
        /// <returns>True if parsing was successful; otherwise, false.</returns>
        public static bool TryParse(string email, out Email result)
        {
            result = null!;

            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            email = email.Trim();

            if (!IsValid(email))
            {
                return false;
            }

            result = new Email(email);
            return true;
        }

        /// <summary>
        /// Checks if a string is a valid email address.
        /// </summary>
        /// <param name="email">The email address to validate.</param>
        /// <returns>True if valid; otherwise, false.</returns>
        public static bool IsValid(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            return EmailRegex.IsMatch(email) && email.Length <= 254;
        }

        /// <inheritdoc />
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        /// <inheritdoc />
        public bool Equals(Email? other)
        {
            if (other is null) return false;
            return Value == other.Value;
        }

        /// <inheritdoc />
        public int CompareTo(Email? other)
        {
            if (other is null) return 1;
            return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public override string ToString() => Value;

        /// <summary>
        /// Implicit conversion from Email to string.
        /// </summary>
        public static implicit operator string(Email email) => email?.Value ?? string.Empty;

        /// <summary>
        /// Explicit conversion from string to Email.
        /// </summary>
        public static explicit operator Email(string email) => Create(email);
    }
}

