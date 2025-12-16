//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Transactions
{
    /// <summary>
    /// Manages MongoDB multi-document transactions with automatic retry on transient errors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides comprehensive transaction management including:
    /// <list type="bullet">
    ///   <item>Multi-document ACID transactions</item>
    ///   <item>Automatic retry on transient errors (TransientTransactionError, UnknownTransactionCommitResult)</item>
    ///   <item>Configurable transaction options</item>
    ///   <item>Logical savepoints for tracking state</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Using automatic retry pattern
    /// var result = await transactionManager.ExecuteInTransactionAsync(async (session, ct) =>
    /// {
    ///     var orders = database.GetCollection&lt;Order&gt;("orders");
    ///     var inventory = database.GetCollection&lt;Inventory&gt;("inventory");
    ///     
    ///     await orders.InsertOneAsync(session, newOrder, cancellationToken: ct);
    ///     await inventory.UpdateOneAsync(session, 
    ///         filter, 
    ///         update, 
    ///         cancellationToken: ct);
    ///     
    ///     return newOrder.Id;
    /// });
    /// 
    /// // Using manual transaction control
    /// await using var session = await transactionManager.BeginTransactionAsync();
    /// try
    /// {
    ///     // Perform operations
    ///     await transactionManager.CommitTransactionAsync();
    /// }
    /// catch
    /// {
    ///     await transactionManager.AbortTransactionAsync();
    ///     throw;
    /// }
    /// </code>
    /// </example>
    public class MongoDbTransactionManager : IMongoDbTransactionManager
    {
        private readonly IMongoClient _client;
        private readonly ILogger<MongoDbTransactionManager> _logger;
        private readonly MongoDbTransactionOptions _options;
        private readonly HashSet<string> _savepoints;
        private IClientSessionHandle _currentSession;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbTransactionManager"/> class.
        /// </summary>
        /// <param name="client">The MongoDB client.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="options">Transaction options.</param>
        public MongoDbTransactionManager(
            IMongoClient client,
            ILogger<MongoDbTransactionManager> logger = null,
            MongoDbTransactionOptions options = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger;
            _options = options ?? new MongoDbTransactionOptions();
            _savepoints = new HashSet<string>();
        }

        /// <inheritdoc/>
        public IClientSessionHandle CurrentSession => _currentSession;

        /// <inheritdoc/>
        public bool IsTransactionActive => _currentSession?.IsInTransaction == true;

        /// <inheritdoc/>
        public async Task<IClientSessionHandle> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return await BeginTransactionAsync(CreateDefaultTransactionOptions(), cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IClientSessionHandle> BeginTransactionAsync(
            TransactionOptions options,
            CancellationToken cancellationToken = default)
        {
            if (_currentSession?.IsInTransaction == true)
            {
                throw new InvalidOperationException("A transaction is already in progress. Commit or abort it before starting a new one.");
            }

            _currentSession = await _client.StartSessionAsync(cancellationToken: cancellationToken);
            _currentSession.StartTransaction(options);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-transaction-started",
                new { SessionId = _currentSession.ServerSession.Id });

            _logger?.LogDebug("MongoDB transaction started. Session ID: {SessionId}", _currentSession.ServerSession.Id);

            return _currentSession;
        }

        /// <inheritdoc/>
        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentSession == null || !_currentSession.IsInTransaction)
            {
                throw new InvalidOperationException("No active transaction to commit.");
            }

            var maxRetries = _options.MaxCommitRetries;
            var retryCount = 0;

            while (true)
            {
                try
                {
                    await _currentSession.CommitTransactionAsync(cancellationToken);

                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-transaction-committed",
                        new { SessionId = _currentSession.ServerSession.Id });

                    _logger?.LogDebug("MongoDB transaction committed successfully. Session ID: {SessionId}",
                        _currentSession.ServerSession.Id);

                    ClearSavepoints();
                    break;
                }
                catch (MongoException ex) when (ex.HasErrorLabel("UnknownTransactionCommitResult") && retryCount < maxRetries)
                {
                    retryCount++;
                    _logger?.LogWarning(ex, "Unknown transaction commit result. Retrying... Attempt {RetryCount}/{MaxRetries}",
                        retryCount, maxRetries);

                    await Task.Delay(TimeSpan.FromMilliseconds(_options.RetryDelayMs * retryCount), cancellationToken);
                }
            }
        }

        /// <inheritdoc/>
        public async Task AbortTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentSession == null || !_currentSession.IsInTransaction)
            {
                throw new InvalidOperationException("No active transaction to abort.");
            }

            await _currentSession.AbortTransactionAsync(cancellationToken);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-transaction-aborted",
                new { SessionId = _currentSession.ServerSession.Id });

            _logger?.LogDebug("MongoDB transaction aborted. Session ID: {SessionId}",
                _currentSession.ServerSession.Id);

            ClearSavepoints();
        }

        /// <inheritdoc/>
        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<IClientSessionHandle, CancellationToken, Task<TResult>> operation,
            TransactionOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            options ??= CreateDefaultTransactionOptions();
            var maxRetries = _options.MaxTransactionRetries;

            using var session = await _client.StartSessionAsync(cancellationToken: cancellationToken);

            while (true)
            {
                try
                {
                    TResult result = default;

                    session.StartTransaction(options);

                    try
                    {
                        result = await operation(session, cancellationToken);
                        await CommitWithRetryAsync(session, cancellationToken);
                    }
                    catch (Exception)
                    {
                        if (session.IsInTransaction)
                        {
                            await session.AbortTransactionAsync(cancellationToken);
                        }
                        throw;
                    }

                    return result;
                }
                catch (MongoException ex) when (ex.HasErrorLabel("TransientTransactionError"))
                {
                    maxRetries--;
                    if (maxRetries <= 0)
                    {
                        _logger?.LogError(ex, "Max transaction retries exceeded.");
                        throw;
                    }

                    _logger?.LogWarning(ex, "Transient transaction error. Retrying transaction... Remaining attempts: {RemainingAttempts}",
                        maxRetries);

                    await Task.Delay(TimeSpan.FromMilliseconds(_options.RetryDelayMs), cancellationToken);
                }
            }
        }

        /// <inheritdoc/>
        public async Task ExecuteInTransactionAsync(
            Func<IClientSessionHandle, CancellationToken, Task> operation,
            TransactionOptions options = null,
            CancellationToken cancellationToken = default)
        {
            await ExecuteInTransactionAsync<object>(async (session, ct) =>
            {
                await operation(session, ct);
                return null;
            }, options, cancellationToken);
        }

        /// <inheritdoc/>
        public void CreateSavepoint(string savepointName)
        {
            if (string.IsNullOrWhiteSpace(savepointName))
            {
                throw new ArgumentException("Savepoint name cannot be empty.", nameof(savepointName));
            }

            if (!IsTransactionActive)
            {
                throw new InvalidOperationException("Cannot create savepoint without an active transaction.");
            }

            if (_savepoints.Contains(savepointName))
            {
                throw new InvalidOperationException($"Savepoint '{savepointName}' already exists.");
            }

            _savepoints.Add(savepointName);

            _logger?.LogDebug("Savepoint '{SavepointName}' created.", savepointName);
        }

        /// <inheritdoc/>
        public async Task RollbackToSavepointAsync(string savepointName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(savepointName))
            {
                throw new ArgumentException("Savepoint name cannot be empty.", nameof(savepointName));
            }

            if (!_savepoints.Contains(savepointName))
            {
                throw new InvalidOperationException($"Savepoint '{savepointName}' does not exist.");
            }

            // MongoDB doesn't support partial rollback, so we abort the entire transaction
            _logger?.LogWarning(
                "Rolling back to savepoint '{SavepointName}'. Note: MongoDB does not support partial rollback. The entire transaction will be aborted.",
                savepointName);

            await AbortTransactionAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public void ReleaseSavepoint(string savepointName)
        {
            if (string.IsNullOrWhiteSpace(savepointName))
            {
                throw new ArgumentException("Savepoint name cannot be empty.", nameof(savepointName));
            }

            if (_savepoints.Remove(savepointName))
            {
                _logger?.LogDebug("Savepoint '{SavepointName}' released.", savepointName);
            }
        }

        private TransactionOptions CreateDefaultTransactionOptions()
        {
            return new TransactionOptions(
                readConcern: _options.DefaultReadConcern ?? ReadConcern.Snapshot,
                writeConcern: _options.DefaultWriteConcern ?? WriteConcern.WMajority,
                readPreference: _options.DefaultReadPreference ?? ReadPreference.Primary,
                maxCommitTime: _options.MaxCommitTime);
        }

        private async Task CommitWithRetryAsync(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            var maxRetries = _options.MaxCommitRetries;
            var retryCount = 0;

            while (true)
            {
                try
                {
                    await session.CommitTransactionAsync(cancellationToken);

                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-transaction-committed",
                        new { SessionId = session.ServerSession.Id });

                    break;
                }
                catch (MongoException ex) when (ex.HasErrorLabel("UnknownTransactionCommitResult") && retryCount < maxRetries)
                {
                    retryCount++;
                    _logger?.LogWarning(ex, "Unknown transaction commit result. Retrying... Attempt {RetryCount}/{MaxRetries}",
                        retryCount, maxRetries);

                    await Task.Delay(TimeSpan.FromMilliseconds(_options.RetryDelayMs * retryCount), cancellationToken);
                }
            }
        }

        private void ClearSavepoints()
        {
            _savepoints.Clear();
        }

        /// <summary>
        /// Disposes the transaction manager and its resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the transaction manager.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (_currentSession != null)
                {
                    if (_currentSession.IsInTransaction)
                    {
                        try
                        {
                            _currentSession.AbortTransaction();
                        }
                        catch
                        {
                            // Ignore errors during disposal
                        }
                    }

                    _currentSession.Dispose();
                    _currentSession = null;
                }

                ClearSavepoints();
            }

            _disposed = true;
        }
    }
}

