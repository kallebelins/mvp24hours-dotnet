//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.MongoDb.Cqrs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;

namespace Mvp24Hours.Infrastructure.Data.MongoDb
{
    /// <summary>
    /// Unit of Work implementation with automatic Domain Event dispatching (synchronous version).
    /// Extends <see cref="UnitOfWork"/> functionality to automatically collect and dispatch
    /// domain events from tracked entities after successful persistence.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Key Features:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Manual tracking of entities with domain events (MongoDB doesn't have automatic change tracking)</description></item>
    /// <item><description>Events dispatched only after successful SaveChanges</description></item>
    /// <item><description>Events cleared from entities after dispatch</description></item>
    /// <item><description>Thread-safe entity tracking</description></item>
    /// </list>
    /// <para>
    /// <strong>Important Note:</strong>
    /// Unlike EF Core, MongoDB doesn't have built-in change tracking. You must explicitly
    /// register entities that have domain events using <see cref="TrackEntity"/> method,
    /// or use the repository integration which does this automatically.
    /// </para>
    /// <para>
    /// <strong>Usage:</strong>
    /// Register this implementation instead of <see cref="UnitOfWork"/> when you need
    /// automatic domain event dispatching. You must also register an <see cref="IDomainEventDispatcherMongoDb"/>
    /// implementation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In Startup/Program.cs
    /// services.AddScoped&lt;IUnitOfWorkWithEvents, UnitOfWorkWithEvents&gt;();
    /// services.AddScoped&lt;IDomainEventDispatcherMongoDb, DomainEventDispatcherAdapter&gt;();
    /// 
    /// // Usage
    /// var unitOfWork = serviceProvider.GetService&lt;IUnitOfWorkWithEvents&gt;();
    /// var order = new Order();
    /// order.Place(); // Raises OrderPlacedEvent
    /// 
    /// var repo = unitOfWork.GetRepository&lt;Order&gt;();
    /// repo.Add(order);
    /// 
    /// // Track entity for event dispatching
    /// unitOfWork.TrackEntity(order);
    /// 
    /// // Automatically dispatches OrderPlacedEvent after save
    /// unitOfWork.SaveChangesWithEvents();
    /// </code>
    /// </example>
    public class UnitOfWorkWithEvents : IUnitOfWorkWithEvents
    {
        #region [ Fields ]

        private readonly Dictionary<Type, object> _repositories;
        private readonly IServiceProvider? _serviceProvider;
        private readonly IDomainEventDispatcherMongoDb? _eventDispatcher;
        private readonly ILogger<UnitOfWorkWithEvents>? _logger;
        private readonly ConcurrentDictionary<object, IHasDomainEvents> _trackedEntities;

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the underlying MongoDB context.
        /// </summary>
        protected Mvp24HoursContext DbContext { get; private set; }

        #endregion

        #region [ Ctor ]

        /// <summary>
        /// Creates a new instance with explicitly provided repositories.
        /// </summary>
        /// <param name="dbContext">The MongoDB context.</param>
        /// <param name="repositories">Dictionary of pre-created repositories.</param>
        /// <param name="eventDispatcher">Optional domain event dispatcher.</param>
        /// <param name="logger">Optional logger.</param>
        public UnitOfWorkWithEvents(
            Mvp24HoursContext dbContext,
            Dictionary<Type, object> repositories,
            IDomainEventDispatcherMongoDb? eventDispatcher = null,
            ILogger<UnitOfWorkWithEvents>? logger = null)
        {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
            _eventDispatcher = eventDispatcher;
            _logger = logger;
            _trackedEntities = new ConcurrentDictionary<object, IHasDomainEvents>();

            DbContext.StartSession();
        }

        /// <summary>
        /// Creates a new instance with DI-based repository resolution.
        /// </summary>
        /// <param name="dbContext">The MongoDB context.</param>
        /// <param name="serviceProvider">Service provider for resolving repositories.</param>
        /// <param name="eventDispatcher">Optional domain event dispatcher.</param>
        /// <param name="logger">Optional logger.</param>
        [ActivatorUtilitiesConstructor]
        public UnitOfWorkWithEvents(
            Mvp24HoursContext dbContext,
            IServiceProvider serviceProvider,
            IDomainEventDispatcherMongoDb? eventDispatcher = null,
            ILogger<UnitOfWorkWithEvents>? logger = null)
        {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _repositories = [];
            _eventDispatcher = eventDispatcher;
            _logger = logger;
            _trackedEntities = new ConcurrentDictionary<object, IHasDomainEvents>();

            DbContext.StartSession();
        }

        #endregion

        #region [ IUnitOfWork Implementation ]

        /// <inheritdoc />
        public IRepository<T> GetRepository<T>() where T : class, IEntityBase
        {
            if (!_repositories.ContainsKey(typeof(T)))
            {
                _repositories.Add(typeof(T), _serviceProvider!.GetService<IRepository<T>>()!);
            }
            return (_repositories[typeof(T)] as IRepository<T>)!;
        }

        /// <inheritdoc />
        [Obsolete("MongoDb does not support IDbConnection. Use the database (IMongoDatabase) from context.", true)]
        public IDbConnection GetConnection()
        {
            throw new NotSupportedException("MongoDb does not support IDbConnection. Use the database (IMongoDatabase) from context.");
        }

        /// <inheritdoc />
        public int SaveChanges(CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-unitofwork-events-savechanges-start");
            try
            {
                DbContext.SaveChanges(cancellationToken);
                return 1;
            }
            catch (Exception)
            {
                Rollback();
                return 0;
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-unitofwork-events-savechanges-end");
            }
        }

        /// <inheritdoc />
        public void Rollback()
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-unitofwork-events-rollback-start");
            try
            {
                DbContext.Rollback();
                ClearTrackedEntities();
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-unitofwork-events-rollback-end");
            }
        }

        #endregion

        #region [ IUnitOfWorkWithEvents Implementation ]

        /// <inheritdoc />
        public int SaveChangesWithEvents(CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-unitofwork-events-savechangeswithevents-start");
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
                DbContext.SaveChanges(cancellationToken);

                _logger?.LogDebug(
                    "[UnitOfWork] Saved changes, dispatching domain events");

                // Dispatch domain events after successful save
                if (_eventDispatcher != null && entitiesWithEvents.Count > 0)
                {
                    _eventDispatcher.DispatchEvents(entitiesWithEvents, cancellationToken);
                }
                else if (_eventDispatcher == null && allEvents.Count > 0)
                {
                    _logger?.LogWarning(
                        "[UnitOfWork] {EventCount} domain events were not dispatched because no IDomainEventDispatcherMongoDb is registered",
                        allEvents.Count);

                    // Clear events even without dispatcher to prevent memory leaks
                    foreach (var entity in entitiesWithEvents)
                    {
                        entity.ClearDomainEvents();
                    }
                }

                // Clear tracked entities after successful dispatch
                ClearTrackedEntities();

                return 1;
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "[UnitOfWork] Error during SaveChangesWithEvents: {Message}",
                    ex.Message);

                Rollback();
                return 0;
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-unitofwork-events-savechangeswithevents-end");
            }
        }

        /// <inheritdoc />
        public IEnumerable<IHasDomainEvents> GetEntitiesWithEvents()
        {
            return _trackedEntities.Values
                .Where(e => e.DomainEvents.Count > 0);
        }

        #endregion

        #region [ Entity Tracking ]

        /// <summary>
        /// Tracks an entity for domain event dispatching.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entity">The entity to track.</param>
        /// <remarks>
        /// <para>
        /// Since MongoDB doesn't have automatic change tracking like EF Core,
        /// entities must be explicitly tracked for domain event dispatching.
        /// </para>
        /// <para>
        /// The repository implementations can automatically call this method
        /// when adding or updating entities that implement <see cref="IHasDomainEvents"/>.
        /// </para>
        /// </remarks>
        public void TrackEntity<T>(T entity) where T : class
        {
            if (entity is IHasDomainEvents entityWithEvents)
            {
                _trackedEntities.TryAdd(entity, entityWithEvents);
            }
        }

        /// <summary>
        /// Tracks multiple entities for domain event dispatching.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entities">The entities to track.</param>
        public void TrackEntities<T>(IEnumerable<T> entities) where T : class
        {
            foreach (var entity in entities)
            {
                TrackEntity(entity);
            }
        }

        /// <summary>
        /// Stops tracking an entity for domain event dispatching.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entity">The entity to stop tracking.</param>
        public void UntrackEntity<T>(T entity) where T : class
        {
            _trackedEntities.TryRemove(entity, out _);
        }

        /// <summary>
        /// Clears all tracked entities.
        /// </summary>
        public void ClearTrackedEntities()
        {
            _trackedEntities.Clear();
        }

        /// <summary>
        /// Gets the count of tracked entities.
        /// </summary>
        public int TrackedEntitiesCount => _trackedEntities.Count;

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
                ClearTrackedEntities();
                DbContext = null!;
            }
        }

        #endregion
    }
}

