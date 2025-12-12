//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mvp24Hours.Core.Helpers
{
    /// <summary>
    /// Provides guard clause methods for defensive programming.
    /// Use these methods to validate method arguments and throw appropriate exceptions.
    /// </summary>
    /// <example>
    /// <code>
    /// public void ProcessOrder(Order order, string customerId)
    /// {
    ///     Guard.Against.Null(order, nameof(order));
    ///     Guard.Against.NullOrEmpty(customerId, nameof(customerId));
    ///     
    ///     // Process the order...
    /// }
    /// </code>
    /// </example>
    public static class Guard
    {
        /// <summary>
        /// Entry point for guard clauses.
        /// </summary>
        public static IGuardClause Against { get; } = new GuardClause();
    }

    /// <summary>
    /// Interface for guard clause implementations.
    /// </summary>
    public interface IGuardClause
    {
    }

    /// <summary>
    /// Default implementation of guard clauses.
    /// </summary>
    internal sealed class GuardClause : IGuardClause
    {
    }

    /// <summary>
    /// Extension methods for <see cref="IGuardClause"/> providing various validation methods.
    /// </summary>
    public static class GuardClauseExtensions
    {
        #region Null Checks

        /// <summary>
        /// Throws <see cref="ArgumentNullException"/> if the value is null.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="value">The value to check.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="message">Optional custom error message.</param>
        /// <returns>The original value if not null.</returns>
        /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
        public static T Null<T>(
            this IGuardClause guardClause,
            [NotNull] T? value,
            string parameterName,
            string? message = null)
        {
            if (value is null)
            {
                throw new ArgumentNullException(parameterName, message ?? $"Parameter '{parameterName}' cannot be null.");
            }
            return value;
        }

        /// <summary>
        /// Throws <see cref="ArgumentNullException"/> if the string is null or empty.
        /// </summary>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="value">The string to check.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="message">Optional custom error message.</param>
        /// <returns>The original string if not null or empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
        /// <exception cref="ArgumentException">Thrown when value is empty.</exception>
        public static string NullOrEmpty(
            this IGuardClause guardClause,
            [NotNull] string? value,
            string parameterName,
            string? message = null)
        {
            guardClause.Null(value, parameterName, message);

            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(message ?? $"Parameter '{parameterName}' cannot be empty.", parameterName);
            }
            return value;
        }

        /// <summary>
        /// Throws <see cref="ArgumentNullException"/> if the string is null, empty, or whitespace.
        /// </summary>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="value">The string to check.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="message">Optional custom error message.</param>
        /// <returns>The original string if not null, empty, or whitespace.</returns>
        /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
        /// <exception cref="ArgumentException">Thrown when value is empty or whitespace.</exception>
        public static string NullOrWhiteSpace(
            this IGuardClause guardClause,
            [NotNull] string? value,
            string parameterName,
            string? message = null)
        {
            guardClause.Null(value, parameterName, message);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(message ?? $"Parameter '{parameterName}' cannot be empty or whitespace.", parameterName);
            }
            return value;
        }

        /// <summary>
        /// Throws <see cref="ArgumentNullException"/> if the collection is null or empty.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="value">The collection to check.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="message">Optional custom error message.</param>
        /// <returns>The original collection if not null or empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
        /// <exception cref="ArgumentException">Thrown when collection is empty.</exception>
        public static IEnumerable<T> NullOrEmpty<T>(
            this IGuardClause guardClause,
            [NotNull] IEnumerable<T>? value,
            string parameterName,
            string? message = null)
        {
            guardClause.Null(value, parameterName, message);

            if (!value!.Any())
            {
                throw new ArgumentException(message ?? $"Parameter '{parameterName}' cannot be an empty collection.", parameterName);
            }
            return value;
        }

        #endregion

        #region Default Value Checks

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the value equals default(T).
        /// Useful for checking struct values like Guid.Empty or default DateTime.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="value">The value to check.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="message">Optional custom error message.</param>
        /// <returns>The original value if not default.</returns>
        /// <exception cref="ArgumentException">Thrown when value equals default(T).</exception>
        public static T Default<T>(
            this IGuardClause guardClause,
            T value,
            string parameterName,
            string? message = null) where T : struct
        {
            if (EqualityComparer<T>.Default.Equals(value, default))
            {
                throw new ArgumentException(message ?? $"Parameter '{parameterName}' cannot be the default value.", parameterName);
            }
            return value;
        }

        #endregion

        #region Range Checks

        /// <summary>
        /// Throws <see cref="ArgumentOutOfRangeException"/> if the value is outside the specified range.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="value">The value to check.</param>
        /// <param name="min">The minimum allowed value (inclusive).</param>
        /// <param name="max">The maximum allowed value (inclusive).</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="message">Optional custom error message.</param>
        /// <returns>The original value if within range.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when value is outside range.</exception>
        public static T OutOfRange<T>(
            this IGuardClause guardClause,
            T value,
            T min,
            T max,
            string parameterName,
            string? message = null) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    message ?? $"Parameter '{parameterName}' must be between {min} and {max}. Actual value: {value}.");
            }
            return value;
        }

        /// <summary>
        /// Throws <see cref="ArgumentOutOfRangeException"/> if the value is negative or zero.
        /// </summary>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="value">The value to check.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="message">Optional custom error message.</param>
        /// <returns>The original value if positive.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when value is negative or zero.</exception>
        public static int NegativeOrZero(
            this IGuardClause guardClause,
            int value,
            string parameterName,
            string? message = null)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    message ?? $"Parameter '{parameterName}' must be a positive number. Actual value: {value}.");
            }
            return value;
        }

        /// <summary>
        /// Throws <see cref="ArgumentOutOfRangeException"/> if the value is negative or zero.
        /// </summary>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="value">The value to check.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="message">Optional custom error message.</param>
        /// <returns>The original value if positive.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when value is negative or zero.</exception>
        public static long NegativeOrZero(
            this IGuardClause guardClause,
            long value,
            string parameterName,
            string? message = null)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    message ?? $"Parameter '{parameterName}' must be a positive number. Actual value: {value}.");
            }
            return value;
        }

        /// <summary>
        /// Throws <see cref="ArgumentOutOfRangeException"/> if the value is negative or zero.
        /// </summary>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="value">The value to check.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="message">Optional custom error message.</param>
        /// <returns>The original value if positive.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when value is negative or zero.</exception>
        public static decimal NegativeOrZero(
            this IGuardClause guardClause,
            decimal value,
            string parameterName,
            string? message = null)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    message ?? $"Parameter '{parameterName}' must be a positive number. Actual value: {value}.");
            }
            return value;
        }

        /// <summary>
        /// Throws <see cref="ArgumentOutOfRangeException"/> if the value is negative.
        /// </summary>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="value">The value to check.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="message">Optional custom error message.</param>
        /// <returns>The original value if non-negative.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when value is negative.</exception>
        public static int Negative(
            this IGuardClause guardClause,
            int value,
            string parameterName,
            string? message = null)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    message ?? $"Parameter '{parameterName}' cannot be negative. Actual value: {value}.");
            }
            return value;
        }

        /// <summary>
        /// Throws <see cref="ArgumentOutOfRangeException"/> if the value is negative.
        /// </summary>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="value">The value to check.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="message">Optional custom error message.</param>
        /// <returns>The original value if non-negative.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when value is negative.</exception>
        public static decimal Negative(
            this IGuardClause guardClause,
            decimal value,
            string parameterName,
            string? message = null)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    message ?? $"Parameter '{parameterName}' cannot be negative. Actual value: {value}.");
            }
            return value;
        }

        #endregion

        #region Format Validations

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the string doesn't match the specified regex pattern.
        /// </summary>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="value">The string to validate.</param>
        /// <param name="pattern">The regex pattern to match.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="message">Optional custom error message.</param>
        /// <returns>The original string if it matches the pattern.</returns>
        /// <exception cref="ArgumentException">Thrown when value doesn't match pattern.</exception>
        public static string InvalidFormat(
            this IGuardClause guardClause,
            string value,
            string pattern,
            string parameterName,
            string? message = null)
        {
            guardClause.NullOrEmpty(value, parameterName);

            if (!Regex.IsMatch(value, pattern))
            {
                throw new ArgumentException(
                    message ?? $"Parameter '{parameterName}' has an invalid format.",
                    parameterName);
            }
            return value;
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the string is not a valid email address.
        /// </summary>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="value">The email to validate.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="message">Optional custom error message.</param>
        /// <returns>The original string if it's a valid email.</returns>
        /// <exception cref="ArgumentException">Thrown when value is not a valid email.</exception>
        public static string InvalidEmail(
            this IGuardClause guardClause,
            string value,
            string parameterName,
            string? message = null)
        {
            guardClause.NullOrEmpty(value, parameterName);

            // RFC 5322 compliant email regex (simplified)
            const string emailPattern = @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$";

            if (!Regex.IsMatch(value, emailPattern))
            {
                throw new ArgumentException(
                    message ?? $"Parameter '{parameterName}' is not a valid email address.",
                    parameterName);
            }
            return value;
        }

        #endregion

        #region Brazilian Document Validations

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the string is not a valid Brazilian CPF.
        /// </summary>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="value">The CPF to validate (with or without formatting).</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="message">Optional custom error message.</param>
        /// <returns>The original string if it's a valid CPF.</returns>
        /// <exception cref="ArgumentException">Thrown when value is not a valid CPF.</exception>
        public static string InvalidCpf(
            this IGuardClause guardClause,
            string value,
            string parameterName,
            string? message = null)
        {
            guardClause.NullOrEmpty(value, parameterName);

            // Remove formatting characters
            var cpf = Regex.Replace(value, @"[^\d]", "");

            if (cpf.Length != 11)
            {
                throw new ArgumentException(
                    message ?? $"Parameter '{parameterName}' is not a valid CPF. CPF must have 11 digits.",
                    parameterName);
            }

            // Check for known invalid CPFs (all same digits)
            if (cpf.All(c => c == cpf[0]))
            {
                throw new ArgumentException(
                    message ?? $"Parameter '{parameterName}' is not a valid CPF.",
                    parameterName);
            }

            // Validate check digits
            if (!ValidateCpfCheckDigits(cpf))
            {
                throw new ArgumentException(
                    message ?? $"Parameter '{parameterName}' is not a valid CPF.",
                    parameterName);
            }

            return value;
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the string is not a valid Brazilian CNPJ.
        /// </summary>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="value">The CNPJ to validate (with or without formatting).</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="message">Optional custom error message.</param>
        /// <returns>The original string if it's a valid CNPJ.</returns>
        /// <exception cref="ArgumentException">Thrown when value is not a valid CNPJ.</exception>
        public static string InvalidCnpj(
            this IGuardClause guardClause,
            string value,
            string parameterName,
            string? message = null)
        {
            guardClause.NullOrEmpty(value, parameterName);

            // Remove formatting characters
            var cnpj = Regex.Replace(value, @"[^\d]", "");

            if (cnpj.Length != 14)
            {
                throw new ArgumentException(
                    message ?? $"Parameter '{parameterName}' is not a valid CNPJ. CNPJ must have 14 digits.",
                    parameterName);
            }

            // Check for known invalid CNPJs (all same digits)
            if (cnpj.All(c => c == cnpj[0]))
            {
                throw new ArgumentException(
                    message ?? $"Parameter '{parameterName}' is not a valid CNPJ.",
                    parameterName);
            }

            // Validate check digits
            if (!ValidateCnpjCheckDigits(cnpj))
            {
                throw new ArgumentException(
                    message ?? $"Parameter '{parameterName}' is not a valid CNPJ.",
                    parameterName);
            }

            return value;
        }

        private static bool ValidateCpfCheckDigits(string cpf)
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

        private static bool ValidateCnpjCheckDigits(string cnpj)
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

        #endregion

        #region Length Validations

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the string length is less than the minimum.
        /// </summary>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="value">The string to check.</param>
        /// <param name="minLength">The minimum allowed length.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="message">Optional custom error message.</param>
        /// <returns>The original string if length is valid.</returns>
        /// <exception cref="ArgumentException">Thrown when string length is less than minimum.</exception>
        public static string LengthLessThan(
            this IGuardClause guardClause,
            string value,
            int minLength,
            string parameterName,
            string? message = null)
        {
            guardClause.Null(value, parameterName);

            if (value.Length < minLength)
            {
                throw new ArgumentException(
                    message ?? $"Parameter '{parameterName}' must be at least {minLength} characters long. Actual length: {value.Length}.",
                    parameterName);
            }
            return value;
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the string length exceeds the maximum.
        /// </summary>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="value">The string to check.</param>
        /// <param name="maxLength">The maximum allowed length.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="message">Optional custom error message.</param>
        /// <returns>The original string if length is valid.</returns>
        /// <exception cref="ArgumentException">Thrown when string length exceeds maximum.</exception>
        public static string LengthGreaterThan(
            this IGuardClause guardClause,
            string value,
            int maxLength,
            string parameterName,
            string? message = null)
        {
            guardClause.Null(value, parameterName);

            if (value.Length > maxLength)
            {
                throw new ArgumentException(
                    message ?? $"Parameter '{parameterName}' must be at most {maxLength} characters long. Actual length: {value.Length}.",
                    parameterName);
            }
            return value;
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the string length is outside the specified range.
        /// </summary>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="value">The string to check.</param>
        /// <param name="minLength">The minimum allowed length.</param>
        /// <param name="maxLength">The maximum allowed length.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="message">Optional custom error message.</param>
        /// <returns>The original string if length is valid.</returns>
        /// <exception cref="ArgumentException">Thrown when string length is outside range.</exception>
        public static string LengthOutOfRange(
            this IGuardClause guardClause,
            string value,
            int minLength,
            int maxLength,
            string parameterName,
            string? message = null)
        {
            guardClause.Null(value, parameterName);

            if (value.Length < minLength || value.Length > maxLength)
            {
                throw new ArgumentException(
                    message ?? $"Parameter '{parameterName}' must be between {minLength} and {maxLength} characters long. Actual length: {value.Length}.",
                    parameterName);
            }
            return value;
        }

        #endregion

        #region Condition Checks

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the condition is true.
        /// </summary>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="condition">The condition to check.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="message">The error message.</param>
        /// <exception cref="ArgumentException">Thrown when condition is true.</exception>
        public static void Condition(
            this IGuardClause guardClause,
            bool condition,
            string parameterName,
            string message)
        {
            if (condition)
            {
                throw new ArgumentException(message, parameterName);
            }
        }

        /// <summary>
        /// Throws <see cref="InvalidOperationException"/> if the condition is true.
        /// Use this for invalid state conditions rather than argument validation.
        /// </summary>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="condition">The condition to check.</param>
        /// <param name="message">The error message.</param>
        /// <exception cref="InvalidOperationException">Thrown when condition is true.</exception>
        public static void InvalidOperation(
            this IGuardClause guardClause,
            bool condition,
            string message)
        {
            if (condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        #endregion

        #region Type Checks

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the value is not of the expected type.
        /// </summary>
        /// <typeparam name="TExpected">The expected type.</typeparam>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="value">The value to check.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="message">Optional custom error message.</param>
        /// <returns>The value cast to the expected type.</returns>
        /// <exception cref="ArgumentException">Thrown when value is not of expected type.</exception>
        public static TExpected NotOfType<TExpected>(
            this IGuardClause guardClause,
            object value,
            string parameterName,
            string? message = null)
        {
            guardClause.Null(value, parameterName);

            if (value is not TExpected typedValue)
            {
                throw new ArgumentException(
                    message ?? $"Parameter '{parameterName}' must be of type {typeof(TExpected).Name}. Actual type: {value.GetType().Name}.",
                    parameterName);
            }
            return typedValue;
        }

        #endregion

        #region Guid Checks

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the Guid is empty (Guid.Empty).
        /// </summary>
        /// <param name="guardClause">The guard clause.</param>
        /// <param name="value">The Guid to check.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="message">Optional custom error message.</param>
        /// <returns>The original Guid if not empty.</returns>
        /// <exception cref="ArgumentException">Thrown when Guid is empty.</exception>
        public static Guid EmptyGuid(
            this IGuardClause guardClause,
            Guid value,
            string parameterName,
            string? message = null)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException(
                    message ?? $"Parameter '{parameterName}' cannot be an empty GUID.",
                    parameterName);
            }
            return value;
        }

        #endregion
    }
}

