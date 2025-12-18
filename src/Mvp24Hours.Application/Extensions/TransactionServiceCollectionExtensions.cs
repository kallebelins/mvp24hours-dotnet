//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Application.Contract.Transaction;
using Mvp24Hours.Application.Logic.Transaction;
using System;

namespace Mvp24Hours.Application.Extensions
{
    /// <summary>
    /// Extension methods for registering transaction services in the DI container.
    /// </summary>
    public static class TransactionServiceCollectionExtensions
    {
        /// <summary>
        /// Adds transaction scope support to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers:
        /// <list type="bullet">
        /// <item><see cref="ITransactionScopeFactory"/> as singleton</item>
        /// <item><see cref="ITransactionScope"/> as transient</item>
        /// <item><see cref="ITransactionScopeSync"/> as transient</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Before calling this method, ensure that <see cref="Mvp24Hours.Core.Contract.Data.IUnitOfWork"/>
        /// and/or <see cref="Mvp24Hours.Core.Contract.Data.IUnitOfWorkAsync"/> are registered.
        /// </para>
        /// <para>
        /// <strong>Example:</strong>
        /// <code>
        /// services.AddMvp24HoursDbContext&lt;MyDbContext&gt;();
        /// services.AddTransactionScope();
        /// </code>
        /// </para>
        /// </remarks>
        public static IServiceCollection AddTransactionScope(this IServiceCollection services)
        {
            // Register factory as singleton
            services.TryAddSingleton<ITransactionScopeFactory, TransactionScopeFactory>();

            // Register transaction scope as transient (new instance per request)
            services.TryAddTransient<ITransactionScope>(sp =>
            {
                var factory = sp.GetRequiredService<ITransactionScopeFactory>();
                return factory.Create();
            });

            services.TryAddTransient<ITransactionScopeSync>(sp =>
            {
                var factory = sp.GetRequiredService<ITransactionScopeFactory>();
                return factory.CreateSync();
            });

            return services;
        }

        /// <summary>
        /// Adds transaction scope support with custom configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration action for transaction options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddTransactionScope(
            this IServiceCollection services,
            Action<TransactionScopeOptions> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var options = new TransactionScopeOptions();
            configure(options);

            services.AddSingleton(options);
            services.AddTransactionScope();

            return services;
        }
    }

    /// <summary>
    /// Options for configuring transaction scope behavior.
    /// </summary>
    public sealed class TransactionScopeOptions
    {
        /// <summary>
        /// Gets or sets the default timeout for transactions in seconds.
        /// Default is 30 seconds.
        /// </summary>
        public int DefaultTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the default isolation level.
        /// Default is ReadCommitted.
        /// </summary>
        public TransactionIsolationLevel DefaultIsolationLevel { get; set; } = TransactionIsolationLevel.ReadCommitted;

        /// <summary>
        /// Gets or sets whether to enable automatic retry on transient failures.
        /// Default is false.
        /// </summary>
        public bool EnableRetryOnTransientFailure { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// Default is 3.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets whether to log transaction events.
        /// Default is true.
        /// </summary>
        public bool EnableLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to automatically rollback on dispose if not committed.
        /// Default is true.
        /// </summary>
        public bool AutoRollbackOnDispose { get; set; } = true;
    }
}

