//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Transactional.Contract;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Transactional
{
    /// <summary>
    /// Provides support for enlisting the transactional bus in .NET System.Transactions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Transaction Enlistment:</strong>
    /// </para>
    /// <para>
    /// When using <see cref="TransactionScope"/>, this class allows the transactional bus
    /// to participate in the ambient transaction. Messages are only flushed to the outbox
    /// when the transaction commits, and are discarded if the transaction rolls back.
    /// </para>
    /// <para>
    /// <strong>Usage:</strong>
    /// </para>
    /// <code>
    /// using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
    /// {
    ///     // Enlist the transactional bus
    ///     transactionalEnlistment.Enlist(transactionalBus);
    ///     
    ///     // Stage messages
    ///     await transactionalBus.PublishAsync(new MyEvent { ... });
    ///     
    ///     // Save to database
    ///     await _repository.SaveAsync();
    ///     
    ///     // Complete transaction - messages are flushed to outbox
    ///     scope.Complete();
    /// }
    /// </code>
    /// <para>
    /// <strong>Important:</strong> Always use <see cref="TransactionScopeAsyncFlowOption.Enabled"/>
    /// when working with async code to ensure the transaction flows correctly across await boundaries.
    /// </para>
    /// </remarks>
    public interface ITransactionalEnlistment
    {
        /// <summary>
        /// Enlists the transactional bus in the current ambient transaction.
        /// </summary>
        /// <param name="bus">The transactional bus to enlist.</param>
        /// <returns>True if enlisted successfully, false if no ambient transaction exists.</returns>
        bool Enlist(TransactionalBus bus);

        /// <summary>
        /// Creates a new transaction scope with the bus automatically enlisted.
        /// </summary>
        /// <param name="bus">The transactional bus to enlist.</param>
        /// <param name="scopeOption">The transaction scope option.</param>
        /// <returns>A transaction scope with the bus enlisted.</returns>
        TransactionScope CreateScope(
            TransactionalBus bus,
            TransactionScopeOption scopeOption = TransactionScopeOption.Required);
    }

    /// <summary>
    /// Implementation of transactional enlistment for System.Transactions support.
    /// </summary>
    public class TransactionalEnlistment : ITransactionalEnlistment
    {
        private readonly ITransactionalOutbox _outbox;
        private readonly ILogger<TransactionalEnlistment> _logger;

        /// <summary>
        /// Creates a new instance of the transactional enlistment.
        /// </summary>
        /// <param name="outbox">The outbox storage for persisting messages.</param>
        /// <param name="logger">Logger for recording operations.</param>
        public TransactionalEnlistment(
            ITransactionalOutbox outbox,
            ILogger<TransactionalEnlistment> logger)
        {
            _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public bool Enlist(TransactionalBus bus)
        {
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));

            var currentTransaction = Transaction.Current;
            if (currentTransaction == null)
            {
                _logger.LogDebug("[TransactionalEnlistment] No ambient transaction to enlist in");
                return false;
            }

            var notification = new TransactionalBusEnlistmentNotification(bus, _outbox, _logger);
            currentTransaction.EnlistVolatile(notification, EnlistmentOptions.None);

            _logger.LogDebug(
                "[TransactionalEnlistment] Enlisted in transaction {TransactionId}",
                currentTransaction.TransactionInformation.LocalIdentifier);

            return true;
        }

        /// <inheritdoc />
        public TransactionScope CreateScope(
            TransactionalBus bus,
            TransactionScopeOption scopeOption = TransactionScopeOption.Required)
        {
            var scope = new TransactionScope(
                scopeOption,
                TransactionScopeAsyncFlowOption.Enabled);

            Enlist(bus);

            return scope;
        }
    }

    /// <summary>
    /// Enlistment notification handler for the transactional bus.
    /// </summary>
    /// <remarks>
    /// This class handles the transaction lifecycle events (Prepare, Commit, Rollback, InDoubt)
    /// and ensures messages are properly flushed or discarded based on the transaction outcome.
    /// </remarks>
    internal sealed class TransactionalBusEnlistmentNotification : IEnlistmentNotification
    {
        private readonly TransactionalBus _bus;
        private readonly ITransactionalOutbox _outbox;
        private readonly ILogger _logger;

        public TransactionalBusEnlistmentNotification(
            TransactionalBus bus,
            ITransactionalOutbox outbox,
            ILogger logger)
        {
            _bus = bus;
            _outbox = outbox;
            _logger = logger;
        }

        /// <summary>
        /// Called during the prepare phase of two-phase commit.
        /// </summary>
        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            try
            {
                var pendingCount = _bus.GetPendingCount();
                _logger.LogDebug(
                    "[TransactionalEnlistment] Prepare phase - {PendingCount} messages pending",
                    pendingCount);

                // We're ready to commit - the actual flush happens in Commit
                preparingEnlistment.Prepared();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TransactionalEnlistment] Prepare phase failed");
                preparingEnlistment.ForceRollback(ex);
            }
        }

        /// <summary>
        /// Called when the transaction is committed.
        /// </summary>
        public void Commit(Enlistment enlistment)
        {
            try
            {
                var pendingMessages = _bus.GetPendingMessages();
                if (pendingMessages.Count > 0)
                {
                    // Synchronously flush to outbox (we're in a transaction callback)
                    _outbox.AddRangeAsync(pendingMessages, CancellationToken.None)
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();

                    _logger.LogDebug(
                        "[TransactionalEnlistment] Commit - flushed {Count} messages to outbox",
                        pendingMessages.Count);
                }

                _bus.ClearPending();
                enlistment.Done();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TransactionalEnlistment] Commit phase failed");
                // We can't really do much here - the DB transaction already committed
                // The messages are lost, which is unfortunate but consistent with
                // the transactional semantics (better to lose messages than publish
                // for a rolled-back transaction)
                enlistment.Done();
            }
        }

        /// <summary>
        /// Called when the transaction is rolled back.
        /// </summary>
        public void Rollback(Enlistment enlistment)
        {
            var pendingCount = _bus.GetPendingCount();

            _logger.LogDebug(
                "[TransactionalEnlistment] Rollback - discarding {Count} pending messages",
                pendingCount);

            _bus.ClearPending();
            enlistment.Done();
        }

        /// <summary>
        /// Called when the transaction outcome is uncertain.
        /// </summary>
        public void InDoubt(Enlistment enlistment)
        {
            var pendingCount = _bus.GetPendingCount();

            _logger.LogWarning(
                "[TransactionalEnlistment] InDoubt - discarding {Count} pending messages due to uncertain outcome",
                pendingCount);

            // When in doubt, don't publish (safer to lose messages than publish for
            // a potentially rolled-back transaction)
            _bus.ClearPending();
            enlistment.Done();
        }
    }
}

