//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Resilience.Native;

namespace Mvp24Hours.Infrastructure.Test.Resilience.Native;

[Trait("Category", "Unit")]
public class NativeResilienceOptionsTest
{
    [Fact]
    public void Default_ShouldEnableCoreStrategies()
    {
        var options = new NativeResilienceOptions();

        options.Name.Should().Be("Mvp24Hours-Resilience");
        options.EnableRetry.Should().BeTrue();
        options.RetryMaxAttempts.Should().Be(3);
        options.RetryBackoffType.Should().Be(ResilienceBackoffType.Exponential);
        options.RetryUseJitter.Should().BeTrue();
        options.EnableCircuitBreaker.Should().BeTrue();
        options.CircuitBreakerFailureRatio.Should().Be(0.5);
        options.CircuitBreakerMinimumThroughput.Should().Be(10);
        options.EnableTimeout.Should().BeTrue();
        options.TimeoutDuration.Should().Be(TimeSpan.FromSeconds(30));
        options.EnableRateLimiting.Should().BeFalse();
        options.EnableTelemetry.Should().BeTrue();
    }

    [Fact]
    public void HighAvailability_ShouldUseAggressiveSettings()
    {
        NativeResilienceOptions options = NativeResilienceOptions.HighAvailability;

        options.Name.Should().Be("Mvp24Hours-HighAvailability");
        options.RetryMaxAttempts.Should().Be(5);
        options.TimeoutDuration.Should().Be(TimeSpan.FromSeconds(60));
        options.CircuitBreakerBreakDuration.Should().Be(TimeSpan.FromSeconds(60));
        options.EnableRetry.Should().BeTrue();
        options.EnableCircuitBreaker.Should().BeTrue();
    }

    [Fact]
    public void LowLatency_ShouldUseFewerRetriesAndShorterTimeouts()
    {
        NativeResilienceOptions options = NativeResilienceOptions.LowLatency;

        options.Name.Should().Be("Mvp24Hours-LowLatency");
        options.RetryMaxAttempts.Should().Be(2);
        options.RetryBackoffType.Should().Be(ResilienceBackoffType.Linear);
        options.TimeoutDuration.Should().Be(TimeSpan.FromSeconds(5));
        options.CircuitBreakerFailureRatio.Should().Be(0.3);
    }

    [Fact]
    public void BatchProcessing_ShouldDisableCircuitBreakerAndTimeout()
    {
        NativeResilienceOptions options = NativeResilienceOptions.BatchProcessing;

        options.Name.Should().Be("Mvp24Hours-BatchProcessing");
        options.RetryMaxAttempts.Should().Be(10);
        options.EnableCircuitBreaker.Should().BeFalse();
        options.EnableTimeout.Should().BeFalse();
    }

    [Fact]
    public void Database_ShouldUseExponentialWithJitter()
    {
        NativeResilienceOptions options = NativeResilienceOptions.Database;

        options.Name.Should().Be("Mvp24Hours-Database");
        options.RetryBackoffType.Should().Be(ResilienceBackoffType.ExponentialWithJitter);
        options.RetryDelay.Should().Be(TimeSpan.FromMilliseconds(500));
        options.EnableCircuitBreaker.Should().BeTrue();
        options.EnableTimeout.Should().BeTrue();
    }

    [Fact]
    public void Messaging_ShouldUseMessagingPreset()
    {
        NativeResilienceOptions options = NativeResilienceOptions.Messaging;

        options.Name.Should().Be("Mvp24Hours-Messaging");
        options.RetryMaxAttempts.Should().Be(5);
        options.TimeoutDuration.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void DefaultPreset_ShouldMatchNewInstance()
    {
        NativeResilienceOptions preset = NativeResilienceOptions.Default;
        var fresh = new NativeResilienceOptions();

        preset.Name.Should().Be(fresh.Name);
        preset.RetryMaxAttempts.Should().Be(fresh.RetryMaxAttempts);
        preset.EnableRetry.Should().Be(fresh.EnableRetry);
    }

    [Fact]
    public void ResilienceBackoffType_ShouldExposeExpectedMembers()
    {
        Enum.GetNames<ResilienceBackoffType>().Should().BeEquivalentTo(
            "Constant", "Linear", "Exponential", "ExponentialWithJitter");
    }

    [Fact]
    public void ResilienceCircuitState_ShouldExposeExpectedMembers()
    {
        Enum.GetNames<ResilienceCircuitState>().Should().BeEquivalentTo(
            "Closed", "Open", "HalfOpen", "Isolated");
    }
}
