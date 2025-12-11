//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.Test.Support;

namespace Mvp24Hours.Infrastructure.Cqrs.Test;

/// <summary>
/// Unit tests for the Mediator implementation.
/// </summary>
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
public class MediatorTest
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public MediatorTest()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(typeof(TestCommand).Assembly);
        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact, Priority(1)]
    public async Task SendAsync_ShouldExecuteCommandHandler()
    {
        // Arrange
        var command = new TestCommand { Name = "Test", Value = 42 };

        // Act
        var result = await _mediator.SendAsync(command);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Test", result);
        Assert.Contains("42", result);
    }

    [Fact, Priority(2)]
    public async Task SendAsync_ShouldExecuteQueryHandler()
    {
        // Arrange
        var query = new GetUserQuery { UserId = 1 };

        // Act
        var result = await _mediator.SendAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("John Doe", result.Name);
    }

    [Fact, Priority(3)]
    public async Task SendAsync_ShouldReturnNullForNonExistentUser()
    {
        // Arrange
        var query = new GetUserQuery { UserId = 999 };

        // Act
        var result = await _mediator.SendAsync(query);

        // Assert
        Assert.Null(result);
    }

    [Fact, Priority(4)]
    public async Task SendAsync_WithVoidCommand_ShouldReturnUnit()
    {
        // Arrange
        TestVoidCommandHandler.ExecutionCount = 0;
        var command = new TestVoidCommand { Action = "Test Action" };

        // Act
        var result = await _mediator.SendAsync(command);

        // Assert
        Assert.Equal(Unit.Value, result);
        Assert.Equal(1, TestVoidCommandHandler.ExecutionCount);
    }

    [Fact, Priority(5)]
    public async Task SendAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _mediator.SendAsync<string>(null!));
    }

    [Fact, Priority(6)]
    public async Task SendAsync_WithUnregisteredHandler_ShouldThrowInvalidOperationException()
    {
        // Arrange - Create a service provider without handlers
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(); // No assembly scanning
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var command = new TestCommand { Name = "Test", Value = 1 };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            mediator.SendAsync(command));
    }

    [Fact, Priority(7)]
    public async Task SendAsync_ShouldRespectCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var command = new SlowCommand { DelayMs = 5000 };
        
        // Act
        var task = _mediator.SendAsync(command, cts.Token);
        cts.Cancel();

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
    }

    [Fact, Priority(8)]
    public async Task SendAsync_ShouldPropagateHandlerException()
    {
        // Arrange
        var command = new FailingCommand { Message = "Custom error" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _mediator.SendAsync(command));
        Assert.Equal("Custom error", ex.Message);
    }

    [Fact, Priority(9)]
    public async Task ISender_ShouldBeResolvable()
    {
        // Arrange
        var sender = _serviceProvider.GetRequiredService<ISender>();
        var command = new TestCommand { Name = "Sender Test", Value = 100 };

        // Act
        var result = await sender.SendAsync(command);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Sender Test", result);
    }

    [Fact, Priority(10)]
    public async Task IPublisher_ShouldBeResolvable()
    {
        // Arrange
        var publisher = _serviceProvider.GetRequiredService<IPublisher>();

        // Act - Should not throw
        await publisher.PublishAsync(new OrderCreatedNotification
        {
            OrderId = 1,
            CustomerName = "Test",
            Amount = 100
        });

        // Assert - Publisher should be resolvable and working
        Assert.NotNull(publisher);
    }

    [Fact, Priority(11)]
    public async Task GetAllUsersQuery_ShouldReturnAllUsers()
    {
        // Arrange
        var query = new GetAllUsersQuery();

        // Act
        var result = await _mediator.SendAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    [Fact, Priority(12)]
    public async Task GetAllUsersQuery_WithLimit_ShouldRespectLimit()
    {
        // Arrange
        var query = new GetAllUsersQuery { Limit = 2 };

        // Act
        var result = await _mediator.SendAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }
}

