//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Resiliency
{
    /// <summary>
    /// Implements retry policy for MongoDB operations with configurable backoff strategies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This policy handles transient failures by automatically retrying operations.
    /// It supports:
    /// <list type="bullet">
    ///   <item><b>Exponential Backoff</b>: Delay doubles with each retry</item>
    ///   <item><b>Jitter</b>: Random variation to prevent thundering herd</item>
    ///   <item><b>Maximum Delay Cap</b>: Prevents excessive wait times</item>
    ///   <item><b>Exception Filtering</b>: Only retries transient exceptions</item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class MongoDbRetryPolicy
    {
        private readonly MongoDbResiliencyOptions _options;
        private readonly Random _random = new();
        private readonly HashSet<Type> _retryableExceptions;
        private readonly HashSet<Type> _nonRetryableExceptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbRetryPolicy"/> class.
        /// </summary>
        /// <param name="options">The resiliency options.</param>
        public MongoDbRetryPolicy(MongoDbResiliencyOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            // Build retryable exceptions set
            _retryableExceptions = new HashSet<Type>
            {
                typeof(MongoConnectionException),
                typeof(MongoNotPrimaryException),
                typeof(MongoNodeIsRecoveringException),
                typeof(TimeoutException),
                typeof(MongoIncompatibleDriverException)
            };

            foreach (var type in _options.AdditionalRetryableExceptions)
            {
                _retryableExceptions.Add(type);
            }

            // Build non-retryable exceptions set
            _nonRetryableExceptions = new HashSet<Type>
            {
                typeof(MongoDuplicateKeyException),
                typeof(MongoWriteException),
                typeof(MongoCommandException),
                typeof(MongoBulkWriteException<>).GetGenericTypeDefinition()
            };

            foreach (var type in _options.NonRetryableExceptions)
            {
                _nonRetryableExceptions.Add(type);
            }
        }

        /// <summary>
        /// Executes an operation with retry policy applied.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="MongoDbRetryExhaustedException">Thrown when all retries are exhausted.</exception>
        public async Task<TResult> ExecuteAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            if (!_options.EnableRetry)
            {
                return await operation(cancellationToken);
            }

            var startTime = DateTimeOffset.UtcNow;
            Exception lastException = null;
            var attempt = 0;

            while (attempt <= _options.RetryCount)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (attempt > 0 && _options.LogRetryAttempts)
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Verbose,
                            "mongodb-retry-attempt",
                            new
                            {
                                Attempt = attempt,
                                MaxAttempts = _options.RetryCount,
                                LastException = lastException?.GetType().Name
                            });
                    }

                    return await operation(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    // Check if we should retry this exception
                    if (!ShouldRetry(ex, attempt))
                    {
                        // Non-retryable exception, throw immediately
                        throw;
                    }

                    attempt++;

                    if (attempt > _options.RetryCount)
                    {
                        // Max retries exceeded
                        break;
                    }

                    var delay = CalculateDelay(attempt);
                    await Task.Delay(delay, cancellationToken);
                }
            }

            var totalDuration = DateTimeOffset.UtcNow - startTime;

            if (_options.LogRetryAttempts)
            {
                TelemetryHelper.Execute(TelemetryLevels.Warning,
                    "mongodb-retry-exhausted",
                    new
                    {
                        TotalAttempts = attempt,
                        TotalDuration = totalDuration.TotalMilliseconds,
                        LastException = lastException?.GetType().Name,
                        LastExceptionMessage = lastException?.Message
                    });
            }

            throw new MongoDbRetryExhaustedException(attempt, totalDuration, lastException);
        }

        /// <summary>
        /// Executes an operation with retry policy applied.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="MongoDbRetryExhaustedException">Thrown when all retries are exhausted.</exception>
        public async Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            await ExecuteAsync(async ct =>
            {
                await operation(ct);
                return true;
            }, cancellationToken);
        }

        /// <summary>
        /// Determines if an exception should trigger a retry.
        /// </summary>
        /// <param name="exception">The exception to evaluate.</param>
        /// <param name="currentAttempt">The current attempt number.</param>
        /// <returns>True if the operation should be retried.</returns>
        public bool ShouldRetry(Exception exception, int currentAttempt)
        {
            // currentAttempt starts at 0, so with RetryCount=3, we allow retries at attempts 0, 1, 2
            if (currentAttempt >= _options.RetryCount)
                return false;

            var exceptionType = exception.GetType();

            // Check if explicitly non-retryable
            if (_nonRetryableExceptions.Contains(exceptionType))
                return false;

            // Check if explicitly retryable
            if (_retryableExceptions.Contains(exceptionType))
                return true;

            // Check base types for retryable
            foreach (var retryableType in _retryableExceptions)
            {
                if (retryableType.IsAssignableFrom(exceptionType))
                    return true;
            }

            // Check for transient MongoDB errors
            if (exception is MongoException mongoEx)
            {
                return IsTransientMongoError(mongoEx);
            }

            // Check inner exception
            if (exception.InnerException != null)
            {
                return ShouldRetry(exception.InnerException, currentAttempt);
            }

            return false;
        }

        /// <summary>
        /// Calculates the delay before the next retry attempt.
        /// </summary>
        /// <param name="attempt">The current attempt number (1-based).</param>
        /// <returns>The delay duration.</returns>
        public TimeSpan CalculateDelay(int attempt)
        {
            double baseDelay = _options.RetryBaseDelayMilliseconds;
            double delay;

            if (_options.UseExponentialBackoff)
            {
                // Exponential backoff: base * 2^(attempt-1)
                delay = baseDelay * Math.Pow(2, attempt - 1);
            }
            else
            {
                // Constant delay
                delay = baseDelay;
            }

            // Apply jitter if configured
            if (_options.RetryJitterFactor > 0)
            {
                var jitter = delay * _options.RetryJitterFactor;
                var randomJitter = (_random.NextDouble() * 2 - 1) * jitter; // -jitter to +jitter
                delay += randomJitter;
            }

            // Cap at maximum delay
            delay = Math.Min(delay, _options.RetryMaxDelayMilliseconds);

            // Ensure minimum of 1ms
            delay = Math.Max(delay, 1);

            return TimeSpan.FromMilliseconds(delay);
        }

        private static bool IsTransientMongoError(MongoException exception)
        {
            // Check for retryable server error codes
            // See: https://github.com/mongodb/specifications/blob/master/source/retryable-writes/retryable-writes.rst

            if (exception is MongoWriteConcernException writeConcernEx)
            {
                // WriteConcernResult may contain error information
                var result = writeConcernEx.WriteConcernResult;
                if (result != null && result.Response != null && result.Response.Contains("code"))
                {
                    var code = result.Response["code"].AsInt32;
                    return IsRetryableErrorCode(code);
                }
                return false;
            }

            if (exception is MongoCommandException commandEx)
            {
                return IsRetryableErrorCode(commandEx.Code);
            }

            // Connection exceptions are generally transient
            return exception is MongoConnectionException;
        }

        private static bool IsRetryableErrorCode(int? code)
        {
            if (!code.HasValue)
                return false;

            // Retryable error codes from MongoDB specification
            return code.Value switch
            {
                // HostUnreachable
                6 => true,
                // HostNotFound
                7 => true,
                // NetworkTimeout
                89 => true,
                // ShutdownInProgress
                91 => true,
                // PrimarySteppedDown
                189 => true,
                // ExceededTimeLimit
                262 => true,
                // SocketException
                9001 => true,
                // NotMaster/NotPrimary
                10107 => true,
                // InterruptedAtShutdown
                11600 => true,
                // InterruptedDueToReplStateChange
                11602 => true,
                // NotMasterNoSlaveOk/NotPrimaryNoSecondaryOk
                13435 => true,
                // NotMasterOrSecondary/NotPrimaryOrSecondary
                13436 => true,
                _ => false
            };
        }
    }
}

