//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Mvp24Hours.Infrastructure.DistributedLocking.Contract;
using Mvp24Hours.Infrastructure.DistributedLocking.Options;
using Mvp24Hours.Infrastructure.DistributedLocking.Results;
using Mvp24Hours.Infrastructure.HealthChecks;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.HealthChecks;

[Trait("Category", "Unit")]
public class DistributedLockHealthCheckTest
{
    [Fact]
    public void Constructor_WithNullLockFactory_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new DistributedLockHealthCheck(
            null!,
            new DistributedLockHealthCheckOptions(),
            HealthChecksTestHelpers.CreateLogger<DistributedLockHealthCheck>());

        act.Should().Throw<ArgumentNullException>().WithParameterName("lockFactory");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new DistributedLockHealthCheck(
            HealthChecksTestHelpers.CreateLockFactoryMock().Object,
            new DistributedLockHealthCheckOptions(),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldUseDefaults()
    {
        var check = new DistributedLockHealthCheck(
            HealthChecksTestHelpers.CreateLockFactoryMock().Object,
            null,
            HealthChecksTestHelpers.CreateLogger<DistributedLockHealthCheck>());

        check.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenAcquireAndReleaseSucceed_ShouldReturnHealthy()
    {
        Mock<IDistributedLockFactory> factory = HealthChecksTestHelpers.CreateLockFactoryMock();
        DistributedLockHealthCheck check = CreateCheck(factory.Object, new DistributedLockHealthCheckOptions
        {
            DegradedThresholdMs = 10_000,
            FailureThresholdMs = 30_000
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["acquired"].Should().Be(true);
        result.Data["released"].Should().Be(true);
        result.Data["providerName"].Should().Be("default");
        result.Data["testResourceName"].ToString().Should().StartWith("health-check-");
    }

    [Fact]
    public async Task CheckHealthAsync_WithProviderName_ShouldCreateNamedProvider()
    {
        Mock<IDistributedLockFactory> factory = HealthChecksTestHelpers.CreateLockFactoryMock(providerName: "Redis");
        DistributedLockHealthCheck check = CreateCheck(factory.Object, new DistributedLockHealthCheckOptions
        {
            ProviderName = "Redis",
            DegradedThresholdMs = 10_000,
            FailureThresholdMs = 30_000
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["providerName"].Should().Be("Redis");
        factory.Verify(f => f.Create("Redis"), Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCreateFails_ShouldReturnUnhealthy()
    {
        Mock<IDistributedLockFactory> factory = HealthChecksTestHelpers.CreateLockFactoryMock(
            createException: new InvalidOperationException("no providers"));
        DistributedLockHealthCheck check = CreateCheck(factory.Object);

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Failed to create distributed lock");
        result.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenAcquireFails_ShouldReturnUnhealthy()
    {
        Mock<IDistributedLockFactory> factory = HealthChecksTestHelpers.CreateLockFactoryMock(
            result: LockAcquisitionResult.Failed("already locked"));
        DistributedLockHealthCheck check = CreateCheck(factory.Object);

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("already locked");
        result.Data["acquired"].Should().Be(false);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenReleaseFails_ShouldReturnDegraded()
    {
        var handle = new Mock<ILockHandle>();
        handle.Setup(h => h.ReleaseAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("release failed"));

        Mock<IDistributedLockFactory> factory = HealthChecksTestHelpers.CreateLockFactoryMock(
            result: LockAcquisitionResult.Acquired(handle.Object));
        DistributedLockHealthCheck check = CreateCheck(factory.Object, new DistributedLockHealthCheckOptions
        {
            DegradedThresholdMs = 10_000,
            FailureThresholdMs = 30_000
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("release failed");
        result.Data["released"].Should().Be(false);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenResponseExceedsFailureThreshold_ShouldReturnUnhealthy()
    {
        Mock<IDistributedLockFactory> factory = HealthChecksTestHelpers.CreateLockFactoryMock();
        DistributedLockHealthCheck check = CreateCheck(factory.Object, new DistributedLockHealthCheckOptions
        {
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
        Mock<IDistributedLockFactory> factory = HealthChecksTestHelpers.CreateLockFactoryMock();
        DistributedLockHealthCheck check = CreateCheck(factory.Object, new DistributedLockHealthCheckOptions
        {
            DegradedThresholdMs = 0,
            FailureThresholdMs = 30_000
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("is slow");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenTryAcquireThrows_ShouldReturnUnhealthy()
    {
        var lockMock = new Mock<IDistributedLock>();
        lockMock.Setup(l => l.TryAcquireAsync(
                It.IsAny<string>(),
                It.IsAny<DistributedLockOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("network"));

        var factory = new Mock<IDistributedLockFactory>();
        factory.Setup(f => f.Create()).Returns(lockMock.Object);

        DistributedLockHealthCheck check = CreateCheck(factory.Object);

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().BeOfType<TimeoutException>();
        result.Description.Should().Contain("network");
    }

    private static DistributedLockHealthCheck CreateCheck(
        IDistributedLockFactory factory,
        DistributedLockHealthCheckOptions? options = null)
    {
        return new DistributedLockHealthCheck(
            factory,
            options,
            HealthChecksTestHelpers.CreateLogger<DistributedLockHealthCheck>());
    }
}
