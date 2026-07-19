//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Http.Resilience;

namespace Mvp24Hours.Infrastructure.Test.Http.Resilience;

[Trait("Category", "Unit")]
public class NativeResilienceOptionsTest
{
    [Fact]
    public void Default_ShouldEnableAllStrategies()
    {
        var options = new NativeResilienceOptions();

        options.EnableRetry.Should().BeTrue();
        options.EnableCircuitBreaker.Should().BeTrue();
        options.EnableAttemptTimeout.Should().BeTrue();
        options.EnableTotalTimeout.Should().BeTrue();
        options.MaxRetryAttempts.Should().Be(3);
        options.UseJitter.Should().BeTrue();
        options.TotalRequestTimeout.Should().Be(TimeSpan.FromSeconds(30));
        options.AttemptTimeout.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void HighAvailability_ShouldUseAggressiveRetries()
    {
        NativeResilienceOptions options = NativeResilienceOptions.HighAvailability;

        options.MaxRetryAttempts.Should().Be(5);
        options.TotalRequestTimeout.Should().Be(TimeSpan.FromMinutes(2));
        options.CircuitBreakerFailureRatio.Should().Be(0.25);
        options.EnableRetry.Should().BeTrue();
    }

    [Fact]
    public void LowLatency_ShouldUseFewerRetriesAndShorterTimeouts()
    {
        NativeResilienceOptions options = NativeResilienceOptions.LowLatency;

        options.MaxRetryAttempts.Should().Be(2);
        options.TotalRequestTimeout.Should().Be(TimeSpan.FromSeconds(10));
        options.AttemptTimeout.Should().Be(TimeSpan.FromSeconds(3));
        options.RetryDelay.Should().Be(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void BatchProcessing_ShouldTolerateLongerFailures()
    {
        NativeResilienceOptions options = NativeResilienceOptions.BatchProcessing;

        options.MaxRetryAttempts.Should().Be(10);
        options.TotalRequestTimeout.Should().Be(TimeSpan.FromMinutes(5));
        options.CircuitBreakerFailureRatio.Should().Be(0.5);
        options.CircuitBreakerBreakDuration.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void Disabled_ShouldTurnOffAllStrategies()
    {
        NativeResilienceOptions options = NativeResilienceOptions.Disabled;

        options.EnableRetry.Should().BeFalse();
        options.EnableCircuitBreaker.Should().BeFalse();
        options.EnableAttemptTimeout.Should().BeFalse();
        options.EnableTotalTimeout.Should().BeFalse();
    }
}
