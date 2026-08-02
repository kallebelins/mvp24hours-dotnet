//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.BackgroundJobs.Contract;
using Mvp24Hours.Infrastructure.BackgroundJobs.Management;
using Mvp24Hours.Infrastructure.BackgroundJobs.Options;
using Mvp24Hours.Infrastructure.BackgroundJobs.Providers;
using Mvp24Hours.Infrastructure.BackgroundJobs.Queues;

namespace Mvp24Hours.Infrastructure.Test.Support;

internal static class BackgroundJobsTestHelpers
{
    public static FailedJob CreateFailedJob(
        string jobId = "job-1",
        string jobType = "Test.Job",
        string? queue = "default",
        string? errorMessage = "boom",
        DateTimeOffset? addedToDlqAt = null)
    {
        return new FailedJob
        {
            JobId = jobId,
            JobType = jobType,
            SerializedArgs = "{}",
            RetryAttempts = 3,
            MaxRetryAttempts = 3,
            FirstFailedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            AddedToDlqAt = addedToDlqAt ?? DateTimeOffset.UtcNow,
            ErrorMessage = errorMessage,
            ExceptionType = "System.Exception",
            Queue = queue,
            Priority = JobPriority.Normal
        };
    }

    public static JobMetric CreateMetric(
        string jobId = "job-1",
        string jobType = "Test.Job",
        string? queue = "default",
        bool success = true,
        JobStatus status = JobStatus.Completed,
        TimeSpan? duration = null,
        DateTimeOffset? recordedAt = null)
    {
        return new JobMetric
        {
            JobId = jobId,
            JobType = jobType,
            Queue = queue,
            Success = success,
            Status = status,
            Duration = duration ?? TimeSpan.FromMilliseconds(100),
            RecordedAt = recordedAt ?? DateTimeOffset.UtcNow,
            AttemptNumber = 1
        };
    }

    public static JobExecutionRecord CreateRecord(
        string jobId = "job-1",
        string jobType = "Test.Job",
        JobStatus status = JobStatus.Completed,
        string? queue = "default",
        TimeSpan? duration = null,
        DateTimeOffset? startedAt = null)
    {
        DateTimeOffset started = startedAt ?? DateTimeOffset.UtcNow;
        return new JobExecutionRecord
        {
            JobId = jobId,
            JobType = jobType,
            Status = status,
            Queue = queue,
            StartedAt = started,
            CompletedAt = started.Add(duration ?? TimeSpan.FromMilliseconds(50)),
            Duration = duration ?? TimeSpan.FromMilliseconds(50),
            AttemptNumber = 1
        };
    }

    public static PriorityQueueManager.QueuedJob CreateQueuedJob(string jobId = "job-1", string jobType = "Test.Job")
    {
        return new PriorityQueueManager.QueuedJob
        {
            JobId = jobId,
            JobType = jobType,
            SerializedArgs = "{}",
            Options = JobOptions.Default,
            ScheduledFor = DateTimeOffset.UtcNow
        };
    }

    public static IOptions<HangfireJobOptions> CreateHangfireOptions(
        HangfireStorageProvider storageProvider = HangfireStorageProvider.Memory)
    {
        return Options.Create(new HangfireJobOptions
        {
            StorageProvider = storageProvider,
            ConnectionString = "Server=.;Database=Hangfire;"
        });
    }

    public static IOptions<QuartzJobOptions> CreateQuartzOptions(
        QuartzStorageProvider storageProvider = QuartzStorageProvider.Memory)
    {
        return Options.Create(new QuartzJobOptions
        {
            StorageProvider = storageProvider,
            ConnectionString = "Server=.;Database=Quartz;"
        });
    }

    public static InMemoryJobProvider CreateInMemoryProvider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<DummyJob>();
        services.AddSingleton<DummyJobWithArgs>();
        services.AddSingleton<TrackingJob>();
        services.AddSingleton<TrackingJobWithArgs>();
        services.AddSingleton<FailingJob>();
        configure?.Invoke(services);
        return new InMemoryJobProvider(services.BuildServiceProvider());
    }

    public static async Task<JobStatus?> WaitForJobStatusAsync(
        InMemoryJobProvider provider,
        string jobId,
        Func<JobStatus?, bool> predicate,
        TimeSpan? timeout = null)
    {
        TimeSpan wait = timeout ?? TimeSpan.FromSeconds(5);
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(wait);

        while (DateTimeOffset.UtcNow < deadline)
        {
            JobStatus? status = await provider.GetStatusAsync(jobId);
            if (predicate(status))
            {
                return status;
            }

            await Task.Delay(50);
        }

        return await provider.GetStatusAsync(jobId);
    }

    public sealed class DummyJobArgs
    {
        public string Value { get; set; } = "test";
    }

    public sealed class DummyJob : IBackgroundJob
    {
        public Task ExecuteAsync(IJobContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public sealed class DummyJobWithArgs : IBackgroundJob<DummyJobArgs>
    {
        public Task ExecuteAsync(DummyJobArgs args, IJobContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public sealed class TrackingJob : IBackgroundJob
    {
        private static int _executionCount;
        public static readonly AsyncLocal<int> AsyncLocalCount = new();

        public static int ExecutionCount => _executionCount;

        public Task ExecuteAsync(IJobContext context, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _executionCount);
            AsyncLocalCount.Value++;
            return Task.CompletedTask;
        }

        public static void Reset()
        {
            Interlocked.Exchange(ref _executionCount, 0);
            AsyncLocalCount.Value = 0;
        }
    }

    public sealed class TrackingJobWithArgs : IBackgroundJob<DummyJobArgs>
    {
        public static string? LastValue { get; private set; }

        public Task ExecuteAsync(DummyJobArgs args, IJobContext context, CancellationToken cancellationToken)
        {
            LastValue = args.Value;
            return Task.CompletedTask;
        }

        public static void Reset()
        {
            LastValue = null;
        }
    }

    public sealed class FailingJob : IBackgroundJob
    {
        public Task ExecuteAsync(IJobContext context, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Job failed intentionally");
        }
    }
}
