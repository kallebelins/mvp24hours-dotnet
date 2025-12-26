//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Mvp24Hours.Infrastructure.Observability.Metrics
{
    /// <summary>
    /// Centralized metrics collection for Infrastructure module using OpenTelemetry Meter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides Prometheus-compatible metrics for all Infrastructure subsystems.
    /// Metrics are collected using OpenTelemetry Meter API, which can be exported to
    /// Prometheus, Azure Monitor, or other observability platforms.
    /// </para>
    /// <para>
    /// <strong>Metric Types:</strong>
    /// <list type="bullet">
    /// <item><c>Counter</c>: Incrementing values (e.g., request count, error count)</item>
    /// <item><c>Histogram</c>: Distribution of values (e.g., latency, duration)</item>
    /// <item><c>Gauge</c>: Current value (e.g., active connections, queue size)</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class InfrastructureMetrics
    {
        private static readonly Meter Meter = new("Mvp24Hours.Infrastructure", "1.0.0");

        // HTTP Client Metrics
        private static readonly Counter<long> HttpRequestsTotal = Meter.CreateCounter<long>(
            "mvp24hours_infrastructure_http_requests_total",
            "requests",
            "Total number of HTTP requests");

        private static readonly Histogram<double> HttpRequestDuration = Meter.CreateHistogram<double>(
            "mvp24hours_infrastructure_http_request_duration_seconds",
            "seconds",
            "HTTP request duration in seconds");

        private static readonly Counter<long> HttpRequestErrors = Meter.CreateCounter<long>(
            "mvp24hours_infrastructure_http_request_errors_total",
            "errors",
            "Total number of HTTP request errors");

        // Email Metrics
        private static readonly Counter<long> EmailSentTotal = Meter.CreateCounter<long>(
            "mvp24hours_infrastructure_email_sent_total",
            "emails",
            "Total number of emails sent");

        private static readonly Histogram<double> EmailSendDuration = Meter.CreateHistogram<double>(
            "mvp24hours_infrastructure_email_send_duration_seconds",
            "seconds",
            "Email send duration in seconds");

        private static readonly Counter<long> EmailSendErrors = Meter.CreateCounter<long>(
            "mvp24hours_infrastructure_email_send_errors_total",
            "errors",
            "Total number of email send errors");

        // SMS Metrics
        private static readonly Counter<long> SmsSentTotal = Meter.CreateCounter<long>(
            "mvp24hours_infrastructure_sms_sent_total",
            "sms",
            "Total number of SMS messages sent");

        private static readonly Histogram<double> SmsSendDuration = Meter.CreateHistogram<double>(
            "mvp24hours_infrastructure_sms_send_duration_seconds",
            "seconds",
            "SMS send duration in seconds");

        private static readonly Counter<long> SmsSendErrors = Meter.CreateCounter<long>(
            "mvp24hours_infrastructure_sms_send_errors_total",
            "errors",
            "Total number of SMS send errors");

        // File Storage Metrics
        private static readonly Counter<long> FileStorageOperationsTotal = Meter.CreateCounter<long>(
            "mvp24hours_infrastructure_file_storage_operations_total",
            "operations",
            "Total number of file storage operations");

        private static readonly Histogram<double> FileStorageOperationDuration = Meter.CreateHistogram<double>(
            "mvp24hours_infrastructure_file_storage_operation_duration_seconds",
            "seconds",
            "File storage operation duration in seconds");

        private static readonly Histogram<long> FileStorageOperationSize = Meter.CreateHistogram<long>(
            "mvp24hours_infrastructure_file_storage_operation_size_bytes",
            "bytes",
            "File storage operation size in bytes");

        // Distributed Locking Metrics
        private static readonly Counter<long> DistributedLockAcquisitionsTotal = Meter.CreateCounter<long>(
            "mvp24hours_infrastructure_distributed_lock_acquisitions_total",
            "acquisitions",
            "Total number of distributed lock acquisition attempts");

        private static readonly Histogram<double> DistributedLockWaitDuration = Meter.CreateHistogram<double>(
            "mvp24hours_infrastructure_distributed_lock_wait_duration_seconds",
            "seconds",
            "Distributed lock wait duration in seconds");

        private static readonly Counter<long> DistributedLockTimeouts = Meter.CreateCounter<long>(
            "mvp24hours_infrastructure_distributed_lock_timeouts_total",
            "timeouts",
            "Total number of distributed lock timeouts");

        // Background Jobs Metrics
        private static readonly Counter<long> BackgroundJobsExecutedTotal = Meter.CreateCounter<long>(
            "mvp24hours_infrastructure_background_jobs_executed_total",
            "jobs",
            "Total number of background jobs executed");

        private static readonly Histogram<double> BackgroundJobDuration = Meter.CreateHistogram<double>(
            "mvp24hours_infrastructure_background_job_duration_seconds",
            "seconds",
            "Background job execution duration in seconds");

        private static readonly Counter<long> BackgroundJobFailures = Meter.CreateCounter<long>(
            "mvp24hours_infrastructure_background_job_failures_total",
            "failures",
            "Total number of background job failures");

        // HTTP Client Metrics Methods
        public static void RecordHttpRequest(string method, string? host, int statusCode, double durationSeconds)
        {
            var tags = new TagList
            {
                { "method", method },
                { "status_code", statusCode.ToString() }
            };

            if (!string.IsNullOrEmpty(host))
            {
                tags.Add("host", host);
            }

            HttpRequestsTotal.Add(1, tags);
            HttpRequestDuration.Record(durationSeconds, tags);

            if (statusCode >= 400)
            {
                HttpRequestErrors.Add(1, tags);
            }
        }

        // Email Metrics Methods
        public static void RecordEmailSent(string provider, bool success, double durationSeconds)
        {
            var tags = new TagList { { "provider", provider }, { "success", success.ToString() } };
            EmailSentTotal.Add(1, tags);
            EmailSendDuration.Record(durationSeconds, tags);

            if (!success)
            {
                EmailSendErrors.Add(1, tags);
            }
        }

        // SMS Metrics Methods
        public static void RecordSmsSent(string provider, bool success, double durationSeconds)
        {
            var tags = new TagList { { "provider", provider }, { "success", success.ToString() } };
            SmsSentTotal.Add(1, tags);
            SmsSendDuration.Record(durationSeconds, tags);

            if (!success)
            {
                SmsSendErrors.Add(1, tags);
            }
        }

        // File Storage Metrics Methods
        public static void RecordFileStorageOperation(
            string operation,
            string provider,
            bool success,
            double durationSeconds,
            long? sizeBytes = null)
        {
            var tags = new TagList
            {
                { "operation", operation },
                { "provider", provider },
                { "success", success.ToString() }
            };

            FileStorageOperationsTotal.Add(1, tags);
            FileStorageOperationDuration.Record(durationSeconds, tags);

            if (sizeBytes.HasValue)
            {
                FileStorageOperationSize.Record(sizeBytes.Value, tags);
            }
        }

        // Distributed Locking Metrics Methods
        public static void RecordDistributedLockAcquisition(
            string resource,
            string provider,
            bool success,
            double waitDurationSeconds,
            bool timeout = false)
        {
            var tags = new TagList
            {
                { "resource", resource },
                { "provider", provider },
                { "success", success.ToString() }
            };

            DistributedLockAcquisitionsTotal.Add(1, tags);
            DistributedLockWaitDuration.Record(waitDurationSeconds, tags);

            if (timeout)
            {
                DistributedLockTimeouts.Add(1, tags);
            }
        }

        // Background Jobs Metrics Methods
        public static void RecordBackgroundJobExecution(
            string jobType,
            string provider,
            bool success,
            double durationSeconds)
        {
            var tags = new TagList
            {
                { "job_type", jobType },
                { "provider", provider },
                { "success", success.ToString() }
            };

            BackgroundJobsExecutedTotal.Add(1, tags);
            BackgroundJobDuration.Record(durationSeconds, tags);

            if (!success)
            {
                BackgroundJobFailures.Add(1, tags);
            }
        }
    }
}

