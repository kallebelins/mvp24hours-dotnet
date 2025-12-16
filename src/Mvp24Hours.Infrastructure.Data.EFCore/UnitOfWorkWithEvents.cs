//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.EFCore.Cqrs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;

namespace Mvp24Hours.Infrastructure.Data.EFCore
{
    /// <summary>
    /// Unit of Work implementation with automatic Domain Event dispatching (synchronous version).
    /// Extends <see cref="UnitOfWork"/> to automatically collect and dispatch
    /// domain events from tracked entities after successful persistence.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Key Features:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Automatic collection of domain events from tracked entities</description></item>
    /// <item><description>Events dispatched only after successful SaveChanges</description></item>
    /// <item><description>Events cleared from entities after dispatch</description></item>
    /// <item><description>Full EF Core change tracking integration</description></item>
    /// </list>
    /// <para>
    /// <strong>Usage:</strong>
    /// Register this implementation instead of <see cref="UnitOfWork"/> when you need
    /// automatic domain event dispatching. You must also register an <see cref="IDomainEventDispatcherEFCore"/>
    /// implementation.
    /// </para>
    /// </remarks>
    public class UnitOfWorkWithEvents : IUnitOfWorkWithEvents
    {
        #region [ Fields ]

        private readonly DbContext _dbContext;
        private readonly Dictionary<Type, object> _repositories;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDomainEventDispatcherEFCore? _eventDispatcher;
        private readonly ILogger<UnitOfWorkWithEvents>? _logger;

        #endregion

        #region [ Ctor ]

        /// <summary>
        /// Creates a new instance with explicitly provided repositories.
        /// </summary>
        public UnitOfWorkWithEvents(
            DbContext dbContext,
            Dictionary<Type, object> repositories,
            IDomainEventDispatcherEFCore? eventDispatcher = null,
            ILogger<UnitOfWorkWithEvents>? logger = null)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
            _eventDispatcher = eventDispatcher;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new instance with DI-based repository resolution.
        /// </summary>
        [ActivatorUtilitiesConstructor]
        public UnitOfWorkWithEvents(
            DbContext dbContext,
            IServiceProvider serviceProvider,
            IDomainEventDispatcherEFCore? eventDispatcher = null,
            ILogger<UnitOfWorkWithEvents>? logger = null)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _repositories = [];
            _eventDispatcher = eventDispatcher;
            _logger = logger;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the underlying DbContext.
        /// </summary>
        protected DbContext DbContext => _dbContext;

        #endregion

        #region [ IUnitOfWork Implementation ]

        /// <inheritdoc />
        public IRepository<T> GetRepository<T>() where T : class, IEntityBase
        {
            if (!_repositories.ContainsKey(typeof(T)))
            {
                _repositories.Add(typeof(T), _serviceProvider.GetService<IRepository<T>>()!);
            }
            return (_repositories[typeof(T)] as IRepository<T>)!;
        }

        /// <inheritdoc />
        public IDbConnection GetConnection()
        {
            return _dbContext.Database.GetDbConnection();
        }

        /// <inheritdoc />
        public int SaveChanges(CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-unitofwork-events-savechanges-start");
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Rollback();
                    return 0;
                }

                return _dbContext.SaveChanges();
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-unitofwork-events-savechanges-end");
            }
        }

        /// <inheritdoc />
        public void Rollback()
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-unitofwork-events-rollback-start");
            try
            {
                var changedEntries = _dbContext.ChangeTracker.Entries()
                    .Where(x => x.State != EntityState.Unchanged)
                    .ToList();

                foreach (var entry in changedEntries)
                {
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            entry.CurrentValues.SetValues(entry.OriginalValues);
                            entry.State = EntityState.Unchanged;
                            break;
                        case EntityState.Added:
                            entry.State = EntityState.Detached;
                            break;
                        case EntityState.Deleted:
                            entry.State = EntityState.Unchanged;
                            break;
                    }
                }
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-unitofwork-events-rollback-end");
            }
        }

        #endregion

        #region [ IUnitOfWorkWithEvents Implementation ]

        /// <inheritdoc />
        public int SaveChangesWithEvents(CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-unitofwork-events-savechangeswithevents-start");
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Rollback();
                    return 0;
                }

                // Collect domain events before saving
                var entitiesWithEvents = GetEntitiesWithEvents().ToList();
                var allEvents = entitiesWithEvents
                    .SelectMany(e => e.DomainEvents)
                    .ToList();

                _logger?.LogDebug(
                    "[UnitOfWork] Saving changes with {EventCount} domain events from {EntityCount} entities",
                    allEvents.Count,
                    entitiesWithEvents.Count);

                // Save changes to the database
                var result = _dbContext.SaveChanges();

                _logger?.LogDebug(
                    "[UnitOfWork] Saved {RowCount} rows, dispatching domain events",
                    result);

                // Dispatch domain events after successful save
                if (_eventDispatcher != null && entitiesWithEvents.Count > 0)
                {
                    _eventDispatcher.DispatchEventsAsync(entitiesWithEvents, cancellationToken)
                        .GetAwaiter()
                        .GetResult();
                }
                else if (_eventDispatcher == null && allEvents.Count > 0)
                {
                    _logger?.LogWarning(
                        "[UnitOfWork] {EventCount} domain events were not dispatched because no IDomainEventDispatcherEFCore is registered",
                        allEvents.Count);

                    // Clear events even without dispatcher to prevent memory leaks
                    foreach (var entity in entitiesWithEvents)
                    {
                        entity.ClearDomainEvents();
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "[UnitOfWork] Error during SaveChangesWithEvents: {Message}",
                    ex.Message);
                throw;
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-unitofwork-events-savechangeswithevents-end");
            }
        }

        /// <inheritdoc />
        public IEnumerable<IHasDomainEvents> GetEntitiesWithEvents()
        {
            return _dbContext.ChangeTracker.Entries()
                .Where(e => e.Entity is IHasDomainEvents)
                .Select(e => (IHasDomainEvents)e.Entity)
                .Where(e => e.DomainEvents.Count > 0);
        }

        #endregion

        #region [ IDisposable ]

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the UnitOfWork and optionally releases the managed resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dbContext?.Dispose();
            }
        }

        #endregion
    }
}

