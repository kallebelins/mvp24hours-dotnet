//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.Behaviors;

namespace Mvp24Hours.Infrastructure.Cqrs.Test;

/// <summary>
/// Unit tests for Timeout and Circuit Breaker behaviors.
/// </summary>
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
public class TimeoutAndCircuitBreakerTest
{
    #region [ TimeoutBehavior Tests ]

    [Fact, Priority(1)]
    public async Task TimeoutBehavior_FastRequest_ShouldComplete()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(FastTimeoutCommand).Assembly);
            options.RegisterTimeoutBehavior = true;
            options.DefaultTimeoutMilliseconds = 5000; // 5 second default
        });
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        var command = new FastTimeoutCommand();

        // Act
        var result = await mediator.SendAsync(command);

        // Assert
        Assert.Equal("Fast completed", result);
    }

    [Fact, Priority(2)]
    public async Task TimeoutBehavior_SlowRequest_ShouldTimeout()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(SlowTimeoutCommand).Assembly);
            options.RegisterTimeoutBehavior = true;
        });
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        var command = new SlowTimeoutCommand { DelayMs = 500 }; // Slower than the 100ms timeout

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RequestTimeoutException>(() =>
            mediator.SendAsync(command));
        
        Assert.Equal("SlowTimeoutCommand", ex.RequestName);
        Assert.Equal(100, ex.TimeoutMilliseconds);
    }

    [Fact, Priority(3)]
    public async Task TimeoutBehavior_NoTimeout_ShouldComplete()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(NoTimeoutCommand).Assembly);
            options.RegisterTimeoutBehavior = true;
            // No default timeout set (0)
        });
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        var command = new NoTimeoutCommand { DelayMs = 200 };

        // Act
        var result = await mediator.SendAsync(command);

        // Assert - Should complete without timeout since no timeout configured
        Assert.Equal("No timeout completed", result);
    }

    [Fact, Priority(4)]
    public void TimeoutPolicy_ShouldHavePresets()
    {
        // Assert
        Assert.Equal(5000, TimeoutPolicy.Fast.TimeoutMilliseconds);
        Assert.Equal(30000, TimeoutPolicy.Default.TimeoutMilliseconds);
        Assert.Equal(120000, TimeoutPolicy.Long.TimeoutMilliseconds);
    }

    [Fact, Priority(5)]
    public void TimeoutPolicy_ShouldThrowOnInvalidTimeout()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimeoutPolicy(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimeoutPolicy(-100));
    }

    #endregion

    #region [ CircuitBreakerBehavior Tests ]

    [Fact, Priority(10)]
    public async Task CircuitBreaker_SuccessfulRequest_ShouldPass()
    {
        // Arrange
        CircuitBreakerBehavior<CircuitBreakerTestCommand, string>.ResetAllCircuits();
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(CircuitBreakerTestCommand).Assembly);
            options.RegisterCircuitBreakerBehavior = true;
        });
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        var command = new CircuitBreakerTestCommand { ShouldFail = false };

        // Act
        var result = await mediator.SendAsync(command);

        // Assert
        Assert.Equal("Success", result);
        
        var metrics = CircuitBreakerBehavior<CircuitBreakerTestCommand, string>.GetMetrics("test-circuit");
        Assert.NotNull(metrics);
        Assert.Equal(CircuitState.Closed, metrics.State);
        Assert.Equal(1, metrics.SuccessCount);
    }

    [Fact, Priority(11)]
    public async Task CircuitBreaker_FailuresUnderThreshold_ShouldStayClosed()
    {
        // Arrange
        CircuitBreakerBehavior<CircuitBreakerTestCommand, string>.ResetAllCircuits();
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(CircuitBreakerTestCommand).Assembly);
            options.RegisterCircuitBreakerBehavior = true;
        });
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        // Act - Cause 2 failures (threshold is 3)
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await mediator.SendAsync(new CircuitBreakerTestCommand { ShouldFail = true });
            }
            catch (InvalidOperationException)
            {
                // Expected
            }
        }

        // Assert - Circuit should still be closed
        var metrics = CircuitBreakerBehavior<CircuitBreakerTestCommand, string>.GetMetrics("test-circuit");
        Assert.NotNull(metrics);
        Assert.Equal(CircuitState.Closed, metrics.State);
        Assert.Equal(2, metrics.FailureCount);
    }

    [Fact, Priority(12)]
    public async Task CircuitBreaker_FailuresOverThreshold_ShouldOpen()
    {
        // Arrange
        CircuitBreakerBehavior<CircuitBreakerTestCommand, string>.ResetAllCircuits();
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(CircuitBreakerTestCommand).Assembly);
            options.RegisterCircuitBreakerBehavior = true;
        });
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        // First, make enough requests to meet minimum throughput
        for (int i = 0; i < 5; i++)
        {
            await mediator.SendAsync(new CircuitBreakerTestCommand { ShouldFail = false });
        }

        // Then cause failures to trip the circuit
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await mediator.SendAsync(new CircuitBreakerTestCommand { ShouldFail = true });
            }
            catch (InvalidOperationException)
            {
                // Expected
            }
        }

        // Assert - Circuit should be open
        var metrics = CircuitBreakerBehavior<CircuitBreakerTestCommand, string>.GetMetrics("test-circuit");
        Assert.NotNull(metrics);
        Assert.Equal(CircuitState.Open, metrics.State);
    }

    [Fact, Priority(13)]
    public async Task CircuitBreaker_WhenOpen_ShouldRejectRequests()
    {
        // Arrange
        CircuitBreakerBehavior<CircuitBreakerTestCommand, string>.ResetAllCircuits();
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(CircuitBreakerTestCommand).Assembly);
            options.RegisterCircuitBreakerBehavior = true;
        });
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        // Trip the circuit
        for (int i = 0; i < 5; i++)
        {
            await mediator.SendAsync(new CircuitBreakerTestCommand { ShouldFail = false });
        }
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await mediator.SendAsync(new CircuitBreakerTestCommand { ShouldFail = true });
            }
            catch (InvalidOperationException) { }
        }

        // Act & Assert - Next request should be rejected immediately
        var ex = await Assert.ThrowsAsync<CircuitBreakerOpenException>(() =>
            mediator.SendAsync(new CircuitBreakerTestCommand { ShouldFail = false }));
        
        Assert.Equal("test-circuit", ex.CircuitKey);
        Assert.Equal("CircuitBreakerTestCommand", ex.RequestName);
    }

    [Fact, Priority(14)]
    public void CircuitBreaker_ResetCircuit_ShouldClearState()
    {
        // Arrange
        CircuitBreakerBehavior<CircuitBreakerTestCommand, string>.ResetAllCircuits();

        // Act
        var result = CircuitBreakerBehavior<CircuitBreakerTestCommand, string>.ResetCircuit("test-circuit");

        // Assert - Returns false because circuit doesn't exist
        Assert.False(result);
    }

    [Fact, Priority(15)]
    public void CircuitBreakerPolicy_ShouldHavePresets()
    {
        // Assert
        var relaxed = CircuitBreakerPolicy.Relaxed;
        Assert.Equal(10, relaxed.FailureThreshold);
        Assert.Equal(60, relaxed.SamplingDurationSeconds);
        Assert.Equal(20, relaxed.MinimumThroughput);
        Assert.Equal(30, relaxed.DurationOfBreakSeconds);

        var aggressive = CircuitBreakerPolicy.Aggressive;
        Assert.Equal(3, aggressive.FailureThreshold);
        Assert.Equal(15, aggressive.SamplingDurationSeconds);
        Assert.Equal(5, aggressive.MinimumThroughput);
        Assert.Equal(120, aggressive.DurationOfBreakSeconds);
    }

    [Fact, Priority(16)]
    public async Task CircuitBreaker_NonProtectedRequest_ShouldBypass()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(NonProtectedCommand).Assembly);
            options.RegisterCircuitBreakerBehavior = true;
        });
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        // Act - Should not use circuit breaker
        var result = await mediator.SendAsync(new NonProtectedCommand());

        // Assert
        Assert.Equal("Not protected", result);
    }

    #endregion

    #region [ WithAdvancedResiliency Configuration Test ]

    [Fact, Priority(20)]
    public void WithAdvancedResiliency_ShouldRegisterAllBehaviors()
    {
        // Arrange
        var options = new MediatorOptions();
        
        // Act
        options.WithAdvancedResiliency(defaultTimeoutMs: 10000);

        // Assert
        Assert.True(options.RegisterTimeoutBehavior);
        Assert.True(options.RegisterCircuitBreakerBehavior);
        Assert.True(options.RegisterRetryBehavior);
        Assert.True(options.RegisterIdempotencyBehavior);
        Assert.Equal(10000, options.DefaultTimeoutMilliseconds);
    }

    #endregion
}

#region [ Test Support Classes ]

/// <summary>
/// Fast command that completes quickly.
/// </summary>
internal record FastTimeoutCommand : IMediatorCommand<string>;

internal class FastTimeoutCommandHandler : IMediatorCommandHandler<FastTimeoutCommand, string>
{
    public Task<string> Handle(FastTimeoutCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult("Fast completed");
    }
}

/// <summary>
/// Slow command with configurable timeout.
/// </summary>
internal record SlowTimeoutCommand : IMediatorCommand<string>, IHasTimeout
{
    public int DelayMs { get; init; }
    public int? TimeoutMilliseconds => 100; // 100ms timeout
}

internal class SlowTimeoutCommandHandler : IMediatorCommandHandler<SlowTimeoutCommand, string>
{
    public async Task<string> Handle(SlowTimeoutCommand request, CancellationToken cancellationToken)
    {
        await Task.Delay(request.DelayMs, cancellationToken);
        return "Slow completed";
    }
}

/// <summary>
/// Command without timeout (uses default or none).
/// </summary>
internal record NoTimeoutCommand : IMediatorCommand<string>
{
    public int DelayMs { get; init; }
}

internal class NoTimeoutCommandHandler : IMediatorCommandHandler<NoTimeoutCommand, string>
{
    public async Task<string> Handle(NoTimeoutCommand request, CancellationToken cancellationToken)
    {
        await Task.Delay(request.DelayMs, cancellationToken);
        return "No timeout completed";
    }
}

/// <summary>
/// Command protected by circuit breaker.
/// </summary>
internal record CircuitBreakerTestCommand : IMediatorCommand<string>, ICircuitBreakerProtected
{
    public bool ShouldFail { get; init; }
    public string? CircuitBreakerKey => "test-circuit";
    public int FailureThreshold => 3;
    public int SamplingDurationSeconds => 60;
    public int MinimumThroughput => 5;
    public int DurationOfBreakSeconds => 30;
}

internal class CircuitBreakerTestCommandHandler : IMediatorCommandHandler<CircuitBreakerTestCommand, string>
{
    public Task<string> Handle(CircuitBreakerTestCommand request, CancellationToken cancellationToken)
    {
        if (request.ShouldFail)
        {
            throw new InvalidOperationException("Simulated failure");
        }
        return Task.FromResult("Success");
    }
}

/// <summary>
/// Command not protected by circuit breaker.
/// </summary>
internal record NonProtectedCommand : IMediatorCommand<string>;

internal class NonProtectedCommandHandler : IMediatorCommandHandler<NonProtectedCommand, string>
{
    public Task<string> Handle(NonProtectedCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult("Not protected");
    }
}

#endregion

