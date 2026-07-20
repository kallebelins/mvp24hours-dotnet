using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.CronJob.Configuration;
using Mvp24Hours.Infrastructure.CronJob.Events;
using Mvp24Hours.Infrastructure.CronJob.Extensions;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Observability;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using Mvp24Hours.Infrastructure.CronJob.Test.Support;
using Mvp24Hours.Infrastructure.CronJob.Test.Support.CronJobs;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Extensions;

/// <summary>
/// DI edge cases and options validation for CronJob registration.
/// </summary>
[Trait("Category", "Unit")]
public class CronJobExtensionsAdvancedTest
{
    [Fact]
    public void AddCronJob_WithCronAndTimeZone_ShouldBindTimezone()
    {
        var services = new ServiceCollection();

        services.AddCronJob<CustomerCronJob>("0 9 * * *", TimeZoneInfo.Utc);

        IScheduleConfig<CustomerCronJob> config = services.BuildServiceProvider()
            .GetRequiredService<IScheduleConfig<CustomerCronJob>>();

        config.CronExpression.Should().Be("0 9 * * *");
        config.TimeZoneInfo.Should().Be(TimeZoneInfo.Utc);
    }

    [Fact]
    public void AddResilientCronJob_WithCronExpression_ShouldUseLocalTimeZone()
    {
        var services = new ServiceCollection();

        services.AddResilientCronJob<TestResilientCronJob>("*/10 * * * *");

        IResilientScheduleConfig<TestResilientCronJob> config = services.BuildServiceProvider()
            .GetRequiredService<IResilientScheduleConfig<TestResilientCronJob>>();

        config.CronExpression.Should().Be("*/10 * * * *");
        config.TimeZoneInfo.Should().Be(TimeZoneInfo.Local);
    }

    [Fact]
    public void AddCronJob_ShouldThrow_WhenServicesNull()
    {
        Action act = () => ScheduledServiceExtensions.AddCronJob<CustomerCronJob>(null!, "0 * * * *");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCronJob_ShouldThrow_WhenOptionsNull()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddCronJob<CustomerCronJob>((Action<IScheduleConfig<CustomerCronJob>>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCronJobResilienceInfrastructure_ShouldSkipObservability_WhenDisabled()
    {
        var services = new ServiceCollection();

        services.AddCronJobResilienceInfrastructure(enableObservability: false);

        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetService<ICronJobExecutionLock>().Should().NotBeNull();
        provider.GetService<CronJobCircuitBreaker>().Should().NotBeNull();
        provider.GetService<ICronJobMetrics>().Should().BeNull();
    }

    [Fact]
    public void AddCronJobEventHandler_ShouldRegisterOnlyImplementedInterfaces()
    {
        var services = new ServiceCollection();

        services.AddCronJobEventHandler<FailingCronJobEventHandler>();

        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetServices<ICronJobStartingHandler>().Should().ContainSingle();
        provider.GetServices<ICronJobFailedHandler>().Should().BeEmpty();
        provider.GetServices<ICronJobCompletedHandler>().Should().BeEmpty();
    }

    [Fact]
    public void CronJobOptions_GetSectionPath_AndToString_ShouldReflectType()
    {
        var options = new CronJobOptions<CustomerCronJob>
        {
            CronExpression = "0 * * * *",
            TimeZone = "UTC",
            InstanceName = "cust-1"
        };

        CronJobOptions<CustomerCronJob>.GetSectionPath().Should().Be("CronJobs:CustomerCronJob");
        options.GetEffectiveInstanceName().Should().Be("cust-1");
        options.ToString().Should().Contain("cust-1").And.Contain("0 * * * *");
    }

    [Fact]
    public void CronJobGlobalOptions_CreateWithDefaults_ShouldCopyResilienceFlags()
    {
        var global = new CronJobGlobalOptions
        {
            DefaultTimeZone = "UTC",
            EnableRetryByDefault = true,
            DefaultMaxRetryAttempts = 7,
            EnableCircuitBreakerByDefault = true,
            DefaultCircuitBreakerFailureThreshold = 4
        };

        CronJobOptions<CustomerCronJob> options = global.CreateWithDefaults<CustomerCronJob>();

        options.TimeZone.Should().Be("UTC");
        options.EnableRetry.Should().BeTrue();
        options.MaxRetryAttempts.Should().Be(7);
        options.EnableCircuitBreaker.Should().BeTrue();
        options.CircuitBreakerFailureThreshold.Should().Be(4);
    }

    [Fact]
    public void CronJobGlobalOptions_ApplyDefaultsTo_ShouldFillMissingTimezone()
    {
        var global = new CronJobGlobalOptions { DefaultTimeZone = "UTC" };
        var options = new CronJobOptions<CustomerCronJob>();

        global.ApplyDefaultsTo(options);

        options.TimeZone.Should().Be("UTC");
    }
}

[Trait("Category", "Unit")]
public class CronJobOptionsValidatorTest
{
    private readonly CronJobOptionsValidator<CustomerCronJob> _validator = new();
    private readonly CronJobGlobalOptionsValidator _globalValidator = new();

    [Fact]
    public void Validate_ShouldSucceed_ForValidOptions()
    {
        var options = new CronJobOptions<CustomerCronJob>
        {
            CronExpression = "*/5 * * * *",
            TimeZone = "UTC"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_ForInvalidCronExpression()
    {
        var options = new CronJobOptions<CustomerCronJob>
        {
            CronExpression = "not-a-cron"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Invalid CRON", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ShouldFail_ForInvalidTimeZone()
    {
        var options = new CronJobOptions<CustomerCronJob>
        {
            TimeZone = "Not/A/Real/Zone"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("TimeZone", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ShouldFail_WhenMaxRetryAttemptsInvalid_WithRetryEnabled()
    {
        var options = new CronJobOptions<CustomerCronJob>
        {
            EnableRetry = true,
            MaxRetryAttempts = 0
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("MaxRetryAttempts", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldFail_WhenCircuitBreakerBreakDurationTooShort()
    {
        var options = new CronJobOptions<CustomerCronJob>
        {
            EnableCircuitBreaker = true,
            CircuitBreakerBreakDuration = TimeSpan.FromMilliseconds(100)
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("CircuitBreakerBreakDuration", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldFail_WhenDistributedLockExpiryTooShort()
    {
        var options = new CronJobOptions<CustomerCronJob>
        {
            EnableDistributedLocking = true,
            DistributedLockExpiry = TimeSpan.FromSeconds(1)
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("DistributedLockExpiry", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldFail_WhenInstanceNameHasInvalidCharacters()
    {
        var options = new CronJobOptions<CustomerCronJob>
        {
            InstanceName = "bad name!"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("InstanceName", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldFail_WhenGracefulShutdownTimeoutNegative()
    {
        var options = new CronJobOptions<CustomerCronJob>
        {
            GracefulShutdownTimeout = TimeSpan.FromSeconds(-1)
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("GracefulShutdownTimeout", StringComparison.Ordinal));
    }

    [Fact]
    public void GlobalValidate_ShouldFail_WhenAggregateHealthCheckNameEmpty()
    {
        var options = new CronJobGlobalOptions
        {
            AggregateHealthCheckName = " "
        };

        ValidateOptionsResult result = _globalValidator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("AggregateHealthCheckName", StringComparison.Ordinal));
    }

    [Fact]
    public void GlobalValidate_ShouldFail_WhenDefaultRetryAttemptsOutOfRange()
    {
        var options = new CronJobGlobalOptions
        {
            DefaultMaxRetryAttempts = 0
        };

        ValidateOptionsResult result = _globalValidator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("DefaultMaxRetryAttempts", StringComparison.Ordinal));
    }

    [Fact]
    public void GlobalValidate_ShouldSucceed_ForDefaults()
    {
        ValidateOptionsResult result = _globalValidator.Validate(null, new CronJobGlobalOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void GlobalValidate_ShouldFail_ForInvalidDefaultTimeZone()
    {
        var options = new CronJobGlobalOptions
        {
            DefaultTimeZone = "Invalid/Zone"
        };

        ValidateOptionsResult result = _globalValidator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("DefaultTimeZone", StringComparison.OrdinalIgnoreCase));
    }
}
