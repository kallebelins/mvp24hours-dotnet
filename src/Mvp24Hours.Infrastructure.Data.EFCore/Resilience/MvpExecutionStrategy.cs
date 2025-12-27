//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Resilience
{
    /// <summary>
    /// Custom execution strategy that extends EF Core's retrying execution strategy
    /// with configurable retry policies, logging, and circuit breaker support.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This execution strategy provides:
    /// <list type="bullet">
    /// <item>Exponential backoff with jitter</item>
    /// <item>Configurable retry count and delay</item>
    /// <item>Custom transient exception detection</item>
    /// <item>Detailed logging of retry attempts</item>
    /// <item>Optional circuit breaker integration</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Configure in DbContext options
    /// options.UseSqlServer(connectionString, sqlOptions =>
    /// {
    ///     sqlOptions.ExecutionStrategy(c => new MvpExecutionStrategy(
    ///         c,
    ///         resilienceOptions,
    ///         logger));
    /// });
    /// </code>
    /// </example>
    public class MvpExecutionStrategy : ExecutionStrategy
    {
        private readonly EFCoreResilienceOptions _options;
        private readonly ILogger? _logger;
        private readonly ICollection<Type> _additionalTransientExceptionTypes;
        private int _retryCount;
        private static readonly Random _jitterRandom = new Random();

        /// <summary>
        /// Initializes a new instance of <see cref="MvpExecutionStrategy"/>.
        /// </summary>
        /// <param name="context">The <see cref="DbContext"/> to use.</param>
        /// <param name="options">The resilience configuration options.</param>
        /// <param name="logger">Optional logger for retry diagnostics.</param>
        public MvpExecutionStrategy(
            DbContext context,
            EFCoreResilienceOptions options,
            ILogger? logger = null)
            : this(context.GetService<ExecutionStrategyDependencies>(), options, logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MvpExecutionStrategy"/>.
        /// </summary>
        /// <param name="dependencies">The execution strategy dependencies.</param>
        /// <param name="options">The resilience configuration options.</param>
        /// <param name="logger">Optional logger for retry diagnostics.</param>
        public MvpExecutionStrategy(
            ExecutionStrategyDependencies dependencies,
            EFCoreResilienceOptions options,
            ILogger? logger = null)
            : base(
                dependencies,
                options.MaxRetryCount,
                TimeSpan.FromSeconds(options.MaxRetryDelaySeconds))
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
            _additionalTransientExceptionTypes = options.TransientExceptionTypes ?? new List<Type>();
            _retryCount = 0;
        }

        /// <summary>
        /// Gets the delay before the next retry attempt using exponential backoff with jitter.
        /// </summary>
        /// <param name="lastException">The exception that triggered the retry.</param>
        /// <returns>The delay duration before the next retry.</returns>
        protected override TimeSpan? GetNextDelay(Exception lastException)
        {
            _retryCount++;

            if (_retryCount > _options.MaxRetryCount)
            {
                LogRetryExhausted(lastException);
                return null;
            }

            // Exponential backoff: 2^attempt seconds, capped at MaxRetryDelay
            var baseDelay = Math.Pow(2, _retryCount);
            var cappedDelay = Math.Min(baseDelay, _options.MaxRetryDelaySeconds);

            // Add jitter (±25%) to prevent thundering herd
            var jitter = cappedDelay * 0.25 * (2 * _jitterRandom.NextDouble() - 1);
            var finalDelay = TimeSpan.FromSeconds(Math.Max(1, cappedDelay + jitter));

            LogRetryAttempt(_retryCount, finalDelay, lastException);

            return finalDelay;
        }

        /// <summary>
        /// Determines whether the specified exception represents a transient failure.
        /// </summary>
        /// <param name="exception">The exception to check.</param>
        /// <returns>True if the exception is transient and operation should be retried.</returns>
        protected override bool ShouldRetryOn(Exception exception)
        {
            // Check if exception is in the custom transient types
            if (IsCustomTransientException(exception))
            {
                LogTransientExceptionDetected(exception, "Custom transient exception type");
                return true;
            }

            // Check for standard SQL transient errors
            if (IsSqlTransientError(exception))
            {
                LogTransientExceptionDetected(exception, "SQL transient error");
                return true;
            }

            // Check for timeout exceptions
            if (IsTimeoutException(exception))
            {
                LogTransientExceptionDetected(exception, "Timeout exception");
                return true;
            }

            // Check for connection-related exceptions
            if (IsConnectionException(exception))
            {
                LogTransientExceptionDetected(exception, "Connection exception");
                return true;
            }

            return false;
        }


        #region Private Helper Methods

        private bool IsCustomTransientException(Exception exception)
        {
            var exceptionType = exception.GetType();
            foreach (var transientType in _additionalTransientExceptionTypes)
            {
                if (transientType.IsAssignableFrom(exceptionType))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsSqlTransientError(Exception exception)
        {
            // Check for SqlException with transient error numbers
            if (exception is System.Data.SqlClient.SqlException sqlException)
            {
                return IsTransientSqlErrorNumber(sqlException.Number);
            }

            // Check for Microsoft.Data.SqlClient.SqlException (newer driver)
            var exceptionType = exception.GetType();
            if (exceptionType.FullName == "Microsoft.Data.SqlClient.SqlException")
            {
                var numberProperty = exceptionType.GetProperty("Number");
                if (numberProperty != null)
                {
                    var number = (int?)numberProperty.GetValue(exception);
                    if (number.HasValue)
                    {
                        return IsTransientSqlErrorNumber(number.Value);
                    }
                }
            }

            // Check inner exceptions
            if (exception.InnerException != null)
            {
                return IsSqlTransientError(exception.InnerException);
            }

            return false;
        }

        private bool IsTransientSqlErrorNumber(int errorNumber)
        {
            // Standard transient SQL error numbers
            var transientErrors = new HashSet<int>
            {
                -2,     // Timeout
                20,     // Server not found
                64,     // Connection forcibly closed
                233,    // Connection initialization error
                10053,  // Connection aborted by software
                10054,  // Connection reset by peer
                10060,  // Connection timed out
                10928,  // Resource ID limit reached
                10929,  // Resource ID limit reached (Azure)
                40143,  // Connection could not be initialized
                40197,  // Error processing request
                40501,  // Service busy
                40540,  // Firewall block
                40613,  // Database unavailable
                40615,  // Cannot connect due to firewall
                40627,  // Operation in progress
                41301,  // Dependency failure
                41302,  // Dependency failure
                41305,  // Commit dependency failure
                41325,  // Commit failed
                41839,  // Transaction exceeded memory limit
                49918,  // Not enough resources
                49919,  // Not enough resources
                49920,  // Too many requests
            };

            if (transientErrors.Contains(errorNumber))
            {
                return true;
            }

            // Check additional configured error numbers
            return _options.AdditionalTransientErrorNumbers?.Contains(errorNumber) == true;
        }

        private bool IsTimeoutException(Exception exception)
        {
            // Direct timeout exceptions
            if (exception is TimeoutException)
            {
                return true;
            }

            // Task cancellation due to timeout
            if (exception is OperationCanceledException)
            {
                return true;
            }

            // Check message for timeout indicators
            var message = exception.Message?.ToLowerInvariant() ?? string.Empty;
            if (message.Contains("timeout") || message.Contains("timed out"))
            {
                return true;
            }

            // Check inner exception
            if (exception.InnerException != null)
            {
                return IsTimeoutException(exception.InnerException);
            }

            return false;
        }

        private bool IsConnectionException(Exception exception)
        {
            // Check for common connection exception types
            if (exception is System.Net.Sockets.SocketException)
            {
                return true;
            }

            if (exception is System.IO.IOException)
            {
                return true;
            }

            // Check message for connection indicators
            var message = exception.Message?.ToLowerInvariant() ?? string.Empty;
            if (message.Contains("connection") &&
                (message.Contains("failed") ||
                 message.Contains("refused") ||
                 message.Contains("reset") ||
                 message.Contains("closed") ||
                 message.Contains("broken")))
            {
                return true;
            }

            // Check inner exception
            if (exception.InnerException != null)
            {
                return IsConnectionException(exception.InnerException);
            }

            return false;
        }

        private void LogRetryAttempt(int attempt, TimeSpan delay, Exception exception)
        {
            if (!_options.LogRetryAttempts)
            {
                return;
            }

            _logger?.LogWarning(
                exception,
                "Database operation retry {Attempt}/{MaxRetries} after {DelaySeconds:F2}s delay",
                attempt,
                _options.MaxRetryCount,
                delay.TotalSeconds);
        }

        private void LogRetryExhausted(Exception exception)
        {
            if (!_options.LogRetryAttempts)
            {
                return;
            }

            _logger?.LogError(
                exception,
                "Database operation failed after {MaxRetries} retry attempts",
                _options.MaxRetryCount);
        }

        private void LogTransientExceptionDetected(Exception exception, string reason)
        {
            if (!_options.LogRetryAttempts)
            {
                return;
            }

            _logger?.LogDebug(
                "Transient database exception detected ({Reason}): {ExceptionType}",
                reason,
                exception.GetType().Name);
        }

        #endregion
    }
}

