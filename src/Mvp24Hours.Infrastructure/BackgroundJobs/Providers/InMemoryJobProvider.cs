//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Contract;
using Mvp24Hours.Infrastructure.BackgroundJobs.Models;
using Mvp24Hours.Infrastructure.BackgroundJobs.Options;
using Mvp24Hours.Infrastructure.BackgroundJobs.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.BackgroundJobs.Providers
{
    /// <summary>
    /// In-memory implementation of <see cref="IJobScheduler"/> for testing purposes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider stores jobs in memory and executes them synchronously or asynchronously
    /// based on configuration. It's designed for unit testing and development scenarios where
    /// a persistent job store is not required.
    /// </para>
    /// <para>
    /// <strong>Limitations:</strong>
    /// - Jobs are lost when the application restarts
    /// - Not suitable for distributed scenarios
    /// - Limited scalability
    /// - No persistence across application instances
    /// </para>
    /// <para>
    /// <strong>Use Cases:</strong>
    /// - Unit testing
    /// - Integration testing
    /// - Development environments
    /// - Prototyping
    /// </para>
    /// </remarks>
    public class InMemoryJobProvider : IJobScheduler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InMemoryJobProvider>? _logger;
        private readonly ConcurrentDictionary<string, JobInfo> _jobs = new();
        private readonly ConcurrentDictionary<string, BatchInfo> _batches = new();
        private readonly ConcurrentDictionary<string, List<string>> _parentChildJobs = new();
        private readonly ConcurrentDictionary<string, List<string>> _continuations = new();
        private readonly SemaphoreSlim _executionSemaphore = new(1, 1);
        private readonly CancellationTokenSource _shutdownCts = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryJobProvider"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for resolving job instances.</param>
        /// <param name="logger">Optional logger for diagnostic information.</param>
        public InMemoryJobProvider(
            IServiceProvider serviceProvider,
            ILogger<InMemoryJobProvider>? logger = null)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<string> EnqueueAsync<TJob, TArgs>(
            TArgs args,
            JobOptions? options = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob<TArgs>
            where TArgs : class
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            var jobId = Guid.NewGuid().ToString();
            var jobType = typeof(TJob).FullName ?? typeof(TJob).Name;
            var serializedArgs = JsonSerializer.Serialize(args);
            var mergedOptions = MergeOptions(options);

            var jobInfo = new JobInfo
            {
                JobId = jobId,
                JobType = jobType,
                SerializedArgs = serializedArgs,
                Options = mergedOptions,
                Status = JobStatus.Scheduled,
                CreatedAt = DateTimeOffset.UtcNow,
                ScheduledFor = DateTimeOffset.UtcNow
            };

            _jobs[jobId] = jobInfo;

            _logger?.LogDebug("Enqueued job {JobId} of type {JobType}", jobId, jobType);

            // Execute immediately in background
            _ = Task.Run(() => ExecuteJobAsync<TJob, TArgs>(jobId, cancellationToken), cancellationToken);

            return Task.FromResult(jobId);
        }

        /// <inheritdoc />
        public Task<string> EnqueueAsync<TJob>(
            JobOptions? options = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob
        {
            var jobId = Guid.NewGuid().ToString();
            var jobType = typeof(TJob).FullName ?? typeof(TJob).Name;
            var mergedOptions = MergeOptions(options);

            var jobInfo = new JobInfo
            {
                JobId = jobId,
                JobType = jobType,
                SerializedArgs = "{}",
                Options = mergedOptions,
                Status = JobStatus.Scheduled,
                CreatedAt = DateTimeOffset.UtcNow,
                ScheduledFor = DateTimeOffset.UtcNow
            };

            _jobs[jobId] = jobInfo;

            _logger?.LogDebug("Enqueued job {JobId} of type {JobType} without arguments", jobId, jobType);

            // Execute immediately in background
            _ = Task.Run(() => ExecuteJobAsync<TJob>(jobId, cancellationToken), cancellationToken);

            return Task.FromResult(jobId);
        }

        /// <inheritdoc />
        public Task<string> ScheduleAsync<TJob, TArgs>(
            TArgs args,
            TimeSpan delay,
            JobOptions? options = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob<TArgs>
            where TArgs : class
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (delay <= TimeSpan.Zero)
            {
                throw new ArgumentException("Delay must be greater than zero.", nameof(delay));
            }

            var scheduledTime = DateTimeOffset.UtcNow.Add(delay);
            return ScheduleAsync<TJob, TArgs>(args, scheduledTime, options, cancellationToken);
        }

        /// <inheritdoc />
        public Task<string> ScheduleAsync<TJob>(
            TimeSpan delay,
            JobOptions? options = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob
        {
            if (delay <= TimeSpan.Zero)
            {
                throw new ArgumentException("Delay must be greater than zero.", nameof(delay));
            }

            var scheduledTime = DateTimeOffset.UtcNow.Add(delay);
            return ScheduleAsync<TJob>(scheduledTime, options, cancellationToken);
        }

        /// <inheritdoc />
        public Task<string> ScheduleAsync<TJob, TArgs>(
            TArgs args,
            DateTimeOffset scheduledTime,
            JobOptions? options = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob<TArgs>
            where TArgs : class
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (scheduledTime <= DateTimeOffset.UtcNow)
            {
                throw new ArgumentException("Scheduled time must be in the future.", nameof(scheduledTime));
            }

            var jobId = Guid.NewGuid().ToString();
            var jobType = typeof(TJob).FullName ?? typeof(TJob).Name;
            var serializedArgs = JsonSerializer.Serialize(args);
            var mergedOptions = MergeOptions(options);

            var jobInfo = new JobInfo
            {
                JobId = jobId,
                JobType = jobType,
                SerializedArgs = serializedArgs,
                Options = mergedOptions,
                Status = JobStatus.Scheduled,
                CreatedAt = DateTimeOffset.UtcNow,
                ScheduledFor = scheduledTime
            };

            _jobs[jobId] = jobInfo;

            _logger?.LogDebug("Scheduled job {JobId} of type {JobType} for {ScheduledTime}", jobId, jobType, scheduledTime);

            // Schedule execution
            var delay = scheduledTime - DateTimeOffset.UtcNow;
            _ = Task.Run(async () =>
            {
                await Task.Delay(delay, cancellationToken);
                await ExecuteJobAsync<TJob, TArgs>(jobId, cancellationToken);
            }, cancellationToken);

            return Task.FromResult(jobId);
        }

        /// <inheritdoc />
        public Task<string> ScheduleAsync<TJob>(
            DateTimeOffset scheduledTime,
            JobOptions? options = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob
        {
            if (scheduledTime <= DateTimeOffset.UtcNow)
            {
                throw new ArgumentException("Scheduled time must be in the future.", nameof(scheduledTime));
            }

            var jobId = Guid.NewGuid().ToString();
            var jobType = typeof(TJob).FullName ?? typeof(TJob).Name;
            var mergedOptions = MergeOptions(options);

            var jobInfo = new JobInfo
            {
                JobId = jobId,
                JobType = jobType,
                SerializedArgs = "{}",
                Options = mergedOptions,
                Status = JobStatus.Scheduled,
                CreatedAt = DateTimeOffset.UtcNow,
                ScheduledFor = scheduledTime
            };

            _jobs[jobId] = jobInfo;

            _logger?.LogDebug("Scheduled job {JobId} of type {JobType} for {ScheduledTime}", jobId, jobType, scheduledTime);

            // Schedule execution
            var delay = scheduledTime - DateTimeOffset.UtcNow;
            _ = Task.Run(async () =>
            {
                await Task.Delay(delay, cancellationToken);
                await ExecuteJobAsync<TJob>(jobId, cancellationToken);
            }, cancellationToken);

            return Task.FromResult(jobId);
        }

        /// <inheritdoc />
        public Task<string> ScheduleRecurringAsync<TJob, TArgs>(
            string cronExpression,
            TArgs args,
            JobOptions? options = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob<TArgs>
            where TArgs : class
        {
            // Simplified implementation - in production, use a proper CRON parser
            _logger?.LogWarning("Recurring jobs are not fully supported in InMemoryJobProvider. Use a production provider like Hangfire or Quartz.NET.");
            
            // For now, just schedule it once
            return EnqueueAsync<TJob, TArgs>(args, options, cancellationToken);
        }

        /// <inheritdoc />
        public Task<string> ScheduleRecurringAsync<TJob>(
            string cronExpression,
            JobOptions? options = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob
        {
            // Simplified implementation - in production, use a proper CRON parser
            _logger?.LogWarning("Recurring jobs are not fully supported in InMemoryJobProvider. Use a production provider like Hangfire or Quartz.NET.");
            
            // For now, just schedule it once
            return EnqueueAsync<TJob>(options, cancellationToken);
        }

        /// <inheritdoc />
        public Task<string> ContinueWithAsync<TJob, TArgs>(
            string parentJobId,
            TArgs args,
            ContinuationOptions? continuationOptions = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob<TArgs>
            where TArgs : class
        {
            if (string.IsNullOrWhiteSpace(parentJobId))
            {
                throw new ArgumentException("Parent job ID cannot be null or empty.", nameof(parentJobId));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            var continuationJobId = Guid.NewGuid().ToString();
            var continuation = continuationOptions ?? new ContinuationOptions();

            if (!_continuations.ContainsKey(parentJobId))
            {
                _continuations[parentJobId] = new List<string>();
            }

            _continuations[parentJobId].Add(continuationJobId);

            // Wait for parent to complete, then schedule continuation
            _ = Task.Run(async () =>
            {
                await WaitForJobCompletionAsync(parentJobId, continuation.MaxWaitTime ?? TimeSpan.FromHours(24), cancellationToken);
                
                var parentStatus = await GetStatusAsync(parentJobId, cancellationToken);
                if (ShouldExecuteContinuation(parentStatus, continuation))
                {
                    await EnqueueAsync<TJob, TArgs>(args, continuation.JobOptions, cancellationToken);
                }
            }, cancellationToken);

            return Task.FromResult(continuationJobId);
        }

        /// <inheritdoc />
        public Task<string> ContinueWithAsync<TJob>(
            string parentJobId,
            ContinuationOptions? continuationOptions = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob
        {
            if (string.IsNullOrWhiteSpace(parentJobId))
            {
                throw new ArgumentException("Parent job ID cannot be null or empty.", nameof(parentJobId));
            }

            var continuationJobId = Guid.NewGuid().ToString();
            var continuation = continuationOptions ?? new ContinuationOptions();

            if (!_continuations.ContainsKey(parentJobId))
            {
                _continuations[parentJobId] = new List<string>();
            }

            _continuations[parentJobId].Add(continuationJobId);

            // Wait for parent to complete, then schedule continuation
            _ = Task.Run(async () =>
            {
                await WaitForJobCompletionAsync(parentJobId, continuation.MaxWaitTime ?? TimeSpan.FromHours(24), cancellationToken);
                
                var parentStatus = await GetStatusAsync(parentJobId, cancellationToken);
                if (ShouldExecuteContinuation(parentStatus, continuation))
                {
                    await EnqueueAsync<TJob>(continuation.JobOptions, cancellationToken);
                }
            }, cancellationToken);

            return Task.FromResult(continuationJobId);
        }

        /// <inheritdoc />
        public Task<string> ScheduleBatchAsync(
            IJobBatch batch,
            CancellationToken cancellationToken = default)
        {
            if (batch == null)
            {
                throw new ArgumentNullException(nameof(batch));
            }

            var batchId = batch.BatchId ?? Guid.NewGuid().ToString();
            var batchInfo = new BatchInfo
            {
                BatchId = batchId,
                Name = batch.Name,
                Options = batch.Options,
                Jobs = batch.Jobs.Select(j => j.JobId).ToList(),
                Status = BatchStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _batches[batchId] = batchInfo;

            _logger?.LogDebug("Scheduled batch {BatchId} with {JobCount} jobs", batchId, batch.Jobs.Count);

            // Execute batch in background
            _ = Task.Run(() => ExecuteBatchAsync(batchId, batch, cancellationToken), cancellationToken);

            return Task.FromResult(batchId);
        }

        /// <inheritdoc />
        public Task<BatchStatus?> GetBatchStatusAsync(
            string batchId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(batchId))
            {
                throw new ArgumentException("Batch ID cannot be null or empty.", nameof(batchId));
            }

            if (_batches.TryGetValue(batchId, out var batchInfo))
            {
                return Task.FromResult<BatchStatus?>(batchInfo.Status);
            }

            return Task.FromResult<BatchStatus?>(null);
        }

        /// <inheritdoc />
        public Task<bool> CancelBatchAsync(
            string batchId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(batchId))
            {
                throw new ArgumentException("Batch ID cannot be null or empty.", nameof(batchId));
            }

            if (_batches.TryGetValue(batchId, out var batchInfo))
            {
                batchInfo.Status = BatchStatus.Cancelled;
                batchInfo.CompletedAt = DateTimeOffset.UtcNow;

                // Cancel all jobs in batch
                foreach (var jobId in batchInfo.Jobs)
                {
                    _ = CancelAsync(jobId, cancellationToken);
                }

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task<string> EnqueueChildAsync<TJob, TArgs>(
            string parentJobId,
            TArgs args,
            JobOptions? options = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob<TArgs>
            where TArgs : class
        {
            if (string.IsNullOrWhiteSpace(parentJobId))
            {
                throw new ArgumentException("Parent job ID cannot be null or empty.", nameof(parentJobId));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            var childJobId = Guid.NewGuid().ToString();

            if (!_parentChildJobs.ContainsKey(parentJobId))
            {
                _parentChildJobs[parentJobId] = new List<string>();
            }

            _parentChildJobs[parentJobId].Add(childJobId);

            return EnqueueAsync<TJob, TArgs>(args, options, cancellationToken);
        }

        /// <inheritdoc />
        public Task<string> EnqueueChildAsync<TJob>(
            string parentJobId,
            JobOptions? options = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob
        {
            if (string.IsNullOrWhiteSpace(parentJobId))
            {
                throw new ArgumentException("Parent job ID cannot be null or empty.", nameof(parentJobId));
            }

            var childJobId = Guid.NewGuid().ToString();

            if (!_parentChildJobs.ContainsKey(parentJobId))
            {
                _parentChildJobs[parentJobId] = new List<string>();
            }

            _parentChildJobs[parentJobId].Add(childJobId);

            return EnqueueAsync<TJob>(options, cancellationToken);
        }

        /// <inheritdoc />
        public Task WaitForChildrenAsync(
            string parentJobId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(parentJobId))
            {
                throw new ArgumentException("Parent job ID cannot be null or empty.", nameof(parentJobId));
            }

            if (_parentChildJobs.TryGetValue(parentJobId, out var childJobIds))
            {
                return Task.WhenAll(childJobIds.Select(async childId =>
                {
                    await WaitForJobCompletionAsync(childId, TimeSpan.MaxValue, cancellationToken);
                }));
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IReadOnlyDictionary<string, JobStatus?>> GetChildJobStatusesAsync(
            string parentJobId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(parentJobId))
            {
                throw new ArgumentException("Parent job ID cannot be null or empty.", nameof(parentJobId));
            }

            var result = new Dictionary<string, JobStatus?>();

            if (_parentChildJobs.TryGetValue(parentJobId, out var childJobIds))
            {
                foreach (var childId in childJobIds)
                {
                    if (_jobs.TryGetValue(childId, out var jobInfo))
                    {
                        result[childId] = jobInfo.Status;
                    }
                    else
                    {
                        result[childId] = null;
                    }
                }
            }

            return Task.FromResult<IReadOnlyDictionary<string, JobStatus?>>(result);
        }

        /// <inheritdoc />
        public async Task<int> CancelChildrenAsync(
            string parentJobId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(parentJobId))
            {
                throw new ArgumentException("Parent job ID cannot be null or empty.", nameof(parentJobId));
            }

            var cancelledCount = 0;

            if (_parentChildJobs.TryGetValue(parentJobId, out var childJobIds))
            {
                foreach (var childId in childJobIds)
                {
                    if (await CancelAsync(childId, cancellationToken))
                    {
                        cancelledCount++;
                    }
                }
            }

            return cancelledCount;
        }

        /// <inheritdoc />
        public Task<bool> CancelAsync(
            string jobId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Job ID cannot be null or empty.", nameof(jobId));
            }

            if (_jobs.TryGetValue(jobId, out var jobInfo))
            {
                if (jobInfo.Status == JobStatus.Scheduled || jobInfo.Status == JobStatus.Running)
                {
                    jobInfo.Status = JobStatus.Cancelled;
                    jobInfo.CompletedAt = DateTimeOffset.UtcNow;
                    _logger?.LogDebug("Cancelled job {JobId}", jobId);
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task<JobStatus?> GetStatusAsync(
            string jobId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Job ID cannot be null or empty.", nameof(jobId));
            }

            if (_jobs.TryGetValue(jobId, out var jobInfo))
            {
                return Task.FromResult<JobStatus?>(jobInfo.Status);
            }

            return Task.FromResult<JobStatus?>(null);
        }

        private async Task ExecuteJobAsync<TJob, TArgs>(string jobId, CancellationToken cancellationToken)
            where TJob : class, IBackgroundJob<TArgs>
            where TArgs : class
        {
            if (!_jobs.TryGetValue(jobId, out var jobInfo))
            {
                return;
            }

            await _executionSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (jobInfo.Status != JobStatus.Scheduled)
                {
                    return;
                }

                jobInfo.Status = JobStatus.Running;
                jobInfo.StartedAt = DateTimeOffset.UtcNow;
                jobInfo.AttemptNumber++;

                _logger?.LogDebug("Executing job {JobId} of type {JobType} (attempt {Attempt})", 
                    jobId, jobInfo.JobType, jobInfo.AttemptNumber);

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var job = scope.ServiceProvider.GetRequiredService<TJob>();

                    var args = JsonSerializer.Deserialize<TArgs>(jobInfo.SerializedArgs);
                    if (args == null)
                    {
                        throw new InvalidOperationException($"Failed to deserialize arguments for job {jobId}");
                    }

                    var metadata = jobInfo.Options.Metadata != null 
                        ? new Dictionary<string, string>(jobInfo.Options.Metadata) 
                        : new Dictionary<string, string>();
                    var context = new JobContext(
                        jobId,
                        jobInfo.AttemptNumber,
                        cancellationToken,
                        metadata,
                        jobInfo.StartedAt.Value,
                        jobInfo.JobType,
                        jobInfo.Options.Queue);

                    var startTime = DateTimeOffset.UtcNow;
                    await job.ExecuteAsync(args, context, cancellationToken);
                    var duration = DateTimeOffset.UtcNow - startTime;

                    jobInfo.Status = JobStatus.Completed;
                    jobInfo.CompletedAt = DateTimeOffset.UtcNow;
                    jobInfo.Duration = duration;

                    _logger?.LogDebug("Job {JobId} completed successfully in {Duration}", jobId, duration);

                    // Check for continuations
                    if (_continuations.TryGetValue(jobId, out var continuationIds))
                    {
                        // Continuations are handled by ContinueWithAsync
                    }
                }
                catch (OperationCanceledException)
                {
                    jobInfo.Status = JobStatus.Cancelled;
                    jobInfo.CompletedAt = DateTimeOffset.UtcNow;
                    _logger?.LogDebug("Job {JobId} was cancelled", jobId);
                }
                catch (Exception ex)
                {
                    jobInfo.LastError = ex.Message;
                    jobInfo.LastException = ex;

                    if (jobInfo.AttemptNumber < jobInfo.Options.MaxRetryAttempts)
                    {
                        jobInfo.Status = JobStatus.Retrying;
                        var delay = CalculateRetryDelay(jobInfo.AttemptNumber, jobInfo.Options);
                        
                        _logger?.LogWarning(ex, "Job {JobId} failed (attempt {Attempt}), will retry after {Delay}", 
                            jobId, jobInfo.AttemptNumber, delay);

                        // Schedule retry
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(delay, cancellationToken);
                            await ExecuteJobAsync<TJob, TArgs>(jobId, cancellationToken);
                        }, cancellationToken);
                    }
                    else
                    {
                        jobInfo.Status = JobStatus.Failed;
                        jobInfo.CompletedAt = DateTimeOffset.UtcNow;
                        _logger?.LogError(ex, "Job {JobId} failed after {Attempts} attempts", 
                            jobId, jobInfo.AttemptNumber);
                    }
                }
            }
            finally
            {
                _executionSemaphore.Release();
            }
        }

        private async Task ExecuteJobAsync<TJob>(string jobId, CancellationToken cancellationToken)
            where TJob : class, IBackgroundJob
        {
            if (!_jobs.TryGetValue(jobId, out var jobInfo))
            {
                return;
            }

            await _executionSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (jobInfo.Status != JobStatus.Scheduled)
                {
                    return;
                }

                jobInfo.Status = JobStatus.Running;
                jobInfo.StartedAt = DateTimeOffset.UtcNow;
                jobInfo.AttemptNumber++;

                _logger?.LogDebug("Executing job {JobId} of type {JobType} (attempt {Attempt})", 
                    jobId, jobInfo.JobType, jobInfo.AttemptNumber);

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var job = scope.ServiceProvider.GetRequiredService<TJob>();

                    var metadata = jobInfo.Options.Metadata != null 
                        ? new Dictionary<string, string>(jobInfo.Options.Metadata) 
                        : new Dictionary<string, string>();
                    var context = new JobContext(
                        jobId,
                        jobInfo.AttemptNumber,
                        cancellationToken,
                        metadata,
                        jobInfo.StartedAt.Value,
                        jobInfo.JobType,
                        jobInfo.Options.Queue);

                    var startTime = DateTimeOffset.UtcNow;
                    await job.ExecuteAsync(context, cancellationToken);
                    var duration = DateTimeOffset.UtcNow - startTime;

                    jobInfo.Status = JobStatus.Completed;
                    jobInfo.CompletedAt = DateTimeOffset.UtcNow;
                    jobInfo.Duration = duration;

                    _logger?.LogDebug("Job {JobId} completed successfully in {Duration}", jobId, duration);
                }
                catch (OperationCanceledException)
                {
                    jobInfo.Status = JobStatus.Cancelled;
                    jobInfo.CompletedAt = DateTimeOffset.UtcNow;
                    _logger?.LogDebug("Job {JobId} was cancelled", jobId);
                }
                catch (Exception ex)
                {
                    jobInfo.LastError = ex.Message;
                    jobInfo.LastException = ex;

                    if (jobInfo.AttemptNumber < jobInfo.Options.MaxRetryAttempts)
                    {
                        jobInfo.Status = JobStatus.Retrying;
                        var delay = CalculateRetryDelay(jobInfo.AttemptNumber, jobInfo.Options);
                        
                        _logger?.LogWarning(ex, "Job {JobId} failed (attempt {Attempt}), will retry after {Delay}", 
                            jobId, jobInfo.AttemptNumber, delay);

                        // Schedule retry
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(delay, cancellationToken);
                            await ExecuteJobAsync<TJob>(jobId, cancellationToken);
                        }, cancellationToken);
                    }
                    else
                    {
                        jobInfo.Status = JobStatus.Failed;
                        jobInfo.CompletedAt = DateTimeOffset.UtcNow;
                        _logger?.LogError(ex, "Job {JobId} failed after {Attempts} attempts", 
                            jobId, jobInfo.AttemptNumber);
                    }
                }
            }
            finally
            {
                _executionSemaphore.Release();
            }
        }

        private async Task ExecuteBatchAsync(string batchId, IJobBatch batch, CancellationToken cancellationToken)
        {
            if (!_batches.TryGetValue(batchId, out var batchInfo))
            {
                return;
            }

            batchInfo.Status = BatchStatus.Running;
            batchInfo.StartedAt = DateTimeOffset.UtcNow;

            try
            {
                var tasks = batch.Jobs.Select(async job =>
                {
                    // Simplified batch execution - in production, respect ExecutionMode and dependencies
                    var jobId = job.JobId;
                    // Jobs are executed individually - batch coordination would be handled by the provider
                    await Task.CompletedTask;
                });

                await Task.WhenAll(tasks);

                batchInfo.Status = BatchStatus.Completed;
                batchInfo.CompletedAt = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                batchInfo.Status = BatchStatus.Failed;
                batchInfo.CompletedAt = DateTimeOffset.UtcNow;
                _logger?.LogError(ex, "Batch {BatchId} failed", batchId);
            }
        }

        private async Task WaitForJobCompletionAsync(string jobId, TimeSpan maxWaitTime, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;

            while (DateTimeOffset.UtcNow - startTime < maxWaitTime)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (_jobs.TryGetValue(jobId, out var jobInfo))
                {
                    if (jobInfo.Status == JobStatus.Completed || 
                        jobInfo.Status == JobStatus.Failed || 
                        jobInfo.Status == JobStatus.Cancelled)
                    {
                        return;
                    }
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }
        }

        private bool ShouldExecuteContinuation(JobStatus? parentStatus, ContinuationOptions options)
        {
            if (!parentStatus.HasValue)
            {
                return false;
            }

            if (options.ExecuteOnFailureOnly)
            {
                return parentStatus == JobStatus.Failed;
            }

            if (options.ExecuteOnSuccessOnly)
            {
                return parentStatus == JobStatus.Completed;
            }

            return parentStatus == JobStatus.Completed || parentStatus == JobStatus.Failed;
        }

        private TimeSpan CalculateRetryDelay(int attemptNumber, JobOptions options)
        {
            if (!options.UseExponentialBackoff)
            {
                return options.InitialRetryDelay;
            }

            var delay = TimeSpan.FromMilliseconds(
                options.InitialRetryDelay.TotalMilliseconds * Math.Pow(2, attemptNumber - 1));

            return delay > options.MaxRetryDelay ? options.MaxRetryDelay : delay;
        }

        private JobOptions MergeOptions(JobOptions? options)
        {
            return options ?? JobOptions.Default;
        }

        private class JobInfo
        {
            public string JobId { get; set; } = string.Empty;
            public string JobType { get; set; } = string.Empty;
            public string SerializedArgs { get; set; } = string.Empty;
            public JobOptions Options { get; set; } = JobOptions.Default;
            public JobStatus Status { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset ScheduledFor { get; set; }
            public DateTimeOffset? StartedAt { get; set; }
            public DateTimeOffset? CompletedAt { get; set; }
            public TimeSpan? Duration { get; set; }
            public int AttemptNumber { get; set; }
            public string? LastError { get; set; }
            public Exception? LastException { get; set; }
        }

        private class BatchInfo
        {
            public string BatchId { get; set; } = string.Empty;
            public string? Name { get; set; }
            public BatchOptions Options { get; set; } = new BatchOptions();
            public List<string> Jobs { get; set; } = new();
            public BatchStatus Status { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? StartedAt { get; set; }
            public DateTimeOffset? CompletedAt { get; set; }
        }
    }
}

