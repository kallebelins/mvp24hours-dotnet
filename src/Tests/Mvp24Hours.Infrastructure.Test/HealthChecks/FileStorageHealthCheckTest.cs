//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using Mvp24Hours.Infrastructure.FileStorage.Results;
using Mvp24Hours.Infrastructure.HealthChecks;
using Mvp24Hours.Infrastructure.Test.Support;
using System.Text;

namespace Mvp24Hours.Infrastructure.Test.HealthChecks;

[Trait("Category", "Unit")]
public class FileStorageHealthCheckTest
{
    [Fact]
    public void Constructor_WithNullFileStorage_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new FileStorageHealthCheck(
            null!,
            new FileStorageHealthCheckOptions(),
            HealthChecksTestHelpers.CreateLogger<FileStorageHealthCheck>());

        act.Should().Throw<ArgumentNullException>().WithParameterName("fileStorage");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new FileStorageHealthCheck(
            HealthChecksTestHelpers.CreateInMemoryStorage(),
            new FileStorageHealthCheckOptions(),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldUseDefaults()
    {
        var check = new FileStorageHealthCheck(
            HealthChecksTestHelpers.CreateInMemoryStorage(),
            null,
            HealthChecksTestHelpers.CreateLogger<FileStorageHealthCheck>());

        check.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_WithInMemoryStorage_ShouldReturnHealthy()
    {
        var storage = HealthChecksTestHelpers.CreateInMemoryStorage();
        var options = new FileStorageHealthCheckOptions
        {
            TestFilePath = "health-check/ok.txt",
            TestContent = "ping",
            DegradedThresholdMs = 10_000,
            FailureThresholdMs = 30_000
        };
        var check = new FileStorageHealthCheck(
            storage,
            options,
            HealthChecksTestHelpers.CreateLogger<FileStorageHealthCheck>());

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("healthy");
        result.Data["testFilePath"].Should().Be("health-check/ok.txt");
        result.Data["deleted"].Should().Be(true);
        result.Data["totalOperations"].Should().Be(4);
        (await storage.ExistsAsync("health-check/ok.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenUploadFails_ShouldReturnUnhealthy()
    {
        Mock<IFileStorage> mock = HealthChecksTestHelpers.CreateFileStorageMock(
            upload: FileUploadResult.Failed("disk full"));
        var check = CreateCheck(mock.Object);

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("upload failed");
        result.Data["error"].Should().Be("disk full");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenFileDoesNotExistAfterUpload_ShouldReturnUnhealthy()
    {
        Mock<IFileStorage> mock = HealthChecksTestHelpers.CreateFileStorageMock(exists: false);
        var check = CreateCheck(mock.Object);

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("does not exist");
        mock.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDownloadFails_ShouldReturnUnhealthy()
    {
        Mock<IFileStorage> mock = HealthChecksTestHelpers.CreateFileStorageMock(
            download: FileDownloadResult.Failed("read error"));
        var check = CreateCheck(mock.Object);

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("download failed");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenContentSizeMismatch_ShouldReturnUnhealthy()
    {
        Mock<IFileStorage> mock = HealthChecksTestHelpers.CreateFileStorageMock(
            download: FileDownloadResult.Successful(Encoding.UTF8.GetBytes("x")));
        var check = CreateCheck(mock.Object, new FileStorageHealthCheckOptions
        {
            TestContent = "Health check test content",
            DegradedThresholdMs = 10_000,
            FailureThresholdMs = 30_000
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("size does not match");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenContentBytesMismatch_ShouldReturnUnhealthy()
    {
        byte[] wrong = Encoding.UTF8.GetBytes("Health check test contenX");
        Mock<IFileStorage> mock = HealthChecksTestHelpers.CreateFileStorageMock(
            download: FileDownloadResult.Successful(wrong));
        var check = CreateCheck(mock.Object, new FileStorageHealthCheckOptions
        {
            TestContent = "Health check test content",
            SkipContentVerification = false,
            DegradedThresholdMs = 10_000,
            FailureThresholdMs = 30_000
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("content does not match");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSkipContentVerification_ShouldIgnoreByteMismatch()
    {
        byte[] wrong = Encoding.UTF8.GetBytes("Health check test contenX");
        Mock<IFileStorage> mock = HealthChecksTestHelpers.CreateFileStorageMock(
            download: FileDownloadResult.Successful(wrong));
        var check = CreateCheck(mock.Object, new FileStorageHealthCheckOptions
        {
            TestContent = "Health check test content",
            SkipContentVerification = true,
            DegradedThresholdMs = 10_000,
            FailureThresholdMs = 30_000
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenResponseExceedsFailureThreshold_ShouldReturnUnhealthy()
    {
        Mock<IFileStorage> mock = HealthChecksTestHelpers.CreateFileStorageMock();
        var check = CreateCheck(mock.Object, new FileStorageHealthCheckOptions
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
        Mock<IFileStorage> mock = HealthChecksTestHelpers.CreateFileStorageMock();
        var check = CreateCheck(mock.Object, new FileStorageHealthCheckOptions
        {
            DegradedThresholdMs = 0,
            FailureThresholdMs = 30_000
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("is slow");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStorageThrows_ShouldReturnUnhealthy()
    {
        var mock = new Mock<IFileStorage>();
        mock.Setup(s => s.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        mock.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var check = CreateCheck(mock.Object);

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().BeOfType<InvalidOperationException>();
        result.Description.Should().Contain("boom");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCancelled_ShouldReturnUnhealthy()
    {
        var mock = new Mock<IFileStorage>();
        mock.Setup(s => s.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException());
        mock.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var check = CreateCheck(mock.Object);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        HealthCheckResult result = await check.CheckHealthAsync(
            HealthChecksTestHelpers.CreateContext(),
            cts.Token);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("timed out");
    }

    [Fact]
    public async Task CheckHealthAsync_WithNullTestFilePath_ShouldGenerateUniquePath()
    {
        var storage = HealthChecksTestHelpers.CreateInMemoryStorage();
        var check = CreateCheck(storage, new FileStorageHealthCheckOptions
        {
            TestFilePath = null,
            DegradedThresholdMs = 10_000,
            FailureThresholdMs = 30_000
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["testFilePath"].ToString().Should().StartWith("health-check/");
    }

    private static FileStorageHealthCheck CreateCheck(
        IFileStorage storage,
        FileStorageHealthCheckOptions? options = null)
    {
        return new FileStorageHealthCheck(
            storage,
            options ?? new FileStorageHealthCheckOptions
            {
                TestFilePath = "health-check/test.txt",
                DegradedThresholdMs = 10_000,
                FailureThresholdMs = 30_000
            },
            HealthChecksTestHelpers.CreateLogger<FileStorageHealthCheck>());
    }
}
