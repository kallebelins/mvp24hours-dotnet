//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.DistributedLocking.Contract;
using Mvp24Hours.Infrastructure.DistributedLocking.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.DistributedLocking.Providers
{
    /// <summary>
    /// SQL Server distributed lock provider using sp_getapplock stored procedure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider uses SQL Server's application-level locking mechanism via
    /// <c>sp_getapplock</c> and <c>sp_releaseapplock</c> stored procedures.
    /// </para>
    /// <para>
    /// <strong>Features:</strong>
    /// <list type="bullet">
    /// <item>Uses SQL Server's built-in locking mechanism</item>
    /// <item>Supports transaction-scoped and session-scoped locks</item>
    /// <item>Automatic lock release on transaction commit/rollback (if transaction-scoped)</item>
    /// <item>Lock timeout support</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Lock Modes:</strong>
    /// The provider uses "Exclusive" lock mode by default, which prevents other
    /// sessions from acquiring the same lock. Other modes (Shared, Update, etc.)
    /// can be configured if needed.
    /// </para>
    /// </remarks>
    public class SqlServerDistributedLockProvider : BaseDistributedLockProvider
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlServerDistributedLockProvider>? _logger;
        private readonly string _lockOwner;
        private readonly string _lockMode;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerDistributedLockProvider"/> class.
        /// </summary>
        /// <param name="connectionString">SQL Server connection string.</param>
        /// <param name="logger">Optional logger.</param>
        /// <param name="metrics">Optional metrics collector.</param>
        /// <param name="lockOwner">Lock owner: "Session" (default) or "Transaction".</param>
        /// <param name="lockMode">Lock mode: "Exclusive" (default), "Shared", "Update", etc.</param>
        public SqlServerDistributedLockProvider(
            string connectionString,
            ILogger<SqlServerDistributedLockProvider>? logger = null,
            DistributedLockMetrics? metrics = null,
            string lockOwner = "Session",
            string lockMode = "Exclusive")
            : base(metrics)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

            _connectionString = connectionString;
            _logger = logger;
            _lockOwner = lockOwner ?? "Session";
            _lockMode = lockMode ?? "Exclusive";

            _logger?.LogInformation(
                "SqlServerDistributedLockProvider initialized: LockOwner={LockOwner}, LockMode={LockMode}",
                _lockOwner, _lockMode);
        }

        /// <inheritdoc />
        protected override string ProviderName => "SqlServer";

        /// <inheritdoc />
        protected override async Task<(bool Success, string LockId, DateTimeOffset ExpiresAt, long? FencedToken)> TryAcquireLockCoreAsync(
            string resource,
            string lockId,
            TimeSpan duration,
            CancellationToken cancellationToken)
        {
            // SQL Server locks don't have explicit expiration, but we track it for consistency
            var expiresAt = DateTimeOffset.UtcNow.Add(duration);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                using var command = new SqlCommand("sp_getapplock", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.Add(new SqlParameter("@Resource", SqlDbType.NVarChar, 255) { Value = GetLockKey(resource) });
                command.Parameters.Add(new SqlParameter("@LockMode", SqlDbType.NVarChar, 32) { Value = _lockMode });
                command.Parameters.Add(new SqlParameter("@LockOwner", SqlDbType.NVarChar, 32) { Value = _lockOwner });
                command.Parameters.Add(new SqlParameter("@LockTimeout", SqlDbType.Int) { Value = 0 }); // No wait, fail immediately
                command.Parameters.Add(new SqlParameter("@DbPrincipal", SqlDbType.NVarChar, 128) { Value = DBNull.Value });

                var returnValue = new SqlParameter("@ReturnValue", SqlDbType.Int)
                {
                    Direction = ParameterDirection.ReturnValue
                };
                command.Parameters.Add(returnValue);

                await command.ExecuteNonQueryAsync(cancellationToken);

                var result = (int)returnValue.Value;

                // Return values:
                // 0 = Success
                // 1 = Lock granted after waiting
                // -1 = Lock request timed out
                // -2 = Lock request canceled
                // -3 = Lock request chosen as deadlock victim
                // -999 = Parameter validation or other error

                if (result >= 0)
                {
                    _logger?.LogDebug(
                        "Lock acquired on SQL Server: Resource={Resource}, LockId={LockId}, ReturnValue={ReturnValue}",
                        resource, lockId, result);

                    // Store lock ID in a way that we can verify it later
                    // Note: SQL Server doesn't return a lock ID, so we use the resource key
                    return (true, GetLockKey(resource), expiresAt, null);
                }
                else
                {
                    _logger?.LogDebug(
                        "Lock acquisition failed on SQL Server: Resource={Resource}, LockId={LockId}, ReturnValue={ReturnValue}",
                        resource, lockId, result);

                    return (false, lockId, expiresAt, null);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error acquiring lock on SQL Server: Resource={Resource}", resource);
                return (false, lockId, expiresAt, null);
            }
        }

        /// <inheritdoc />
        protected override async Task<bool> ReleaseLockCoreAsync(
            string resource,
            string lockId,
            CancellationToken cancellationToken)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                using var command = new SqlCommand("sp_releaseapplock", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.Add(new SqlParameter("@Resource", SqlDbType.NVarChar, 255) { Value = GetLockKey(resource) });
                command.Parameters.Add(new SqlParameter("@LockOwner", SqlDbType.NVarChar, 32) { Value = _lockOwner });
                command.Parameters.Add(new SqlParameter("@DbPrincipal", SqlDbType.NVarChar, 128) { Value = DBNull.Value });

                var returnValue = new SqlParameter("@ReturnValue", SqlDbType.Int)
                {
                    Direction = ParameterDirection.ReturnValue
                };
                command.Parameters.Add(returnValue);

                await command.ExecuteNonQueryAsync(cancellationToken);

                var result = (int)returnValue.Value;

                // Return values:
                // 0 = Success
                // -999 = Parameter validation or other error

                if (result == 0)
                {
                    RecordRelease(resource);
                    _logger?.LogDebug(
                        "Lock released on SQL Server: Resource={Resource}",
                        resource);
                    return true;
                }
                else
                {
                    _logger?.LogWarning(
                        "Lock release failed on SQL Server: Resource={Resource}, ReturnValue={ReturnValue}",
                        resource, result);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error releasing lock on SQL Server: Resource={Resource}", resource);
                return false;
            }
        }

        /// <inheritdoc />
        protected override async Task<bool> RenewLockCoreAsync(
            string resource,
            string lockId,
            TimeSpan duration,
            CancellationToken cancellationToken)
        {
            // SQL Server locks don't have explicit expiration, so renewal is a no-op
            // However, we can verify the lock is still held by attempting to acquire it again
            // For session-scoped locks, they remain until released or connection closes

            // Check if lock is still held
            return await IsLockedCoreAsync(resource, cancellationToken);
        }

        /// <inheritdoc />
        protected override async Task<bool> IsLockedCoreAsync(
            string resource,
            CancellationToken cancellationToken)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                // Query sys.dm_tran_locks to check if lock exists
                var query = @"
                    SELECT COUNT(*) 
                    FROM sys.dm_tran_locks 
                    WHERE resource_type = 'APPLICATION' 
                    AND resource_database_id = DB_ID() 
                    AND resource_associated_entity_id IS NULL
                    AND resource_description = @Resource";

                using var command = new SqlCommand(query, connection);
                command.Parameters.Add(new SqlParameter("@Resource", SqlDbType.NVarChar, 255) { Value = GetLockKey(resource) });

                var count = (int)await command.ExecuteScalarAsync(cancellationToken);
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking lock status on SQL Server: Resource={Resource}", resource);
                return false;
            }
        }

        /// <inheritdoc />
        protected override ILockHandle CreateLockHandle(
            string resource,
            string lockId,
            DateTimeOffset expiresAt,
            long? fencedToken,
            DistributedLockOptions options)
        {
            return new SqlServerLockHandle(this, resource, lockId, expiresAt, fencedToken, options);
        }

        private static string GetLockKey(string resource)
        {
            // SQL Server resource names are limited to 255 characters
            // Use a prefix to avoid conflicts
            return $"Mvp24Hours_Lock_{resource}";
        }

        private class SqlServerLockHandle : LockHandleBase
        {
            private readonly SqlServerDistributedLockProvider _provider;

            public SqlServerLockHandle(
                SqlServerDistributedLockProvider provider,
                string resource,
                string lockId,
                DateTimeOffset expiresAt,
                long? fencedToken,
                DistributedLockOptions options)
                : base(resource, lockId, expiresAt, fencedToken, options)
            {
                _provider = provider;
            }

            protected override Task<bool> ReleaseLockAsync(CancellationToken cancellationToken)
            {
                return _provider.ReleaseLockCoreAsync(Resource, LockId, cancellationToken);
            }

            protected override Task<bool> RenewLockAsync(CancellationToken cancellationToken)
            {
                return _provider.RenewLockCoreAsync(Resource, LockId, _options.LockDuration, cancellationToken);
            }
        }
    }
}

