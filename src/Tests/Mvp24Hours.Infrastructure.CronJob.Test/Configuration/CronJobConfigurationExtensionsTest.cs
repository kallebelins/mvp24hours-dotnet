using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.CronJob.Configuration;
using Mvp24Hours.Infrastructure.CronJob.Control;
using Mvp24Hours.Infrastructure.CronJob.Dependencies;
using Mvp24Hours.Infrastructure.CronJob.Events;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Observability;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using Mvp24Hours.Infrastructure.CronJob.State;
using Mvp24Hours.Infrastructure.CronJob.Test.Support;
using Mvp24Hours.Infrastructure.CronJob.Test.Support.CronJobs;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Configuration;

[Trait("Category", "Unit")]
public class CronJobConfigurationExtensionsTest
{
    [Fact]
    public void AddCronJobGlobalOptions_ShouldRegisterOptionsAndValidator()
    {
        var services = new ServiceCollection();

        services.AddCronJobGlobalOptions(options => options.DefaultTimeZone = "UTC");

        ServiceProvider provider = services.BuildServiceProvider();
        CronJobGlobalOptions options = provider.GetRequiredService<IOptions<CronJobGlobalOptions>>().Value;

        options.DefaultTimeZone.Should().Be("UTC");
        provider.GetServices<IValidateOptions<CronJobGlobalOptions>>().Should().NotBeEmpty();
    }

    [Fact]
    public void AddCronJobGlobalOptionsFromConfiguration_ShouldBindSection()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = CronJobTestHelpers.CreateConfiguration(new Dictionary<string, string?>
        {
            ["CronJobs:Global:DefaultTimeZone"] = "UTC",
            ["CronJobs:Global:EnableObservability"] = "false"
        });

        services.AddCronJobGlobalOptionsFromConfiguration(configuration);

        CronJobGlobalOptions options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<CronJobGlobalOptions>>().Value;

        options.DefaultTimeZone.Should().Be("UTC");
        options.EnableObservability.Should().BeFalse();
    }

    [Fact]
    public void AddCronJobFromConfiguration_ShouldThrow_WhenSectionMissing()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = CronJobTestHelpers.CreateConfiguration([]);

        Action act = () => services.AddCronJobFromConfiguration<CustomerCronJob>(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Configuration section*CustomerCronJob*not found*");
    }

    [Fact]
    public void AddCronJobWithOptions_ShouldRegisterHostedServiceAndScheduleConfig()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddCronJobWithOptions<CustomerCronJob>(options =>
        {
            options.CronExpression = "0 * * * *";
            options.TimeZone = "UTC";
        });

        ServiceDescriptor[] descriptors = [.. services];
        descriptors.Should().Contain(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(CustomerCronJob));
        descriptors.Should().Contain(d => d.ServiceType == typeof(IScheduleConfig<CustomerCronJob>));
    }

    [Fact]
    public void AddCronJobWithOptions_ShouldDisableSchedule_WhenJobDisabled()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddCronJobWithOptions<CustomerCronJob>(options =>
        {
            options.Enabled = false;
            options.CronExpression = "0 * * * *";
        });

        ServiceProvider provider = services.BuildServiceProvider();
        IScheduleConfig<CustomerCronJob> config = provider.GetRequiredService<IScheduleConfig<CustomerCronJob>>();

        config.CronExpression.Should().BeNull();
    }

    [Fact]
    public void AddResilientCronJobWithOptions_ShouldRegisterResilienceInfrastructure()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddResilientCronJobWithOptions<TestResilientCronJob>(options =>
        {
            options.CronExpression = "0 * * * *";
            options.EnableRetry = true;
        });

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<IResilientScheduleConfig<TestResilientCronJob>>().Should().NotBeNull();
        provider.GetService<ICronJobExecutionLock>().Should().NotBeNull();
        provider.GetService<CronJobCircuitBreaker>().Should().NotBeNull();
        services.Should().Contain(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(TestResilientCronJob));
    }

    [Fact]
    public void AddAdvancedCronJobWithOptions_ShouldRegisterAdvancedInfrastructure()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAdvancedCronJobWithOptions<TestAdvancedCronJob>(options =>
        {
            options.CronExpression = "0 * * * *";
        });

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<ICronJobStateStore>().Should().NotBeNull();
        provider.GetService<ICronJobController>().Should().NotBeNull();
        provider.GetService<ICronJobDependencyTracker>().Should().NotBeNull();
        provider.GetService<ICronJobEventDispatcher>().Should().NotBeNull();
    }

    [Fact]
    public void AddCronJobInstances_ShouldRegisterKeyedOptions()
    {
        var services = new ServiceCollection();

        services.AddCronJobInstances<CustomerCronJob>(
            new CronJobOptions<CustomerCronJob> { InstanceName = "US", CronExpression = "0 0 * * *", TimeZone = "UTC" },
            new CronJobOptions<CustomerCronJob> { InstanceName = "EU", CronExpression = "0 1 * * *", TimeZone = "UTC" });

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetKeyedService<CronJobOptions<CustomerCronJob>>("US")!.CronExpression.Should().Be("0 0 * * *");
        provider.GetKeyedService<IScheduleConfig<CustomerCronJob>>("EU")!.CronExpression.Should().Be("0 1 * * *");
        provider.GetService<ICronJobMetrics>().Should().NotBeNull();
    }

    [Fact]
    public void AddCronJobInstances_ShouldThrow_WhenNoConfigurations()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddCronJobInstances<CustomerCronJob>();

        act.Should().Throw<ArgumentException>().WithMessage("*At least one configuration*");
    }

    [Fact]
    public void AddCronJobInstancesFromConfiguration_ShouldBindInstances()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = CronJobTestHelpers.CreateConfiguration(new Dictionary<string, string?>
        {
            ["CronJobs:CustomerCronJob:Instances:US:CronExpression"] = "0 0 * * *",
            ["CronJobs:CustomerCronJob:Instances:US:TimeZone"] = "UTC",
            ["CronJobs:CustomerCronJob:Instances:EU:CronExpression"] = "0 1 * * *",
            ["CronJobs:CustomerCronJob:Instances:EU:TimeZone"] = "UTC"
        });

        services.AddCronJobInstancesFromConfiguration<CustomerCronJob>(configuration);

        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetKeyedService<CronJobOptions<CustomerCronJob>>("US")!.InstanceName.Should().Be("US");
        provider.GetKeyedService<CronJobOptions<CustomerCronJob>>("EU")!.CronExpression.Should().Be("0 1 * * *");
    }

    [Fact]
    public void ValidateCronJobConfigurations_ShouldReturnTrue_WhenValid()
    {
        var services = new ServiceCollection();
        services.AddCronJobGlobalOptions(options => options.DefaultTimeZone = "UTC");
        ServiceProvider provider = services.BuildServiceProvider();

        provider.ValidateCronJobConfigurations().Should().BeTrue();
    }
}
