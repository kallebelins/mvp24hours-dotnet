//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Contract.Transaction;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic.Transaction
{
    /// <summary>
    /// Default implementation of <see cref="ITransactionScope"/> that wraps an <see cref="IUnitOfWorkAsync"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation provides:
    /// <list type="bullet">
    /// <item>Explicit transaction control (Begin/Commit/Rollback)</item>
    /// <item>Savepoint support for nested transaction semantics</item>
    /// <item>Automatic cleanup via IAsyncDisposable</item>
    /// <item>Integration with ambient transaction context</item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class TransactionScope : ITransactionScope
    {
        #region [ Fields ]

        private readonly IUnitOfWorkAsync _unitOfWork;
        private readonly ILogger<TransactionScope>? _logger;
        private readonly Stack<string> _savepoints = new();
        private bool _disposed;

        #endregion

        #region [ Properties ]

        /// <inheritdoc />
        public Guid TransactionId { get; }

        /// <inheritdoc />
        public TransactionStatus Status { get; private set; } = TransactionStatus.NotStarted;

        /// <inheritdoc />
        public bool IsActive => Status == TransactionStatus.Active;

        /// <inheritdoc />
        public int NestingLevel => _savepoints.Count;

        #endregion

        #region [ Constructor ]

        /// <summary>
        /// Creates a new instance of the <see cref="TransactionScope"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work to manage transactions for.</param>
        /// <param name="logger">Optional logger for recording transaction operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
        public TransactionScope(IUnitOfWorkAsync unitOfWork, ILogger<TransactionScope>? logger = null)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger;
            TransactionId = Guid.NewGuid();
        }

        #endregion

        #region [ Transaction Operations ]

        /// <inheritdoc />
        public Task BeginAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (Status == TransactionStatus.Active)
            {
                throw new InvalidOperationException(
                    $"Transaction {TransactionId} is already active. Use CreateSavepointAsync for nested transactions.");
            }

            if (Status is TransactionStatus.Committed or TransactionStatus.RolledBack)
            {
                throw new InvalidOperationException(
                    $"Transaction {TransactionId} has already been completed with status '{Status}'.");
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-transactionscope-begin-start");

            _logger?.LogDebug(
                "[Transaction] Beginning transaction {TransactionId}",
                TransactionId);

            Status = TransactionStatus.Active;

            // Register with ambient context
            AmbientTransactionContext.SetCurrent(this);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-transactionscope-begin-end");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotActive();

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-transactionscope-commit-start");

            try
            {
                // If we have savepoints, release them first
                while (_savepoints.Count > 0)
                {
                    var savepoint = _savepoints.Pop();
                    await ReleaseSavepointAsync(savepoint, cancellationToken);
                }

                var affectedRows = await _unitOfWork.SaveChangesAsync(cancellationToken);

                Status = TransactionStatus.Committed;

                _logger?.LogDebug(
                    "[Transaction] Committed transaction {TransactionId} ({AffectedRows} rows affected)",
                    TransactionId,
                    affectedRows);

                return affectedRows;
            }
            catch (Exception ex)
            {
                Status = TransactionStatus.Error;

                _logger?.LogError(
                    ex,
                    "[Transaction] Failed to commit transaction {TransactionId}: {Message}",
                    TransactionId,
                    ex.Message);

                throw TransactionException.CommitFailed(TransactionId, ex);
            }
            finally
            {
                AmbientTransactionContext.Clear();
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-transactionscope-commit-end");
            }
        }

        /// <inheritdoc />
        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (Status == TransactionStatus.NotStarted)
            {
                _logger?.LogDebug(
                    "[Transaction] Skipping rollback for transaction {TransactionId} - not started",
                    TransactionId);
                return;
            }

            if (Status is TransactionStatus.Committed or TransactionStatus.RolledBack)
            {
                _logger?.LogDebug(
                    "[Transaction] Skipping rollback for transaction {TransactionId} - already {Status}",
                    TransactionId,
                    Status);
                return;
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-transactionscope-rollback-start");

            try
            {
                await _unitOfWork.RollbackAsync();
                _savepoints.Clear();
                Status = TransactionStatus.RolledBack;

                _logger?.LogDebug(
                    "[Transaction] Rolled back transaction {TransactionId}",
                    TransactionId);
            }
            catch (Exception ex)
            {
                Status = TransactionStatus.Error;

                _logger?.LogError(
                    ex,
                    "[Transaction] Failed to rollback transaction {TransactionId}: {Message}",
                    TransactionId,
                    ex.Message);

                throw TransactionException.RollbackFailed(TransactionId, ex);
            }
            finally
            {
                AmbientTransactionContext.Clear();
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-transactionscope-rollback-end");
            }
        }

        #endregion

        #region [ Savepoint Operations ]

        /// <inheritdoc />
        public Task CreateSavepointAsync(string savepointName, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotActive();

            if (string.IsNullOrWhiteSpace(savepointName))
            {
                throw new ArgumentNullException(nameof(savepointName));
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-transactionscope-savepoint-create");

            _savepoints.Push(savepointName);

            _logger?.LogDebug(
                "[Transaction] Created savepoint '{SavepointName}' in transaction {TransactionId} (level {Level})",
                savepointName,
                TransactionId,
                NestingLevel);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task RollbackToSavepointAsync(string savepointName, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotActive();

            if (string.IsNullOrWhiteSpace(savepointName))
            {
                throw new ArgumentNullException(nameof(savepointName));
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-transactionscope-savepoint-rollback");

            // Pop all savepoints until we reach the target
            while (_savepoints.Count > 0)
            {
                var current = _savepoints.Pop();
                if (current == savepointName)
                {
                    // Rollback the unit of work changes
                    await _unitOfWork.RollbackAsync();

                    _logger?.LogDebug(
                        "[Transaction] Rolled back to savepoint '{SavepointName}' in transaction {TransactionId}",
                        savepointName,
                        TransactionId);

                    return;
                }
            }

            throw new InvalidOperationException($"Savepoint '{savepointName}' not found in transaction {TransactionId}.");
        }

        /// <inheritdoc />
        public Task ReleaseSavepointAsync(string savepointName, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotActive();

            if (string.IsNullOrWhiteSpace(savepointName))
            {
                throw new ArgumentNullException(nameof(savepointName));
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-transactionscope-savepoint-release");

            _logger?.LogDebug(
                "[Transaction] Released savepoint '{SavepointName}' in transaction {TransactionId}",
                savepointName,
                TransactionId);

            return Task.CompletedTask;
        }

        #endregion

        #region [ Execute Methods ]

        /// <inheritdoc />
        public async Task<int> ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            await BeginAsync(cancellationToken);

            try
            {
                await action();
                return await CommitAsync(cancellationToken);
            }
            catch
            {
                await RollbackAsync(cancellationToken);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<(TResult Result, int AffectedRows)> ExecuteAsync<TResult>(
            Func<Task<TResult>> func,
            CancellationToken cancellationToken = default)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            await BeginAsync(cancellationToken);

            try
            {
                var result = await func();
                var affectedRows = await CommitAsync(cancellationToken);
                return (result, affectedRows);
            }
            catch
            {
                await RollbackAsync(cancellationToken);
                throw;
            }
        }

        #endregion

        #region [ Helper Methods ]

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        private void ThrowIfNotActive()
        {
            if (Status != TransactionStatus.Active)
            {
                throw TransactionException.InvalidState(TransactionId, Status, "operation requiring active transaction");
            }
        }

        #endregion

        #region [ Disposal ]

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            // Auto-rollback if transaction was started but not completed
            if (Status == TransactionStatus.Active)
            {
                _logger?.LogWarning(
                    "[Transaction] Transaction {TransactionId} was not committed or rolled back - auto-rolling back",
                    TransactionId);

                try
                {
                    await RollbackAsync();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(
                        ex,
                        "[Transaction] Error during auto-rollback of transaction {TransactionId}",
                        TransactionId);
                }
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            // Auto-rollback if transaction was started but not completed
            if (Status == TransactionStatus.Active)
            {
                _logger?.LogWarning(
                    "[Transaction] Transaction {TransactionId} was not committed or rolled back - auto-rolling back",
                    TransactionId);

                try
                {
                    _unitOfWork.RollbackAsync().GetAwaiter().GetResult();
                    Status = TransactionStatus.RolledBack;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(
                        ex,
                        "[Transaction] Error during auto-rollback of transaction {TransactionId}",
                        TransactionId);
                    Status = TransactionStatus.Error;
                }
            }

            AmbientTransactionContext.Clear();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    /// <summary>
    /// Synchronous implementation of transaction scope.
    /// </summary>
    public sealed class TransactionScopeSync : ITransactionScopeSync
    {
        #region [ Fields ]

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TransactionScopeSync>? _logger;
        private bool _disposed;

        #endregion

        #region [ Properties ]

        /// <inheritdoc />
        public Guid TransactionId { get; }

        /// <inheritdoc />
        public TransactionStatus Status { get; private set; } = TransactionStatus.NotStarted;

        /// <inheritdoc />
        public bool IsActive => Status == TransactionStatus.Active;

        #endregion

        #region [ Constructor ]

        /// <summary>
        /// Creates a new instance of the <see cref="TransactionScopeSync"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work to manage transactions for.</param>
        /// <param name="logger">Optional logger for recording transaction operations.</param>
        public TransactionScopeSync(IUnitOfWork unitOfWork, ILogger<TransactionScopeSync>? logger = null)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger;
            TransactionId = Guid.NewGuid();
        }

        #endregion

        #region [ Transaction Operations ]

        /// <inheritdoc />
        public void Begin()
        {
            ThrowIfDisposed();

            if (Status == TransactionStatus.Active)
            {
                throw new InvalidOperationException($"Transaction {TransactionId} is already active.");
            }

            _logger?.LogDebug("[Transaction] Beginning sync transaction {TransactionId}", TransactionId);
            Status = TransactionStatus.Active;
        }

        /// <inheritdoc />
        public int Commit()
        {
            ThrowIfDisposed();
            ThrowIfNotActive();

            try
            {
                var affectedRows = _unitOfWork.SaveChanges();
                Status = TransactionStatus.Committed;

                _logger?.LogDebug(
                    "[Transaction] Committed sync transaction {TransactionId} ({AffectedRows} rows)",
                    TransactionId,
                    affectedRows);

                return affectedRows;
            }
            catch (Exception ex)
            {
                Status = TransactionStatus.Error;
                throw TransactionException.CommitFailed(TransactionId, ex);
            }
        }

        /// <inheritdoc />
        public void Rollback()
        {
            ThrowIfDisposed();

            if (Status != TransactionStatus.Active)
            {
                return;
            }

            try
            {
                _unitOfWork.Rollback();
                Status = TransactionStatus.RolledBack;

                _logger?.LogDebug("[Transaction] Rolled back sync transaction {TransactionId}", TransactionId);
            }
            catch (Exception ex)
            {
                Status = TransactionStatus.Error;
                throw TransactionException.RollbackFailed(TransactionId, ex);
            }
        }

        /// <inheritdoc />
        public int Execute(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            Begin();
            try
            {
                action();
                return Commit();
            }
            catch
            {
                Rollback();
                throw;
            }
        }

        /// <inheritdoc />
        public (TResult Result, int AffectedRows) Execute<TResult>(Func<TResult> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            Begin();
            try
            {
                var result = func();
                var affectedRows = Commit();
                return (result, affectedRows);
            }
            catch
            {
                Rollback();
                throw;
            }
        }

        #endregion

        #region [ Helpers ]

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        private void ThrowIfNotActive()
        {
            if (Status != TransactionStatus.Active)
            {
                throw TransactionException.InvalidState(TransactionId, Status, "commit/rollback");
            }
        }

        #endregion

        #region [ Disposal ]

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;

            if (Status == TransactionStatus.Active)
            {
                _logger?.LogWarning(
                    "[Transaction] Sync transaction {TransactionId} not completed - auto-rolling back",
                    TransactionId);

                try
                {
                    Rollback();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "[Transaction] Error during auto-rollback of {TransactionId}", TransactionId);
                }
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}

