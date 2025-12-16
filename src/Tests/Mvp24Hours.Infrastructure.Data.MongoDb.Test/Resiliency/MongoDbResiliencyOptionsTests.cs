//=====================================================================================
// Tests for MongoDbResiliencyOptions
//=====================================================================================
using FluentAssertions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Resiliency;
using Xunit;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Resiliency;

public class MongoDbResiliencyOptionsTests
{
    [Fact]
    public void Should_Have_Sensible_Defaults()
    {
        // Arrange & Act
        var options = new MongoDbResiliencyOptions();

        // Assert - Connection resiliency
        options.EnableAutoReconnect.Should().BeTrue();
        options.MaxReconnectAttempts.Should().Be(5);
        options.ReconnectDelayMilliseconds.Should().Be(1000);
        options.MaxReconnectDelayMilliseconds.Should().Be(30000);
        options.UseExponentialBackoffForReconnect.Should().BeTrue();
        options.ReconnectJitterFactor.Should().Be(0.2);

        // Assert - Retry policy
        options.EnableRetry.Should().BeTrue();
        options.RetryCount.Should().Be(3);
        options.RetryBaseDelayMilliseconds.Should().Be(100);
        options.RetryMaxDelayMilliseconds.Should().Be(5000);
        options.UseExponentialBackoff.Should().BeTrue();
        options.RetryJitterFactor.Should().Be(0.2);

        // Assert - Circuit breaker
        options.EnableCircuitBreaker.Should().BeTrue();
        options.CircuitBreakerFailureThreshold.Should().Be(5);
        options.CircuitBreakerSamplingDurationSeconds.Should().Be(60);
        options.CircuitBreakerDurationSeconds.Should().Be(30);
        options.CircuitBreakerMinimumThroughput.Should().Be(10);

        // Assert - Timeouts
        options.EnableOperationTimeout.Should().BeTrue();
        options.DefaultOperationTimeoutSeconds.Should().Be(30);
        options.BulkOperationTimeoutSeconds.Should().Be(120);

        // Assert - Failover
        options.EnableAutomaticFailover.Should().BeTrue();
        options.ServerSelectionTimeoutSeconds.Should().Be(30);
        options.HeartbeatFrequencySeconds.Should().Be(10);
        options.EnableServerMonitoring.Should().BeTrue();
        options.AllowReadsWithoutPrimary.Should().BeTrue();

        // Assert - Logging
        options.LogRetryAttempts.Should().BeTrue();
        options.LogCircuitBreakerStateChanges.Should().BeTrue();
        options.LogConnectionEvents.Should().BeTrue();
        options.LogTimeoutEvents.Should().BeTrue();
    }

    [Fact]
    public void Should_Get_Read_Timeout_From_Default()
    {
        // Arrange
        var options = new MongoDbResiliencyOptions
        {
            DefaultOperationTimeoutSeconds = 30,
            ReadOperationTimeoutSeconds = null
        };

        // Act
        var timeout = options.GetReadTimeout();

        // Assert
        timeout.TotalSeconds.Should().Be(30);
    }

    [Fact]
    public void Should_Get_Read_Timeout_From_Specific_Setting()
    {
        // Arrange
        var options = new MongoDbResiliencyOptions
        {
            DefaultOperationTimeoutSeconds = 30,
            ReadOperationTimeoutSeconds = 15
        };

        // Act
        var timeout = options.GetReadTimeout();

        // Assert
        timeout.TotalSeconds.Should().Be(15);
    }

    [Fact]
    public void Should_Get_Write_Timeout_From_Default()
    {
        // Arrange
        var options = new MongoDbResiliencyOptions
        {
            DefaultOperationTimeoutSeconds = 30,
            WriteOperationTimeoutSeconds = null
        };

        // Act
        var timeout = options.GetWriteTimeout();

        // Assert
        timeout.TotalSeconds.Should().Be(30);
    }

    [Fact]
    public void Should_Get_Write_Timeout_From_Specific_Setting()
    {
        // Arrange
        var options = new MongoDbResiliencyOptions
        {
            DefaultOperationTimeoutSeconds = 30,
            WriteOperationTimeoutSeconds = 45
        };

        // Act
        var timeout = options.GetWriteTimeout();

        // Assert
        timeout.TotalSeconds.Should().Be(45);
    }

    [Fact]
    public void Should_Get_Bulk_Operation_Timeout()
    {
        // Arrange
        var options = new MongoDbResiliencyOptions
        {
            BulkOperationTimeoutSeconds = 300
        };

        // Act
        var timeout = options.GetBulkOperationTimeout();

        // Assert
        timeout.TotalSeconds.Should().Be(300);
    }

    [Fact]
    public void Should_Return_MaxValue_When_Timeout_Is_Zero()
    {
        // Arrange
        var options = new MongoDbResiliencyOptions
        {
            DefaultOperationTimeoutSeconds = 0
        };

        // Act
        var timeout = options.GetReadTimeout();

        // Assert
        timeout.Should().Be(TimeSpan.MaxValue);
    }

    [Fact]
    public void Should_Create_Production_Configuration()
    {
        // Act
        var options = MongoDbResiliencyOptions.CreateProduction();

        // Assert - Production has more aggressive settings
        options.EnableAutoReconnect.Should().BeTrue();
        options.MaxReconnectAttempts.Should().Be(10);
        options.EnableCircuitBreaker.Should().BeTrue();
        options.EnableRetry.Should().BeTrue();
        options.RetryCount.Should().Be(3);
        options.LogRetryAttempts.Should().BeTrue();
        options.LogCircuitBreakerStateChanges.Should().BeTrue();
        options.LogConnectionEvents.Should().BeTrue();
    }

    [Fact]
    public void Should_Create_Development_Configuration()
    {
        // Act
        var options = MongoDbResiliencyOptions.CreateDevelopment();

        // Assert - Development has more lenient settings
        options.EnableAutoReconnect.Should().BeTrue();
        options.MaxReconnectAttempts.Should().Be(3); // Fewer attempts
        options.EnableCircuitBreaker.Should().BeFalse(); // Disabled for easier debugging
        options.EnableRetry.Should().BeTrue();
        options.RetryCount.Should().Be(2); // Fewer retries
        options.DefaultOperationTimeoutSeconds.Should().Be(60); // Longer for debugging
    }

    [Fact]
    public void Should_Allow_Custom_Exception_Types()
    {
        // Arrange
        var options = new MongoDbResiliencyOptions();

        // Act
        options.AdditionalRetryableExceptions.Add(typeof(InvalidOperationException));
        options.NonRetryableExceptions.Add(typeof(ArgumentException));

        // Assert
        options.AdditionalRetryableExceptions.Should().Contain(typeof(InvalidOperationException));
        options.NonRetryableExceptions.Should().Contain(typeof(ArgumentException));
    }

    [Fact]
    public void Should_Support_Failure_Rate_Threshold()
    {
        // Arrange
        var options = new MongoDbResiliencyOptions
        {
            CircuitBreakerFailureRateThreshold = 0.5 // 50%
        };

        // Assert
        options.CircuitBreakerFailureRateThreshold.Should().Be(0.5);
    }

    [Fact]
    public void Should_Track_Circuit_Breaker_Metrics_By_Default()
    {
        // Arrange
        var options = new MongoDbResiliencyOptions();

        // Assert
        options.TrackCircuitBreakerMetrics.Should().BeTrue();
    }
}

