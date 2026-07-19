//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Mvp24Hours.Infrastructure.Email.Contract;
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Email.Results;
using Mvp24Hours.Infrastructure.HealthChecks;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.HealthChecks;

[Trait("Category", "Unit")]
public class EmailServiceHealthCheckTest
{
    [Fact]
    public void Constructor_WithNullEmailService_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new EmailServiceHealthCheck(
            null!,
            new EmailServiceHealthCheckOptions(),
            HealthChecksTestHelpers.CreateLogger<EmailServiceHealthCheck>());

        act.Should().Throw<ArgumentNullException>().WithParameterName("emailService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new EmailServiceHealthCheck(
            HealthChecksTestHelpers.CreateEmailServiceMock().Object,
            new EmailServiceHealthCheckOptions(),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldUseDefaults()
    {
        var check = new EmailServiceHealthCheck(
            HealthChecksTestHelpers.CreateEmailServiceMock().Object,
            null,
            HealthChecksTestHelpers.CreateLogger<EmailServiceHealthCheck>());

        check.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSendTestEmailDisabled_ShouldReturnHealthyWithoutSending()
    {
        Mock<IEmailService> mock = HealthChecksTestHelpers.CreateEmailServiceMock();
        var check = CreateCheck(mock.Object, new EmailServiceHealthCheckOptions { SendTestEmail = false });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("test email sending disabled");
        result.Data["testEmailSent"].Should().Be(false);
        mock.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSendTestEmailSucceeds_ShouldReturnHealthy()
    {
        Mock<IEmailService> mock = HealthChecksTestHelpers.CreateEmailServiceMock(
            EmailSendResult.Successful("email-id-1"));
        var options = new EmailServiceHealthCheckOptions
        {
            SendTestEmail = true,
            TestEmailRecipient = "ops@example.com",
            TestEmailSubject = "HC",
            TestEmailBody = "body",
            DegradedThresholdMs = 10_000,
            FailureThresholdMs = 30_000
        };
        var check = CreateCheck(mock.Object, options);

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["testEmailSent"].Should().Be(true);
        result.Data["testEmailRecipient"].Should().Be("ops@example.com");
        result.Data["messageId"].Should().Be("email-id-1");
        result.Data["sendSuccess"].Should().Be(true);

        mock.Verify(s => s.SendAsync(
            It.Is<EmailMessage>(m =>
                m.To.Contains("ops@example.com") &&
                m.Subject == "HC" &&
                m.PlainTextBody == "body"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSendFails_ShouldReturnUnhealthy()
    {
        Mock<IEmailService> mock = HealthChecksTestHelpers.CreateEmailServiceMock(
            EmailSendResult.Failed("smtp down"));
        var check = CreateCheck(mock.Object, new EmailServiceHealthCheckOptions
        {
            SendTestEmail = true,
            DegradedThresholdMs = 10_000,
            FailureThresholdMs = 30_000
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("smtp down");
        result.Data["error"].Should().Be("smtp down");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenResponseExceedsFailureThreshold_ShouldReturnUnhealthy()
    {
        Mock<IEmailService> mock = HealthChecksTestHelpers.CreateEmailServiceMock();
        var check = CreateCheck(mock.Object, new EmailServiceHealthCheckOptions
        {
            SendTestEmail = true,
            DegradedThresholdMs = 0,
            FailureThresholdMs = 0
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("exceeded threshold");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenResponseExceedsDegradedThreshold_ShouldReturnDegraded()
    {
        Mock<IEmailService> mock = HealthChecksTestHelpers.CreateEmailServiceMock();
        var check = CreateCheck(mock.Object, new EmailServiceHealthCheckOptions
        {
            SendTestEmail = true,
            DegradedThresholdMs = 0,
            FailureThresholdMs = 30_000
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("is slow");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSendThrows_ShouldReturnUnhealthy()
    {
        var mock = new Mock<IEmailService>();
        mock.Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("provider error"));
        var check = CreateCheck(mock.Object, new EmailServiceHealthCheckOptions { SendTestEmail = true });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().BeOfType<InvalidOperationException>();
        result.Description.Should().Contain("provider error");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenTimedOut_ShouldReturnUnhealthy()
    {
        var mock = new Mock<IEmailService>();
        mock.Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException());
        var check = CreateCheck(mock.Object, new EmailServiceHealthCheckOptions
        {
            SendTestEmail = true,
            TimeoutSeconds = 1
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("timed out");
        result.Data["error"].Should().Be("Operation timeout");
    }

    [Fact]
    public async Task CheckHealthAsync_WithDefaultRecipient_ShouldUseExampleAddress()
    {
        Mock<IEmailService> mock = HealthChecksTestHelpers.CreateEmailServiceMock();
        var check = CreateCheck(mock.Object, new EmailServiceHealthCheckOptions
        {
            SendTestEmail = true,
            TestEmailRecipient = null,
            DegradedThresholdMs = 10_000,
            FailureThresholdMs = 30_000
        });

        await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        mock.Verify(s => s.SendAsync(
            It.Is<EmailMessage>(m => m.To.Contains("health-check@example.com")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static EmailServiceHealthCheck CreateCheck(
        IEmailService emailService,
        EmailServiceHealthCheckOptions? options = null)
    {
        return new EmailServiceHealthCheck(
            emailService,
            options,
            HealthChecksTestHelpers.CreateLogger<EmailServiceHealthCheck>());
    }
}
