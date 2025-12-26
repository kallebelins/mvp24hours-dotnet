//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mvp24Hours.Infrastructure.Security.Helpers
{
    /// <summary>
    /// Helper class for masking sensitive data in logs and strings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides utilities for masking sensitive information like passwords,
    /// API keys, credit card numbers, and other PII (Personally Identifiable Information)
    /// before logging or displaying them.
    /// </para>
    /// <para>
    /// <strong>Common Use Cases:</strong>
    /// - Masking passwords in log messages
    /// - Masking API keys in exception messages
    /// - Masking credit card numbers in logs
    /// - Masking email addresses or phone numbers
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Mask a password
    /// var masked = SensitiveDataMasker.MaskPassword("MySecretPassword123");
    /// // Returns: "***************"
    /// 
    /// // Mask an API key
    /// var masked = SensitiveDataMasker.MaskApiKey("sk_live_1234567890abcdef");
    /// // Returns: "sk_live_************"
    /// 
    /// // Mask credit card number
    /// var masked = SensitiveDataMasker.MaskCreditCard("4111111111111111");
    /// // Returns: "************1111"
    /// 
    /// // Mask custom pattern
    /// var masked = SensitiveDataMasker.MaskPattern("MyValue123", @"\d+", '*');
    /// // Returns: "MyValue***"
    /// </code>
    /// </example>
    public static class SensitiveDataMasker
    {
        private const int DefaultMaskLength = 4;
        private const char DefaultMaskChar = '*';

        /// <summary>
        /// Masks a password, showing only asterisks.
        /// </summary>
        /// <param name="password">The password to mask.</param>
        /// <param name="visibleChars">Number of characters to show at the end (default: 0).</param>
        /// <returns>The masked password.</returns>
        public static string MaskPassword(string? password, int visibleChars = 0)
        {
            if (string.IsNullOrEmpty(password))
            {
                return string.Empty;
            }

            if (visibleChars >= password.Length)
            {
                return new string(DefaultMaskChar, password.Length);
            }

            var maskedLength = password.Length - visibleChars;
            return new string(DefaultMaskChar, maskedLength) + password.Substring(password.Length - visibleChars);
        }

        /// <summary>
        /// Masks an API key, showing only the prefix and masking the rest.
        /// </summary>
        /// <param name="apiKey">The API key to mask.</param>
        /// <param name="prefixLength">Number of prefix characters to show (default: 7).</param>
        /// <returns>The masked API key.</returns>
        public static string MaskApiKey(string? apiKey, int prefixLength = 7)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                return string.Empty;
            }

            if (prefixLength >= apiKey.Length)
            {
                return new string(DefaultMaskChar, apiKey.Length);
            }

            return apiKey.Substring(0, prefixLength) + new string(DefaultMaskChar, apiKey.Length - prefixLength);
        }

        /// <summary>
        /// Masks a credit card number, showing only the last 4 digits.
        /// </summary>
        /// <param name="cardNumber">The credit card number to mask.</param>
        /// <param name="visibleDigits">Number of digits to show at the end (default: 4).</param>
        /// <returns>The masked credit card number.</returns>
        public static string MaskCreditCard(string? cardNumber, int visibleDigits = DefaultMaskLength)
        {
            if (string.IsNullOrEmpty(cardNumber))
            {
                return string.Empty;
            }

            // Remove spaces and dashes
            var cleaned = cardNumber.Replace(" ", "").Replace("-", "");

            if (visibleDigits >= cleaned.Length)
            {
                return new string(DefaultMaskChar, cleaned.Length);
            }

            var maskedLength = cleaned.Length - visibleDigits;
            return new string(DefaultMaskChar, maskedLength) + cleaned.Substring(cleaned.Length - visibleDigits);
        }

        /// <summary>
        /// Masks an email address, showing only the first character and domain.
        /// </summary>
        /// <param name="email">The email address to mask.</param>
        /// <returns>The masked email address.</returns>
        public static string MaskEmail(string? email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return string.Empty;
            }

            var atIndex = email.IndexOf('@');
            if (atIndex <= 0)
            {
                return new string(DefaultMaskChar, email.Length);
            }

            var localPart = email.Substring(0, atIndex);
            var domain = email.Substring(atIndex + 1);

            if (localPart.Length == 1)
            {
                return localPart + "@" + domain;
            }

            return localPart[0] + new string(DefaultMaskChar, localPart.Length - 1) + "@" + domain;
        }

        /// <summary>
        /// Masks a phone number, showing only the last few digits.
        /// </summary>
        /// <param name="phoneNumber">The phone number to mask.</param>
        /// <param name="visibleDigits">Number of digits to show at the end (default: 4).</param>
        /// <returns>The masked phone number.</returns>
        public static string MaskPhoneNumber(string? phoneNumber, int visibleDigits = DefaultMaskLength)
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return string.Empty;
            }

            // Extract only digits
            var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

            if (visibleDigits >= digits.Length)
            {
                return new string(DefaultMaskChar, phoneNumber.Length);
            }

            var maskedLength = digits.Length - visibleDigits;
            var masked = new string(DefaultMaskChar, maskedLength) + digits.Substring(digits.Length - visibleDigits);

            // Try to preserve original format
            if (phoneNumber.Contains("-"))
            {
                return masked.Insert(masked.Length - 4, "-");
            }

            return masked;
        }

        /// <summary>
        /// Masks a pattern in a string using a regular expression.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="maskChar">The character to use for masking (default: '*').</param>
        /// <returns>The string with matched patterns masked.</returns>
        public static string MaskPattern(string? input, string pattern, char maskChar = DefaultMaskChar)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(pattern))
            {
                return input;
            }

            return Regex.Replace(input, pattern, match => new string(maskChar, match.Length));
        }

        /// <summary>
        /// Masks sensitive data in a dictionary of key-value pairs.
        /// </summary>
        /// <param name="data">The dictionary to mask.</param>
        /// <param name="sensitiveKeys">List of keys that contain sensitive data.</param>
        /// <returns>A new dictionary with sensitive values masked.</returns>
        public static IDictionary<string, string?> MaskDictionary(
            IDictionary<string, string?> data,
            IEnumerable<string> sensitiveKeys)
        {
            if (data == null)
            {
                return new Dictionary<string, string?>();
            }

            var sensitiveKeySet = new HashSet<string>(sensitiveKeys, StringComparer.OrdinalIgnoreCase);
            var result = new Dictionary<string, string?>(data.Count);

            foreach (var kvp in data)
            {
                if (sensitiveKeySet.Contains(kvp.Key) && !string.IsNullOrEmpty(kvp.Value))
                {
                    result[kvp.Key] = MaskPassword(kvp.Value);
                }
                else
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return result;
        }

        /// <summary>
        /// Masks sensitive data in a JSON-like string by masking values for specified keys.
        /// </summary>
        /// <param name="json">The JSON string to mask.</param>
        /// <param name="sensitiveKeys">List of keys that contain sensitive data.</param>
        /// <returns>The JSON string with sensitive values masked.</returns>
        public static string MaskJson(string? json, IEnumerable<string> sensitiveKeys)
        {
            if (string.IsNullOrEmpty(json))
            {
                return string.Empty;
            }

            var sensitiveKeySet = new HashSet<string>(sensitiveKeys, StringComparer.OrdinalIgnoreCase);
            var result = json;

            foreach (var key in sensitiveKeySet)
            {
                // Match JSON key-value pairs: "key": "value"
                var pattern = $@"""{Regex.Escape(key)}""\s*:\s*""([^""]+)""";
                result = Regex.Replace(result, pattern, match =>
                {
                    var value = match.Groups[1].Value;
                    var masked = MaskPassword(value);
                    return $@"""{key}"": ""{masked}""";
                }, RegexOptions.IgnoreCase);
            }

            return result;
        }
    }
}

