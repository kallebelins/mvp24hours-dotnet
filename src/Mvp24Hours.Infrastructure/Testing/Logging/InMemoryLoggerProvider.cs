//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Testing.Logging
{
    /// <summary>
    /// An in-memory ILoggerProvider for capturing logs across all categories in integration tests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider captures all logs from all categories and provides methods to query
    /// them for assertions. It's particularly useful for integration tests where you want
    /// to verify logging behavior across multiple components.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Setup in test
    /// var loggerProvider = new InMemoryLoggerProvider();
    /// var services = new ServiceCollection();
    /// services.AddLogging(builder => builder.AddProvider(loggerProvider));
    /// 
    /// // ... run test ...
    /// 
    /// // Verify logs
    /// Assert.True(loggerProvider.ContainsLog(LogLevel.Information, "Order created"));
    /// Assert.True(loggerProvider.ContainsLogInCategory("OrderService", "Processing"));
    /// </code>
    /// </example>
    public sealed class InMemoryLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, InMemoryCategoryLogger> _loggers = new();
        private readonly ConcurrentBag<CategorizedLogEntry> _allLogs = new();
        private LogLevel _minimumLevel = LogLevel.Trace;
        private bool _disposed;

        /// <summary>
        /// Gets or sets the minimum log level to capture across all loggers.
        /// </summary>
        public LogLevel MinimumLevel
        {
            get => _minimumLevel;
            set
            {
                _minimumLevel = value;
                foreach (var logger in _loggers.Values)
                {
                    logger.MinimumLevel = value;
                }
            }
        }

        /// <summary>
        /// Gets all captured log entries from all categories.
        /// </summary>
        public IReadOnlyList<CategorizedLogEntry> AllLogs => _allLogs.ToList().AsReadOnly();

        /// <summary>
        /// Gets the total count of captured logs.
        /// </summary>
        public int LogCount => _allLogs.Count;

        /// <summary>
        /// Gets the category names of all loggers that have been created.
        /// </summary>
        public IReadOnlyList<string> Categories => _loggers.Keys.ToList().AsReadOnly();

        /// <summary>
        /// Event raised when a new log entry is added.
        /// </summary>
        public event EventHandler<CategorizedLogEntry>? LogAdded;

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name =>
            {
                var logger = new InMemoryCategoryLogger(name, this);
                logger.MinimumLevel = _minimumLevel;
                return logger;
            });
        }

        /// <summary>
        /// Records a log entry from a category logger.
        /// </summary>
        internal void RecordLog(CategorizedLogEntry entry)
        {
            _allLogs.Add(entry);
            LogAdded?.Invoke(this, entry);
        }

        /// <summary>
        /// Gets all logs for a specific category.
        /// </summary>
        /// <param name="categoryName">The category name (or part of it).</param>
        /// <param name="exactMatch">If true, matches exact category name. If false, uses Contains.</param>
        /// <returns>A list of matching log entries.</returns>
        public IReadOnlyList<CategorizedLogEntry> GetLogsForCategory(string categoryName, bool exactMatch = false)
        {
            return _allLogs
                .Where(l => exactMatch
                    ? l.CategoryName == categoryName
                    : l.CategoryName.Contains(categoryName, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Gets all logs at the specified level.
        /// </summary>
        /// <param name="logLevel">The log level to filter by.</param>
        /// <returns>A list of matching log entries.</returns>
        public IReadOnlyList<CategorizedLogEntry> GetLogs(LogLevel logLevel)
        {
            return _allLogs.Where(l => l.LogLevel == logLevel).ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets all logs containing the specified message text.
        /// </summary>
        /// <param name="messageContains">Text to search for in log messages.</param>
        /// <param name="comparison">String comparison type.</param>
        /// <returns>A list of matching log entries.</returns>
        public IReadOnlyList<CategorizedLogEntry> GetLogs(string messageContains, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return _allLogs
                .Where(l => l.Message.Contains(messageContains, comparison))
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Gets all logs matching a predicate.
        /// </summary>
        /// <param name="predicate">The predicate to filter logs.</param>
        /// <returns>A list of matching log entries.</returns>
        public IReadOnlyList<CategorizedLogEntry> GetLogs(Func<CategorizedLogEntry, bool> predicate)
        {
            return _allLogs.Where(predicate).ToList().AsReadOnly();
        }

        /// <summary>
        /// Checks if any log was recorded at the specified level containing the specified message.
        /// </summary>
        /// <param name="logLevel">The log level to check.</param>
        /// <param name="messageContains">Text to search for in log messages.</param>
        /// <param name="comparison">String comparison type.</param>
        /// <returns>True if a matching log was found.</returns>
        public bool ContainsLog(LogLevel logLevel, string messageContains, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return _allLogs.Any(l =>
                l.LogLevel == logLevel &&
                l.Message.Contains(messageContains, comparison));
        }

        /// <summary>
        /// Checks if any log was recorded containing the specified message.
        /// </summary>
        /// <param name="messageContains">Text to search for in log messages.</param>
        /// <param name="comparison">String comparison type.</param>
        /// <returns>True if a matching log was found.</returns>
        public bool ContainsLog(string messageContains, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return _allLogs.Any(l => l.Message.Contains(messageContains, comparison));
        }

        /// <summary>
        /// Checks if any log was recorded in the specified category containing the specified message.
        /// </summary>
        /// <param name="categoryNameContains">The category name or part of it.</param>
        /// <param name="messageContains">Text to search for in log messages.</param>
        /// <param name="comparison">String comparison type.</param>
        /// <returns>True if a matching log was found.</returns>
        public bool ContainsLogInCategory(string categoryNameContains, string messageContains, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return _allLogs.Any(l =>
                l.CategoryName.Contains(categoryNameContains, comparison) &&
                l.Message.Contains(messageContains, comparison));
        }

        /// <summary>
        /// Checks if any error or critical log was recorded.
        /// </summary>
        /// <returns>True if an error or critical log was found.</returns>
        public bool HasErrors()
        {
            return _allLogs.Any(l => l.LogLevel >= LogLevel.Error);
        }

        /// <summary>
        /// Checks if any warning or higher level log was recorded.
        /// </summary>
        /// <returns>True if a warning or higher log was found.</returns>
        public bool HasWarnings()
        {
            return _allLogs.Any(l => l.LogLevel >= LogLevel.Warning);
        }

        /// <summary>
        /// Gets all logs with exceptions.
        /// </summary>
        /// <returns>A list of log entries that have exceptions.</returns>
        public IReadOnlyList<CategorizedLogEntry> GetLogsWithExceptions()
        {
            return _allLogs.Where(l => l.Exception != null).ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets the count of logs at each level.
        /// </summary>
        /// <returns>A dictionary of log level to count.</returns>
        public IReadOnlyDictionary<LogLevel, int> GetLogCountsByLevel()
        {
            return _allLogs
                .GroupBy(l => l.LogLevel)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Gets the count of logs for each category.
        /// </summary>
        /// <returns>A dictionary of category name to count.</returns>
        public IReadOnlyDictionary<string, int> GetLogCountsByCategory()
        {
            return _allLogs
                .GroupBy(l => l.CategoryName)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Clears all captured logs from all categories.
        /// </summary>
        public void Clear()
        {
            _allLogs.Clear();
            foreach (var logger in _loggers.Values)
            {
                logger.Clear();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _loggers.Clear();
                _disposed = true;
            }
        }

        /// <summary>
        /// Internal logger implementation for a specific category.
        /// </summary>
        private sealed class InMemoryCategoryLogger : ILogger
        {
            private readonly string _categoryName;
            private readonly InMemoryLoggerProvider _provider;
            private readonly ConcurrentStack<object?> _scopes = new();

            public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

            public InMemoryCategoryLogger(string categoryName, InMemoryLoggerProvider provider)
            {
                _categoryName = categoryName;
                _provider = provider;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            {
                _scopes.Push(state);
                return new ScopeDisposable(() => _scopes.TryPop(out _));
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return logLevel >= MinimumLevel && logLevel != LogLevel.None;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel))
                    return;

                var message = formatter(state, exception);
                var currentScopes = _scopes.ToArray().Reverse();
                var entry = new CategorizedLogEntry(
                    _categoryName,
                    logLevel,
                    eventId,
                    message,
                    exception,
                    state,
                    currentScopes);

                _provider.RecordLog(entry);
            }

            public void Clear()
            {
                _scopes.Clear();
            }

            private sealed class ScopeDisposable : IDisposable
            {
                private readonly Action _onDispose;
                private bool _disposed;

                public ScopeDisposable(Action onDispose)
                {
                    _onDispose = onDispose;
                }

                public void Dispose()
                {
                    if (!_disposed)
                    {
                        _onDispose();
                        _disposed = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Represents a log entry with its category name.
    /// </summary>
    public sealed class CategorizedLogEntry : LogEntry
    {
        /// <summary>
        /// Gets the category name of the logger.
        /// </summary>
        public string CategoryName { get; }

        /// <summary>
        /// Creates a new categorized log entry.
        /// </summary>
        public CategorizedLogEntry(
            string categoryName,
            LogLevel logLevel,
            EventId eventId,
            string message,
            Exception? exception,
            object? state,
            IEnumerable<object?>? scopes = null)
            : base(logLevel, eventId, message, exception, state, scopes)
        {
            CategoryName = categoryName;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var exceptionPart = Exception != null ? $" [Exception: {Exception.Message}]" : "";
            return $"[{Timestamp:HH:mm:ss.fff}] [{LogLevel}] [{CategoryName}] {Message}{exceptionPart}";
        }
    }
}

