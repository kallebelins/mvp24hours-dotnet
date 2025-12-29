//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Testing.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Testing.Assertions
{
    /// <summary>
    /// Provides assertion helpers for log entries captured by FakeLogger and InMemoryLoggerProvider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These helpers provide fluent assertions for verifying log output in tests.
    /// They work with both <see cref="FakeLogger"/> and <see cref="InMemoryLoggerProvider"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var logger = new FakeLogger&lt;MyService&gt;();
    /// var service = new MyService(logger);
    /// await service.ProcessAsync();
    /// 
    /// LogAssertions.AssertLogged(logger, LogLevel.Information, "Processing started");
    /// LogAssertions.AssertLoggedCount(logger, LogLevel.Debug, 5);
    /// LogAssertions.AssertNoErrorsLogged(logger);
    /// </code>
    /// </example>
    public static class LogAssertions
    {
        #region FakeLogger Assertions

        /// <summary>
        /// Asserts that a log was recorded at the specified level containing the specified message.
        /// </summary>
        /// <param name="logger">The fake logger to check.</param>
        /// <param name="logLevel">The expected log level.</param>
        /// <param name="messageContains">Text that should be in the log message.</param>
        /// <param name="comparison">String comparison type.</param>
        /// <exception cref="AssertionException">Thrown when no matching log was found.</exception>
        public static void AssertLogged(FakeLogger logger, LogLevel logLevel, string messageContains, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (string.IsNullOrEmpty(messageContains)) throw new ArgumentNullException(nameof(messageContains));

            if (!logger.ContainsLog(logLevel, messageContains, comparison))
            {
                var existingLogs = FormatLogs(logger.Logs);
                throw new AssertionException(
                    $"Expected a {logLevel} log containing '{messageContains}', but no such log was found.\n" +
                    $"Captured logs:\n{existingLogs}");
            }
        }

        /// <summary>
        /// Asserts that a log was recorded containing the specified message at any level.
        /// </summary>
        /// <param name="logger">The fake logger to check.</param>
        /// <param name="messageContains">Text that should be in the log message.</param>
        /// <param name="comparison">String comparison type.</param>
        /// <exception cref="AssertionException">Thrown when no matching log was found.</exception>
        public static void AssertLogged(FakeLogger logger, string messageContains, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (string.IsNullOrEmpty(messageContains)) throw new ArgumentNullException(nameof(messageContains));

            if (!logger.ContainsLog(messageContains, comparison))
            {
                var existingLogs = FormatLogs(logger.Logs);
                throw new AssertionException(
                    $"Expected a log containing '{messageContains}', but no such log was found.\n" +
                    $"Captured logs:\n{existingLogs}");
            }
        }

        /// <summary>
        /// Asserts that exactly the specified number of logs were recorded at the given level.
        /// </summary>
        /// <param name="logger">The fake logger to check.</param>
        /// <param name="logLevel">The log level to count.</param>
        /// <param name="expectedCount">The expected count.</param>
        /// <exception cref="AssertionException">Thrown when the count doesn't match.</exception>
        public static void AssertLoggedCount(FakeLogger logger, LogLevel logLevel, int expectedCount)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            var actualCount = logger.GetLogs(logLevel).Count;
            if (actualCount != expectedCount)
            {
                throw new AssertionException(
                    $"Expected {expectedCount} {logLevel} log(s), but found {actualCount}.");
            }
        }

        /// <summary>
        /// Asserts that at least one log was recorded at the specified level.
        /// </summary>
        /// <param name="logger">The fake logger to check.</param>
        /// <param name="logLevel">The expected log level.</param>
        /// <exception cref="AssertionException">Thrown when no log at that level was found.</exception>
        public static void AssertLoggedAtLevel(FakeLogger logger, LogLevel logLevel)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            if (!logger.GetLogs(logLevel).Any())
            {
                throw new AssertionException(
                    $"Expected at least one {logLevel} log, but none were found.");
            }
        }

        /// <summary>
        /// Asserts that no logs were recorded at Error or Critical level.
        /// </summary>
        /// <param name="logger">The fake logger to check.</param>
        /// <exception cref="AssertionException">Thrown when an error or critical log was found.</exception>
        public static void AssertNoErrorsLogged(FakeLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            var errors = logger.GetLogsAtOrAbove(LogLevel.Error);
            if (errors.Any())
            {
                var errorLogs = FormatLogs(errors);
                throw new AssertionException(
                    $"Expected no errors to be logged, but found {errors.Count}:\n{errorLogs}");
            }
        }

        /// <summary>
        /// Asserts that no logs were recorded at Warning level or higher.
        /// </summary>
        /// <param name="logger">The fake logger to check.</param>
        /// <exception cref="AssertionException">Thrown when a warning or higher log was found.</exception>
        public static void AssertNoWarningsOrErrorsLogged(FakeLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            var warnings = logger.GetLogsAtOrAbove(LogLevel.Warning);
            if (warnings.Any())
            {
                var warningLogs = FormatLogs(warnings);
                throw new AssertionException(
                    $"Expected no warnings or errors to be logged, but found {warnings.Count}:\n{warningLogs}");
            }
        }

        /// <summary>
        /// Asserts that a log was recorded with an exception of the specified type.
        /// </summary>
        /// <typeparam name="TException">The expected exception type.</typeparam>
        /// <param name="logger">The fake logger to check.</param>
        /// <exception cref="AssertionException">Thrown when no log with that exception type was found.</exception>
        public static void AssertLoggedException<TException>(FakeLogger logger) where TException : Exception
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            if (!logger.ContainsException<TException>())
            {
                throw new AssertionException(
                    $"Expected a log with exception of type {typeof(TException).Name}, but none was found.");
            }
        }

        /// <summary>
        /// Asserts that no logs were recorded.
        /// </summary>
        /// <param name="logger">The fake logger to check.</param>
        /// <exception cref="AssertionException">Thrown when any log was found.</exception>
        public static void AssertNoLogsRecorded(FakeLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            if (logger.LogCount > 0)
            {
                var logs = FormatLogs(logger.Logs);
                throw new AssertionException(
                    $"Expected no logs to be recorded, but found {logger.LogCount}:\n{logs}");
            }
        }

        /// <summary>
        /// Asserts that a log matching the predicate was recorded.
        /// </summary>
        /// <param name="logger">The fake logger to check.</param>
        /// <param name="predicate">The predicate to match.</param>
        /// <param name="description">Description of what was expected (for error message).</param>
        /// <exception cref="AssertionException">Thrown when no matching log was found.</exception>
        public static void AssertLogged(FakeLogger logger, Func<LogEntry, bool> predicate, string? description = null)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            if (!logger.GetLogs(predicate).Any())
            {
                var desc = description ?? "matching predicate";
                throw new AssertionException(
                    $"Expected a log {desc}, but no such log was found.");
            }
        }

        #endregion

        #region InMemoryLoggerProvider Assertions

        /// <summary>
        /// Asserts that a log was recorded at the specified level containing the specified message.
        /// </summary>
        /// <param name="provider">The logger provider to check.</param>
        /// <param name="logLevel">The expected log level.</param>
        /// <param name="messageContains">Text that should be in the log message.</param>
        /// <param name="comparison">String comparison type.</param>
        /// <exception cref="AssertionException">Thrown when no matching log was found.</exception>
        public static void AssertLogged(InMemoryLoggerProvider provider, LogLevel logLevel, string messageContains, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (string.IsNullOrEmpty(messageContains)) throw new ArgumentNullException(nameof(messageContains));

            if (!provider.ContainsLog(logLevel, messageContains, comparison))
            {
                var existingLogs = FormatLogs(provider.AllLogs);
                throw new AssertionException(
                    $"Expected a {logLevel} log containing '{messageContains}', but no such log was found.\n" +
                    $"Captured logs:\n{existingLogs}");
            }
        }

        /// <summary>
        /// Asserts that a log was recorded in a specific category containing the specified message.
        /// </summary>
        /// <param name="provider">The logger provider to check.</param>
        /// <param name="categoryContains">Part of the category name to match.</param>
        /// <param name="messageContains">Text that should be in the log message.</param>
        /// <param name="comparison">String comparison type.</param>
        /// <exception cref="AssertionException">Thrown when no matching log was found.</exception>
        public static void AssertLoggedInCategory(InMemoryLoggerProvider provider, string categoryContains, string messageContains, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (string.IsNullOrEmpty(categoryContains)) throw new ArgumentNullException(nameof(categoryContains));
            if (string.IsNullOrEmpty(messageContains)) throw new ArgumentNullException(nameof(messageContains));

            if (!provider.ContainsLogInCategory(categoryContains, messageContains, comparison))
            {
                var existingLogs = FormatLogs(provider.AllLogs);
                throw new AssertionException(
                    $"Expected a log in category containing '{categoryContains}' with message containing '{messageContains}', " +
                    $"but no such log was found.\n" +
                    $"Captured logs:\n{existingLogs}");
            }
        }

        /// <summary>
        /// Asserts that no errors were logged across all categories.
        /// </summary>
        /// <param name="provider">The logger provider to check.</param>
        /// <exception cref="AssertionException">Thrown when an error or critical log was found.</exception>
        public static void AssertNoErrorsLogged(InMemoryLoggerProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            if (provider.HasErrors())
            {
                var errors = provider.GetLogs(l => l.LogLevel >= LogLevel.Error);
                var errorLogs = FormatLogs(errors);
                throw new AssertionException(
                    $"Expected no errors to be logged, but found {errors.Count}:\n{errorLogs}");
            }
        }

        /// <summary>
        /// Asserts that no warnings or errors were logged across all categories.
        /// </summary>
        /// <param name="provider">The logger provider to check.</param>
        /// <exception cref="AssertionException">Thrown when a warning or higher log was found.</exception>
        public static void AssertNoWarningsOrErrorsLogged(InMemoryLoggerProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            if (provider.HasWarnings())
            {
                var warnings = provider.GetLogs(l => l.LogLevel >= LogLevel.Warning);
                var warningLogs = FormatLogs(warnings);
                throw new AssertionException(
                    $"Expected no warnings or errors to be logged, but found {warnings.Count}:\n{warningLogs}");
            }
        }

        /// <summary>
        /// Asserts that the specified category has at least one log.
        /// </summary>
        /// <param name="provider">The logger provider to check.</param>
        /// <param name="categoryContains">Part of the category name to match.</param>
        /// <exception cref="AssertionException">Thrown when no log in that category was found.</exception>
        public static void AssertCategoryHasLogs(InMemoryLoggerProvider provider, string categoryContains)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (string.IsNullOrEmpty(categoryContains)) throw new ArgumentNullException(nameof(categoryContains));

            if (!provider.GetLogsForCategory(categoryContains).Any())
            {
                var categories = string.Join(", ", provider.Categories);
                throw new AssertionException(
                    $"Expected at least one log in category containing '{categoryContains}', but none were found.\n" +
                    $"Available categories: [{categories}]");
            }
        }

        #endregion

        #region Helpers

        private static string FormatLogs(IEnumerable<LogEntry> logs)
        {
            var entries = logs.Take(20).Select(l => $"  {l}");
            var result = string.Join("\n", entries);
            
            var count = logs.Count();
            if (count > 20)
            {
                result += $"\n  ... and {count - 20} more";
            }

            return result.Length > 0 ? result : "  (none)";
        }

        #endregion
    }
}

