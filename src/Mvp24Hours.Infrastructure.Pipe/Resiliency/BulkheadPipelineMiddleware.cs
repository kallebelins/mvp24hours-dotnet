//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Resiliency
{
    /// <summary>
    /// Tracks the state of a bulkhead (semaphore with queue).
    /// </summary>
    internal class BulkheadState : IDisposable
    {
        private readonly SemaphoreSlim _executionSemaphore;
        private readonly SemaphoreSlim _queueSemaphore;
        private readonly BulkheadOptions _options;
        private int _queuedCount;
        private bool _disposed;

        public BulkheadState(BulkheadOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _executionSemaphore = new SemaphoreSlim(options.MaxConcurrency, options.MaxConcurrency);

            // Queue semaphore controls how many can wait
            var totalPermits = options.MaxConcurrency + options.QueueLimit;
            _queueSemaphore = new SemaphoreSlim(totalPermits, totalPermits);
        }

        public int CurrentConcurrency => _options.MaxConcurrency - _executionSemaphore.CurrentCount;
        public int QueuedCount => _queuedCount;
        public int AvailableSlots => _executionSemaphore.CurrentCount;
        public int AvailableQueueSlots => _queueSemaphore.CurrentCount - _executionSemaphore.CurrentCount;

        public async Task<bool> TryEnterAsync(
            IBulkheadOperation? operation,
            CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var timeout = operation?.QueueTimeout ?? _options.QueueTimeout;

            // Try to acquire a slot (execution or queue)
            bool acquired;
            if (timeout.HasValue)
            {
                acquired = await _queueSemaphore.WaitAsync(timeout.Value, cancellationToken);
            }
            else
            {
                await _queueSemaphore.WaitAsync(cancellationToken);
                acquired = true;
            }

            if (!acquired)
            {
                // Queue timeout
                operation?.OnRejected();
                _options.OnRejected?.Invoke();
                return false;
            }

            // Check if we can execute immediately
            if (_executionSemaphore.CurrentCount > 0)
            {
                await _executionSemaphore.WaitAsync(cancellationToken);
                return true;
            }

            // Check if queueing is allowed
            if (_options.QueueLimit <= 0)
            {
                _queueSemaphore.Release();
                operation?.OnRejected();
                _options.OnRejected?.Invoke();
                return false;
            }

            // We're queued
            var queuePosition = Interlocked.Increment(ref _queuedCount);
            operation?.OnQueued(queuePosition);
            _options.OnQueued?.Invoke(queuePosition);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Wait for execution slot
                if (timeout.HasValue)
                {
                    var remainingTimeout = timeout.Value - stopwatch.Elapsed;
                    if (remainingTimeout <= TimeSpan.Zero)
                    {
                        _queueSemaphore.Release();
                        Interlocked.Decrement(ref _queuedCount);
                        operation?.OnRejected();
                        _options.OnRejected?.Invoke();
                        return false;
                    }

                    acquired = await _executionSemaphore.WaitAsync(remainingTimeout, cancellationToken);
                }
                else
                {
                    await _executionSemaphore.WaitAsync(cancellationToken);
                    acquired = true;
                }
            }
            finally
            {
                Interlocked.Decrement(ref _queuedCount);
            }

            if (!acquired)
            {
                _queueSemaphore.Release();
                operation?.OnRejected();
                _options.OnRejected?.Invoke();
                return false;
            }

            stopwatch.Stop();
            operation?.OnDequeued(stopwatch.Elapsed);
            _options.OnDequeued?.Invoke(stopwatch.Elapsed);

            return true;
        }

        public void Exit()
        {
            if (!_disposed)
            {
                _executionSemaphore.Release();
                _queueSemaphore.Release();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _executionSemaphore.Dispose();
                _queueSemaphore.Dispose();
            }
        }
    }

    /// <summary>
    /// Middleware that implements bulkhead (isolation) pattern for operations.
    /// Limits concurrent execution to prevent resource exhaustion.
    /// </summary>
    public class BulkheadPipelineMiddleware : IPipelineMiddleware, IDisposable
    {
        private readonly ILogger<BulkheadPipelineMiddleware>? _logger;
        private readonly BulkheadOptions _defaultOptions;
        private readonly ConcurrentDictionary<string, BulkheadState> _bulkheads = new();
        private bool _disposed;

        /// <summary>
        /// Creates a new instance of BulkheadPipelineMiddleware with default options.
        /// </summary>
        /// <param name="logger">Optional logger instance.</param>
        public BulkheadPipelineMiddleware(ILogger<BulkheadPipelineMiddleware>? logger = null)
            : this(BulkheadOptions.Default, logger)
        {
        }

        /// <summary>
        /// Creates a new instance of BulkheadPipelineMiddleware with custom options.
        /// </summary>
        /// <param name="defaultOptions">Default bulkhead options.</param>
        /// <param name="logger">Optional logger instance.</param>
        public BulkheadPipelineMiddleware(BulkheadOptions defaultOptions, ILogger<BulkheadPipelineMiddleware>? logger = null)
        {
            _defaultOptions = defaultOptions ?? throw new ArgumentNullException(nameof(defaultOptions));
            _logger = logger;
        }

        /// <inheritdoc />
        public int Order => -350; // Run after retry, before circuit breaker

        /// <summary>
        /// Gets the current state of a bulkhead.
        /// </summary>
        /// <param name="key">The bulkhead key.</param>
        /// <returns>Current concurrency and queue information.</returns>
        public (int CurrentConcurrency, int QueuedCount, int AvailableSlots)? GetBulkheadState(string key)
        {
            if (_bulkheads.TryGetValue(key, out var state))
            {
                return (state.CurrentConcurrency, state.QueuedCount, state.AvailableSlots);
            }
            return null;
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(IPipelineMessage message, Func<Task> next, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var bulkheadOperation = GetBulkheadOperation(message);
            var key = bulkheadOperation?.BulkheadKey ?? _defaultOptions.Key;
            var options = GetEffectiveOptions(bulkheadOperation);

            var bulkheadState = _bulkheads.GetOrAdd(key, _ => new BulkheadState(options));

            _logger?.LogDebug(
                "Bulkhead '{Key}' - Current: {Current}, Queued: {Queued}, Available: {Available}",
                key,
                bulkheadState.CurrentConcurrency,
                bulkheadState.QueuedCount,
                bulkheadState.AvailableSlots);

            bool acquired;
            try
            {
                acquired = await bulkheadState.TryEnterAsync(bulkheadOperation, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger?.LogDebug("Bulkhead '{Key}' wait was cancelled", key);
                throw;
            }

            if (!acquired)
            {
                var reason = options.QueueLimit <= 0
                    ? BulkheadRejectionReason.AtCapacity
                    : BulkheadRejectionReason.QueueTimeout;

                _logger?.LogWarning(
                    "Bulkhead '{Key}' rejected operation. Reason: {Reason}",
                    key,
                    reason);

                _logger?.LogDebug(
                    "Bulkhead: Rejected. Key: {Key}, Reason: {Reason}",
                    key,
                    reason);

                throw new PipelineBulkheadRejectedException(key, reason);
            }

            try
            {
                _logger?.LogDebug(
                    "Bulkhead '{Key}' executing operation. Current: {Current}",
                    key,
                    bulkheadState.CurrentConcurrency);

                _logger?.LogDebug(
                    "Bulkhead: Executing. Key: {Key}, Current: {CurrentConcurrency}",
                    key,
                    bulkheadState.CurrentConcurrency);

                await next();
            }
            finally
            {
                bulkheadState.Exit();

                _logger?.LogDebug(
                    "Bulkhead '{Key}' released. Current: {Current}",
                    key,
                    bulkheadState.CurrentConcurrency);
            }
        }

        private static IBulkheadOperation? GetBulkheadOperation(IPipelineMessage message)
        {
            if (message.HasContent("CurrentOperation"))
            {
                var operation = message.GetContent<object>("CurrentOperation");
                if (operation is IBulkheadOperation bulkheadOp)
                {
                    return bulkheadOp;
                }
            }
            return null;
        }

        private BulkheadOptions GetEffectiveOptions(IBulkheadOperation? bulkheadOperation)
        {
            if (bulkheadOperation == null)
                return _defaultOptions;

            return new BulkheadOptions
            {
                Key = bulkheadOperation.BulkheadKey,
                MaxConcurrency = bulkheadOperation.MaxConcurrency,
                QueueLimit = bulkheadOperation.QueueLimit,
                QueueTimeout = bulkheadOperation.QueueTimeout,
                OnQueued = (pos) =>
                {
                    bulkheadOperation.OnQueued(pos);
                    _defaultOptions.OnQueued?.Invoke(pos);
                },
                OnRejected = () =>
                {
                    bulkheadOperation.OnRejected();
                    _defaultOptions.OnRejected?.Invoke();
                },
                OnDequeued = (wait) =>
                {
                    bulkheadOperation.OnDequeued(wait);
                    _defaultOptions.OnDequeued?.Invoke(wait);
                }
            };
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                foreach (var kvp in _bulkheads)
                {
                    kvp.Value.Dispose();
                }
                _bulkheads.Clear();
            }
            GC.SuppressFinalize(this);
        }
    }
}

