using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mvp24Hours.Infrastructure.CronJob.Context;
using Mvp24Hours.Infrastructure.CronJob.Extensions;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using Mvp24Hours.Infrastructure.CronJob.Services;
using Mvp24Hours.Infrastructure.CronJob.Test.Support.CronJobs;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Support;

internal static class CronJobTestHelpers
{
    public static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    public static Mock<IHostApplicationLifetime> CreateHostLifetimeMock()
    {
        var mock = new Mock<IHostApplicationLifetime>();
        mock.SetupGet(x => x.ApplicationStopping).Returns(CancellationToken.None);
        return mock;
    }

    public static ResilientScheduleConfig<T> CreateResilientConfig<T>(
        string? cronExpression = null,
        ICronJobResilienceConfig<T>? resilience = null)
    {
        return new ResilientScheduleConfig<T>
        {
            CronExpression = cronExpression,
            TimeZoneInfo = TimeZoneInfo.Utc,
            Resilience = resilience ?? new CronJobResilienceConfig<T> { PreventOverlapping = false }
        };
    }

    public static ServiceProvider BuildAdvancedJobServices(
        Action<IServiceCollection>? configure = null,
        bool useDistributedLocking = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(CreateHostLifetimeMock().Object);
        services.AddSingleton<ICronJobExecutionLock, InMemoryCronJobExecutionLock>();
        services.AddSingleton<CronJobCircuitBreaker>();
        services.AddCronJobAdvancedInfrastructure(options => options.UseDistributedLocking = useDistributedLocking);

        if (useDistributedLocking)
        {
            services.AddSingleton<IAdvancedCronJobOptions<TestAdvancedCronJob>>(new AdvancedCronJobOptions<TestAdvancedCronJob>
            {
                UseDistributedLocking = true,
                DistributedLockDuration = TimeSpan.FromMinutes(1)
            });
        }

        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    public static TestAdvancedCronJob CreateAdvancedJob(
        IServiceProvider serviceProvider,
        IResilientScheduleConfig<TestAdvancedCronJob>? config = null,
        Func<ICronJobContext, CancellationToken, Task>? execute = null)
    {
        config ??= CreateResilientConfig<TestAdvancedCronJob>(cronExpression: null);

        return new TestAdvancedCronJob(
            config,
            serviceProvider.GetRequiredService<IHostApplicationLifetime>(),
            serviceProvider,
            serviceProvider.GetRequiredService<ICronJobExecutionLock>(),
            serviceProvider.GetRequiredService<CronJobCircuitBreaker>(),
            NullLogger<AdvancedCronJobService<TestAdvancedCronJob>>.Instance,
            execute);
    }

    public static CronJobContext CreateContext(
        string jobName = "TestJob",
        string? cronExpression = "* * * * *",
        int maxAttempts = 1,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        return new CronJobContext(
            jobName,
            cronExpression,
            TimeZoneInfo.Utc,
            cancellationToken,
            executionCount: 1,
            maxAttempts: maxAttempts,
            timeout: timeout);
    }
}
