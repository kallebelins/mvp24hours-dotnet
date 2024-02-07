//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb
{
    /// <summary>
    /// A Mvp24HoursContext instance represents a session with the database MongoDb and can be used to query and save instances of your entities.
    /// </summary>
    public class Mvp24HoursContext : IDisposable
    {
        #region [ Properties / Fields ]
        public string DatabaseName { get; private set; }
        public string ConnectionString { get; private set; }
        public bool EnableTls { get; private set; }
        public bool EnableTransaction { get; private set; }

        public virtual IMongoDatabase Database { get; private set; }
        public MongoClient MongoClient { get; private set; }

        public IClientSessionHandle Session { get; private set; }

        private bool _isTransactionAsync;
        #endregion

        #region [ Ctors ]
        [ActivatorUtilitiesConstructor]
        public Mvp24HoursContext(IOptions<MongoDbOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options), "Options is required.");
            }

            DatabaseName = options.Value.DatabaseName;
            ConnectionString = options.Value.ConnectionString;
            EnableTls = options.Value.EnableTls;
            EnableTransaction = options.Value.EnableTransaction;

            Configure();
        }

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

            Configure();
        }

        private void Configure()
        {
            MongoClientSettings settings = MongoClientSettings.FromConnectionString(ConnectionString);
            if (EnableTls)
            {
                settings.SslSettings = new SslSettings { EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 };
            }
            MongoClient = new MongoClient(settings);
            Database = MongoClient.GetDatabase(DatabaseName);
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
