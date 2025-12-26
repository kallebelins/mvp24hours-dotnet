//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.DistributedLocking.Contract;
using System;

namespace Mvp24Hours.Infrastructure.DistributedLocking.Results
{
    /// <summary>
    /// Represents the result of a distributed lock acquisition attempt.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class encapsulates the outcome of a lock acquisition operation, including
    /// whether the lock was acquired, timed out, or failed. If acquired, it provides
    /// a <see cref="ILockHandle"/> for managing the lock lifecycle.
    /// </para>
    /// <para>
    /// The result is immutable and provides convenient properties for checking status
    /// and accessing the lock handle when available.
    /// </para>
    /// </remarks>
    public class LockAcquisitionResult
    {
        /// <summary>
        /// Gets the status of the lock acquisition.
        /// </summary>
        public LockAcquisitionStatus Status { get; }

        /// <summary>
        /// Gets the lock handle if the lock was successfully acquired; otherwise, <c>null</c>.
        /// </summary>
        /// <remarks>
        /// Always check <see cref="IsAcquired"/> before using the lock handle.
        /// The handle implements <see cref="IDisposable"/> and should be disposed
        /// when no longer needed (preferably using a <c>using</c> statement).
        /// </remarks>
        public ILockHandle? LockHandle { get; }

        /// <summary>
        /// Gets the fenced token if fencing is enabled and the lock was acquired; otherwise, <c>null</c>.
        /// </summary>
        /// <remarks>
        /// Fenced tokens are monotonically increasing numbers that help detect stale locks
        /// in split-brain scenarios. Compare this token with previously known tokens to
        /// verify lock validity before performing operations.
        /// </remarks>
        public long? FencedToken { get; }

        /// <summary>
        /// Gets the error message if the acquisition failed; otherwise, <c>null</c>.
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Gets the exception that occurred during acquisition, if any; otherwise, <c>null</c>.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Gets when the acquisition attempt started.
        /// </summary>
        public DateTimeOffset AttemptedAt { get; }

        /// <summary>
        /// Gets when the acquisition attempt completed.
        /// </summary>
        public DateTimeOffset CompletedAt { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LockAcquisitionResult"/> class.
        /// </summary>
        /// <param name="status">The acquisition status.</param>
        /// <param name="lockHandle">The lock handle if acquired; otherwise, <c>null</c>.</param>
        /// <param name="fencedToken">The fenced token if fencing is enabled; otherwise, <c>null</c>.</param>
        /// <param name="errorMessage">The error message if failed; otherwise, <c>null</c>.</param>
        /// <param name="exception">The exception if failed; otherwise, <c>null</c>.</param>
        /// <param name="attemptedAt">When the acquisition attempt started.</param>
        /// <param name="completedAt">When the acquisition attempt completed.</param>
        private LockAcquisitionResult(
            LockAcquisitionStatus status,
            ILockHandle? lockHandle = null,
            long? fencedToken = null,
            string? errorMessage = null,
            Exception? exception = null,
            DateTimeOffset? attemptedAt = null,
            DateTimeOffset? completedAt = null)
        {
            Status = status;
            LockHandle = lockHandle;
            FencedToken = fencedToken;
            ErrorMessage = errorMessage;
            Exception = exception;
            AttemptedAt = attemptedAt ?? DateTimeOffset.UtcNow;
            CompletedAt = completedAt ?? DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets whether the lock was successfully acquired.
        /// </summary>
        public bool IsAcquired => Status == LockAcquisitionStatus.Acquired;

        /// <summary>
        /// Gets whether the lock acquisition timed out.
        /// </summary>
        public bool IsTimeout => Status == LockAcquisitionStatus.Timeout;

        /// <summary>
        /// Gets whether the lock acquisition failed.
        /// </summary>
        public bool IsFailed => Status == LockAcquisitionStatus.Failed;

        /// <summary>
        /// Creates a successful lock acquisition result.
        /// </summary>
        /// <param name="lockHandle">The acquired lock handle.</param>
        /// <param name="fencedToken">Optional fenced token if fencing is enabled.</param>
        /// <param name="attemptedAt">When the acquisition attempt started.</param>
        /// <param name="completedAt">When the acquisition attempt completed.</param>
        /// <returns>A result indicating successful acquisition.</returns>
        public static LockAcquisitionResult Acquired(
            ILockHandle lockHandle,
            long? fencedToken = null,
            DateTimeOffset? attemptedAt = null,
            DateTimeOffset? completedAt = null)
        {
            if (lockHandle == null)
                throw new ArgumentNullException(nameof(lockHandle));

            return new LockAcquisitionResult(
                LockAcquisitionStatus.Acquired,
                lockHandle,
                fencedToken,
                attemptedAt: attemptedAt,
                completedAt: completedAt);
        }

        /// <summary>
        /// Creates a timeout lock acquisition result.
        /// </summary>
        /// <param name="errorMessage">Optional error message describing the timeout.</param>
        /// <param name="attemptedAt">When the acquisition attempt started.</param>
        /// <param name="completedAt">When the acquisition attempt completed.</param>
        /// <returns>A result indicating timeout.</returns>
        public static LockAcquisitionResult Timeout(
            string? errorMessage = null,
            DateTimeOffset? attemptedAt = null,
            DateTimeOffset? completedAt = null)
        {
            return new LockAcquisitionResult(
                LockAcquisitionStatus.Timeout,
                errorMessage: errorMessage ?? "Lock acquisition timed out.",
                attemptedAt: attemptedAt,
                completedAt: completedAt);
        }

        /// <summary>
        /// Creates a failed lock acquisition result.
        /// </summary>
        /// <param name="errorMessage">Error message describing the failure.</param>
        /// <param name="exception">Optional exception that caused the failure.</param>
        /// <param name="attemptedAt">When the acquisition attempt started.</param>
        /// <param name="completedAt">When the acquisition attempt completed.</param>
        /// <returns>A result indicating failure.</returns>
        public static LockAcquisitionResult Failed(
            string errorMessage,
            Exception? exception = null,
            DateTimeOffset? attemptedAt = null,
            DateTimeOffset? completedAt = null)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentException("Error message cannot be null or empty.", nameof(errorMessage));

            return new LockAcquisitionResult(
                LockAcquisitionStatus.Failed,
                errorMessage: errorMessage,
                exception: exception,
                attemptedAt: attemptedAt,
                completedAt: completedAt);
        }
    }

    /// <summary>
    /// Represents the status of a distributed lock acquisition attempt.
    /// </summary>
    public enum LockAcquisitionStatus
    {
        /// <summary>
        /// The lock was successfully acquired.
        /// </summary>
        Acquired,

        /// <summary>
        /// The lock acquisition timed out (could not acquire within the timeout period).
        /// </summary>
        Timeout,

        /// <summary>
        /// The lock acquisition failed due to an error.
        /// </summary>
        Failed
    }
}

