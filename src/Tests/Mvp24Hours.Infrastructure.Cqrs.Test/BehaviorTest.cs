//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using FluentValidation;
using Mvp24Hours.Infrastructure.Cqrs.Test.Support;

namespace Mvp24Hours.Infrastructure.Cqrs.Test;

/// <summary>
/// Unit tests for pipeline behaviors.
/// </summary>
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
public class BehaviorTest
{
    [Fact, Priority(1)]
    public async Task LoggingBehavior_ShouldLogRequestExecution()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            options.RegisterLoggingBehavior = true;
        });
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        var command = new TestCommand { Name = "LogTest", Value = 1 };

        // Act
        var result = await mediator.SendAsync(command);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("LogTest", result);
    }

    [Fact, Priority(2)]
    public async Task PerformanceBehavior_ShouldLogSlowRequests()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(SlowCommand).Assembly);
            options.RegisterPerformanceBehavior = true;
            options.PerformanceThresholdMilliseconds = 50; // Low threshold for testing
        });
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        var command = new SlowCommand { DelayMs = 100 }; // Slower than threshold

        // Act
        var result = await mediator.SendAsync(command);

        // Assert
        Assert.Equal("Completed", result);
    }

    [Fact, Priority(3)]
    public async Task UnhandledExceptionBehavior_ShouldLogAndRethrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Error));
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(FailingCommand).Assembly);
            options.RegisterUnhandledExceptionBehavior = true;
        });
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        var command = new FailingCommand { Message = "Behavior test exception" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.SendAsync(command));
        Assert.Equal("Behavior test exception", ex.Message);
    }

    [Fact, Priority(4)]
    public async Task ValidationBehavior_ShouldValidateRequest()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(CreateUserCommand).Assembly);
            options.RegisterValidationBehavior = true;
        });
        services.AddTransient<IValidator<CreateUserCommand>, CreateUserCommandValidator>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var validCommand = new CreateUserCommand
        {
            Name = "John Doe",
            Email = "john@example.com",
            Age = 30
        };

        // Act
        var result = await mediator.SendAsync(validCommand);

        // Assert
        Assert.True(result > 0);
    }

    [Fact, Priority(5)]
    public async Task ValidationBehavior_WithInvalidRequest_ShouldThrowValidationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(CreateUserCommand).Assembly);
            options.RegisterValidationBehavior = true;
        });
        services.AddTransient<IValidator<CreateUserCommand>, CreateUserCommandValidator>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var invalidCommand = new CreateUserCommand
        {
            Name = "", // Empty - invalid
            Email = "invalid-email", // Invalid format
            Age = -1 // Negative - invalid
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Mvp24Hours.Core.Exceptions.ValidationException>(() =>
            mediator.SendAsync(invalidCommand));
        
        Assert.True(ex.ValidationErrors?.Count > 0);
    }

    [Fact, Priority(6)]
    public async Task ValidationBehavior_WithNoValidators_ShouldPassThrough()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            options.RegisterValidationBehavior = true;
        });
        // No validator registered for TestCommand
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        var command = new TestCommand { Name = "NoValidator", Value = 1 };

        // Act - Should not throw
        var result = await mediator.SendAsync(command);

        // Assert
        Assert.Contains("NoValidator", result);
    }

    [Fact, Priority(7)]
    public void WithDefaultBehaviors_ShouldRegisterLoggingPerformanceException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            options.WithDefaultBehaviors();
        });
        var sp = services.BuildServiceProvider();

        // Act
        var behaviors = sp.GetServices<IPipelineBehavior<TestCommand, string>>().ToList();

        // Assert
        Assert.Equal(3, behaviors.Count); // Logging, Performance, Exception
    }

    [Fact, Priority(8)]
    public async Task MultipleBehaviors_ShouldExecuteInOrder()
    {
        // Arrange
        var executionOrder = new List<string>();
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
        });
        
        // Add custom tracking behavior
        services.AddTransient<IPipelineBehavior<TestCommand, string>>(_ => 
            new TrackingBehavior<TestCommand, string>("First", executionOrder));
        services.AddTransient<IPipelineBehavior<TestCommand, string>>(_ => 
            new TrackingBehavior<TestCommand, string>("Second", executionOrder));
        
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new TestCommand { Name = "Order Test", Value = 1 };

        // Act
        await mediator.SendAsync(command);

        // Assert - Behaviors add both Before and After entries (4 total for 2 behaviors)
        Assert.Equal(4, executionOrder.Count);
        Assert.Equal("First-Before", executionOrder[0]);
        Assert.Equal("Second-Before", executionOrder[1]);
        Assert.Equal("Second-After", executionOrder[2]);
        Assert.Equal("First-After", executionOrder[3]);
    }

    [Fact, Priority(9)]
    public async Task Behavior_ShouldBeAbleToShortCircuit()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
        });
        
        // Add short-circuiting behavior
        services.AddTransient<IPipelineBehavior<TestCommand, string>>(_ => 
            new ShortCircuitBehavior<TestCommand, string>("Short-circuited!"));
        
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new TestCommand { Name = "Should not reach", Value = 1 };

        // Act
        var result = await mediator.SendAsync(command);

        // Assert
        Assert.Equal("Short-circuited!", result);
    }
}

/// <summary>
/// Behavior for tracking execution order in tests.
/// </summary>
internal class TrackingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly string _name;
    private readonly List<string> _tracker;

    public TrackingBehavior(string name, List<string> tracker)
    {
        _name = name;
        _tracker = tracker;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _tracker.Add($"{_name}-Before");
        var response = await next();
        _tracker.Add($"{_name}-After");
        return response;
    }
}

/// <summary>
/// Behavior that short-circuits the pipeline.
/// </summary>
internal class ShortCircuitBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly TResponse _response;

    public ShortCircuitBehavior(TResponse response)
    {
        _response = response;
    }

    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Don't call next() - short circuit
        return Task.FromResult(_response);
    }
}

