//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using Mvp24Hours.Infrastructure.HealthChecks;

namespace Mvp24Hours.Infrastructure.Test.HealthChecks;

[Trait("Category", "Unit")]
public class HealthCheckOptionsTest
{
    [Fact]
    public void FileStorageHealthCheckOptions_Defaults_ShouldMatchExpected()
    {
        var options = new FileStorageHealthCheckOptions();

        options.TestFilePath.Should().BeNull();
        options.TestContent.Should().Be("Health check test content");
        options.TimeoutSeconds.Should().Be(10);
        options.SkipContentVerification.Should().BeFalse();
        options.DegradedThresholdMs.Should().Be(1000);
        options.FailureThresholdMs.Should().Be(5000);
        options.Tags.Should().BeEquivalentTo(["file-storage", "storage", "ready"]);
    }

    [Fact]
    public void EmailServiceHealthCheckOptions_Defaults_ShouldMatchExpected()
    {
        var options = new EmailServiceHealthCheckOptions();

        options.SendTestEmail.Should().BeFalse();
        options.TestEmailRecipient.Should().BeNull();
        options.TimeoutSeconds.Should().Be(10);
        options.DegradedThresholdMs.Should().Be(2000);
        options.FailureThresholdMs.Should().Be(10000);
        options.Tags.Should().BeEquivalentTo(["email", "email-service", "ready"]);
    }

    [Fact]
    public void SmsServiceHealthCheckOptions_Defaults_ShouldMatchExpected()
    {
        var options = new SmsServiceHealthCheckOptions();

        options.SendTestSms.Should().BeFalse();
        options.TestSmsRecipient.Should().BeNull();
        options.TimeoutSeconds.Should().Be(10);
        options.DegradedThresholdMs.Should().Be(2000);
        options.FailureThresholdMs.Should().Be(10000);
        options.Tags.Should().BeEquivalentTo(["sms", "sms-service", "ready"]);
    }

    [Fact]
    public void HttpClientHealthCheckOptions_Defaults_ShouldMatchExpected()
    {
        var options = new HttpClientHealthCheckOptions();

        options.HealthEndpoint.Should().Be("/health");
        options.TimeoutSeconds.Should().Be(5);
        options.ExpectedStatusCode.Should().Be(HttpStatusCode.OK);
        options.UseHeadRequest.Should().BeFalse();
        options.ValidateResponseContent.Should().BeFalse();
        options.DegradedThresholdMs.Should().Be(500);
        options.FailureThresholdMs.Should().Be(2000);
        options.Tags.Should().BeEquivalentTo(["http", "httpclient", "ready"]);
    }

    [Fact]
    public void DistributedLockHealthCheckOptions_Defaults_ShouldMatchExpected()
    {
        var options = new DistributedLockHealthCheckOptions();

        options.ProviderName.Should().BeNull();
        options.LockTimeoutSeconds.Should().Be(5);
        options.LockExpirationSeconds.Should().Be(10);
        options.DegradedThresholdMs.Should().Be(500);
        options.FailureThresholdMs.Should().Be(2000);
        options.Tags.Should().BeEquivalentTo(["distributed-lock", "locking", "ready"]);
    }

    [Fact]
    public void BackgroundJobHealthCheckOptions_Defaults_ShouldMatchExpected()
    {
        var options = new BackgroundJobHealthCheckOptions();

        options.ScheduleTestJob.Should().BeFalse();
        options.TimeoutSeconds.Should().Be(5);
        options.DegradedThresholdMs.Should().Be(1000);
        options.FailureThresholdMs.Should().Be(5000);
        options.Tags.Should().BeEquivalentTo(["background-jobs", "jobs", "scheduler", "ready"]);
    }

    [Fact]
    public void InfrastructureHealthCheckOptions_Defaults_ShouldCreateNestedOptions()
    {
        var options = new InfrastructureHealthCheckOptions();

        options.DistributedLock.Should().NotBeNull();
        options.FileStorage.Should().NotBeNull();
        options.Email.Should().NotBeNull();
        options.Sms.Should().NotBeNull();
        options.BackgroundJobs.Should().NotBeNull();
    }
}
