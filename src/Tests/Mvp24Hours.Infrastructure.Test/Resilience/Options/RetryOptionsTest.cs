//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Resilience.Options;

namespace Mvp24Hours.Infrastructure.Test.Resilience.Options;

[Trait("Category", "Unit")]
public class RetryOptionsTest
{
    [Fact]
    public void Default_ShouldUseExpectedValues()
    {
        var options = new RetryOptions();

        options.MaxRetries.Should().Be(3);
        options.InitialDelay.Should().Be(TimeSpan.FromSeconds(1));
        options.MaxDelay.Should().Be(TimeSpan.FromSeconds(30));
        options.BackoffType.Should().Be(RetryBackoffType.Exponential);
        options.JitterFactor.Should().Be(0.1);
        options.UseExponentialBackoff.Should().BeTrue();
        options.ShouldRetryOnException.Should().BeNull();
        options.OnRetry.Should().BeNull();
        options.OnRetryExhausted.Should().BeNull();
    }

    [Fact]
    public void RetryAttemptInfo_Defaults_ShouldSetTimestamp()
    {
        var info = new RetryAttemptInfo
        {
            AttemptNumber = 1,
            MaxAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(10),
            Exception = new InvalidOperationException()
        };

        info.AttemptNumber.Should().Be(1);
        info.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RetryExhaustedInfo_Defaults_ShouldSetTimestamp()
    {
        var info = new RetryExhaustedInfo
        {
            TotalAttempts = 4,
            FinalException = new TimeoutException()
        };

        info.TotalAttempts.Should().Be(4);
        info.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
