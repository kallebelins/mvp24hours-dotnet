//=====================================================================================
// Tests for MongoDbCircuitBreaker
//=====================================================================================
using FluentAssertions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Resiliency;
using Xunit;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Resiliency;

public class MongoDbCircuitBreakerTests
{
    private MongoDbResiliencyOptions CreateDefaultOptions() => new()
    {
        EnableCircuitBreaker = true,
        CircuitBreakerFailureThreshold = 3,
        CircuitBreakerSamplingDurationSeconds = 60,
        CircuitBreakerDurationSeconds = 5,
        CircuitBreakerMinimumThroughput = 1
    };

    [Fact]
    public void Should_Start_In_Closed_State()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var circuitBreaker = new MongoDbCircuitBreaker(options);

        // Act & Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public void Should_Allow_Requests_When_Closed()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var circuitBreaker = new MongoDbCircuitBreaker(options);

        // Act
        var allowed = circuitBreaker.AllowRequest();

        // Assert
        allowed.Should().BeTrue();
    }

    [Fact]
    public void Should_Open_Circuit_After_Failure_Threshold()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.CircuitBreakerFailureThreshold = 3;
        options.CircuitBreakerMinimumThroughput = 1;
        var circuitBreaker = new MongoDbCircuitBreaker(options);

        // Act - Record failures
        for (int i = 0; i < 3; i++)
        {
            circuitBreaker.RecordFailure(new Exception($"Test failure {i}"));
        }

        // Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Open);
    }

    [Fact]
    public void Should_Not_Open_Circuit_Before_Failure_Threshold()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.CircuitBreakerFailureThreshold = 5;
        options.CircuitBreakerMinimumThroughput = 1;
        var circuitBreaker = new MongoDbCircuitBreaker(options);

        // Act - Record some failures but not enough
        for (int i = 0; i < 3; i++)
        {
            circuitBreaker.RecordFailure(new Exception($"Test failure {i}"));
        }

        // Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public void Should_Reject_Requests_When_Open()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.CircuitBreakerFailureThreshold = 2;
        options.CircuitBreakerMinimumThroughput = 1;
        var circuitBreaker = new MongoDbCircuitBreaker(options);

        // Open the circuit
        circuitBreaker.RecordFailure(new Exception("Failure 1"));
        circuitBreaker.RecordFailure(new Exception("Failure 2"));

        // Act
        var allowed = circuitBreaker.AllowRequest();

        // Assert
        allowed.Should().BeFalse();
    }

    [Fact]
    public void Should_Track_Rejected_Count()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.CircuitBreakerFailureThreshold = 2;
        options.CircuitBreakerMinimumThroughput = 1;
        var circuitBreaker = new MongoDbCircuitBreaker(options);

        // Open the circuit
        circuitBreaker.RecordFailure(new Exception("Failure 1"));
        circuitBreaker.RecordFailure(new Exception("Failure 2"));

        // Act - try to make requests when circuit is open
        circuitBreaker.AllowRequest();
        circuitBreaker.AllowRequest();
        circuitBreaker.AllowRequest();

        // Assert
        circuitBreaker.TotalRejectedCount.Should().Be(3);
    }

    [Fact]
    public void Should_Increment_Success_Count()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var circuitBreaker = new MongoDbCircuitBreaker(options);

        // Act
        circuitBreaker.RecordSuccess();
        circuitBreaker.RecordSuccess();
        circuitBreaker.RecordSuccess();

        // Assert
        circuitBreaker.TotalSuccessCount.Should().Be(3);
    }

    [Fact]
    public void Should_Increment_Failure_Count()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var circuitBreaker = new MongoDbCircuitBreaker(options);

        // Act
        circuitBreaker.RecordFailure(new Exception("Failure 1"));
        circuitBreaker.RecordFailure(new Exception("Failure 2"));

        // Assert
        circuitBreaker.TotalFailureCount.Should().Be(2);
    }

    [Fact]
    public void Should_Handle_Failure_After_Reset()
    {
        // Test that after reset, failures can re-open the circuit
        
        // Arrange
        var options = CreateDefaultOptions();
        options.CircuitBreakerFailureThreshold = 2;
        options.CircuitBreakerMinimumThroughput = 1;
        var circuitBreaker = new MongoDbCircuitBreaker(options);

        // Open the circuit
        circuitBreaker.RecordFailure(new Exception("Failure 1"));
        circuitBreaker.RecordFailure(new Exception("Failure 2"));
        circuitBreaker.State.Should().Be(CircuitBreakerState.Open);

        // Reset
        circuitBreaker.ResetState();
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);

        // Act - Record failures again
        circuitBreaker.RecordFailure(new Exception("Failure 3"));
        circuitBreaker.RecordFailure(new Exception("Failure 4"));

        // Assert - Should be Open again
        circuitBreaker.State.Should().Be(CircuitBreakerState.Open);
    }

    [Fact]
    public void Should_Manually_Trip_Circuit()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var circuitBreaker = new MongoDbCircuitBreaker(options);
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);

        // Act
        circuitBreaker.Trip();

        // Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Open);
    }

    [Fact]
    public void Should_Manually_Reset_Circuit()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.CircuitBreakerFailureThreshold = 2;
        options.CircuitBreakerMinimumThroughput = 1;
        var circuitBreaker = new MongoDbCircuitBreaker(options);

        // Open the circuit
        circuitBreaker.RecordFailure(new Exception("Failure 1"));
        circuitBreaker.RecordFailure(new Exception("Failure 2"));
        circuitBreaker.State.Should().Be(CircuitBreakerState.Open);

        // Act
        circuitBreaker.ResetState();

        // Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public void Should_Track_Circuit_Trip_Count()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.CircuitBreakerFailureThreshold = 2;
        options.CircuitBreakerMinimumThroughput = 1;
        var circuitBreaker = new MongoDbCircuitBreaker(options);

        // Act - Trip the circuit multiple times
        // First trip
        circuitBreaker.RecordFailure(new Exception("Failure 1"));
        circuitBreaker.RecordFailure(new Exception("Failure 2"));
        
        // Reset and trip again
        circuitBreaker.ResetState();
        circuitBreaker.RecordFailure(new Exception("Failure 3"));
        circuitBreaker.RecordFailure(new Exception("Failure 4"));

        // Assert
        circuitBreaker.CircuitTripCount.Should().Be(2);
    }

    [Fact]
    public void Should_Track_Last_Success_Time()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var circuitBreaker = new MongoDbCircuitBreaker(options);
        var beforeSuccess = DateTimeOffset.UtcNow;

        // Act
        Thread.Sleep(10);
        circuitBreaker.RecordSuccess();
        var afterSuccess = DateTimeOffset.UtcNow;

        // Assert
        circuitBreaker.LastSuccessTime.Should().NotBeNull();
        circuitBreaker.LastSuccessTime.Should().BeAfter(beforeSuccess);
        circuitBreaker.LastSuccessTime.Should().BeBefore(afterSuccess);
    }

    [Fact]
    public void Should_Track_Last_Failure_Time()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var circuitBreaker = new MongoDbCircuitBreaker(options);
        var beforeFailure = DateTimeOffset.UtcNow;

        // Act
        Thread.Sleep(10);
        circuitBreaker.RecordFailure(new Exception("Test failure"));
        var afterFailure = DateTimeOffset.UtcNow;

        // Assert
        circuitBreaker.LastFailureTime.Should().NotBeNull();
        circuitBreaker.LastFailureTime.Should().BeAfter(beforeFailure);
        circuitBreaker.LastFailureTime.Should().BeBefore(afterFailure);
    }

    [Fact]
    public void Should_Calculate_Failure_Rate()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var circuitBreaker = new MongoDbCircuitBreaker(options);

        // Act - 3 successes, 2 failures = 40% failure rate
        circuitBreaker.RecordSuccess();
        circuitBreaker.RecordSuccess();
        circuitBreaker.RecordSuccess();
        circuitBreaker.RecordFailure(new Exception("Failure 1"));
        circuitBreaker.RecordFailure(new Exception("Failure 2"));

        // Assert
        circuitBreaker.CurrentFailureRate.Should().BeApproximately(0.4, 0.01);
    }

    [Fact]
    public void Should_Reset_All_Metrics()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var circuitBreaker = new MongoDbCircuitBreaker(options);

        // Record some activity
        circuitBreaker.RecordSuccess();
        circuitBreaker.RecordSuccess();
        circuitBreaker.RecordFailure(new Exception("Failure 1"));

        // Act
        circuitBreaker.Reset();

        // Assert
        circuitBreaker.TotalSuccessCount.Should().Be(0);
        circuitBreaker.TotalFailureCount.Should().Be(0);
        circuitBreaker.TotalRejectedCount.Should().Be(0);
        circuitBreaker.CircuitTripCount.Should().Be(0);
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public void Should_Get_Remaining_Open_Duration()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.CircuitBreakerFailureThreshold = 2;
        options.CircuitBreakerMinimumThroughput = 1;
        options.CircuitBreakerDurationSeconds = 10;
        var circuitBreaker = new MongoDbCircuitBreaker(options);

        // Open the circuit
        circuitBreaker.RecordFailure(new Exception("Failure 1"));
        circuitBreaker.RecordFailure(new Exception("Failure 2"));

        // Act
        var remaining = circuitBreaker.GetRemainingOpenDuration();

        // Assert
        remaining.Should().NotBeNull();
        remaining.Value.TotalSeconds.Should().BeGreaterThan(0);
        remaining.Value.TotalSeconds.Should().BeLessOrEqualTo(10);
    }

    [Fact]
    public void Should_Return_Null_Duration_When_Not_Open()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var circuitBreaker = new MongoDbCircuitBreaker(options);

        // Act
        var remaining = circuitBreaker.GetRemainingOpenDuration();

        // Assert
        remaining.Should().BeNull();
    }

    [Fact]
    public void Should_Not_Open_If_Below_Minimum_Throughput()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.CircuitBreakerFailureThreshold = 3;
        options.CircuitBreakerMinimumThroughput = 10; // Require at least 10 operations
        var circuitBreaker = new MongoDbCircuitBreaker(options);

        // Act - Record failures but not enough throughput
        for (int i = 0; i < 5; i++)
        {
            circuitBreaker.RecordFailure(new Exception($"Failure {i}"));
        }

        // Assert - Should stay closed due to low throughput
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }
}

