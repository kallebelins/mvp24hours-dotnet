//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Resilience.Options;

namespace Mvp24Hours.Infrastructure.Test.Resilience.Options;

[Trait("Category", "Unit")]
public class CircuitBreakerOptionsTest
{
    [Fact]
    public void Default_ShouldUseExpectedValues()
    {
        var options = new CircuitBreakerOptions();

        options.FailureThreshold.Should().Be(5);
        options.SamplingDuration.Should().Be(TimeSpan.FromSeconds(30));
        options.MinimumThroughput.Should().Be(10);
        options.BreakDuration.Should().Be(TimeSpan.FromSeconds(30));
        options.FailureRatio.Should().Be(0.5);
        options.ShouldCountAsFailure.Should().BeNull();
        options.OnBreak.Should().BeNull();
        options.OnReset.Should().BeNull();
        options.OnHalfOpen.Should().BeNull();
    }

    [Fact]
    public void CircuitBreakerStateChangeInfo_Defaults_ShouldSetTimestamp()
    {
        var info = new CircuitBreakerStateChangeInfo
        {
            OperationName = "op",
            NewState = CircuitBreakerState.Open,
            BreakDuration = TimeSpan.FromSeconds(10),
            Reason = "too many failures"
        };

        info.OperationName.Should().Be("op");
        info.NewState.Should().Be(CircuitBreakerState.Open);
        info.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CircuitBreakerState_ShouldExposeExpectedMembers()
    {
        Enum.GetNames<CircuitBreakerState>().Should().BeEquivalentTo(
            "Closed", "Open", "HalfOpen");
    }
}
