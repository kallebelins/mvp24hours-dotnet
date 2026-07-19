//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Mvp24Hours.Infrastructure.HealthChecks;
using Mvp24Hours.Infrastructure.Sms.Contract;
using Mvp24Hours.Infrastructure.Sms.Models;
using Mvp24Hours.Infrastructure.Sms.Results;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.HealthChecks;

[Trait("Category", "Unit")]
public class SmsServiceHealthCheckTest
{
    [Fact]
    public void Constructor_WithNullSmsService_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new SmsServiceHealthCheck(
            null!,
            new SmsServiceHealthCheckOptions(),
            HealthChecksTestHelpers.CreateLogger<SmsServiceHealthCheck>());

        act.Should().Throw<ArgumentNullException>().WithParameterName("smsService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new SmsServiceHealthCheck(
            HealthChecksTestHelpers.CreateSmsServiceMock().Object,
            new SmsServiceHealthCheckOptions(),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldUseDefaults()
    {
        var check = new SmsServiceHealthCheck(
            HealthChecksTestHelpers.CreateSmsServiceMock().Object,
            null,
            HealthChecksTestHelpers.CreateLogger<SmsServiceHealthCheck>());

        check.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSendTestSmsDisabled_ShouldReturnHealthyWithoutSending()
    {
        Mock<ISmsService> mock = HealthChecksTestHelpers.CreateSmsServiceMock();
        var check = CreateCheck(mock.Object, new SmsServiceHealthCheckOptions { SendTestSms = false });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("test SMS sending disabled");
        result.Data["testSmsSent"].Should().Be(false);
        mock.Verify(s => s.SendAsync(It.IsAny<SmsMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSendTestSmsSucceeds_ShouldReturnHealthy()
    {
        Mock<ISmsService> mock = HealthChecksTestHelpers.CreateSmsServiceMock(
            SmsSendResult.Successful("sms-42", SmsDeliveryStatus.Delivered));
        var options = new SmsServiceHealthCheckOptions
        {
            SendTestSms = true,
            TestSmsRecipient = "+5511999999999",
            TestSmsBody = "hc",
            DegradedThresholdMs = 10_000,
            FailureThresholdMs = 30_000
        };
        var check = CreateCheck(mock.Object, options);

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["testSmsSent"].Should().Be(true);
        result.Data["testSmsRecipient"].Should().Be("+5511999999999");
        result.Data["messageId"].Should().Be("sms-42");
        result.Data["status"].Should().Be(nameof(SmsDeliveryStatus.Delivered));

        mock.Verify(s => s.SendAsync(
            It.Is<SmsMessage>(m => m.To == "+5511999999999" && m.Body == "hc"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSendFails_ShouldReturnUnhealthy()
    {
        Mock<ISmsService> mock = HealthChecksTestHelpers.CreateSmsServiceMock(
            SmsSendResult.Failed("invalid number"));
        var check = CreateCheck(mock.Object, new SmsServiceHealthCheckOptions
        {
            SendTestSms = true,
            DegradedThresholdMs = 10_000,
            FailureThresholdMs = 30_000
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("invalid number");
        result.Data["status"].Should().Be(nameof(SmsDeliveryStatus.Failed));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenResponseExceedsFailureThreshold_ShouldReturnUnhealthy()
    {
        Mock<ISmsService> mock = HealthChecksTestHelpers.CreateSmsServiceMock();
        var check = CreateCheck(mock.Object, new SmsServiceHealthCheckOptions
        {
            SendTestSms = true,
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
        Mock<ISmsService> mock = HealthChecksTestHelpers.CreateSmsServiceMock();
        var check = CreateCheck(mock.Object, new SmsServiceHealthCheckOptions
        {
            SendTestSms = true,
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
        var mock = new Mock<ISmsService>();
        mock.Setup(s => s.SendAsync(It.IsAny<SmsMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("twilio error"));
        var check = CreateCheck(mock.Object, new SmsServiceHealthCheckOptions { SendTestSms = true });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().BeOfType<InvalidOperationException>();
        result.Description.Should().Contain("twilio error");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenTimedOut_ShouldReturnUnhealthy()
    {
        var mock = new Mock<ISmsService>();
        mock.Setup(s => s.SendAsync(It.IsAny<SmsMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException());
        var check = CreateCheck(mock.Object, new SmsServiceHealthCheckOptions
        {
            SendTestSms = true,
            TimeoutSeconds = 1
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("timed out");
    }

    [Fact]
    public async Task CheckHealthAsync_WithDefaultRecipient_ShouldUsePlaceholderNumber()
    {
        Mock<ISmsService> mock = HealthChecksTestHelpers.CreateSmsServiceMock();
        var check = CreateCheck(mock.Object, new SmsServiceHealthCheckOptions
        {
            SendTestSms = true,
            TestSmsRecipient = null,
            DegradedThresholdMs = 10_000,
            FailureThresholdMs = 30_000
        });

        await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        mock.Verify(s => s.SendAsync(
            It.Is<SmsMessage>(m => m.To == "+1234567890"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static SmsServiceHealthCheck CreateCheck(
        ISmsService smsService,
        SmsServiceHealthCheckOptions? options = null)
    {
        return new SmsServiceHealthCheck(
            smsService,
            options,
            HealthChecksTestHelpers.CreateLogger<SmsServiceHealthCheck>());
    }
}
