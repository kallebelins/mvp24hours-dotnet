//=====================================================================================
// Tests for MongoDbResiliencyExtensions
//=====================================================================================
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Resiliency;
using Xunit;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Resiliency;

public class MongoDbResiliencyExtensionsTests
{
    [Fact]
    public void Should_Register_Default_Resiliency_Services()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMongoDbResiliency();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<MongoDbResiliencyOptions>();
        var policy = provider.GetService<IMongoDbResiliencyPolicy>();
        var connectionManager = provider.GetService<MongoDbConnectionManager>();

        options.Should().NotBeNull();
        policy.Should().NotBeNull();
        connectionManager.Should().NotBeNull();
    }

    [Fact]
    public void Should_Configure_Custom_Options()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMongoDbResiliency(options =>
        {
            options.RetryCount = 5;
            options.CircuitBreakerFailureThreshold = 10;
            options.DefaultOperationTimeoutSeconds = 60;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<MongoDbResiliencyOptions>();

        // Assert
        options.RetryCount.Should().Be(5);
        options.CircuitBreakerFailureThreshold.Should().Be(10);
        options.DefaultOperationTimeoutSeconds.Should().Be(60);
    }

    [Fact]
    public void Should_Register_Production_Configuration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMongoDbResiliencyForProduction();
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<MongoDbResiliencyOptions>();

        // Assert
        options.EnableCircuitBreaker.Should().BeTrue();
        options.MaxReconnectAttempts.Should().Be(10);
        options.RetryCount.Should().Be(3);
    }

    [Fact]
    public void Should_Register_Development_Configuration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMongoDbResiliencyForDevelopment();
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<MongoDbResiliencyOptions>();

        // Assert
        options.EnableCircuitBreaker.Should().BeFalse();
        options.MaxReconnectAttempts.Should().Be(3);
        options.RetryCount.Should().Be(2);
        options.DefaultOperationTimeoutSeconds.Should().Be(60);
    }

    [Fact]
    public void Should_Register_Retry_Policy_Only()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMongoDbRetryPolicy(retryCount: 5);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<MongoDbResiliencyOptions>();

        // Assert
        options.EnableRetry.Should().BeTrue();
        options.RetryCount.Should().Be(5);
        options.EnableCircuitBreaker.Should().BeFalse();
        options.EnableAutoReconnect.Should().BeFalse();
    }

    [Fact]
    public void Should_Register_Circuit_Breaker_Only()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMongoDbCircuitBreaker(failureThreshold: 3, durationSeconds: 60);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<MongoDbResiliencyOptions>();

        // Assert
        options.EnableCircuitBreaker.Should().BeTrue();
        options.CircuitBreakerFailureThreshold.Should().Be(3);
        options.CircuitBreakerDurationSeconds.Should().Be(60);
        options.EnableRetry.Should().BeFalse();
        options.EnableAutoReconnect.Should().BeFalse();
    }

    [Fact]
    public void Should_Add_Custom_Retryable_Exceptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMongoDbResiliency()
                .AddRetryableExceptions(typeof(InvalidOperationException), typeof(NotSupportedException));

        var provider = services.BuildServiceProvider();

        // Assert - We can't directly access PostConfigure results in a simple way,
        // but we can verify the extension doesn't throw
        var options = provider.GetRequiredService<MongoDbResiliencyOptions>();
        options.Should().NotBeNull();
    }

    [Fact]
    public void Should_Add_Custom_Non_Retryable_Exceptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMongoDbResiliency()
                .AddNonRetryableExceptions(typeof(ArgumentException), typeof(FormatException));

        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<MongoDbResiliencyOptions>();
        options.Should().NotBeNull();
    }

    [Fact]
    public void Should_Create_Singleton_Policy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoDbResiliency();
        var provider = services.BuildServiceProvider();

        // Act
        var policy1 = provider.GetRequiredService<IMongoDbResiliencyPolicy>();
        var policy2 = provider.GetRequiredService<IMongoDbResiliencyPolicy>();

        // Assert - Should be same instance (singleton)
        policy1.Should().BeSameAs(policy2);
    }

    [Fact]
    public void Should_Create_Singleton_ConnectionManager()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoDbResiliency();
        var provider = services.BuildServiceProvider();

        // Act
        var manager1 = provider.GetRequiredService<MongoDbConnectionManager>();
        var manager2 = provider.GetRequiredService<MongoDbConnectionManager>();

        // Assert - Should be same instance (singleton)
        manager1.Should().BeSameAs(manager2);
    }

    [Fact]
    public void Should_Share_Options_Between_Services()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoDbResiliency(options =>
        {
            options.RetryCount = 7;
        });
        var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<MongoDbResiliencyOptions>();

        // Assert
        options.RetryCount.Should().Be(7);
    }

    [Fact]
    public async Task Should_Use_Registered_Policy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoDbResiliency(options =>
        {
            options.EnableRetry = true;
            options.RetryCount = 2;
            options.RetryBaseDelayMilliseconds = 10;
            options.EnableCircuitBreaker = false;
            options.EnableOperationTimeout = false;
        });
        var provider = services.BuildServiceProvider();
        var policy = provider.GetRequiredService<IMongoDbResiliencyPolicy>();

        var executionCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async ct =>
        {
            executionCount++;
            await Task.Delay(1, ct);
            if (executionCount < 2)
                throw new TimeoutException("Transient");
            return "success";
        });

        // Assert
        result.Should().Be("success");
        executionCount.Should().Be(2);
    }

    [Fact]
    public void Should_Allow_Method_Chaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert - Should not throw and support chaining
        var action = () => services
            .AddMongoDbResiliency()
            .AddRetryableExceptions(typeof(InvalidOperationException))
            .AddNonRetryableExceptions(typeof(ArgumentException));

        action.Should().NotThrow();
    }
}

