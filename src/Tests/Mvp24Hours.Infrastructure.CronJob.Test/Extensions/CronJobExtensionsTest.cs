using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.CronJob.Context;
using Mvp24Hours.Infrastructure.CronJob.Control;
using Mvp24Hours.Infrastructure.CronJob.Dependencies;
using Mvp24Hours.Infrastructure.CronJob.Events;
using Mvp24Hours.Infrastructure.CronJob.Extensions;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Observability;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using Mvp24Hours.Infrastructure.CronJob.State;
using Mvp24Hours.Infrastructure.CronJob.Test.Support;
using Mvp24Hours.Infrastructure.CronJob.Test.Support.CronJobs;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Extensions;

[Trait("Category", "Unit")]
public class ScheduledServiceExtensionsTest
{
    [Fact]
    public void AddCronJob_ShouldRegisterScheduleConfigAndHostedService()
    {
        var services = new ServiceCollection();

        services.AddCronJob<CustomerCronJob>(config =>
        {
            config.CronExpression = "0 * * * *";
            config.TimeZoneInfo = TimeZoneInfo.Utc;
        });

        ServiceDescriptor[] descriptors = [.. services];
        descriptors.Should().Contain(d => d.ServiceType == typeof(IScheduleConfig<CustomerCronJob>));
        descriptors.Should().Contain(d => d.ImplementationType == typeof(CustomerCronJob));
    }

    [Fact]
    public void AddCronJob_WithCronExpression_ShouldUseLocalTimeZone()
    {
        var services = new ServiceCollection();

        services.AddCronJob<CustomerCronJob>("*/15 * * * *");

        IScheduleConfig<CustomerCronJob> config = services.BuildServiceProvider()
            .GetRequiredService<IScheduleConfig<CustomerCronJob>>();

        config.CronExpression.Should().Be("*/15 * * * *");
        config.TimeZoneInfo.Should().Be(TimeZoneInfo.Local);
    }

    [Fact]
    public void AddCronJobRunOnce_ShouldRegisterJobWithoutCronExpression()
    {
        var services = new ServiceCollection();

        services.AddCronJobRunOnce<CustomerCronJob>();

        IScheduleConfig<CustomerCronJob> config = services.BuildServiceProvider()
            .GetRequiredService<IScheduleConfig<CustomerCronJob>>();

        config.CronExpression.Should().BeNull();
    }

    [Fact]
    public void AddAdvancedCronJob_ShouldRegisterResilienceAndInfrastructure()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAdvancedCronJob<TestAdvancedCronJob>(config =>
        {
            config.CronExpression = "0 * * * *";
            config.Resilience.PreventOverlapping = false;
        });

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<IResilientScheduleConfig<TestAdvancedCronJob>>().Should().NotBeNull();
        provider.GetService<ICronJobExecutionLock>().Should().NotBeNull();
        provider.GetService<ICronJobStateStore>().Should().NotBeNull();
        provider.GetService<ICronJobController>().Should().NotBeNull();
    }

    [Fact]
    public void AddAdvancedCronJobWithFullFeatures_ShouldApplyFullResilience()
    {
        var services = new ServiceCollection();

        services.AddAdvancedCronJobWithFullFeatures<TestAdvancedCronJob>("0 0 * * *", TimeZoneInfo.Utc);

        IResilientScheduleConfig<TestAdvancedCronJob> config = services.BuildServiceProvider()
            .GetRequiredService<IResilientScheduleConfig<TestAdvancedCronJob>>();

        config.CronExpression.Should().Be("0 0 * * *");
        config.TimeZoneInfo.Should().Be(TimeZoneInfo.Utc);
        config.Resilience.EnableRetry.Should().BeTrue();
        config.Resilience.EnableCircuitBreaker.Should().BeTrue();
        config.Resilience.PreventOverlapping.Should().BeTrue();
    }

    [Fact]
    public void AddResilientCronJob_ShouldRegisterResilienceInfrastructure()
    {
        var services = new ServiceCollection();

        services.AddResilientCronJob<TestResilientCronJob>(config =>
        {
            config.CronExpression = "0 * * * *";
            config.Resilience.EnableRetry = true;
        });

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<IResilientScheduleConfig<TestResilientCronJob>>().Should().NotBeNull();
        provider.GetService<ICronJobExecutionLock>().Should().NotBeNull();
        provider.GetService<CronJobCircuitBreaker>().Should().NotBeNull();
    }

    [Fact]
    public void AddResilientCronJobWithFullResilience_ShouldEnableAllFeatures()
    {
        var services = new ServiceCollection();

        services.AddResilientCronJobWithFullResilience<TestResilientCronJob>("0 * * * *", TimeZoneInfo.Utc);

        ICronJobResilienceConfig<TestResilientCronJob> resilience = services.BuildServiceProvider()
            .GetRequiredService<IResilientScheduleConfig<TestResilientCronJob>>().Resilience;

        resilience.EnableRetry.Should().BeTrue();
        resilience.EnableCircuitBreaker.Should().BeTrue();
        resilience.PreventOverlapping.Should().BeTrue();
    }

    [Fact]
    public void AddResilientCronJobWithRetry_ShouldConfigureRetryPolicy()
    {
        var services = new ServiceCollection();

        services.AddResilientCronJobWithRetry<TestResilientCronJob>("0 * * * *", maxRetryAttempts: 5, useExponentialBackoff: false);

        ICronJobResilienceConfig<TestResilientCronJob> resilience = services.BuildServiceProvider()
            .GetRequiredService<IResilientScheduleConfig<TestResilientCronJob>>().Resilience;

        resilience.EnableRetry.Should().BeTrue();
        resilience.MaxRetryAttempts.Should().Be(5);
        resilience.UseExponentialBackoff.Should().BeFalse();
    }

    [Fact]
    public void AddResilientCronJobWithCircuitBreaker_ShouldConfigureBreaker()
    {
        var services = new ServiceCollection();

        services.AddResilientCronJobWithCircuitBreaker<TestResilientCronJob>(
            "0 * * * *",
            failureThreshold: 3,
            breakDuration: TimeSpan.FromMinutes(2));

        ICronJobResilienceConfig<TestResilientCronJob> resilience = services.BuildServiceProvider()
            .GetRequiredService<IResilientScheduleConfig<TestResilientCronJob>>().Resilience;

        resilience.EnableCircuitBreaker.Should().BeTrue();
        resilience.CircuitBreakerFailureThreshold.Should().Be(3);
        resilience.CircuitBreakerDuration.Should().Be(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void AddCronJobResilienceInfrastructure_ShouldRegisterCoreServices()
    {
        var services = new ServiceCollection();

        services.AddCronJobResilienceInfrastructure(enableObservability: true);

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<ICronJobExecutionLock>().Should().NotBeNull();
        provider.GetService<CronJobCircuitBreaker>().Should().NotBeNull();
        provider.GetService<ICronJobMetrics>().Should().NotBeNull();
    }

    [Fact]
    public void AddCronJobResilienceInfrastructureTLock_ShouldUseCustomLock()
    {
        var services = new ServiceCollection();

        services.AddCronJobResilienceInfrastructure<InMemoryCronJobExecutionLock>();

        services.BuildServiceProvider().GetService<ICronJobExecutionLock>()
            .Should().BeOfType<InMemoryCronJobExecutionLock>();
    }
}

[Trait("Category", "Unit")]
public class CronJobAdvancedExtensionsTest
{
    [Fact]
    public void AddCronJobAdvancedInfrastructure_ShouldRegisterDefaultServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<CronJobCircuitBreaker>();

        services.AddCronJobAdvancedInfrastructure();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<ICronJobContextAccessor>().Should().NotBeNull();
        provider.GetService<ICronJobStateStore>().Should().NotBeNull();
        provider.GetService<ICronJobController>().Should().NotBeNull();
        provider.GetService<ICronJobDependencyTracker>().Should().NotBeNull();
        provider.GetService<ICronJobEventDispatcher>().Should().NotBeNull();
    }

    [Fact]
    public void AddCronJobAdvancedInfrastructure_ShouldRespectOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddCronJobAdvancedInfrastructure(options =>
        {
            options.UseStatePersistence = false;
            options.UseController = false;
            options.UseDependencies = false;
            options.UseEventHandlers = false;
            options.UseDistributedLocking = true;
        });

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<ICronJobStateStore>().Should().BeNull();
        provider.GetService<ICronJobController>().Should().BeNull();
        provider.GetService<ICronJobDependencyTracker>().Should().BeNull();
        provider.GetService<ICronJobEventDispatcher>().Should().BeNull();
        provider.GetService<IDistributedCronJobLock>().Should().NotBeNull();
    }

    [Fact]
    public void AddCronJobStateStore_ShouldRegisterCustomStore()
    {
        var services = new ServiceCollection();
        services.AddCronJobStateStore<InMemoryCronJobStateStore>();

        services.BuildServiceProvider().GetService<ICronJobStateStore>()
            .Should().BeOfType<InMemoryCronJobStateStore>();
    }

    [Fact]
    public void AddCronJobDistributedLock_ShouldRegisterCustomLock()
    {
        var services = new ServiceCollection();
        services.AddCronJobDistributedLock<InMemoryDistributedCronJobLock>();

        services.BuildServiceProvider().GetService<IDistributedCronJobLock>()
            .Should().BeOfType<InMemoryDistributedCronJobLock>();
    }

    [Fact]
    public void AddCronJobEventHandler_ShouldRegisterHandlerInterfaces()
    {
        var services = new ServiceCollection();
        services.AddSingleton<RecordingCronJobEventHandler>();
        services.AddCronJobEventHandler<RecordingCronJobEventHandler>();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetServices<ICronJobStartingHandler>().Should().NotBeEmpty();
        provider.GetServices<ICronJobCompletedHandler>().Should().NotBeEmpty();
        provider.GetServices<ICronJobSkippedHandler>().Should().NotBeEmpty();
    }

    [Fact]
    public void AddCronJobDependency_ShouldRegisterWithTracker()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICronJobDependencyTracker, InMemoryCronJobDependencyTracker>();
        ICronJobDependency dependency = CronJobDependency.For("ReportJob").DependsOn("DataJob").Build();

        services.AddCronJobDependency(dependency);

        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<ICronJobDependency>();
        provider.GetRequiredService<ICronJobDependencyTracker>().GetDependencies("ReportJob").Should().NotBeEmpty();
    }

    [Fact]
    public void AddCronJobDependencyT_ShouldConfigureViaBuilder()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICronJobDependencyTracker, InMemoryCronJobDependencyTracker>();

        services.AddCronJobDependency<ReportJobMarker>(builder =>
            builder.DependsOn<DataJobMarker>().WithSuccessRequired().WithMaxAge(TimeSpan.FromHours(1)));

        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<ICronJobDependency>();
        ICronJobDependency dependency = provider.GetRequiredService<ICronJobDependencyTracker>()
            .GetDependencies(nameof(ReportJobMarker)).Single();

        dependency.RequiredJobNames.Should().Contain(nameof(DataJobMarker));
        dependency.RequireSuccess.Should().BeTrue();
        dependency.MaxAge.Should().Be(TimeSpan.FromHours(1));
    }

    private sealed class ReportJobMarker;
    private sealed class DataJobMarker;
}
