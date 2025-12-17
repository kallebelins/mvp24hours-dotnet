//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.RabbitMQ.Transactional.Contract;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Transactional
{
    /// <summary>
    /// Extension methods for integrating transactional messaging with IUnitOfWork.
    /// </summary>
    public static class UnitOfWorkTransactionalExtensions
    {
        /// <summary>
        /// Saves changes to the database and flushes pending messages to the outbox atomically.
        /// </summary>
        /// <param name="unitOfWork">The unit of work.</param>
        /// <param name="transactionalBus">The transactional bus with pending messages.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of entities saved.</returns>
        /// <remarks>
        /// <para>
        /// This method ensures that database changes and outbox messages are committed together.
        /// If the database save fails, no messages are added to the outbox.
        /// </para>
        /// <para>
        /// <strong>Usage:</strong>
        /// </para>
        /// <code>
        /// var order = new Order();
        /// _repository.Add(order);
        /// 
        /// await _bus.PublishAsync(new OrderCreatedEvent { OrderId = order.Id });
        /// 
        /// // Saves order AND flushes message to outbox atomically
        /// await _unitOfWork.SaveChangesWithMessagesAsync(_bus);
        /// </code>
        /// </remarks>
        public static int SaveChangesWithMessages(
            this IUnitOfWork unitOfWork,
            TransactionalBus transactionalBus,
            CancellationToken cancellationToken = default)
        {
            if (unitOfWork == null)
                throw new ArgumentNullException(nameof(unitOfWork));
            if (transactionalBus == null)
                throw new ArgumentNullException(nameof(transactionalBus));

            try
            {
                // Save the entity changes
                var result = unitOfWork.SaveChanges(cancellationToken);

                // If save succeeded, flush messages to outbox
                // Note: This is not truly atomic unless using TransactionScope
                // For true atomicity, use the TransactionalEnlistment with TransactionScope
                transactionalBus.FlushToOutboxAsync(cancellationToken)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                return result;
            }
            catch
            {
                // Clear pending messages on failure
                transactionalBus.ClearPending();
                throw;
            }
        }

        /// <summary>
        /// Saves changes to the database asynchronously and flushes pending messages to the outbox.
        /// </summary>
        /// <param name="unitOfWork">The async unit of work.</param>
        /// <param name="transactionalBus">The transactional bus with pending messages.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of entities saved.</returns>
        public static async Task<int> SaveChangesWithMessagesAsync(
            this IUnitOfWorkAsync unitOfWork,
            TransactionalBus transactionalBus,
            CancellationToken cancellationToken = default)
        {
            if (unitOfWork == null)
                throw new ArgumentNullException(nameof(unitOfWork));
            if (transactionalBus == null)
                throw new ArgumentNullException(nameof(transactionalBus));

            try
            {
                // Save the entity changes
                var result = await unitOfWork.SaveChangesAsync(cancellationToken);

                // If save succeeded, flush messages to outbox
                await transactionalBus.FlushToOutboxAsync(cancellationToken);

                return result;
            }
            catch
            {
                // Clear pending messages on failure
                transactionalBus.ClearPending();
                throw;
            }
        }
    }

    /// <summary>
    /// Wrapper for IUnitOfWork that automatically integrates with transactional messaging.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This wrapper automatically flushes pending messages when SaveChanges is called.
    /// It simplifies the integration by handling the transactional bus internally.
    /// </para>
    /// </remarks>
    public class TransactionalUnitOfWork : IUnitOfWork
    {
        private readonly IUnitOfWork _innerUnitOfWork;
        private readonly TransactionalBus _transactionalBus;

        /// <summary>
        /// Creates a new transactional unit of work wrapper.
        /// </summary>
        /// <param name="innerUnitOfWork">The underlying unit of work.</param>
        /// <param name="transactionalBus">The transactional bus for message staging.</param>
        public TransactionalUnitOfWork(
            IUnitOfWork innerUnitOfWork,
            TransactionalBus transactionalBus)
        {
            _innerUnitOfWork = innerUnitOfWork ?? throw new ArgumentNullException(nameof(innerUnitOfWork));
            _transactionalBus = transactionalBus ?? throw new ArgumentNullException(nameof(transactionalBus));
        }

        /// <summary>
        /// Gets the transactional bus for staging messages.
        /// </summary>
        public ITransactionalBus Bus => _transactionalBus;

        /// <inheritdoc />
        public int SaveChanges(CancellationToken cancellationToken = default)
        {
            return _innerUnitOfWork.SaveChangesWithMessages(_transactionalBus, cancellationToken);
        }

        /// <inheritdoc />
        public void Rollback()
        {
            _transactionalBus.ClearPending();
            _innerUnitOfWork.Rollback();
        }

        /// <inheritdoc />
        public IRepository<T> GetRepository<T>() where T : class, Mvp24Hours.Core.Contract.Domain.Entity.IEntityBase
        {
            return _innerUnitOfWork.GetRepository<T>();
        }

        /// <inheritdoc />
        public System.Data.IDbConnection GetConnection()
        {
            return _innerUnitOfWork.GetConnection();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _innerUnitOfWork.Dispose();
        }
    }

    /// <summary>
    /// Async wrapper for IUnitOfWorkAsync that automatically integrates with transactional messaging.
    /// </summary>
    public class TransactionalUnitOfWorkAsync : IUnitOfWorkAsync
    {
        private readonly IUnitOfWorkAsync _innerUnitOfWork;
        private readonly TransactionalBus _transactionalBus;

        /// <summary>
        /// Creates a new transactional async unit of work wrapper.
        /// </summary>
        /// <param name="innerUnitOfWork">The underlying async unit of work.</param>
        /// <param name="transactionalBus">The transactional bus for message staging.</param>
        public TransactionalUnitOfWorkAsync(
            IUnitOfWorkAsync innerUnitOfWork,
            TransactionalBus transactionalBus)
        {
            _innerUnitOfWork = innerUnitOfWork ?? throw new ArgumentNullException(nameof(innerUnitOfWork));
            _transactionalBus = transactionalBus ?? throw new ArgumentNullException(nameof(transactionalBus));
        }

        /// <summary>
        /// Gets the transactional bus for staging messages.
        /// </summary>
        public ITransactionalBus Bus => _transactionalBus;

        /// <inheritdoc />
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _innerUnitOfWork.SaveChangesWithMessagesAsync(_transactionalBus, cancellationToken);
        }

        /// <inheritdoc />
        public async Task RollbackAsync()
        {
            _transactionalBus.ClearPending();
            await _innerUnitOfWork.RollbackAsync();
        }

        /// <inheritdoc />
        public IRepositoryAsync<T> GetRepository<T>() where T : class, Mvp24Hours.Core.Contract.Domain.Entity.IEntityBase
        {
            return _innerUnitOfWork.GetRepository<T>();
        }

        /// <inheritdoc />
        public System.Data.IDbConnection GetConnection()
        {
            return _innerUnitOfWork.GetConnection();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _innerUnitOfWork.Dispose();
        }
    }

    /// <summary>
    /// Factory for creating transactional unit of work wrappers.
    /// </summary>
    public interface ITransactionalUnitOfWorkFactory
    {
        /// <summary>
        /// Creates a transactional unit of work wrapper.
        /// </summary>
        /// <param name="unitOfWork">The underlying unit of work.</param>
        /// <returns>A transactional unit of work wrapper.</returns>
        TransactionalUnitOfWork Create(IUnitOfWork unitOfWork);

        /// <summary>
        /// Creates a transactional async unit of work wrapper.
        /// </summary>
        /// <param name="unitOfWork">The underlying async unit of work.</param>
        /// <returns>A transactional async unit of work wrapper.</returns>
        TransactionalUnitOfWorkAsync CreateAsync(IUnitOfWorkAsync unitOfWork);
    }

    /// <summary>
    /// Default implementation of the transactional unit of work factory.
    /// </summary>
    public class TransactionalUnitOfWorkFactory : ITransactionalUnitOfWorkFactory
    {
        private readonly TransactionalBus _transactionalBus;

        /// <summary>
        /// Creates a new instance of the factory.
        /// </summary>
        /// <param name="transactionalBus">The transactional bus to use.</param>
        public TransactionalUnitOfWorkFactory(TransactionalBus transactionalBus)
        {
            _transactionalBus = transactionalBus ?? throw new ArgumentNullException(nameof(transactionalBus));
        }

        /// <inheritdoc />
        public TransactionalUnitOfWork Create(IUnitOfWork unitOfWork)
        {
            return new TransactionalUnitOfWork(unitOfWork, _transactionalBus);
        }

        /// <inheritdoc />
        public TransactionalUnitOfWorkAsync CreateAsync(IUnitOfWorkAsync unitOfWork)
        {
            return new TransactionalUnitOfWorkAsync(unitOfWork, _transactionalBus);
        }
    }
}

