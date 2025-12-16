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
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore
{
    /// <summary>
    /// Unit of Work implementation with automatic Domain Event dispatching.
    /// Extends <see cref="UnitOfWorkAsync"/> to automatically collect and dispatch
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
    /// Register this implementation instead of <see cref="UnitOfWorkAsync"/> when you need
    /// automatic domain event dispatching. You must also register an <see cref="IDomainEventDispatcherEFCore"/>
    /// implementation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In Startup/Program.cs
    /// services.AddScoped&lt;IUnitOfWorkWithEventsAsync, UnitOfWorkWithEventsAsync&gt;();
    /// services.AddScoped&lt;IDomainEventDispatcherEFCore, DomainEventDispatcherEFCore&gt;();
    /// 
    /// // Usage
    /// var unitOfWork = serviceProvider.GetService&lt;IUnitOfWorkWithEventsAsync&gt;();
    /// var order = new Order();
    /// order.Place(); // Raises OrderPlacedEvent
    /// 
    /// var repo = unitOfWork.GetRepository&lt;Order&gt;();
    /// await repo.AddAsync(order);
    /// 
    /// // Automatically dispatches OrderPlacedEvent after save
    /// await unitOfWork.SaveChangesWithEventsAsync();
    /// </code>
    /// </example>
    public class UnitOfWorkWithEventsAsync : IUnitOfWorkWithEventsAsync
    {
        #region [ Fields ]

        private readonly DbContext _dbContext;
        private readonly Dictionary<Type, object> _repositories;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDomainEventDispatcherEFCore? _eventDispatcher;
        private readonly ILogger<UnitOfWorkWithEventsAsync>? _logger;

        #endregion

        #region [ Ctor ]

        /// <summary>
        /// Creates a new instance with explicitly provided repositories.
        /// </summary>
        /// <param name="dbContext">The EF Core DbContext.</param>
        /// <param name="repositories">Dictionary of pre-created repositories.</param>
        /// <param name="eventDispatcher">Optional domain event dispatcher.</param>
        /// <param name="logger">Optional logger.</param>
        public UnitOfWorkWithEventsAsync(
            DbContext dbContext,
            Dictionary<Type, object> repositories,
            IDomainEventDispatcherEFCore? eventDispatcher = null,
            ILogger<UnitOfWorkWithEventsAsync>? logger = null)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
            _eventDispatcher = eventDispatcher;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new instance with DI-based repository resolution.
        /// </summary>
        /// <param name="dbContext">The EF Core DbContext.</param>
        /// <param name="serviceProvider">Service provider for resolving repositories.</param>
        /// <param name="eventDispatcher">Optional domain event dispatcher.</param>
        /// <param name="logger">Optional logger.</param>
        [ActivatorUtilitiesConstructor]
        public UnitOfWorkWithEventsAsync(
            DbContext dbContext,
            IServiceProvider serviceProvider,
            IDomainEventDispatcherEFCore? eventDispatcher = null,
            ILogger<UnitOfWorkWithEventsAsync>? logger = null)
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

        #region [ IUnitOfWorkAsync Implementation ]

        /// <inheritdoc />
        public IRepositoryAsync<T> GetRepository<T>() where T : class, IEntityBase
        {
            if (!_repositories.ContainsKey(typeof(T)))
            {
                _repositories.Add(typeof(T), _serviceProvider.GetService<IRepositoryAsync<T>>()!);
            }
            return (_repositories[typeof(T)] as IRepositoryAsync<T>)!;
        }

        /// <inheritdoc />
        public IDbConnection GetConnection()
        {
            return _dbContext.Database.GetDbConnection();
        }

        /// <inheritdoc />
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-unitofwork-events-savechangesasync-start");
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    await RollbackAsync();
                    return 0;
                }

                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-unitofwork-events-savechangesasync-end");
            }
        }

        /// <inheritdoc />
        public async Task RollbackAsync()
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-unitofwork-events-rollbackasync-start");
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

                await Task.CompletedTask;
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-unitofwork-events-rollbackasync-end");
            }
        }

        #endregion

        #region [ IUnitOfWorkWithEventsAsync Implementation ]

        /// <inheritdoc />
        public async Task<int> SaveChangesWithEventsAsync(CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-unitofwork-events-savechangeswitheventsasync-start");
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    await RollbackAsync();
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
                var result = await _dbContext.SaveChangesAsync(cancellationToken);

                _logger?.LogDebug(
                    "[UnitOfWork] Saved {RowCount} rows, dispatching domain events",
                    result);

                // Dispatch domain events after successful save
                if (_eventDispatcher != null && entitiesWithEvents.Count > 0)
                {
                    await _eventDispatcher.DispatchEventsAsync(entitiesWithEvents, cancellationToken);
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
                    "[UnitOfWork] Error during SaveChangesWithEventsAsync: {Message}",
                    ex.Message);
                throw;
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-unitofwork-events-savechangeswitheventsasync-end");
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
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
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

