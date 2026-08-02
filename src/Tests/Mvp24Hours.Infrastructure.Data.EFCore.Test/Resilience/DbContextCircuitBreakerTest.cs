using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using Mvp24Hours.Infrastructure.Data.EFCore.Resilience;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Resilience;

[Trait("Category", "Unit")]
public class DbContextCircuitBreakerTest
{
    private static EFCoreResilienceOptions CreateOptions(int failureThreshold = 3)
    {
        return new()
        {
            EnableCircuitBreaker = true,
            CircuitBreakerFailureThreshold = failureThreshold,
            CircuitBreakerDurationSeconds = 30
        };
    }

    [Fact]
    public void RecordFailure_AfterThreshold_OpensCircuit()
    {
        var breaker = new DbContextCircuitBreaker(CreateOptions(failureThreshold: 2));

        breaker.State.Should().Be(CircuitState.Closed);
        breaker.RecordFailure();
        breaker.State.Should().Be(CircuitState.Closed);
        breaker.RecordFailure();

        breaker.State.Should().Be(CircuitState.Open);
        breaker.IsAllowingRequests.Should().BeFalse();
    }

    [Fact]
    public void EnsureCircuitClosed_WhenOpen_ThrowsCircuitBreakerOpenException()
    {
        var breaker = new DbContextCircuitBreaker(CreateOptions(failureThreshold: 1));
        breaker.RecordFailure();

        breaker.State.Should().Be(CircuitState.Open);

        Action act = () => breaker.EnsureCircuitClosed();

        act.Should().Throw<CircuitBreakerOpenException>()
            .Which.RetryAfter.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void RecordSuccess_ResetsConsecutiveFailures()
    {
        var breaker = new DbContextCircuitBreaker(CreateOptions(failureThreshold: 5));

        breaker.RecordFailure();
        breaker.RecordFailure();
        breaker.ConsecutiveFailures.Should().Be(2);

        breaker.RecordSuccess();

        breaker.ConsecutiveFailures.Should().Be(0);
        breaker.State.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public void Reset_ReturnsCircuitToClosedState()
    {
        var breaker = new DbContextCircuitBreaker(CreateOptions(failureThreshold: 1));
        breaker.RecordFailure();
        breaker.State.Should().Be(CircuitState.Open);

        breaker.Reset();

        breaker.State.Should().Be(CircuitState.Closed);
        breaker.ConsecutiveFailures.Should().Be(0);
        breaker.TotalFailureCount.Should().Be(0);
        breaker.TotalSuccessCount.Should().Be(0);
        breaker.IsAllowingRequests.Should().BeTrue();
    }
}
