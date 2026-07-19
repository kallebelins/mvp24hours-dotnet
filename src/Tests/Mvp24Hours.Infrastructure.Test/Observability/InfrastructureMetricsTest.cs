//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Observability.Metrics;
using Mvp24Hours.Infrastructure.Testing.Observability;

namespace Mvp24Hours.Infrastructure.Test.Observability;

[Trait("Category", "Unit")]
public class InfrastructureMetricsTest
{
    private const string HttpRequestsTotal = "mvp24hours_infrastructure_http_requests_total";
    private const string HttpRequestErrorsTotal = "mvp24hours_infrastructure_http_request_errors_total";
    private const string EmailSentTotal = "mvp24hours_infrastructure_email_sent_total";
    private const string EmailSendErrorsTotal = "mvp24hours_infrastructure_email_send_errors_total";
    private const string SmsSentTotal = "mvp24hours_infrastructure_sms_sent_total";
    private const string SmsSendErrorsTotal = "mvp24hours_infrastructure_sms_send_errors_total";
    private const string FileStorageOperationsTotal = "mvp24hours_infrastructure_file_storage_operations_total";
    private const string FileStorageOperationSize = "mvp24hours_infrastructure_file_storage_operation_size_bytes";
    private const string DistributedLockAcquisitionsTotal = "mvp24hours_infrastructure_distributed_lock_acquisitions_total";
    private const string DistributedLockTimeoutsTotal = "mvp24hours_infrastructure_distributed_lock_timeouts_total";
    private const string BackgroundJobsExecutedTotal = "mvp24hours_infrastructure_background_jobs_executed_total";
    private const string BackgroundJobFailuresTotal = "mvp24hours_infrastructure_background_job_failures_total";

    [Fact]
    public void RecordHttpRequest_WithSuccessStatus_ShouldRecordCounterWithoutError()
    {
        using var listener = new FakeMeterListener("Mvp24Hours.Infrastructure");

        InfrastructureMetrics.RecordHttpRequest("GET", "api.example.com", 200, 0.25);

        listener.GetSum(HttpRequestsTotal).Should().Be(1);
        listener.GetSum(HttpRequestErrorsTotal).Should().Be(0);
    }

    [Fact]
    public void RecordHttpRequest_WithErrorStatus_ShouldRecordErrorCounter()
    {
        using var listener = new FakeMeterListener("Mvp24Hours.Infrastructure");

        InfrastructureMetrics.RecordHttpRequest("POST", "api.example.com", 500, 1.5);

        listener.GetSum(HttpRequestsTotal).Should().Be(1);
        listener.GetSum(HttpRequestErrorsTotal).Should().Be(1);
    }

    [Fact]
    public void RecordEmailSent_WithSuccess_ShouldRecordCounterWithoutError()
    {
        using var listener = new FakeMeterListener("Mvp24Hours.Infrastructure");

        InfrastructureMetrics.RecordEmailSent("SendGrid", success: true, 0.1);

        listener.GetSum(EmailSentTotal).Should().Be(1);
        listener.GetSum(EmailSendErrorsTotal).Should().Be(0);
    }

    [Fact]
    public void RecordEmailSent_WithFailure_ShouldRecordErrorCounter()
    {
        using var listener = new FakeMeterListener("Mvp24Hours.Infrastructure");

        InfrastructureMetrics.RecordEmailSent("SendGrid", success: false, 0.2);

        listener.GetSum(EmailSentTotal).Should().Be(1);
        listener.GetSum(EmailSendErrorsTotal).Should().Be(1);
    }

    [Fact]
    public void RecordSmsSent_WithSuccess_ShouldRecordCounterWithoutError()
    {
        using var listener = new FakeMeterListener("Mvp24Hours.Infrastructure");

        InfrastructureMetrics.RecordSmsSent("Twilio", success: true, 0.05);

        listener.GetSum(SmsSentTotal).Should().Be(1);
        listener.GetSum(SmsSendErrorsTotal).Should().Be(0);
    }

    [Fact]
    public void RecordSmsSent_WithFailure_ShouldRecordErrorCounter()
    {
        using var listener = new FakeMeterListener("Mvp24Hours.Infrastructure");

        InfrastructureMetrics.RecordSmsSent("Twilio", success: false, 0.08);

        listener.GetSum(SmsSentTotal).Should().Be(1);
        listener.GetSum(SmsSendErrorsTotal).Should().Be(1);
    }

    [Fact]
    public void RecordFileStorageOperation_WithoutSizeBytes_ShouldRecordOperationCounterOnly()
    {
        using var listener = new FakeMeterListener("Mvp24Hours.Infrastructure");

        InfrastructureMetrics.RecordFileStorageOperation("upload", "AzureBlob", success: true, 0.3);

        listener.GetSum(FileStorageOperationsTotal).Should().Be(1);
        listener.GetCount(FileStorageOperationSize).Should().Be(0);
    }

    [Fact]
    public void RecordFileStorageOperation_WithSizeBytes_ShouldRecordSizeHistogram()
    {
        using var listener = new FakeMeterListener("Mvp24Hours.Infrastructure");

        InfrastructureMetrics.RecordFileStorageOperation("download", "AwsS3", success: true, 0.4, sizeBytes: 2048);

        listener.GetSum(FileStorageOperationsTotal).Should().Be(1);
        listener.GetMeasurements(FileStorageOperationSize).Single().Value.Should().Be(2048);
    }

    [Fact]
    public void RecordDistributedLockAcquisition_WithTimeout_ShouldRecordTimeoutCounter()
    {
        using var listener = new FakeMeterListener("Mvp24Hours.Infrastructure");

        InfrastructureMetrics.RecordDistributedLockAcquisition(
            "orders",
            "Redis",
            success: false,
            waitDurationSeconds: 5,
            timeout: true);

        listener.GetSum(DistributedLockAcquisitionsTotal).Should().Be(1);
        listener.GetSum(DistributedLockTimeoutsTotal).Should().Be(1);
    }

    [Fact]
    public void RecordBackgroundJobExecution_WithFailure_ShouldRecordFailuresCounter()
    {
        using var listener = new FakeMeterListener("Mvp24Hours.Infrastructure");

        InfrastructureMetrics.RecordBackgroundJobExecution("SendEmail", "Hangfire", success: false, 2.5);

        listener.GetSum(BackgroundJobsExecutedTotal).Should().Be(1);
        listener.GetSum(BackgroundJobFailuresTotal).Should().Be(1);
    }
}
