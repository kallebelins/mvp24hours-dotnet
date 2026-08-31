using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.CronJob.Configuration;
using Mvp24Hours.Infrastructure.CronJob.Test.Support.CronJobs;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Configuration;

/// <summary>
/// Covers upper-bound and otherwise-unexercised branches of
/// <see cref="CronJobOptionsValidator{T}"/> and <see cref="CronJobGlobalOptionsValidator"/>
/// that are not already covered by
/// <see cref="Mvp24Hours.Infrastructure.CronJob.Test.Extensions.CronJobOptionsValidatorTest"/>
/// (which focuses on lower-bound/negative branches).
/// </summary>
[Trait("Category", "Unit")]
public class CronJobOptionsValidatorBoundaryTest
{
    private readonly CronJobOptionsValidator<CustomerCronJob> _validator = new();
    private readonly CronJobGlobalOptionsValidator _globalValidator = new();

    [Fact]
    public void Validate_ShouldFail_WhenMaxRetryAttemptsExceedsMaximum()
    {
        var options = new CronJobOptions<CustomerCronJob>
        {
            EnableRetry = true,
            MaxRetryAttempts = 101
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("MaxRetryAttempts exceeds maximum", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldFail_WhenRetryDelayIsNegative()
    {
        var options = new CronJobOptions<CustomerCronJob>
        {
            EnableRetry = true,
            RetryDelay = TimeSpan.FromSeconds(-1)
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("RetryDelay cannot be negative", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldFail_WhenRetryDelayExceedsOneHour()
    {
        var options = new CronJobOptions<CustomerCronJob>
        {
            EnableRetry = true,
            RetryDelay = TimeSpan.FromHours(2)
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("RetryDelay exceeds maximum", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldFail_WhenCircuitBreakerFailureThresholdBelowMinimum()
    {
        var options = new CronJobOptions<CustomerCronJob>
        {
            EnableCircuitBreaker = true,
            CircuitBreakerFailureThreshold = 0
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("CircuitBreakerFailureThreshold must be at least", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldFail_WhenCircuitBreakerFailureThresholdExceedsMaximum()
    {
        var options = new CronJobOptions<CustomerCronJob>
        {
            EnableCircuitBreaker = true,
            CircuitBreakerFailureThreshold = 1001
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("CircuitBreakerFailureThreshold exceeds maximum", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldFail_WhenCircuitBreakerBreakDurationExceedsTwentyFourHours()
    {
        var options = new CronJobOptions<CustomerCronJob>
        {
            EnableCircuitBreaker = true,
            CircuitBreakerBreakDuration = TimeSpan.FromHours(25)
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("CircuitBreakerBreakDuration exceeds maximum", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldFail_WhenGracefulShutdownTimeoutExceedsThirtyMinutes()
    {
        var options = new CronJobOptions<CustomerCronJob>
        {
            GracefulShutdownTimeout = TimeSpan.FromMinutes(31)
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("GracefulShutdownTimeout exceeds maximum", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldFail_WhenDistributedLockExpiryExceedsTwentyFourHours()
    {
        var options = new CronJobOptions<CustomerCronJob>
        {
            EnableDistributedLocking = true,
            DistributedLockExpiry = TimeSpan.FromHours(25)
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("DistributedLockExpiry exceeds maximum", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldFail_WhenInstanceNameExceedsMaximumLength()
    {
        var options = new CronJobOptions<CustomerCronJob>
        {
            InstanceName = new string('a', 101)
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("InstanceName exceeds maximum length", StringComparison.Ordinal));
    }

    [Fact]
    public void GlobalValidate_ShouldFail_WhenDefaultMaxRetryAttemptsExceedsMaximum()
    {
        var options = new CronJobGlobalOptions
        {
            DefaultMaxRetryAttempts = 101
        };

        ValidateOptionsResult result = _globalValidator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("DefaultMaxRetryAttempts", StringComparison.Ordinal));
    }

    [Fact]
    public void GlobalValidate_ShouldFail_WhenDefaultRetryDelayOutOfRange()
    {
        var options = new CronJobGlobalOptions
        {
            DefaultRetryDelay = TimeSpan.FromHours(2)
        };

        ValidateOptionsResult result = _globalValidator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("DefaultRetryDelay", StringComparison.Ordinal));
    }

    [Fact]
    public void GlobalValidate_ShouldFail_WhenDefaultCircuitBreakerFailureThresholdOutOfRange()
    {
        var options = new CronJobGlobalOptions
        {
            DefaultCircuitBreakerFailureThreshold = 1001
        };

        ValidateOptionsResult result = _globalValidator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("DefaultCircuitBreakerFailureThreshold", StringComparison.Ordinal));
    }

    [Fact]
    public void GlobalValidate_ShouldFail_WhenDefaultCircuitBreakerBreakDurationOutOfRange()
    {
        var options = new CronJobGlobalOptions
        {
            DefaultCircuitBreakerBreakDuration = TimeSpan.FromHours(25)
        };

        ValidateOptionsResult result = _globalValidator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("DefaultCircuitBreakerBreakDuration", StringComparison.Ordinal));
    }

    [Fact]
    public void GlobalValidate_ShouldFail_WhenDefaultGracefulShutdownTimeoutOutOfRange()
    {
        var options = new CronJobGlobalOptions
        {
            DefaultGracefulShutdownTimeout = TimeSpan.FromMinutes(31)
        };

        ValidateOptionsResult result = _globalValidator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("DefaultGracefulShutdownTimeout", StringComparison.Ordinal));
    }

    [Fact]
    public void GlobalValidate_ShouldFail_WhenDefaultDistributedLockExpiryOutOfRange()
    {
        var options = new CronJobGlobalOptions
        {
            DefaultDistributedLockExpiry = TimeSpan.FromSeconds(1)
        };

        ValidateOptionsResult result = _globalValidator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("DefaultDistributedLockExpiry", StringComparison.Ordinal));
    }
}
