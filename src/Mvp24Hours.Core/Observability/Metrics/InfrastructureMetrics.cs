//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Mvp24Hours.Core.Observability.Metrics;

/// <summary>
/// Provides metrics instrumentation for cross-cutting infrastructure operations.
/// </summary>
/// <remarks>
/// <para>
/// This class provides counters, histograms, and gauges for monitoring
/// HTTP client requests, email, SMS, file storage, distributed locks, and background jobs.
/// </para>
/// <para>
/// <strong>Metrics provided:</strong>
/// <list type="bullet">
/// <item><c>http_client.requests_total</c> - Counter for outbound HTTP requests</item>
/// <item><c>email.sent_total</c> - Counter for emails sent</item>
/// <item><c>sms.sent_total</c> - Counter for SMS sent</item>
/// <item><c>file_storage.operations_total</c> - Counter for file operations</item>
/// <item><c>distributed_lock.acquisitions_total</c> - Counter for lock acquisitions</item>
/// <item><c>background_job.total</c> - Counter for background jobs</item>
/// </list>
/// </para>
/// </remarks>
public sealed class InfrastructureMetrics
{
    #region HTTP Client Metrics

    private readonly Counter<long> _httpClientRequestsTotal;
    private readonly Counter<long> _httpClientRequestsFailedTotal;
    private readonly Histogram<double> _httpClientRequestDuration;

    #endregion

    #region Email Metrics

    private readonly Counter<long> _emailsSentTotal;
    private readonly Counter<long> _emailsFailedTotal;

    #endregion

    #region SMS Metrics

    private readonly Counter<long> _smsSentTotal;
    private readonly Counter<long> _smsFailedTotal;

    #endregion

    #region File Storage Metrics

    private readonly Counter<long> _fileStorageOperationsTotal;
    private readonly Histogram<int> _fileStorageFileSizeBytes;

    #endregion

    #region Distributed Lock Metrics

    private readonly Counter<long> _lockAcquisitionsTotal;
    private readonly Counter<long> _lockFailuresTotal;
    private readonly Histogram<double> _lockHoldDuration;
    private readonly Histogram<double> _lockWaitDuration;

    #endregion

    #region Background Job Metrics

    private readonly Counter<long> _backgroundJobsTotal;
    private readonly Counter<long> _backgroundJobsFailedTotal;
    private readonly Histogram<double> _backgroundJobDuration;
    private readonly UpDownCounter<int> _backgroundJobsPending;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="InfrastructureMetrics"/> class.
    /// </summary>
    public InfrastructureMetrics()
    {
        var meter = Mvp24HoursMeters.Infrastructure.Meter;

        // HTTP Client
        _httpClientRequestsTotal = meter.CreateCounter<long>(
            MetricNames.HttpClientRequestsTotal,
            unit: "{requests}",
            description: "Total number of outbound HTTP client requests");

        _httpClientRequestsFailedTotal = meter.CreateCounter<long>(
            MetricNames.HttpClientRequestsFailedTotal,
            unit: "{requests}",
            description: "Total number of failed HTTP client requests");

        _httpClientRequestDuration = meter.CreateHistogram<double>(
            MetricNames.HttpClientRequestDuration,
            unit: "ms",
            description: "Duration of HTTP client requests in milliseconds");

        // Email
        _emailsSentTotal = meter.CreateCounter<long>(
            MetricNames.EmailsSentTotal,
            unit: "{emails}",
            description: "Total number of emails sent");

        _emailsFailedTotal = meter.CreateCounter<long>(
            MetricNames.EmailsFailedTotal,
            unit: "{emails}",
            description: "Total number of failed email sends");

        // SMS
        _smsSentTotal = meter.CreateCounter<long>(
            MetricNames.SmsSentTotal,
            unit: "{messages}",
            description: "Total number of SMS sent");

        _smsFailedTotal = meter.CreateCounter<long>(
            MetricNames.SmsFailedTotal,
            unit: "{messages}",
            description: "Total number of failed SMS sends");

        // File Storage
        _fileStorageOperationsTotal = meter.CreateCounter<long>(
            MetricNames.FileStorageOperationsTotal,
            unit: "{operations}",
            description: "Total number of file storage operations");

        _fileStorageFileSizeBytes = meter.CreateHistogram<int>(
            MetricNames.FileStorageFileSizeBytes,
            unit: "By",
            description: "Size of files in storage operations");

        // Distributed Lock
        _lockAcquisitionsTotal = meter.CreateCounter<long>(
            MetricNames.DistributedLockAcquisitionsTotal,
            unit: "{acquisitions}",
            description: "Total number of distributed lock acquisitions");

        _lockFailuresTotal = meter.CreateCounter<long>(
            MetricNames.DistributedLockFailuresTotal,
            unit: "{failures}",
            description: "Total number of distributed lock acquisition failures");

        _lockHoldDuration = meter.CreateHistogram<double>(
            MetricNames.DistributedLockHoldDuration,
            unit: "ms",
            description: "Duration of lock hold time in milliseconds");

        _lockWaitDuration = meter.CreateHistogram<double>(
            MetricNames.DistributedLockWaitDuration,
            unit: "ms",
            description: "Time waiting for lock acquisition in milliseconds");

        // Background Jobs
        _backgroundJobsTotal = meter.CreateCounter<long>(
            MetricNames.BackgroundJobsTotal,
            unit: "{jobs}",
            description: "Total number of background jobs executed");

        _backgroundJobsFailedTotal = meter.CreateCounter<long>(
            MetricNames.BackgroundJobsFailedTotal,
            unit: "{jobs}",
            description: "Total number of failed background jobs");

        _backgroundJobDuration = meter.CreateHistogram<double>(
            MetricNames.BackgroundJobDuration,
            unit: "ms",
            description: "Duration of background job execution in milliseconds");

        _backgroundJobsPending = meter.CreateUpDownCounter<int>(
            MetricNames.BackgroundJobsPending,
            unit: "{jobs}",
            description: "Number of pending background jobs");
    }

    #region HTTP Client Methods

    /// <summary>
    /// Begins tracking an HTTP client request.
    /// </summary>
    /// <param name="method">HTTP method.</param>
    /// <param name="host">Target host.</param>
    /// <returns>A scope that should be disposed when request completes.</returns>
    public HttpClientRequestScope BeginHttpClientRequest(string method, string host)
    {
        return new HttpClientRequestScope(this, method, host);
    }

    /// <summary>
    /// Records an HTTP client request.
    /// </summary>
    /// <param name="method">HTTP method.</param>
    /// <param name="host">Target host.</param>
    /// <param name="statusCode">HTTP status code.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    public void RecordHttpClientRequest(string method, string host, int statusCode, double durationMs)
    {
        var success = statusCode < 400;
        var tags = new TagList
        {
            { MetricTags.HttpMethod, method },
            { "host", host },
            { MetricTags.HttpStatusCode, statusCode },
            { MetricTags.Status, success ? MetricTags.StatusSuccess : MetricTags.StatusFailure }
        };

        _httpClientRequestsTotal.Add(1, tags);

        if (!success)
        {
            _httpClientRequestsFailedTotal.Add(1, tags);
        }

        _httpClientRequestDuration.Record(durationMs, tags);
    }

    #endregion

    #region Email Methods

    /// <summary>
    /// Records an email send operation.
    /// </summary>
    /// <param name="success">Whether the send was successful.</param>
    /// <param name="provider">Email provider name (optional).</param>
    public void RecordEmailSend(bool success, string? provider = null)
    {
        var tags = new TagList();

        if (!string.IsNullOrEmpty(provider))
        {
            tags.Add("provider", provider);
        }

        if (success)
        {
            _emailsSentTotal.Add(1, tags);
        }
        else
        {
            _emailsFailedTotal.Add(1, tags);
        }
    }

    #endregion

    #region SMS Methods

    /// <summary>
    /// Records an SMS send operation.
    /// </summary>
    /// <param name="success">Whether the send was successful.</param>
    /// <param name="provider">SMS provider name (optional).</param>
    public void RecordSmsSend(bool success, string? provider = null)
    {
        var tags = new TagList();

        if (!string.IsNullOrEmpty(provider))
        {
            tags.Add("provider", provider);
        }

        if (success)
        {
            _smsSentTotal.Add(1, tags);
        }
        else
        {
            _smsFailedTotal.Add(1, tags);
        }
    }

    #endregion

    #region File Storage Methods

    /// <summary>
    /// Records a file storage operation.
    /// </summary>
    /// <param name="operation">Operation type (upload, download, delete).</param>
    /// <param name="fileSizeBytes">File size in bytes (optional).</param>
    /// <param name="provider">Storage provider name (optional).</param>
    public void RecordFileStorageOperation(string operation, int fileSizeBytes = 0, string? provider = null)
    {
        var tags = new TagList { { MetricTags.Operation, operation } };

        if (!string.IsNullOrEmpty(provider))
        {
            tags.Add("provider", provider);
        }

        _fileStorageOperationsTotal.Add(1, tags);

        if (fileSizeBytes > 0)
        {
            _fileStorageFileSizeBytes.Record(fileSizeBytes, tags);
        }
    }

    #endregion

    #region Distributed Lock Methods

    /// <summary>
    /// Begins tracking a lock acquisition.
    /// </summary>
    /// <param name="lockName">Name of the lock resource.</param>
    /// <returns>A scope that should be disposed when lock is released.</returns>
    public LockScope BeginLock(string lockName)
    {
        return new LockScope(this, lockName);
    }

    /// <summary>
    /// Records a lock acquisition attempt.
    /// </summary>
    /// <param name="lockName">Name of the lock resource.</param>
    /// <param name="acquired">Whether the lock was acquired.</param>
    /// <param name="waitDurationMs">Time spent waiting in milliseconds.</param>
    public void RecordLockAttempt(string lockName, bool acquired, double waitDurationMs)
    {
        var tags = new TagList { { "lock_name", lockName } };

        if (acquired)
        {
            _lockAcquisitionsTotal.Add(1, tags);
        }
        else
        {
            _lockFailuresTotal.Add(1, tags);
        }

        _lockWaitDuration.Record(waitDurationMs, tags);
    }

    /// <summary>
    /// Records a lock release.
    /// </summary>
    /// <param name="lockName">Name of the lock resource.</param>
    /// <param name="holdDurationMs">Time the lock was held in milliseconds.</param>
    public void RecordLockRelease(string lockName, double holdDurationMs)
    {
        var tags = new TagList { { "lock_name", lockName } };
        _lockHoldDuration.Record(holdDurationMs, tags);
    }

    #endregion

    #region Background Job Methods

    /// <summary>
    /// Begins tracking a background job execution.
    /// </summary>
    /// <param name="jobType">Type name of the job.</param>
    /// <returns>A scope that should be disposed when job completes.</returns>
    public BackgroundJobScope BeginBackgroundJob(string jobType)
    {
        return new BackgroundJobScope(this, jobType);
    }

    /// <summary>
    /// Records a background job execution.
    /// </summary>
    /// <param name="jobType">Type name of the job.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="success">Whether the job was successful.</param>
    public void RecordBackgroundJob(string jobType, double durationMs, bool success)
    {
        var tags = new TagList
        {
            { MetricTags.JobType, jobType },
            { MetricTags.Status, success ? MetricTags.StatusSuccess : MetricTags.StatusFailure }
        };

        _backgroundJobsTotal.Add(1, tags);

        if (!success)
        {
            _backgroundJobsFailedTotal.Add(1, tags);
        }

        _backgroundJobDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Updates the pending job count.
    /// </summary>
    /// <param name="delta">Change in count.</param>
    /// <param name="jobQueue">Job queue name (optional).</param>
    public void UpdatePendingJobs(int delta, string? jobQueue = null)
    {
        var tags = new TagList();

        if (!string.IsNullOrEmpty(jobQueue))
        {
            tags.Add(MetricTags.JobQueue, jobQueue);
        }

        _backgroundJobsPending.Add(delta, tags);
    }

    #endregion

    #region Scope Structs

    /// <summary>
    /// Represents a scope for tracking HTTP client request duration.
    /// </summary>
    public struct HttpClientRequestScope : IDisposable
    {
        private readonly InfrastructureMetrics _metrics;
        private readonly string _method;
        private readonly string _host;
        private readonly long _startTimestamp;

        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int StatusCode { get; private set; }

        internal HttpClientRequestScope(InfrastructureMetrics metrics, string method, string host)
        {
            _metrics = metrics;
            _method = method;
            _host = host;
            _startTimestamp = Stopwatch.GetTimestamp();
            StatusCode = 0;
        }

        /// <summary>
        /// Sets the response status code.
        /// </summary>
        /// <param name="statusCode">HTTP status code.</param>
        public void SetStatusCode(int statusCode) => StatusCode = statusCode;

        /// <inheritdoc />
        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);
            _metrics.RecordHttpClientRequest(_method, _host, StatusCode, elapsed.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Represents a scope for tracking lock hold duration.
    /// </summary>
    public readonly struct LockScope : IDisposable
    {
        private readonly InfrastructureMetrics _metrics;
        private readonly string _lockName;
        private readonly long _startTimestamp;

        internal LockScope(InfrastructureMetrics metrics, string lockName)
        {
            _metrics = metrics;
            _lockName = lockName;
            _startTimestamp = Stopwatch.GetTimestamp();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);
            _metrics.RecordLockRelease(_lockName, elapsed.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Represents a scope for tracking background job duration.
    /// </summary>
    public struct BackgroundJobScope : IDisposable
    {
        private readonly InfrastructureMetrics _metrics;
        private readonly string _jobType;
        private readonly long _startTimestamp;

        /// <summary>
        /// Gets or sets whether the job succeeded.
        /// </summary>
        public bool Succeeded { get; private set; }

        internal BackgroundJobScope(InfrastructureMetrics metrics, string jobType)
        {
            _metrics = metrics;
            _jobType = jobType;
            _startTimestamp = Stopwatch.GetTimestamp();
            Succeeded = false;
        }

        /// <summary>
        /// Marks the job as completed successfully.
        /// </summary>
        public void Complete() => Succeeded = true;

        /// <summary>
        /// Marks the job as failed.
        /// </summary>
        public void Fail() => Succeeded = false;

        /// <inheritdoc />
        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);
            _metrics.RecordBackgroundJob(_jobType, elapsed.TotalMilliseconds, Succeeded);
        }
    }

    #endregion
}

