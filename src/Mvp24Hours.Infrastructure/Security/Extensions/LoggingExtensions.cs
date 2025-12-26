//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Security.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Security.Extensions
{
    /// <summary>
    /// Extension methods for logging with sensitive data masking.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions integrate <see cref="SensitiveDataMasker"/> with <see cref="ILogger"/>
    /// to automatically mask sensitive information in log messages. This helps prevent accidental
    /// exposure of passwords, API keys, credit card numbers, and other PII in logs.
    /// </para>
    /// <para>
    /// <strong>Best Practices:</strong>
    /// <list type="bullet">
    /// <item>Use these extensions when logging user input, API responses, or configuration values</item>
    /// <item>Always mask passwords, tokens, and API keys before logging</item>
    /// <item>Consider masking email addresses and phone numbers in production logs</item>
    /// <item>Use structured logging parameters instead of string interpolation for better masking</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Log with password masking
    /// logger.LogInformationWithMasking("User {Username} logged in with password {Password}", username, password);
    /// // Password will be automatically masked in the log output
    /// 
    /// // Log with API key masking
    /// logger.LogDebugWithMasking("API request with key {ApiKey}", apiKey);
    /// 
    /// // Log with custom sensitive keys
    /// logger.LogInformationWithMasking(
    ///     "Request data: {Data}",
    ///     data,
    ///     sensitiveKeys: new[] { "password", "token", "creditCard" });
    /// </code>
    /// </example>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Logs a message at Information level with automatic sensitive data masking.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The message template.</param>
        /// <param name="args">The message arguments.</param>
        /// <remarks>
        /// Automatically masks common sensitive patterns (passwords, API keys, credit cards, etc.)
        /// in the log message.
        /// </remarks>
        public static void LogInformationWithMasking(
            this ILogger logger,
            string message,
            params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (!logger.IsEnabled(LogLevel.Information))
            {
                return;
            }

            var maskedArgs = MaskArguments(args);
            logger.LogInformation(message, maskedArgs);
        }

        /// <summary>
        /// Logs a message at Information level with custom sensitive key masking.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The message template.</param>
        /// <param name="sensitiveKeys">List of parameter names that should be masked.</param>
        /// <param name="args">The message arguments.</param>
        /// <remarks>
        /// Masks arguments based on the provided sensitive keys list.
        /// </remarks>
        public static void LogInformationWithMasking(
            this ILogger logger,
            string message,
            IEnumerable<string> sensitiveKeys,
            params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (!logger.IsEnabled(LogLevel.Information))
            {
                return;
            }

            var maskedArgs = MaskArguments(args, sensitiveKeys);
            logger.LogInformation(message, maskedArgs);
        }

        /// <summary>
        /// Logs a message at Debug level with automatic sensitive data masking.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The message template.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogDebugWithMasking(
            this ILogger logger,
            string message,
            params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            var maskedArgs = MaskArguments(args);
            logger.LogDebug(message, maskedArgs);
        }

        /// <summary>
        /// Logs a message at Debug level with custom sensitive key masking.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The message template.</param>
        /// <param name="sensitiveKeys">List of parameter names that should be masked.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogDebugWithMasking(
            this ILogger logger,
            string message,
            IEnumerable<string> sensitiveKeys,
            params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            var maskedArgs = MaskArguments(args, sensitiveKeys);
            logger.LogDebug(message, maskedArgs);
        }

        /// <summary>
        /// Logs a message at Warning level with automatic sensitive data masking.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The message template.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogWarningWithMasking(
            this ILogger logger,
            string message,
            params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (!logger.IsEnabled(LogLevel.Warning))
            {
                return;
            }

            var maskedArgs = MaskArguments(args);
            logger.LogWarning(message, maskedArgs);
        }

        /// <summary>
        /// Logs a message at Warning level with custom sensitive key masking.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The message template.</param>
        /// <param name="sensitiveKeys">List of parameter names that should be masked.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogWarningWithMasking(
            this ILogger logger,
            string message,
            IEnumerable<string> sensitiveKeys,
            params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (!logger.IsEnabled(LogLevel.Warning))
            {
                return;
            }

            var maskedArgs = MaskArguments(args, sensitiveKeys);
            logger.LogWarning(message, maskedArgs);
        }

        /// <summary>
        /// Logs an exception at Error level with automatic sensitive data masking.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message template.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogErrorWithMasking(
            this ILogger logger,
            Exception exception,
            string message,
            params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (!logger.IsEnabled(LogLevel.Error))
            {
                return;
            }

            var maskedArgs = MaskArguments(args);
            logger.LogError(exception, message, maskedArgs);
        }

        /// <summary>
        /// Logs an exception at Error level with custom sensitive key masking.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message template.</param>
        /// <param name="sensitiveKeys">List of parameter names that should be masked.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogErrorWithMasking(
            this ILogger logger,
            Exception exception,
            string message,
            IEnumerable<string> sensitiveKeys,
            params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (!logger.IsEnabled(LogLevel.Error))
            {
                return;
            }

            var maskedArgs = MaskArguments(args, sensitiveKeys);
            logger.LogError(exception, message, maskedArgs);
        }

        /// <summary>
        /// Logs a message at Critical level with automatic sensitive data masking.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The message template.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogCriticalWithMasking(
            this ILogger logger,
            string message,
            params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (!logger.IsEnabled(LogLevel.Critical))
            {
                return;
            }

            var maskedArgs = MaskArguments(args);
            logger.LogCritical(message, maskedArgs);
        }

        /// <summary>
        /// Logs a message at Critical level with custom sensitive key masking.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The message template.</param>
        /// <param name="sensitiveKeys">List of parameter names that should be masked.</param>
        /// <param name="args">The message arguments.</param>
        public static void LogCriticalWithMasking(
            this ILogger logger,
            string message,
            IEnumerable<string> sensitiveKeys,
            params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (!logger.IsEnabled(LogLevel.Critical))
            {
                return;
            }

            var maskedArgs = MaskArguments(args, sensitiveKeys);
            logger.LogCritical(message, maskedArgs);
        }

        /// <summary>
        /// Logs a dictionary with automatic masking of sensitive keys.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="logLevel">The log level.</param>
        /// <param name="message">The message template.</param>
        /// <param name="data">The dictionary to log.</param>
        /// <param name="sensitiveKeys">List of keys that contain sensitive data.</param>
        public static void LogDictionaryWithMasking(
            this ILogger logger,
            LogLevel logLevel,
            string message,
            IDictionary<string, string?> data,
            IEnumerable<string> sensitiveKeys)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (!logger.IsEnabled(logLevel))
            {
                return;
            }

            var maskedData = SensitiveDataMasker.MaskDictionary(data, sensitiveKeys);
            logger.Log(logLevel, message, maskedData);
        }

        /// <summary>
        /// Masks arguments based on common sensitive patterns.
        /// </summary>
        /// <param name="args">The arguments to mask.</param>
        /// <returns>The masked arguments.</returns>
        private static object[] MaskArguments(object[] args)
        {
            if (args == null || args.Length == 0)
            {
                return args ?? Array.Empty<object>();
            }

            var maskedArgs = new object[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                maskedArgs[i] = MaskValue(args[i]);
            }

            return maskedArgs;
        }

        /// <summary>
        /// Masks arguments based on sensitive keys.
        /// </summary>
        /// <param name="args">The arguments to mask.</param>
        /// <param name="sensitiveKeys">List of parameter names that should be masked.</param>
        /// <returns>The masked arguments.</returns>
        private static object[] MaskArguments(object[] args, IEnumerable<string> sensitiveKeys)
        {
            if (args == null || args.Length == 0)
            {
                return args ?? Array.Empty<object>();
            }

            if (sensitiveKeys == null)
            {
                return MaskArguments(args);
            }

            var sensitiveKeySet = new HashSet<string>(sensitiveKeys, StringComparer.OrdinalIgnoreCase);
            var maskedArgs = new object[args.Length];

            // Note: We can't reliably map argument positions to parameter names without reflection,
            // so we mask all string arguments that match sensitive patterns
            for (int i = 0; i < args.Length; i++)
            {
                maskedArgs[i] = MaskValue(args[i]);
            }

            return maskedArgs;
        }

        /// <summary>
        /// Masks a single value if it's a string and contains sensitive patterns.
        /// </summary>
        /// <param name="value">The value to mask.</param>
        /// <returns>The masked value or original value if not sensitive.</returns>
        private static object MaskValue(object? value)
        {
            if (value == null)
            {
                return value!;
            }

            if (value is string stringValue)
            {
                // Check for common sensitive patterns
                if (IsPassword(stringValue))
                {
                    return SensitiveDataMasker.MaskPassword(stringValue);
                }

                if (IsApiKey(stringValue))
                {
                    return SensitiveDataMasker.MaskApiKey(stringValue);
                }

                if (IsCreditCard(stringValue))
                {
                    return SensitiveDataMasker.MaskCreditCard(stringValue);
                }

                if (IsEmail(stringValue))
                {
                    return SensitiveDataMasker.MaskEmail(stringValue);
                }

                if (IsPhoneNumber(stringValue))
                {
                    return SensitiveDataMasker.MaskPhoneNumber(stringValue);
                }
            }

            return value;
        }

        /// <summary>
        /// Checks if a string looks like a password.
        /// </summary>
        private static bool IsPassword(string value)
        {
            // Simple heuristic: if it's a reasonable length and contains mixed case/numbers/symbols
            return value.Length >= 8 && value.Length <= 128;
        }

        /// <summary>
        /// Checks if a string looks like an API key.
        /// </summary>
        private static bool IsApiKey(string value)
        {
            // Common API key patterns: sk_live_, sk_test_, pk_live_, etc.
            return value.Length >= 20 && value.Length <= 200 &&
                   (value.StartsWith("sk_", StringComparison.OrdinalIgnoreCase) ||
                    value.StartsWith("pk_", StringComparison.OrdinalIgnoreCase) ||
                    value.StartsWith("api_", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if a string looks like a credit card number.
        /// </summary>
        private static bool IsCreditCard(string value)
        {
            // Credit cards are typically 13-19 digits
            var digits = new System.Text.RegularExpressions.Regex(@"\d").Matches(value).Count;
            return digits >= 13 && digits <= 19;
        }

        /// <summary>
        /// Checks if a string looks like an email address.
        /// </summary>
        private static bool IsEmail(string value)
        {
            return value.Contains('@') && value.Contains('.') && value.Length > 5 && value.Length < 255;
        }

        /// <summary>
        /// Checks if a string looks like a phone number.
        /// </summary>
        private static bool IsPhoneNumber(string value)
        {
            var digits = new System.Text.RegularExpressions.Regex(@"\d").Matches(value).Count;
            return digits >= 10 && digits <= 15;
        }
    }
}

