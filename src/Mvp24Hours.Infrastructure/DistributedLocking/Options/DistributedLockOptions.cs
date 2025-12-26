//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.DistributedLocking.Options
{
    /// <summary>
    /// Configuration options for distributed lock acquisition and management.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options control how locks are acquired, how long they are held, and
    /// how they are renewed. Different scenarios may require different configurations:
    /// </para>
    /// <list type="bullet">
    /// <item><strong>Short operations:</strong> Disable auto-renewal, use short duration</item>
    /// <item><strong>Long operations:</strong> Enable auto-renewal, use longer duration</item>
    /// <item><strong>High contention:</strong> Use shorter acquisition timeout to fail fast</item>
    /// <item><strong>Critical operations:</strong> Enable fencing for split-brain protection</item>
    /// </list>
    /// </remarks>
    public class DistributedLockOptions
    {
        /// <summary>
        /// Gets or sets the maximum time to wait when attempting to acquire the lock.
        /// Default is 30 seconds.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the lock cannot be acquired within this time, the acquisition will fail
        /// with <see cref="LockAcquisitionStatus.Timeout"/>.
        /// </para>
        /// <para>
        /// For high-contention scenarios, consider using a shorter timeout to fail fast
        /// and allow other operations to proceed. For critical operations, use a longer
        /// timeout to ensure the lock is eventually acquired.
        /// </para>
        /// </remarks>
        public TimeSpan AcquisitionTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the duration for which the lock will be held before expiring.
        /// Default is 5 minutes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The lock will automatically expire after this duration unless it is renewed
        /// (manually or via auto-renewal). Once expired, the lock handle becomes invalid
        /// and the lock may be acquired by another instance.
        /// </para>
        /// <para>
        /// Choose a duration that is longer than your expected operation time, but not
        /// so long that it prevents recovery if the lock holder crashes. A good rule of
        /// thumb is 2-3x the expected operation duration.
        /// </para>
        /// </remarks>
        public TimeSpan LockDuration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets whether automatic lock renewal is enabled.
        /// Default is <c>false</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When enabled, the lock will be automatically renewed at regular intervals
        /// (<see cref="RenewalInterval"/>) to prevent expiration during long-running operations.
        /// Renewal continues until the lock handle is disposed or renewal fails.
        /// </para>
        /// <para>
        /// Auto-renewal is recommended for operations with unpredictable duration or
        /// operations that may take longer than <see cref="LockDuration"/>.
        /// </para>
        /// <para>
        /// <strong>Note:</strong> Auto-renewal requires a background task and consumes
        /// resources. For short operations, disable auto-renewal to reduce overhead.
        /// </para>
        /// </remarks>
        public bool EnableAutoRenewal { get; set; } = false;

        /// <summary>
        /// Gets or sets the interval at which the lock is automatically renewed.
        /// Default is 2 minutes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The lock is renewed at this interval when <see cref="EnableAutoRenewal"/> is <c>true</c>.
        /// The renewal extends the lock expiration by <see cref="LockDuration"/>.
        /// </para>
        /// <para>
        /// Choose an interval that is shorter than <see cref="LockDuration"/> to ensure
        /// the lock is renewed before it expires. A good rule of thumb is to renew at
        /// 40-50% of the lock duration (e.g., renew every 2 minutes for a 5-minute lock).
        /// </para>
        /// </remarks>
        public TimeSpan RenewalInterval { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Gets or sets whether fencing (fenced tokens) is enabled.
        /// Default is <c>false</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Fencing provides protection against split-brain scenarios where multiple
        /// instances believe they hold the lock. When enabled, each lock acquisition
        /// receives a monotonically increasing fenced token that can be used to verify
        /// lock validity.
        /// </para>
        /// <para>
        /// <strong>When to use fencing:</strong>
        /// <list type="bullet">
        /// <item>Critical operations where stale locks could cause data corruption</item>
        /// <item>Distributed systems with potential network partitions</item>
        /// <item>Operations that modify shared state</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Note:</strong> Not all providers support fencing. If fencing is requested
        /// but not supported, the lock acquisition will proceed without fencing (no error).
        /// </para>
        /// </remarks>
        public bool EnableFencing { get; set; } = false;

        /// <summary>
        /// Gets or sets a retry delay when lock acquisition fails due to contention.
        /// Default is 100 milliseconds.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When the lock is held by another instance, the acquisition will retry after
        /// this delay. The retry continues until <see cref="AcquisitionTimeout"/> is reached.
        /// </para>
        /// <para>
        /// Shorter delays provide faster acquisition when the lock becomes available, but
        /// increase load on the lock provider. Longer delays reduce load but may delay
        /// acquisition.
        /// </para>
        /// </remarks>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets or sets whether to throw an exception if lock acquisition fails.
        /// Default is <c>false</c> (returns result with status instead of throwing).
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, a <see cref="DistributedLockAcquisitionException"/> is thrown
        /// if the lock cannot be acquired (timeout or failure). When <c>false</c>, the
        /// method returns a result with the appropriate status.
        /// </para>
        /// <para>
        /// Use <c>true</c> when lock acquisition failure should be treated as an exceptional
        /// condition. Use <c>false</c> when you want to handle failures gracefully.
        /// </para>
        /// </remarks>
        public bool ThrowOnFailure { get; set; } = false;

        /// <summary>
        /// Creates default distributed lock options suitable for most scenarios.
        /// </summary>
        /// <returns>Default options instance.</returns>
        public static DistributedLockOptions Default => new();

        /// <summary>
        /// Creates distributed lock options optimized for short operations.
        /// </summary>
        /// <returns>Options with short timeout and duration, auto-renewal disabled.</returns>
        public static DistributedLockOptions ShortOperation => new()
        {
            AcquisitionTimeout = TimeSpan.FromSeconds(5),
            LockDuration = TimeSpan.FromMinutes(1),
            EnableAutoRenewal = false,
            RetryDelay = TimeSpan.FromMilliseconds(50)
        };

        /// <summary>
        /// Creates distributed lock options optimized for long operations.
        /// </summary>
        /// <returns>Options with longer timeout and duration, auto-renewal enabled.</returns>
        public static DistributedLockOptions LongOperation => new()
        {
            AcquisitionTimeout = TimeSpan.FromMinutes(1),
            LockDuration = TimeSpan.FromMinutes(10),
            EnableAutoRenewal = true,
            RenewalInterval = TimeSpan.FromMinutes(4),
            RetryDelay = TimeSpan.FromMilliseconds(200)
        };

        /// <summary>
        /// Creates distributed lock options optimized for critical operations with fencing.
        /// </summary>
        /// <returns>Options with fencing enabled and conservative timeouts.</returns>
        public static DistributedLockOptions CriticalOperation => new()
        {
            AcquisitionTimeout = TimeSpan.FromMinutes(2),
            LockDuration = TimeSpan.FromMinutes(5),
            EnableAutoRenewal = true,
            RenewalInterval = TimeSpan.FromMinutes(2),
            EnableFencing = true,
            RetryDelay = TimeSpan.FromMilliseconds(100)
        };

        /// <summary>
        /// Creates distributed lock options optimized for high-contention scenarios.
        /// </summary>
        /// <returns>Options with short timeout to fail fast.</returns>
        public static DistributedLockOptions HighContention => new()
        {
            AcquisitionTimeout = TimeSpan.FromSeconds(5),
            LockDuration = TimeSpan.FromMinutes(2),
            EnableAutoRenewal = false,
            RetryDelay = TimeSpan.FromMilliseconds(25)
        };
    }
}

