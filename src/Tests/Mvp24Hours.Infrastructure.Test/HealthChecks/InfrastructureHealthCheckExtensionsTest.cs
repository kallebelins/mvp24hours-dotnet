//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Mvp24Hours.Infrastructure.BackgroundJobs.Contract;
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using Mvp24Hours.Infrastructure.HealthChecks;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.HealthChecks;

[Trait("Category", "Unit")]
public class InfrastructureHealthCheckExtensionsTest
{
    [Fact]
    public void AddFileStorageHealthCheck_ShouldRegisterAndResolveHealthy()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IFileStorage>(HealthChecksTestHelpers.CreateInMemoryStorage());
        services.AddHealthChecks()
            .AddFileStorageHealthCheck("fs", o =>
            {
                o.TestFilePath = "hc/ext.txt";
                o.TestContent = "ok";
                o.DegradedThresholdMs = 10_000;
                o.FailureThresholdMs = 30_000;
            });

        using ServiceProvider sp = services.BuildServiceProvider();
        HealthCheckService health = sp.GetRequiredService<HealthCheckService>();

        HealthReport report = health.CheckHealthAsync().GetAwaiter().GetResult();

        report.Status.Should().Be(HealthStatus.Healthy);
        report.Entries.Should().ContainKey("fs");
        report.Entries["fs"].Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void AddEmailServiceHealthCheck_ShouldRegisterWithoutSending()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(HealthChecksTestHelpers.CreateEmailServiceMock().Object);
        services.AddHealthChecks()
            .AddEmailServiceHealthCheck("email", o => o.SendTestEmail = false);

        using ServiceProvider sp = services.BuildServiceProvider();
        HealthReport report = sp.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync().GetAwaiter().GetResult();

        report.Entries.Should().ContainKey("email");
        report.Entries["email"].Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void AddSmsServiceHealthCheck_ShouldRegisterWithoutSending()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(HealthChecksTestHelpers.CreateSmsServiceMock().Object);
        services.AddHealthChecks()
            .AddSmsServiceHealthCheck("sms", o => o.SendTestSms = false);

        using ServiceProvider sp = services.BuildServiceProvider();
        HealthReport report = sp.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync().GetAwaiter().GetResult();

        report.Entries.Should().ContainKey("sms");
        report.Entries["sms"].Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void AddDistributedLockHealthCheck_ShouldRegisterAndResolveHealthy()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(HealthChecksTestHelpers.CreateLockFactoryMock().Object);
        services.AddHealthChecks()
            .AddDistributedLockHealthCheck("locks", o =>
            {
                o.DegradedThresholdMs = 10_000;
                o.FailureThresholdMs = 30_000;
            });

        using ServiceProvider sp = services.BuildServiceProvider();
        HealthReport report = sp.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync().GetAwaiter().GetResult();

        report.Entries.Should().ContainKey("locks");
        report.Entries["locks"].Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void AddBackgroundJobHealthCheck_ShouldRegisterAndResolveHealthy()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new Mock<IJobScheduler>().Object);
        services.AddHealthChecks()
            .AddBackgroundJobHealthCheck("jobs");

        using ServiceProvider sp = services.BuildServiceProvider();
        HealthReport report = sp.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync().GetAwaiter().GetResult();

        report.Entries.Should().ContainKey("jobs");
        report.Entries["jobs"].Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void AddHttpClientHealthCheck_ShouldRegisterAndResolveHealthy()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(HealthChecksTestHelpers.CreateTypedHttpClientMock<HealthCheckTestApi>().Object);
        services.AddHealthChecks()
            .AddHttpClientHealthCheck<HealthCheckTestApi>("http", o =>
            {
                o.DegradedThresholdMs = 10_000;
                o.FailureThresholdMs = 30_000;
            });

        using ServiceProvider sp = services.BuildServiceProvider();
        HealthReport report = sp.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync().GetAwaiter().GetResult();

        report.Entries.Should().ContainKey("http");
        report.Entries["http"].Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void AddHttpClientHealthCheck_WithDefaultName_ShouldUseTypeName()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(HealthChecksTestHelpers.CreateTypedHttpClientMock<HealthCheckTestApi>().Object);
        services.AddHealthChecks()
            .AddHttpClientHealthCheck<HealthCheckTestApi>(configureOptions: o =>
            {
                o.DegradedThresholdMs = 10_000;
                o.FailureThresholdMs = 30_000;
            });

        using ServiceProvider sp = services.BuildServiceProvider();
        HealthReport report = sp.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync().GetAwaiter().GetResult();

        report.Entries.Should().ContainKey("httpclient-HealthCheckTestApi");
    }

    [Fact]
    public void AddInfrastructureHealthChecks_WithNoServices_ShouldNotRegisterEntries()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks().AddInfrastructureHealthChecks();

        using ServiceProvider sp = services.BuildServiceProvider();
        HealthReport report = sp.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync().GetAwaiter().GetResult();

        report.Entries.Should().BeEmpty();
    }

    [Fact]
    public void AddInfrastructureHealthChecks_WithRegisteredServices_ShouldAddMatchingChecks()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IFileStorage>(HealthChecksTestHelpers.CreateInMemoryStorage());
        services.AddSingleton(HealthChecksTestHelpers.CreateEmailServiceMock().Object);
        services.AddSingleton(HealthChecksTestHelpers.CreateSmsServiceMock().Object);
        services.AddSingleton(HealthChecksTestHelpers.CreateLockFactoryMock().Object);
        services.AddSingleton(new Mock<IJobScheduler>().Object);

        services.AddHealthChecks().AddInfrastructureHealthChecks(o =>
        {
            o.FileStorage.TestFilePath = "hc/all.txt";
            o.FileStorage.DegradedThresholdMs = 10_000;
            o.FileStorage.FailureThresholdMs = 30_000;
            o.Email.SendTestEmail = false;
            o.Sms.SendTestSms = false;
            o.DistributedLock.DegradedThresholdMs = 10_000;
            o.DistributedLock.FailureThresholdMs = 30_000;
            o.BackgroundJobs.ScheduleTestJob = false;
        });

        using ServiceProvider sp = services.BuildServiceProvider();
        HealthReport report = sp.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync().GetAwaiter().GetResult();

        report.Entries.Keys.Should().BeEquivalentTo(
            "distributed-lock", "file-storage", "email-service", "sms-service", "background-jobs");
        report.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void AddInfrastructureHealthChecks_WithPartialServices_ShouldOnlyAddRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(HealthChecksTestHelpers.CreateEmailServiceMock().Object);

        services.AddHealthChecks().AddInfrastructureHealthChecks(o => o.Email.SendTestEmail = false);

        using ServiceProvider sp = services.BuildServiceProvider();
        HealthReport report = sp.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync().GetAwaiter().GetResult();

        report.Entries.Keys.Should().BeEquivalentTo("email-service");
    }
}
