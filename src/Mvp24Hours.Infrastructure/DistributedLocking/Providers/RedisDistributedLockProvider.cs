//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.DistributedLocking.Contract;
using Mvp24Hours.Infrastructure.DistributedLocking.Metrics;
using Mvp24Hours.Infrastructure.DistributedLocking.Options;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.DistributedLocking.Providers
{
    /// <summary>
    /// Redis-based distributed lock provider using RedLock algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider implements a simplified RedLock algorithm for distributed locking.
    /// It supports both single Redis instance and multiple Redis instances (RedLock).
    /// </para>
    /// <para>
    /// <strong>RedLock Algorithm:</strong>
    /// The RedLock algorithm requires acquiring locks on a majority of Redis instances
    /// to ensure high availability and prevent split-brain scenarios. This implementation
    /// supports both single-instance and multi-instance configurations.
    /// </para>
    /// <para>
    /// <strong>Lock Acquisition:</strong>
    /// Locks are acquired using Redis SET command with NX (only if not exists) and EX
    /// (expiration) options, ensuring atomicity. The lock value contains the lock ID
    /// for verification during release.
    /// </para>
    /// </remarks>
    public class RedisDistributedLockProvider : BaseDistributedLockProvider
    {
        private readonly IConnectionMultiplexer[] _redisConnections;
        private readonly ILogger<RedisDistributedLockProvider>? _logger;
        private readonly bool _useRedLock;
        private readonly int _quorum;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisDistributedLockProvider"/> class with a single Redis instance.
        /// </summary>
        /// <param name="redisConnection">The Redis connection multiplexer.</param>
        /// <param name="logger">Optional logger.</param>
        /// <param name="metrics">Optional metrics collector.</param>
        public RedisDistributedLockProvider(
            IConnectionMultiplexer redisConnection,
            ILogger<RedisDistributedLockProvider>? logger = null,
            DistributedLockMetrics? metrics = null)
            : this(new[] { redisConnection }, logger, metrics)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisDistributedLockProvider"/> class with multiple Redis instances (RedLock).
        /// </summary>
        /// <param name="redisConnections">Array of Redis connection multiplexers.</param>
        /// <param name="logger">Optional logger.</param>
        /// <param name="metrics">Optional metrics collector.</param>
        /// <remarks>
        /// When multiple instances are provided, the RedLock algorithm is used.
        /// The lock is acquired on a majority (quorum) of instances.
        /// </remarks>
        public RedisDistributedLockProvider(
            IConnectionMultiplexer[] redisConnections,
            ILogger<RedisDistributedLockProvider>? logger = null,
            DistributedLockMetrics? metrics = null)
            : base(metrics)
        {
            if (redisConnections == null || redisConnections.Length == 0)
                throw new ArgumentException("At least one Redis connection is required.", nameof(redisConnections));

            _redisConnections = redisConnections;
            _logger = logger;
            _useRedLock = redisConnections.Length > 1;
            _quorum = (redisConnections.Length / 2) + 1;

            _logger?.LogInformation(
                "RedisDistributedLockProvider initialized: Instances={InstanceCount}, UseRedLock={UseRedLock}, Quorum={Quorum}",
                redisConnections.Length, _useRedLock, _quorum);
        }

        /// <inheritdoc />
        protected override string ProviderName => _useRedLock ? "RedisRedLock" : "Redis";

        /// <inheritdoc />
        protected override bool SupportsFencing => true;

        /// <inheritdoc />
        protected override async Task<(bool Success, string LockId, DateTimeOffset ExpiresAt, long? FencedToken)> TryAcquireLockCoreAsync(
            string resource,
            string lockId,
            TimeSpan duration,
            CancellationToken cancellationToken)
        {
            var key = GetLockKey(resource);
            var lockValue = $"{lockId}:{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            var expiresAt = DateTimeOffset.UtcNow.Add(duration);
            var milliseconds = (int)duration.TotalMilliseconds;

            if (_useRedLock)
            {
                return await TryAcquireRedLockAsync(key, lockValue, milliseconds, expiresAt, cancellationToken);
            }
            else
            {
                return await TryAcquireSingleInstanceAsync(key, lockValue, milliseconds, expiresAt, cancellationToken);
            }
        }

        /// <summary>
        /// Attempts to acquire a lock on a single Redis instance.
        /// </summary>
        private async Task<(bool Success, string LockId, DateTimeOffset ExpiresAt, long? FencedToken)> TryAcquireSingleInstanceAsync(
            string key,
            string lockValue,
            int milliseconds,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken)
        {
            var db = _redisConnections[0].GetDatabase();

            try
            {
                // SET key value NX EX seconds - atomic operation
                var acquired = await db.StringSetAsync(
                    key,
                    lockValue,
                    TimeSpan.FromMilliseconds(milliseconds),
                    When.NotExists);

                if (acquired)
                {
                    var fencedToken = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    _logger?.LogDebug(
                        "Lock acquired on single Redis instance: Key={Key}, LockId={LockId}, ExpiresAt={ExpiresAt}",
                        key, lockValue, expiresAt);

                    return (true, lockValue, expiresAt, fencedToken);
                }

                return (false, lockValue, expiresAt, null);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error acquiring lock on Redis instance: Key={Key}", key);
                return (false, lockValue, expiresAt, null);
            }
        }

        /// <summary>
        /// Attempts to acquire a lock using RedLock algorithm (multiple Redis instances).
        /// </summary>
        private async Task<(bool Success, string LockId, DateTimeOffset ExpiresAt, long? FencedToken)> TryAcquireRedLockAsync(
            string key,
            string lockValue,
            int milliseconds,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            var acquiredCount = 0;
            var failedInstances = new List<int>();

            // Try to acquire lock on all instances
            var tasks = _redisConnections.Select((connection, index) =>
                TryAcquireOnInstanceAsync(connection, key, lockValue, milliseconds, index, cancellationToken));

            var results = await Task.WhenAll(tasks);

            // Count successful acquisitions
            foreach (var (success, instanceIndex) in results)
            {
                if (success)
                {
                    acquiredCount++;
                }
                else
                {
                    failedInstances.Add(instanceIndex);
                }
            }

            var elapsed = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            var validityTime = milliseconds - elapsed - (milliseconds * 0.01); // Subtract clock drift

            // Check if we acquired quorum
            if (acquiredCount >= _quorum && validityTime > 0)
            {
                var fencedToken = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                _logger?.LogDebug(
                    "Lock acquired using RedLock: Key={Key}, AcquiredCount={AcquiredCount}/{Total}, Quorum={Quorum}, ValidityTime={ValidityTime}ms",
                    key, acquiredCount, _redisConnections.Length, _quorum, validityTime);

                return (true, lockValue, DateTimeOffset.UtcNow.AddMilliseconds(validityTime), fencedToken);
            }

            // Failed to acquire quorum, release locks on acquired instances
            _logger?.LogWarning(
                "Failed to acquire quorum: Key={Key}, AcquiredCount={AcquiredCount}/{Total}, Quorum={Quorum}",
                key, acquiredCount, _redisConnections.Length, _quorum);

            await ReleaseLocksOnInstancesAsync(key, lockValue, results.Where(r => r.Success).Select(r => r.InstanceIndex), cancellationToken);

            return (false, lockValue, expiresAt, null);
        }

        /// <summary>
        /// Attempts to acquire a lock on a specific Redis instance.
        /// </summary>
        private async Task<(bool Success, int InstanceIndex)> TryAcquireOnInstanceAsync(
            IConnectionMultiplexer connection,
            string key,
            string lockValue,
            int milliseconds,
            int instanceIndex,
            CancellationToken cancellationToken)
        {
            try
            {
                var db = connection.GetDatabase();
                var acquired = await db.StringSetAsync(
                    key,
                    lockValue,
                    TimeSpan.FromMilliseconds(milliseconds),
                    When.NotExists);

                return (acquired, instanceIndex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error acquiring lock on Redis instance {InstanceIndex}: Key={Key}", instanceIndex, key);
                return (false, instanceIndex);
            }
        }

        /// <summary>
        /// Releases locks on the specified Redis instances.
        /// </summary>
        private async Task ReleaseLocksOnInstancesAsync(
            string key,
            string lockValue,
            IEnumerable<int> instanceIndices,
            CancellationToken cancellationToken)
        {
            var tasks = instanceIndices.Select(index =>
                ReleaseLockOnInstanceFireAndForgetAsync(_redisConnections[index], key, lockValue, index, cancellationToken));

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Releases a lock on a specific Redis instance (fire and forget, no result).
        /// </summary>
        private async Task ReleaseLockOnInstanceFireAndForgetAsync(
            IConnectionMultiplexer connection,
            string key,
            string lockValue,
            int instanceIndex,
            CancellationToken cancellationToken)
        {
            try
            {
                var db = connection.GetDatabase();

                // Lua script to atomically check and delete the lock
                const string script = @"
                    if redis.call('get', KEYS[1]) == ARGV[1] then
                        return redis.call('del', KEYS[1])
                    else
                        return 0
                    end";

                await db.ScriptEvaluateAsync(script, new RedisKey[] { key }, new RedisValue[] { lockValue });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error releasing lock on Redis instance {InstanceIndex}: Key={Key}", instanceIndex, key);
            }
        }

        /// <inheritdoc />
        protected override async Task<bool> ReleaseLockCoreAsync(
            string resource,
            string lockId,
            CancellationToken cancellationToken)
        {
            var key = GetLockKey(resource);

            if (_useRedLock)
            {
                // Release on all instances (fire-and-forget for parallel execution)
                var tasks = _redisConnections.Select((connection, index) =>
                    ReleaseLockOnInstanceFireAndForgetAsync(connection, key, lockId, index, cancellationToken));

                await Task.WhenAll(tasks);
                return true; // RedLock: assume success if we attempt release on all instances
            }
            else
            {
                return await ReleaseLockOnInstanceAsync(_redisConnections[0], key, lockId, 0, cancellationToken);
            }
        }

        /// <summary>
        /// Releases a lock on a specific Redis instance and returns success status.
        /// </summary>
        private async Task<bool> ReleaseLockOnInstanceAsync(
            IConnectionMultiplexer connection,
            string key,
            string lockValue,
            int instanceIndex,
            CancellationToken cancellationToken)
        {
            try
            {
                var db = connection.GetDatabase();

                // Lua script to atomically check and delete the lock
                const string script = @"
                    if redis.call('get', KEYS[1]) == ARGV[1] then
                        return redis.call('del', KEYS[1])
                    else
                        return 0
                    end";

                var result = await db.ScriptEvaluateAsync(script, new RedisKey[] { key }, new RedisValue[] { lockValue });
                var released = (int)result == 1;

                if (released)
                {
                    _logger?.LogDebug(
                        "Lock released on Redis instance {InstanceIndex}: Key={Key}",
                        instanceIndex, key);
                }

                return released;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error releasing lock on Redis instance {InstanceIndex}: Key={Key}", instanceIndex, key);
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
            var key = GetLockKey(resource);
            var milliseconds = (int)duration.TotalMilliseconds;

            if (_useRedLock)
            {
                // Renew on all instances where we hold the lock
                var tasks = _redisConnections.Select((connection, index) =>
                    RenewLockOnInstanceAsync(connection, key, lockId, milliseconds, index, cancellationToken));

                var results = await Task.WhenAll(tasks);
                var renewedCount = results.Count(r => r);

                // Require quorum for renewal
                return renewedCount >= _quorum;
            }
            else
            {
                return await RenewLockOnInstanceAsync(_redisConnections[0], key, lockId, milliseconds, 0, cancellationToken);
            }
        }

        /// <summary>
        /// Renews a lock on a specific Redis instance.
        /// </summary>
        private async Task<bool> RenewLockOnInstanceAsync(
            IConnectionMultiplexer connection,
            string key,
            string lockValue,
            int milliseconds,
            int instanceIndex,
            CancellationToken cancellationToken)
        {
            try
            {
                var db = connection.GetDatabase();

                // Lua script to atomically check and extend the lock
                const string script = @"
                    if redis.call('get', KEYS[1]) == ARGV[1] then
                        return redis.call('pexpire', KEYS[1], ARGV[2])
                    else
                        return 0
                    end";

                var result = await db.ScriptEvaluateAsync(
                    script,
                    new RedisKey[] { key },
                    new RedisValue[] { lockValue, milliseconds });

                var renewed = (bool)result;

                if (renewed)
                {
                    _logger?.LogDebug(
                        "Lock renewed on Redis instance {InstanceIndex}: Key={Key}, Duration={Duration}ms",
                        instanceIndex, key, milliseconds);
                }

                return renewed;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error renewing lock on Redis instance {InstanceIndex}: Key={Key}", instanceIndex, key);
                return false;
            }
        }

        /// <inheritdoc />
        protected override async Task<bool> IsLockedCoreAsync(
            string resource,
            CancellationToken cancellationToken)
        {
            var key = GetLockKey(resource);

            try
            {
                var db = _redisConnections[0].GetDatabase();
                var value = await db.StringGetAsync(key);
                return value.HasValue;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking lock status: Key={Key}", key);
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
            return new RedisLockHandle(this, resource, lockId, expiresAt, fencedToken, options);
        }

        private static string GetLockKey(string resource)
        {
            return $"lock:{resource}";
        }

        private class RedisLockHandle : LockHandleBase
        {
            private readonly RedisDistributedLockProvider _provider;

            public RedisLockHandle(
                RedisDistributedLockProvider provider,
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

