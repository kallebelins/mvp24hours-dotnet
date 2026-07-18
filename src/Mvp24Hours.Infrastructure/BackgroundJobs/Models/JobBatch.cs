//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Contract;
using Mvp24Hours.Infrastructure.BackgroundJobs.Options;

namespace Mvp24Hours.Infrastructure.BackgroundJobs.Models;

/// <summary>
/// Default implementation of <see cref="IJobBatch"/>.
/// </summary>
/// <remarks>
/// This class provides a concrete implementation of a job batch that can be used
/// to group multiple jobs together for coordinated execution.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="JobBatch"/> class.
/// </remarks>
/// <param name="name">Optional name for the batch (for identification).</param>
/// <param name="options">Batch options that control execution behavior.</param>
public class JobBatch(string? name = null, BatchOptions? options = null) : IJobBatch
{
    private readonly List<BatchJob> _jobs = [];

    /// <inheritdoc />
    public string BatchId { get; } = Guid.NewGuid().ToString();

    /// <inheritdoc />
    public string? Name { get; } = name;

    /// <inheritdoc />
    public BatchOptions Options { get; } = options ?? new BatchOptions();

    /// <inheritdoc />
    public IReadOnlyList<IBatchJob> Jobs => _jobs.AsReadOnly();

    /// <inheritdoc />
    public BatchStatus Status { get; internal set; } = BatchStatus.Pending;

    /// <inheritdoc />
    public DateTimeOffset? StartedAt { get; internal set; }

    /// <inheritdoc />
    public DateTimeOffset? CompletedAt { get; internal set; }

    /// <summary>
    /// Adds a job to the batch.
    /// </summary>
    /// <typeparam name="TJob">The job type.</typeparam>
    /// <typeparam name="TArgs">The job arguments type.</typeparam>
    /// <param name="args">The job arguments.</param>
    /// <param name="jobOptions">Optional job options.</param>
    /// <param name="dependencies">IDs of other jobs in the batch that must complete before this job executes.</param>
    /// <returns>The batch job that was added.</returns>
    /// <exception cref="ArgumentNullException">Thrown when args is null.</exception>
    public BatchJob AddJob<TJob, TArgs>(
        TArgs args,
        JobOptions? jobOptions = null,
        IEnumerable<string>? dependencies = null)
        where TJob : class, IBackgroundJob<TArgs>
        where TArgs : class
    {
        if (args == null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        string jobId = Guid.NewGuid().ToString();
        string jobType = typeof(TJob).FullName ?? typeof(TJob).Name;

        // Serialize arguments (simplified - in real implementation, use proper serializer)
        string serializedArgs = System.Text.Json.JsonSerializer.Serialize(args);

        var batchJob = new BatchJob(
            jobId,
            jobType,
            serializedArgs,
            jobOptions,
            dependencies?.ToList() ?? []);

        _jobs.Add(batchJob);
        return batchJob;
    }

    /// <summary>
    /// Adds a job without arguments to the batch.
    /// </summary>
    /// <typeparam name="TJob">The job type.</typeparam>
    /// <param name="jobOptions">Optional job options.</param>
    /// <param name="dependencies">IDs of other jobs in the batch that must complete before this job executes.</param>
    /// <returns>The batch job that was added.</returns>
    public BatchJob AddJob<TJob>(
        JobOptions? jobOptions = null,
        IEnumerable<string>? dependencies = null)
        where TJob : class, IBackgroundJob
    {
        string jobId = Guid.NewGuid().ToString();
        string jobType = typeof(TJob).FullName ?? typeof(TJob).Name;

        var batchJob = new BatchJob(
            jobId,
            jobType,
            "{}", // Empty JSON object for jobs without arguments
            jobOptions,
            dependencies?.ToList() ?? []);

        _jobs.Add(batchJob);
        return batchJob;
    }

    /// <summary>
    /// Removes a job from the batch.
    /// </summary>
    /// <param name="jobId">The job ID to remove.</param>
    /// <returns><c>true</c> if the job was removed; <c>false</c> if the job was not found.</returns>
    public bool RemoveJob(string jobId)
    {
        BatchJob? job = _jobs.FirstOrDefault(j => j.JobId == jobId);
        if (job != null)
        {
            _jobs.Remove(job);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Clears all jobs from the batch.
    /// </summary>
    public void Clear()
    {
        _jobs.Clear();
    }
}

/// <summary>
/// Default implementation of <see cref="IBatchJob"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BatchJob"/> class.
/// </remarks>
/// <param name="jobId">The unique identifier of the job within the batch.</param>
/// <param name="jobType">The job type name.</param>
/// <param name="serializedArgs">The serialized job arguments.</param>
/// <param name="jobOptions">The job options.</param>
/// <param name="dependencies">The IDs of jobs that must complete before this job executes.</param>
public class BatchJob(
    string jobId,
    string jobType,
    string serializedArgs,
    JobOptions? jobOptions = null,
    IReadOnlyList<string>? dependencies = null) : IBatchJob
{

    /// <inheritdoc />
    public string JobId { get; } = jobId ?? throw new ArgumentNullException(nameof(jobId));

    /// <inheritdoc />
    public string JobType { get; } = jobType ?? throw new ArgumentNullException(nameof(jobType));

    /// <inheritdoc />
    public string SerializedArgs { get; } = serializedArgs ?? throw new ArgumentNullException(nameof(serializedArgs));

    /// <inheritdoc />
    public JobOptions? JobOptions { get; } = jobOptions;

    /// <inheritdoc />
    public IReadOnlyList<string> Dependencies { get; } = dependencies ?? [];
}

