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
    /// Represents a single log entry captured by FakeLogger.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Gets the log level.
        /// </summary>
        public LogLevel LogLevel { get; }

        /// <summary>
        /// Gets the event ID.
        /// </summary>
        public EventId EventId { get; }

        /// <summary>
        /// Gets the log message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the exception, if any.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Gets the state object for structured logging.
        /// </summary>
        public object? State { get; }

        /// <summary>
        /// Gets the timestamp when the log was recorded.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Gets the scope stack at the time of logging.
        /// </summary>
        public IReadOnlyList<object?> Scopes { get; }

        /// <summary>
        /// Creates a new log entry.
        /// </summary>
        public LogEntry(
            LogLevel logLevel,
            EventId eventId,
            string message,
            Exception? exception,
            object? state,
            IEnumerable<object?>? scopes = null)
        {
            LogLevel = logLevel;
            EventId = eventId;
            Message = message;
            Exception = exception;
            State = state;
            Timestamp = DateTimeOffset.UtcNow;
            Scopes = scopes?.ToList().AsReadOnly() ?? (IReadOnlyList<object?>)Array.Empty<object?>();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var exceptionPart = Exception != null ? $" [Exception: {Exception.Message}]" : "";
            return $"[{Timestamp:HH:mm:ss.fff}] [{LogLevel}] {Message}{exceptionPart}";
        }
    }

    /// <summary>
    /// A fake ILogger implementation that captures log entries for assertions in tests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// FakeLogger provides a way to capture and verify log entries in unit and integration tests.
    /// It supports all ILogger functionality including scopes, structured logging, and filtering by log level.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a fake logger
    /// var logger = new FakeLogger&lt;MyService&gt;();
    /// 
    /// // Inject into service under test
    /// var service = new MyService(logger);
    /// 
    /// // Perform action
    /// await service.ProcessAsync();
    /// 
    /// // Verify logs
    /// Assert.True(logger.ContainsLog(LogLevel.Information, "Processing started"));
    /// Assert.Equal(2, logger.GetLogs(LogLevel.Debug).Count);
    /// </code>
    /// </example>
    /// <typeparam name="T">The type for the logger category.</typeparam>
    public sealed class FakeLogger<T> : FakeLogger, ILogger<T>
    {
        /// <summary>
        /// Creates a new instance of FakeLogger with the category name based on type T.
        /// </summary>
        public FakeLogger() : base(typeof(T).FullName ?? typeof(T).Name)
        {
        }
    }

    /// <summary>
    /// A fake ILogger implementation that captures log entries for assertions in tests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// FakeLogger provides a way to capture and verify log entries in unit and integration tests.
    /// It supports all ILogger functionality including scopes, structured logging, and filtering by log level.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var logger = new FakeLogger("TestCategory");
    /// 
    /// logger.LogInformation("User {UserId} logged in", userId);
    /// 
    /// Assert.True(logger.ContainsLog(LogLevel.Information, "logged in"));
    /// Assert.Equal(1, logger.LogCount);
    /// </code>
    /// </example>
    public class FakeLogger : ILogger
    {
        private readonly ConcurrentBag<LogEntry> _logs = new();
        private readonly ConcurrentStack<object?> _scopes = new();
        private readonly string _categoryName;
        private LogLevel _minimumLevel = LogLevel.Trace;

        /// <summary>
        /// Gets or sets the minimum log level to capture. Logs below this level are ignored.
        /// </summary>
        public LogLevel MinimumLevel
        {
            get => _minimumLevel;
            set => _minimumLevel = value;
        }

        /// <summary>
        /// Gets the category name for this logger.
        /// </summary>
        public string CategoryName => _categoryName;

        /// <summary>
        /// Gets the total number of logs captured.
        /// </summary>
        public int LogCount => _logs.Count;

        /// <summary>
        /// Gets all captured log entries.
        /// </summary>
        public IReadOnlyList<LogEntry> Logs => _logs.ToList().AsReadOnly();

        /// <summary>
        /// Event raised when a new log entry is added.
        /// </summary>
        public event EventHandler<LogEntry>? LogAdded;

        /// <summary>
        /// Creates a new instance of FakeLogger.
        /// </summary>
        /// <param name="categoryName">The category name for this logger.</param>
        public FakeLogger(string? categoryName = null)
        {
            _categoryName = categoryName ?? "FakeLogger";
        }

        /// <inheritdoc />
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            _scopes.Push(state);
            return new ScopeDisposable(() => _scopes.TryPop(out _));
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _minimumLevel && logLevel != LogLevel.None;
        }

        /// <inheritdoc />
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
            var entry = new LogEntry(logLevel, eventId, message, exception, state, currentScopes);
            
            _logs.Add(entry);
            LogAdded?.Invoke(this, entry);
        }

        /// <summary>
        /// Gets all logs at the specified level.
        /// </summary>
        /// <param name="logLevel">The log level to filter by.</param>
        /// <returns>A list of matching log entries.</returns>
        public IReadOnlyList<LogEntry> GetLogs(LogLevel logLevel)
        {
            return _logs.Where(l => l.LogLevel == logLevel).ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets all logs at the specified level or higher.
        /// </summary>
        /// <param name="minimumLevel">The minimum log level.</param>
        /// <returns>A list of matching log entries.</returns>
        public IReadOnlyList<LogEntry> GetLogsAtOrAbove(LogLevel minimumLevel)
        {
            return _logs.Where(l => l.LogLevel >= minimumLevel).ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets all logs containing the specified message text.
        /// </summary>
        /// <param name="messageContains">Text to search for in log messages.</param>
        /// <param name="comparison">String comparison type.</param>
        /// <returns>A list of matching log entries.</returns>
        public IReadOnlyList<LogEntry> GetLogs(string messageContains, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return _logs
                .Where(l => l.Message.Contains(messageContains, comparison))
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Gets all logs matching a predicate.
        /// </summary>
        /// <param name="predicate">The predicate to filter logs.</param>
        /// <returns>A list of matching log entries.</returns>
        public IReadOnlyList<LogEntry> GetLogs(Func<LogEntry, bool> predicate)
        {
            return _logs.Where(predicate).ToList().AsReadOnly();
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
            return _logs.Any(l =>
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
            return _logs.Any(l => l.Message.Contains(messageContains, comparison));
        }

        /// <summary>
        /// Checks if any log with an exception was recorded.
        /// </summary>
        /// <typeparam name="TException">The type of exception to check for.</typeparam>
        /// <returns>True if a log with the specified exception type was found.</returns>
        public bool ContainsException<TException>() where TException : Exception
        {
            return _logs.Any(l => l.Exception is TException);
        }

        /// <summary>
        /// Gets the last log entry, or null if no logs exist.
        /// </summary>
        public LogEntry? LastLog => _logs.LastOrDefault();

        /// <summary>
        /// Gets the first log entry, or null if no logs exist.
        /// </summary>
        public LogEntry? FirstLog => _logs.FirstOrDefault();

        /// <summary>
        /// Clears all captured logs.
        /// </summary>
        public void Clear()
        {
            _logs.Clear();
        }

        /// <summary>
        /// Gets logs with exceptions.
        /// </summary>
        /// <returns>A list of log entries that have exceptions.</returns>
        public IReadOnlyList<LogEntry> GetLogsWithExceptions()
        {
            return _logs.Where(l => l.Exception != null).ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets the count of logs at each level.
        /// </summary>
        /// <returns>A dictionary of log level to count.</returns>
        public IReadOnlyDictionary<LogLevel, int> GetLogCountsByLevel()
        {
            return _logs
                .GroupBy(l => l.LogLevel)
                .ToDictionary(g => g.Key, g => g.Count());
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

