//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Contract;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Mvp24Hours.Infrastructure.BackgroundJobs.Models
{
    /// <summary>
    /// Default implementation of <see cref="IJobContext"/>.
    /// </summary>
    /// <remarks>
    /// This class provides a concrete implementation of the job context interface.
    /// It is typically created by the job scheduler when a job is executed.
    /// </remarks>
    public class JobContext : IJobContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobContext"/> class.
        /// </summary>
        /// <param name="jobId">The unique identifier of the job execution.</param>
        /// <param name="attemptNumber">The attempt number of this job execution.</param>
        /// <param name="cancellationToken">The cancellation token for this job execution.</param>
        /// <param name="metadata">The metadata associated with this job execution.</param>
        /// <param name="startedAt">When the job execution started.</param>
        /// <param name="jobType">The job type name.</param>
        /// <param name="queue">The queue name where this job is being processed.</param>
        /// <exception cref="ArgumentNullException">Thrown when jobId, cancellationToken, metadata, or jobType is null.</exception>
        /// <exception cref="ArgumentException">Thrown when jobId or jobType is empty, or when attemptNumber is less than 1.</exception>
        public JobContext(
            string jobId,
            int attemptNumber,
            CancellationToken cancellationToken,
            IReadOnlyDictionary<string, string> metadata,
            DateTimeOffset startedAt,
            string jobType,
            string? queue = null)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Job ID cannot be null or empty.", nameof(jobId));
            }

            if (attemptNumber < 1)
            {
                throw new ArgumentException("Attempt number must be greater than or equal to 1.", nameof(attemptNumber));
            }

            if (string.IsNullOrWhiteSpace(jobType))
            {
                throw new ArgumentException("Job type cannot be null or empty.", nameof(jobType));
            }

            JobId = jobId;
            AttemptNumber = attemptNumber;
            CancellationToken = cancellationToken;
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            StartedAt = startedAt;
            JobType = jobType;
            Queue = queue;
        }

        /// <inheritdoc />
        public string JobId { get; }

        /// <inheritdoc />
        public int AttemptNumber { get; }

        /// <inheritdoc />
        public CancellationToken CancellationToken { get; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> Metadata { get; }

        /// <inheritdoc />
        public DateTimeOffset StartedAt { get; }

        /// <inheritdoc />
        public string JobType { get; }

        /// <inheritdoc />
        public string? Queue { get; }
    }
}

