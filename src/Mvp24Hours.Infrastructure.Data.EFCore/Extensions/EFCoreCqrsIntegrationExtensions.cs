//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using Mvp24Hours.Infrastructure.Data.EFCore.Cqrs;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using System;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Extensions
{
    /// <summary>
    /// Extension methods for integrating EF Core with CQRS patterns.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions provide convenient methods for setting up:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Unit of Work with domain event support</description></item>
    /// <item><description>Domain event dispatching via interceptors</description></item>
    /// <item><description>Read/Write DbContext separation (CQRS)</description></item>
    /// <item><description>Integration with the Mvp24Hours.Infrastructure.Cqrs module</description></item>
    /// </list>
    /// </remarks>
    public static class EFCoreCqrsIntegrationExtensions
    {
        #region [ Unit of Work with Events ]

        /// <summary>
        /// Adds the Unit of Work with domain event support.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">Service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This registers <see cref="IUnitOfWorkWithEventsAsync"/> with the
        /// <see cref="UnitOfWorkWithEventsAsync"/> implementation.
        /// </para>
        /// <para>
        /// <strong>Requirements:</strong>
        /// </para>
        /// <list type="bullet">
        /// <item><description>A DbContext must be registered</description></item>
        /// <item><description>Optionally register <see cref="IDomainEventDispatcherEFCore"/> for event dispatch</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddDbContext&lt;AppDbContext&gt;(options => ...);
        /// services.AddMvp24HoursUnitOfWorkWithEvents();
        /// services.AddMvp24HoursDomainEventDispatcher(); // Optional
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursUnitOfWorkWithEvents(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-cqrs-addunitofworkwithevents");

            services.Add(new ServiceDescriptor(
                typeof(IUnitOfWorkWithEventsAsync),
                typeof(UnitOfWorkWithEventsAsync),
                lifetime));

            return services;
        }

        /// <summary>
        /// Adds the Unit of Work with domain event support and repository.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Optional repository configuration.</param>
        /// <param name="repositoryAsync">Custom repository type (default: RepositoryAsync&lt;T&gt;).</param>
        /// <param name="lifetime">Service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursRepositoryWithEvents();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursRepositoryWithEvents(
            this IServiceCollection services,
            Action<EFCoreRepositoryOptions>? options = null,
            Type? repositoryAsync = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-cqrs-addrepositorywithevents");

            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<EFCoreRepositoryOptions>(opt => { });
            }

            // Register Unit of Work with Events
            services.Add(new ServiceDescriptor(
                typeof(IUnitOfWorkWithEventsAsync),
                typeof(UnitOfWorkWithEventsAsync),
                lifetime));

            // Also register as IUnitOfWorkAsync for compatibility
            services.Add(new ServiceDescriptor(
                typeof(IUnitOfWorkAsync),
                typeof(UnitOfWorkWithEventsAsync),
                lifetime));

            // Register repository
            if (repositoryAsync != null)
            {
                services.Add(new ServiceDescriptor(typeof(IRepositoryAsync<>), repositoryAsync, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IRepositoryAsync<>), typeof(RepositoryAsync<>), lifetime));
            }

            return services;
        }

        #endregion

        #region [ Domain Event Dispatcher ]

        /// <summary>
        /// Adds the no-op domain event dispatcher.
        /// Use this when you want event collection but no actual dispatch.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">Service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursNoOpEventDispatcher(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-cqrs-addnoopeventdispatcher");

            services.Add(new ServiceDescriptor(
                typeof(IDomainEventDispatcherEFCore),
                typeof(NoOpDomainEventDispatcher),
                lifetime));

            return services;
        }

        /// <summary>
        /// Adds a custom domain event dispatcher using a delegate.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="dispatchFunc">
        /// Function to dispatch events. Receives events and cancellation token.
        /// </param>
        /// <param name="lifetime">Service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursDomainEventDispatcher(async (events, ct) =>
        /// {
        ///     foreach (var evt in events)
        ///     {
        ///         Console.WriteLine($"Event: {evt.GetType().Name}");
        ///     }
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursDomainEventDispatcher(
            this IServiceCollection services,
            Func<System.Collections.Generic.IEnumerable<IDomainEvent>, System.Threading.CancellationToken, System.Threading.Tasks.Task> dispatchFunc,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-cqrs-addeventdispatcher-delegate");

            services.Add(new ServiceDescriptor(
                typeof(IDomainEventDispatcherEFCore),
                sp => new DomainEventDispatcherAdapter(dispatchFunc),
                lifetime));

            return services;
        }

        /// <summary>
        /// Adds the domain event SaveChanges interceptor to DbContext options.
        /// This enables automatic domain event dispatch on SaveChanges.
        /// </summary>
        /// <param name="optionsBuilder">The DbContext options builder.</param>
        /// <param name="eventDispatcher">
        /// The event dispatcher. If null, events are cleared but not dispatched.
        /// </param>
        /// <returns>The options builder for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddDbContext&lt;AppDbContext&gt;((sp, options) =>
        /// {
        ///     var dispatcher = sp.GetService&lt;IDomainEventDispatcherEFCore&gt;();
        ///     options.UseSqlServer(connectionString)
        ///            .AddDomainEventInterceptor(dispatcher);
        /// });
        /// </code>
        /// </example>
        public static DbContextOptionsBuilder AddDomainEventInterceptor(
            this DbContextOptionsBuilder optionsBuilder,
            IDomainEventDispatcherEFCore? eventDispatcher = null)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-cqrs-adddomaineventinterceptor");

            optionsBuilder.AddInterceptors(new DomainEventSaveChangesInterceptor(eventDispatcher));
            return optionsBuilder;
        }

        #endregion

        #region [ Read/Write Separation ]

        /// <summary>
        /// Registers read and write DbContext types for CQRS separation.
        /// </summary>
        /// <typeparam name="TReadContext">Read-only DbContext type.</typeparam>
        /// <typeparam name="TWriteContext">Write DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="readOptions">Action to configure read DbContext.</param>
        /// <param name="writeOptions">Action to configure write DbContext.</param>
        /// <param name="lifetime">Service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// <strong>Usage:</strong>
        /// </para>
        /// <list type="bullet">
        /// <item><description>TReadContext should inherit from ReadDbContextBase</description></item>
        /// <item><description>TWriteContext should inherit from WriteDbContextBase</description></item>
        /// <item><description>Configure different connection strings for read replicas</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursCqrsDbContexts&lt;AppReadDbContext, AppWriteDbContext&gt;(
        ///     readOptions => readOptions.UseSqlServer(readConnectionString),
        ///     writeOptions => writeOptions.UseSqlServer(writeConnectionString)
        ///                                 .AddDomainEventInterceptor(eventDispatcher));
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursCqrsDbContexts<TReadContext, TWriteContext>(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder>? readOptions = null,
            Action<DbContextOptionsBuilder>? writeOptions = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TReadContext : DbContext, IReadDbContext
            where TWriteContext : DbContext, IWriteDbContext
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-cqrs-addcqrsdbcontexts");

            // Register read context
            if (readOptions != null)
            {
                services.AddDbContext<TReadContext>(readOptions, lifetime);
            }
            else
            {
                services.AddDbContext<TReadContext>((Action<DbContextOptionsBuilder>?)null, lifetime);
            }

            // Register write context
            if (writeOptions != null)
            {
                services.AddDbContext<TWriteContext>(writeOptions, lifetime);
            }
            else
            {
                services.AddDbContext<TWriteContext>((Action<DbContextOptionsBuilder>?)null, lifetime);
            }

            // Register interfaces
            services.TryAdd(new ServiceDescriptor(typeof(IReadDbContext), sp => sp.GetRequiredService<TReadContext>(), lifetime));
            services.TryAdd(new ServiceDescriptor(typeof(IWriteDbContext), sp => sp.GetRequiredService<TWriteContext>(), lifetime));

            return services;
        }

        /// <summary>
        /// Registers read and write DbContext types with DI-based configuration.
        /// </summary>
        /// <typeparam name="TReadContext">Read-only DbContext type.</typeparam>
        /// <typeparam name="TWriteContext">Write DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="readOptions">Action to configure read DbContext with service provider.</param>
        /// <param name="writeOptions">Action to configure write DbContext with service provider.</param>
        /// <param name="lifetime">Service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursCqrsDbContexts&lt;AppReadDbContext, AppWriteDbContext&gt;(
        ///     (sp, options) => options.UseSqlServer(sp.GetRequiredService&lt;IConfiguration&gt;()["ReadDb"]),
        ///     (sp, options) => 
        ///     {
        ///         var dispatcher = sp.GetService&lt;IDomainEventDispatcherEFCore&gt;();
        ///         options.UseSqlServer(sp.GetRequiredService&lt;IConfiguration&gt;()["WriteDb"])
        ///                .AddDomainEventInterceptor(dispatcher);
        ///     });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursCqrsDbContexts<TReadContext, TWriteContext>(
            this IServiceCollection services,
            Action<IServiceProvider, DbContextOptionsBuilder> readOptions,
            Action<IServiceProvider, DbContextOptionsBuilder> writeOptions,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TReadContext : DbContext, IReadDbContext
            where TWriteContext : DbContext, IWriteDbContext
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-cqrs-addcqrsdbcontexts-sp");

            // Register read context
            services.AddDbContext<TReadContext>(readOptions, lifetime);

            // Register write context
            services.AddDbContext<TWriteContext>(writeOptions, lifetime);

            // Register interfaces
            services.TryAdd(new ServiceDescriptor(typeof(IReadDbContext), sp => sp.GetRequiredService<TReadContext>(), lifetime));
            services.TryAdd(new ServiceDescriptor(typeof(IWriteDbContext), sp => sp.GetRequiredService<TWriteContext>(), lifetime));

            return services;
        }

        #endregion

        #region [ Full CQRS Setup ]

        /// <summary>
        /// Options for configuring CQRS integration with EF Core.
        /// </summary>
        public class EFCoreCqrsOptions
        {
            /// <summary>
            /// Gets or sets whether to use the domain event SaveChanges interceptor.
            /// Default: true
            /// </summary>
            public bool UseDomainEventInterceptor { get; set; } = true;

            /// <summary>
            /// Gets or sets whether to register the Unit of Work with events.
            /// Default: true
            /// </summary>
            public bool UseUnitOfWorkWithEvents { get; set; } = true;

            /// <summary>
            /// Gets or sets whether to use the no-op dispatcher when no CQRS module is present.
            /// Default: true
            /// </summary>
            public bool UseNoOpDispatcherAsFallback { get; set; } = true;
        }

        /// <summary>
        /// Adds full CQRS integration for EF Core.
        /// </summary>
        /// <typeparam name="TDbContext">The DbContext type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="dbContextOptions">Action to configure DbContext.</param>
        /// <param name="cqrsOptions">Action to configure CQRS options.</param>
        /// <param name="lifetime">Service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursEFCoreCqrs&lt;AppDbContext&gt;(
        ///     (sp, options) => options.UseSqlServer(connectionString),
        ///     cqrs => cqrs.UseDomainEventInterceptor = true);
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursEFCoreCqrs<TDbContext>(
            this IServiceCollection services,
            Action<IServiceProvider, DbContextOptionsBuilder> dbContextOptions,
            Action<EFCoreCqrsOptions>? cqrsOptions = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TDbContext : DbContext
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-cqrs-addefcorecqrs");

            var options = new EFCoreCqrsOptions();
            cqrsOptions?.Invoke(options);

            // Register fallback dispatcher if needed
            if (options.UseNoOpDispatcherAsFallback)
            {
                services.TryAdd(new ServiceDescriptor(
                    typeof(IDomainEventDispatcherEFCore),
                    typeof(NoOpDomainEventDispatcher),
                    lifetime));
            }

            // Register DbContext with optional interceptor
            services.AddDbContext<TDbContext>((sp, builder) =>
            {
                dbContextOptions(sp, builder);

                if (options.UseDomainEventInterceptor)
                {
                    var dispatcher = sp.GetService<IDomainEventDispatcherEFCore>();
                    builder.AddDomainEventInterceptor(dispatcher);
                }
            }, lifetime);

            // Register as DbContext for UnitOfWork
            services.TryAdd(new ServiceDescriptor(
                typeof(DbContext),
                sp => sp.GetRequiredService<TDbContext>(),
                lifetime));

            // Register Unit of Work with events
            if (options.UseUnitOfWorkWithEvents)
            {
                services.AddMvp24HoursUnitOfWorkWithEvents(lifetime);
            }

            return services;
        }

        #endregion
    }
}

