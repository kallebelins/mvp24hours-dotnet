//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.DistributedLocking.Contract;
using Mvp24Hours.Infrastructure.DistributedLocking.Metrics;
using Mvp24Hours.Infrastructure.DistributedLocking.Options;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.DistributedLocking.Providers
{
    /// <summary>
    /// PostgreSQL distributed lock provider using advisory locks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider uses PostgreSQL's advisory lock functions (<c>pg_advisory_lock</c>,
    /// <c>pg_advisory_unlock</c>, etc.) for distributed locking.
    /// </para>
    /// <para>
    /// <strong>Advisory Locks:</strong>
    /// PostgreSQL provides advisory locks that are application-level locks managed
    /// by the database. They can be session-level or transaction-level and are
    /// automatically released when the session or transaction ends.
    /// </para>
    /// <para>
    /// <strong>Lock Types:</strong>
    /// <list type="bullet">
    /// <item><c>pg_advisory_lock</c> - Exclusive lock, blocks until acquired</item>
    /// <item><c>pg_try_advisory_lock</c> - Exclusive lock, returns immediately</item>
    /// <item><c>pg_advisory_lock_shared</c> - Shared lock, blocks until acquired</item>
    /// <item><c>pg_try_advisory_lock_shared</c> - Shared lock, returns immediately</item>
    /// </list>
    /// </para>
    /// <para>
    /// This provider uses <c>pg_try_advisory_lock</c> for non-blocking acquisition
    /// and <c>pg_advisory_unlock</c> for release.
    /// </para>
    /// </remarks>
    public class PostgreSqlDistributedLockProvider : BaseDistributedLockProvider
    {
        private readonly string _connectionString;
        private readonly ILogger<PostgreSqlDistributedLockProvider>? _logger;
        private readonly bool _useSharedLock;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlDistributedLockProvider"/> class.
        /// </summary>
        /// <param name="connectionString">PostgreSQL connection string.</param>
        /// <param name="logger">Optional logger.</param>
        /// <param name="metrics">Optional metrics collector.</param>
        /// <param name="useSharedLock">Whether to use shared locks instead of exclusive locks.</param>
        public PostgreSqlDistributedLockProvider(
            string connectionString,
            ILogger<PostgreSqlDistributedLockProvider>? logger = null,
            DistributedLockMetrics? metrics = null,
            bool useSharedLock = false)
            : base(metrics)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

            _connectionString = connectionString;
            _logger = logger;
            _useSharedLock = useSharedLock;

            _logger?.LogInformation(
                "PostgreSqlDistributedLockProvider initialized: UseSharedLock={UseSharedLock}",
                _useSharedLock);
        }

        /// <inheritdoc />
        protected override string ProviderName => "PostgreSQL";

        /// <inheritdoc />
        protected override async Task<(bool Success, string LockId, DateTimeOffset ExpiresAt, long? FencedToken)> TryAcquireLockCoreAsync(
            string resource,
            string lockId,
            TimeSpan duration,
            CancellationToken cancellationToken)
        {
            var expiresAt = DateTimeOffset.UtcNow.Add(duration);
            var lockKey = GetLockKey(resource);

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                // Use pg_try_advisory_lock for non-blocking acquisition
                // Convert resource string to a 64-bit integer hash
                var lockIdInt = GetLockIdFromResource(resource);

                string query;
                if (_useSharedLock)
                {
                    query = "SELECT pg_try_advisory_lock_shared($1)";
                }
                else
                {
                    query = "SELECT pg_try_advisory_lock($1)";
                }

                await using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue(lockIdInt);

                var result = await command.ExecuteScalarAsync(cancellationToken);
                var acquired = (bool)result!;

                if (acquired)
                {
                    _logger?.LogDebug(
                        "Lock acquired on PostgreSQL: Resource={Resource}, LockId={LockId}, LockKey={LockKey}",
                        resource, lockId, lockKey);

                    return (true, lockId, expiresAt, null);
                }
                else
                {
                    _logger?.LogDebug(
                        "Lock acquisition failed on PostgreSQL: Resource={Resource}, LockId={LockId}, LockKey={LockKey}",
                        resource, lockId, lockKey);

                    return (false, lockId, expiresAt, null);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error acquiring lock on PostgreSQL: Resource={Resource}", resource);
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
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                var lockIdInt = GetLockIdFromResource(resource);

                string query;
                if (_useSharedLock)
                {
                    query = "SELECT pg_advisory_unlock_shared($1)";
                }
                else
                {
                    query = "SELECT pg_advisory_unlock($1)";
                }

                await using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue(lockIdInt);

                var result = await command.ExecuteScalarAsync(cancellationToken);
                var released = (bool)result!;

                if (released)
                {
                    RecordRelease(resource);
                    _logger?.LogDebug(
                        "Lock released on PostgreSQL: Resource={Resource}",
                        resource);
                }
                else
                {
                    _logger?.LogWarning(
                        "Lock release failed on PostgreSQL (lock may not have been held): Resource={Resource}",
                        resource);
                }

                return released;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error releasing lock on PostgreSQL: Resource={Resource}", resource);
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
            // PostgreSQL advisory locks don't have explicit expiration
            // They are held until explicitly released or the session ends
            // So renewal is essentially checking if the lock is still held

            return await IsLockedCoreAsync(resource, cancellationToken);
        }

        /// <inheritdoc />
        protected override async Task<bool> IsLockedCoreAsync(
            string resource,
            CancellationToken cancellationToken)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                var lockIdInt = GetLockIdFromResource(resource);

                // Query pg_locks to check if advisory lock exists
                var query = @"
                    SELECT COUNT(*) 
                    FROM pg_locks 
                    WHERE locktype = 'advisory' 
                    AND objid = $1 
                    AND granted = true";

                await using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue(lockIdInt);

                var count = (long)await command.ExecuteScalarAsync(cancellationToken)!;
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking lock status on PostgreSQL: Resource={Resource}", resource);
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
            return new PostgreSqlLockHandle(this, resource, lockId, expiresAt, fencedToken, options);
        }

        /// <summary>
        /// Converts a resource string to a 64-bit integer for use with PostgreSQL advisory locks.
        /// </summary>
        /// <param name="resource">The resource identifier.</param>
        /// <returns>A 64-bit integer hash of the resource.</returns>
        private static long GetLockIdFromResource(string resource)
        {
            // Use a simple hash function to convert string to long
            // PostgreSQL advisory locks use bigint (64-bit integer)
            var hash = resource.GetHashCode();
            var hash64 = (long)hash;

            // Ensure positive value (PostgreSQL advisory locks work with positive integers)
            return Math.Abs(hash64);
        }

        private static string GetLockKey(string resource)
        {
            return $"lock:{resource}";
        }

        private class PostgreSqlLockHandle : LockHandleBase
        {
            private readonly PostgreSqlDistributedLockProvider _provider;

            public PostgreSqlLockHandle(
                PostgreSqlDistributedLockProvider provider,
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

