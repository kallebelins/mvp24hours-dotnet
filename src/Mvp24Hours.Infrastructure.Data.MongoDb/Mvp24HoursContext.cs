//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.Security;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb
{
    /// <summary>
    /// A Mvp24HoursContext instance represents a session with the database MongoDb and can be used to query and save instances of your entities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This context provides:
    /// <list type="bullet">
    ///   <item>Connection management with configurable authentication</item>
    ///   <item>Transaction support for MongoDB 4.0+ replica sets</item>
    ///   <item>TLS/SSL configuration</item>
    ///   <item>Multi-tenancy support via ITenantProvider</item>
    ///   <item>Session management for transactions</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class Mvp24HoursContext : IDisposable
    {
        #region [ Properties / Fields ]
        /// <summary>
        /// Gets the database name.
        /// </summary>
        public string DatabaseName { get; private set; }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// Gets whether TLS is enabled.
        /// </summary>
        public bool EnableTls { get; private set; }

        /// <summary>
        /// Gets whether transactions are enabled.
        /// </summary>
        public bool EnableTransaction { get; private set; }

        /// <summary>
        /// Gets whether multi-tenancy is enabled.
        /// </summary>
        public bool EnableMultiTenancy { get; private set; }

        /// <summary>
        /// Gets the MongoDB database instance.
        /// </summary>
        public virtual IMongoDatabase Database { get; private set; }

        /// <summary>
        /// Gets the MongoDB client instance.
        /// </summary>
        public MongoClient MongoClient { get; private set; }

        /// <summary>
        /// Gets the current session handle for transactions.
        /// </summary>
        public IClientSessionHandle Session { get; private set; }

        /// <summary>
        /// Gets the tenant provider for multi-tenancy support.
        /// </summary>
        public ITenantProvider TenantProvider { get; private set; }

        /// <summary>
        /// Gets the row-level security helper.
        /// </summary>
        public MongoDbRowLevelSecurity RowLevelSecurity { get; private set; }

        /// <summary>
        /// Gets the MongoDB options.
        /// </summary>
        protected MongoDbOptions Options { get; private set; }

        private bool _isTransactionAsync;
        #endregion

        #region [ Ctors ]
        /// <summary>
        /// Initializes a new instance of the <see cref="Mvp24HoursContext"/> class with options from DI.
        /// </summary>
        /// <param name="options">The MongoDB options.</param>
        /// <param name="tenantProvider">Optional tenant provider for multi-tenancy.</param>
        /// <param name="currentUserProvider">Optional current user provider for RLS.</param>
        [ActivatorUtilitiesConstructor]
        public Mvp24HoursContext(
            IOptions<MongoDbOptions> options,
            ITenantProvider tenantProvider = null,
            ICurrentUserProvider currentUserProvider = null)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options), "Options is required.");
            }

            Options = options.Value;
            DatabaseName = Options.DatabaseName;
            ConnectionString = Options.ConnectionString;
            EnableTls = Options.EnableTls;
            EnableTransaction = Options.EnableTransaction;
            EnableMultiTenancy = Options.EnableMultiTenancy;
            TenantProvider = tenantProvider;

            // Initialize Row-Level Security
            RowLevelSecurity = new MongoDbRowLevelSecurity(tenantProvider, currentUserProvider);

            Configure(Options);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Mvp24HoursContext"/> class with basic parameters.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="enableTls">Whether to enable TLS.</param>
        /// <param name="enableTransaction">Whether to enable transactions.</param>
        public Mvp24HoursContext(string databaseName, string connectionString, bool enableTls = false, bool enableTransaction = false)
        {
            if (!databaseName.HasValue())
            {
                throw new ArgumentNullException(nameof(databaseName), "Database name is required.");
            }

            if (!connectionString.HasValue())
            {
                throw new ArgumentNullException(nameof(connectionString), "ConnectionString is required.");
            }

            DatabaseName = databaseName;
            ConnectionString = connectionString;
            EnableTls = enableTls;
            EnableTransaction = enableTransaction;

            Options = new MongoDbOptions
            {
                DatabaseName = databaseName,
                ConnectionString = connectionString,
                EnableTls = enableTls,
                EnableTransaction = enableTransaction
            };

            Configure(Options);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Mvp24HoursContext"/> class with full options.
        /// </summary>
        /// <param name="options">The MongoDB options.</param>
        /// <param name="tenantProvider">Optional tenant provider.</param>
        /// <param name="currentUserProvider">Optional current user provider.</param>
        public Mvp24HoursContext(
            MongoDbOptions options,
            ITenantProvider tenantProvider = null,
            ICurrentUserProvider currentUserProvider = null)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            DatabaseName = options.DatabaseName;
            ConnectionString = options.ConnectionString;
            EnableTls = options.EnableTls;
            EnableTransaction = options.EnableTransaction;
            EnableMultiTenancy = options.EnableMultiTenancy;
            TenantProvider = tenantProvider;

            RowLevelSecurity = new MongoDbRowLevelSecurity(tenantProvider, currentUserProvider);

            Configure(options);
        }

        private void Configure(MongoDbOptions options)
        {
            MongoClientSettings settings = MongoClientSettings.FromConnectionString(ConnectionString);

            // Apply authentication options
            if (options.Authentication != null)
            {
                options.Authentication.ApplyTo(settings);
                TelemetryHelper.Execute(TelemetryLevels.Verbose,
                    "mongodb-context-auth-configured",
                    new { Mechanism = options.Authentication.Mechanism });
            }
            else if (EnableTls)
            {
                // Basic TLS without advanced authentication
                settings.SslSettings = new SslSettings
                {
                    EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
                };
                settings.UseTls = true;
            }

            // Apply read/write preferences
            ConfigureReadWriteSettings(settings, options);

            // Apply connection pool settings
            ConfigureConnectionPool(settings, options);

            // Apply retry settings
            settings.RetryReads = options.RetryReads;
            settings.RetryWrites = options.RetryWrites;

            // Enable command logging if configured
            if (options.EnableCommandLogging)
            {
                ConfigureCommandLogging(settings);
            }

            MongoClient = new MongoClient(settings);
            Database = MongoClient.GetDatabase(DatabaseName);

            TelemetryHelper.Execute(TelemetryLevels.Verbose,
                "mongodb-context-configured",
                new
                {
                    Database = DatabaseName,
                    TLS = settings.UseTls,
                    MultiTenancy = EnableMultiTenancy,
                    Transactions = EnableTransaction
                });
        }

        private static void ConfigureReadWriteSettings(MongoClientSettings settings, MongoDbOptions options)
        {
            // Read preference
            if (!string.IsNullOrEmpty(options.ReadPreference))
            {
                settings.ReadPreference = options.ReadPreference.ToLower() switch
                {
                    "primary" => ReadPreference.Primary,
                    "primarypreferred" => ReadPreference.PrimaryPreferred,
                    "secondary" => ReadPreference.Secondary,
                    "secondarypreferred" => ReadPreference.SecondaryPreferred,
                    "nearest" => ReadPreference.Nearest,
                    _ => ReadPreference.Primary
                };
            }

            // Write concern
            if (!string.IsNullOrEmpty(options.WriteConcern))
            {
                settings.WriteConcern = options.WriteConcern.ToLower() switch
                {
                    "w1" or "1" => WriteConcern.W1,
                    "w2" or "2" => WriteConcern.W2,
                    "w3" or "3" => WriteConcern.W3,
                    "majority" => WriteConcern.WMajority,
                    "acknowledged" => WriteConcern.Acknowledged,
                    "unacknowledged" => WriteConcern.Unacknowledged,
                    _ => WriteConcern.Acknowledged
                };
            }

            // Read concern
            if (!string.IsNullOrEmpty(options.ReadConcern))
            {
                settings.ReadConcern = options.ReadConcern.ToLower() switch
                {
                    "local" => ReadConcern.Local,
                    "available" => ReadConcern.Available,
                    "majority" => ReadConcern.Majority,
                    "linearizable" => ReadConcern.Linearizable,
                    "snapshot" => ReadConcern.Snapshot,
                    _ => ReadConcern.Local
                };
            }
        }

        private static void ConfigureConnectionPool(MongoClientSettings settings, MongoDbOptions options)
        {
            if (options.ConnectionTimeoutSeconds.HasValue)
            {
                settings.ConnectTimeout = TimeSpan.FromSeconds(options.ConnectionTimeoutSeconds.Value);
            }

            if (options.SocketTimeoutSeconds.HasValue)
            {
                settings.SocketTimeout = TimeSpan.FromSeconds(options.SocketTimeoutSeconds.Value);
            }

            if (options.MaxConnectionPoolSize.HasValue)
            {
                settings.MaxConnectionPoolSize = options.MaxConnectionPoolSize.Value;
            }

            if (options.MinConnectionPoolSize.HasValue)
            {
                settings.MinConnectionPoolSize = options.MinConnectionPoolSize.Value;
            }
        }

        private static void ConfigureCommandLogging(MongoClientSettings settings)
        {
            settings.ClusterConfigurator = builder =>
            {
                builder.Subscribe<CommandStartedEvent>(e =>
                {
                    TelemetryHelper.Execute(TelemetryLevels.Verbose,
                        $"mongodb-command-started-{e.CommandName}",
                        new { CommandName = e.CommandName, DatabaseName = e.DatabaseNamespace?.DatabaseName });
                });

                builder.Subscribe<CommandSucceededEvent>(e =>
                {
                    TelemetryHelper.Execute(TelemetryLevels.Verbose,
                        $"mongodb-command-succeeded-{e.CommandName}",
                        new { CommandName = e.CommandName, Duration = e.Duration });
                });

                builder.Subscribe<CommandFailedEvent>(e =>
                {
                    TelemetryHelper.Execute(TelemetryLevels.Warning,
                        $"mongodb-command-failed-{e.CommandName}",
                        new { CommandName = e.CommandName, Duration = e.Duration, Error = e.Failure?.Message });
                });
            };
        }
        #endregion

        #region [ Methods ]
        public IMongoCollection<TEntity> Set<TEntity>()
        {
            return Set<TEntity>(typeof(TEntity).Name);
        }

        public IMongoCollection<TEntity> Set<TEntity>(string name)
        {
            return Database.GetCollection<TEntity>(name);
        }

        public void StartSession(CancellationToken cancellationToken = default)
        {
            Session = MongoClient.StartSession(cancellationToken: cancellationToken);
            if (EnableTransaction)
            {
                Session.StartTransaction();
            }
        }

        public void SaveChanges(CancellationToken cancellationToken = default)
        {
            if (Session != null && Session.IsInTransaction)
            {
                Session.CommitTransaction(cancellationToken);
            }
        }

        public void Rollback(CancellationToken cancellationToken = default)
        {
            if (Session != null && Session.IsInTransaction)
            {
                Session.AbortTransaction(cancellationToken);
            }
        }

        public async Task StartSessionAsync(CancellationToken cancellationToken = default)
        {
            Session = await MongoClient.StartSessionAsync(cancellationToken: cancellationToken);
            if (EnableTransaction)
            {
                Session.StartTransaction();
                this._isTransactionAsync = true;
            }
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (Session != null && Session.IsInTransaction)
            {
                await Session.CommitTransactionAsync(cancellationToken);
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (Session != null && Session.IsInTransaction)
            {
                await Session.AbortTransactionAsync(cancellationToken);
            }
        }

        #endregion

        #region [ IDisposable ]

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            while (Session != null && Session.IsInTransaction)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
            }

            if (Session != null)
            {
                if (Session.IsInTransaction)
                {
                    if (this._isTransactionAsync)
                    {
                        Session.CommitTransactionAsync();
                    }
                    else
                    {
                        Session.CommitTransaction();
                    }
                }
                Session.Dispose();
            }
        }

        #endregion
    }
}
