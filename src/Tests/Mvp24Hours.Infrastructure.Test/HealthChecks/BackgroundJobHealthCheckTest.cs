//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Mvp24Hours.Infrastructure.BackgroundJobs.Contract;
using Mvp24Hours.Infrastructure.HealthChecks;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.HealthChecks;

[Trait("Category", "Unit")]
public class BackgroundJobHealthCheckTest
{
    [Fact]
    public void Constructor_WithNullJobScheduler_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new BackgroundJobHealthCheck(
            null!,
            new BackgroundJobHealthCheckOptions(),
            HealthChecksTestHelpers.CreateLogger<BackgroundJobHealthCheck>());

        act.Should().Throw<ArgumentNullException>().WithParameterName("jobScheduler");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new BackgroundJobHealthCheck(
            new Mock<IJobScheduler>().Object,
            new BackgroundJobHealthCheckOptions(),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldUseDefaults()
    {
        var check = new BackgroundJobHealthCheck(
            new Mock<IJobScheduler>().Object,
            null,
            HealthChecksTestHelpers.CreateLogger<BackgroundJobHealthCheck>());

        check.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenScheduleTestJobDisabled_ShouldReturnHealthy()
    {
        var scheduler = new Mock<IJobScheduler>();
        var check = new BackgroundJobHealthCheck(
            scheduler.Object,
            new BackgroundJobHealthCheckOptions { ScheduleTestJob = false },
            HealthChecksTestHelpers.CreateLogger<BackgroundJobHealthCheck>());

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("test job scheduling disabled");
        result.Data["testJobScheduled"].Should().Be(false);
        result.Data.Should().ContainKey("note");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenScheduleTestJobEnabled_ShouldReturnHealthyWithoutScheduling()
    {
        // Current implementation skips actual scheduling even when ScheduleTestJob is true
        var scheduler = new Mock<IJobScheduler>();
        var check = new BackgroundJobHealthCheck(
            scheduler.Object,
            new BackgroundJobHealthCheckOptions { ScheduleTestJob = true },
            HealthChecksTestHelpers.CreateLogger<BackgroundJobHealthCheck>());

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["testJobScheduled"].Should().Be(false);
        result.Description.Should().Contain("available");
    }
}
