//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.DistributedLocking.Exceptions
{
    /// <summary>
    /// Exception thrown when distributed lock acquisition fails.
    /// </summary>
    /// <remarks>
    /// This exception is thrown when lock acquisition fails and
    /// <see cref="Options.DistributedLockOptions.ThrowOnFailure"/> is <c>true</c>.
    /// </remarks>
    public class DistributedLockAcquisitionException : Exception
    {
        /// <summary>
        /// Gets the resource identifier that failed to acquire.
        /// </summary>
        public string Resource { get; }

        /// <summary>
        /// Gets the status of the failed acquisition.
        /// </summary>
        public Results.LockAcquisitionStatus Status { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DistributedLockAcquisitionException"/> class with default values.
        /// </summary>
        public DistributedLockAcquisitionException()
            : base("Distributed lock acquisition failed.")
        {
            Resource = string.Empty;
            Status = Results.LockAcquisitionStatus.Failed;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DistributedLockAcquisitionException"/> class.
        /// </summary>
        /// <param name="resource">The resource identifier.</param>
        /// <param name="status">The acquisition status.</param>
        /// <param name="message">The error message.</param>
        public DistributedLockAcquisitionException(string resource, Results.LockAcquisitionStatus status, string message)
            : base(message)
        {
            Resource = resource ?? throw new ArgumentNullException(nameof(resource));
            Status = status;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DistributedLockAcquisitionException"/> class.
        /// </summary>
        /// <param name="resource">The resource identifier.</param>
        /// <param name="status">The acquisition status.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public DistributedLockAcquisitionException(string resource, Results.LockAcquisitionStatus status, string message, Exception innerException)
            : base(message, innerException)
        {
            Resource = resource ?? throw new ArgumentNullException(nameof(resource));
            Status = status;
        }
    }
}

