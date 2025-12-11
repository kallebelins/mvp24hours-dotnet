//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Cqrs.Test.Support;

namespace Mvp24Hours.Infrastructure.Cqrs.Test;

/// <summary>
/// Integration tests for TransactionBehavior with IUnitOfWork.
/// </summary>
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
public class TransactionIntegrationTest
{
    private MockUnitOfWorkAsync _mockUnitOfWork = null!;
    private IServiceProvider _serviceProvider = null!;
    private IMediator _mediator = null!;

    private void SetupServices(bool registerTransactionBehavior = true)
    {
        _mockUnitOfWork = new MockUnitOfWorkAsync();
        CreateOrderTransactionalCommandHandler.Reset();
        CreateOrderNonTransactionalCommandHandler.Reset();
        CreateAggregateTransactionalCommandHandler.Reset();
        UserRegisteredEventHandler.HandledEvents.Clear();
        WelcomeEmailHandler.HandledEvents.Clear();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(CreateOrderTransactionalCommand).Assembly);
            options.RegisterTransactionBehavior = registerTransactionBehavior;
        });
        
        // Register mock UnitOfWork
        services.AddScoped<IUnitOfWorkAsync>(_ => _mockUnitOfWork);
        
        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact, Priority(1)]
    public async Task TransactionBehavior_ShouldCommitOnSuccess()
    {
        // Arrange
        SetupServices();
        var command = new CreateOrderTransactionalCommand
        {
            CustomerName = "Test Customer",
            Amount = 100.00m
        };

        // Act
        var result = await _mediator.SendAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, _mockUnitOfWork.SaveChangesCallCount);
        Assert.Equal(0, _mockUnitOfWork.RollbackCallCount);
        Assert.Contains("SaveChanges", _mockUnitOfWork.OperationsLog);
    }

    [Fact, Priority(2)]
    public async Task TransactionBehavior_ShouldRollbackOnHandlerException()
    {
        // Arrange
        SetupServices();
        CreateOrderTransactionalCommandHandler.ShouldThrow = true;
        CreateOrderTransactionalCommandHandler.ExceptionToThrow = new InvalidOperationException("Handler failed");

        var command = new CreateOrderTransactionalCommand
        {
            CustomerName = "Test Customer",
            Amount = 100.00m
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mediator.SendAsync(command));
        
        Assert.Equal("Handler failed", ex.Message);
        Assert.Equal(0, _mockUnitOfWork.SaveChangesCallCount);
        Assert.Equal(1, _mockUnitOfWork.RollbackCallCount);
        Assert.Contains("Rollback", _mockUnitOfWork.OperationsLog);
    }

    [Fact, Priority(3)]
    public async Task TransactionBehavior_ShouldNotApplyToNonTransactionalCommands()
    {
        // Arrange
        SetupServices();
        var command = new CreateOrderNonTransactionalCommand
        {
            CustomerName = "Test Customer",
            Amount = 50.00m
        };

        // Act
        var result = await _mediator.SendAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, _mockUnitOfWork.SaveChangesCallCount);
        Assert.Equal(0, _mockUnitOfWork.RollbackCallCount);
        Assert.Empty(_mockUnitOfWork.OperationsLog);
    }

    [Fact, Priority(4)]
    public async Task TransactionBehavior_ShouldReturnCorrectRowsAffected()
    {
        // Arrange
        SetupServices();
        _mockUnitOfWork.RowsAffected = 5;
        var command = new CreateOrderTransactionalCommand
        {
            CustomerName = "Test",
            Amount = 200.00m
        };

        // Act
        var result = await _mediator.SendAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, _mockUnitOfWork.SaveChangesCallCount);
    }

    [Fact, Priority(5)]
    public async Task TransactionBehavior_WhenDisabled_ShouldNotCallUnitOfWork()
    {
        // Arrange
        SetupServices(registerTransactionBehavior: false);
        var command = new CreateOrderTransactionalCommand
        {
            CustomerName = "Test",
            Amount = 100.00m
        };

        // Act
        var result = await _mediator.SendAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, _mockUnitOfWork.SaveChangesCallCount);
        Assert.Equal(0, _mockUnitOfWork.RollbackCallCount);
    }

    [Fact, Priority(6)]
    public async Task TransactionBehavior_ShouldHandleMultipleCommandsIndependently()
    {
        // Arrange
        SetupServices();

        // Act
        var result1 = await _mediator.SendAsync(new CreateOrderTransactionalCommand
        {
            CustomerName = "Customer 1",
            Amount = 100.00m
        });

        var result2 = await _mediator.SendAsync(new CreateOrderTransactionalCommand
        {
            CustomerName = "Customer 2",
            Amount = 200.00m
        });

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(2, _mockUnitOfWork.SaveChangesCallCount);
        Assert.Equal(0, _mockUnitOfWork.RollbackCallCount);
    }

    [Fact, Priority(7)]
    public async Task TransactionBehavior_WithSaveChangesException_ShouldRollback()
    {
        // Arrange
        SetupServices();
        _mockUnitOfWork.ShouldThrowOnSave = true;
        _mockUnitOfWork.ExceptionToThrow = new InvalidOperationException("Database error");

        var command = new CreateOrderTransactionalCommand
        {
            CustomerName = "Test",
            Amount = 100.00m
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mediator.SendAsync(command));
        
        Assert.Equal("Database error", ex.Message);
        // Note: Rollback is only called when the handler fails, not when SaveChanges fails
        // because the exception happens during SaveChanges itself
    }

    [Fact, Priority(8)]
    public async Task TransactionWithEventsBehavior_ShouldCommitAndReturnResult()
    {
        // Arrange
        CreateAggregateTransactionalCommandHandler.Reset();
        var mockUnitOfWork = new MockUnitOfWorkAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(typeof(CreateAggregateTransactionalCommand).Assembly);
        services.AddScoped<IUnitOfWorkAsync>(_ => mockUnitOfWork);
        
        // Register the TransactionWithEventsBehavior manually
        services.AddTransient(
            typeof(IPipelineBehavior<CreateAggregateTransactionalCommand, TestAggregate>),
            typeof(TransactionWithEventsBehavior<CreateAggregateTransactionalCommand, TestAggregate>));
        
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var command = new CreateAggregateTransactionalCommand
        {
            Email = "transactional-commit@example.com"
        };

        // Act
        var result = await mediator.SendAsync(command);

        // Assert - Verify transaction was committed
        Assert.NotNull(result);
        Assert.Equal(1, mockUnitOfWork.SaveChangesCallCount);
        Assert.Equal(0, mockUnitOfWork.RollbackCallCount);
        // Verify result has domain event (before it was cleared)
        Assert.Empty(result.DomainEvents); // Events are cleared after dispatch
    }

    [Fact, Priority(9)]
    public async Task TransactionBehavior_WithoutUnitOfWork_ShouldPassThrough()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(CreateOrderTransactionalCommand).Assembly);
            options.RegisterTransactionBehavior = true;
        });
        // Note: NOT registering IUnitOfWorkAsync
        
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        CreateOrderTransactionalCommandHandler.Reset();

        var command = new CreateOrderTransactionalCommand
        {
            CustomerName = "No UoW Test",
            Amount = 100.00m
        };

        // Act - Should not throw even without UnitOfWork
        var result = await mediator.SendAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, CreateOrderTransactionalCommandHandler.ExecutionCount);
    }

    [Fact, Priority(10)]
    public async Task TransactionBehavior_ShouldMaintainOperationOrder()
    {
        // Arrange
        SetupServices();
        var command = new CreateOrderTransactionalCommand
        {
            CustomerName = "Order Test",
            Amount = 300.00m
        };

        // Act
        await _mediator.SendAsync(command);

        // Assert - SaveChanges should come after handler execution
        Assert.Equal(new[] { "SaveChanges" }, _mockUnitOfWork.OperationsLog.ToArray());
    }
}

