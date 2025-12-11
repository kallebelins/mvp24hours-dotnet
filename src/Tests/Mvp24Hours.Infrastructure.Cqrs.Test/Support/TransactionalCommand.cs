//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.Behaviors;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Support;

/// <summary>
/// Transactional command for testing transaction behavior.
/// </summary>
public class CreateOrderTransactionalCommand : IMediatorCommand<OrderResult>, ITransactional
{
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

/// <summary>
/// Handler for transactional order creation.
/// </summary>
public class CreateOrderTransactionalCommandHandler : IMediatorCommandHandler<CreateOrderTransactionalCommand, OrderResult>
{
    public static int ExecutionCount { get; set; }
    public static bool ShouldThrow { get; set; }
    public static Exception? ExceptionToThrow { get; set; }

    public Task<OrderResult> Handle(CreateOrderTransactionalCommand request, CancellationToken cancellationToken)
    {
        ExecutionCount++;

        if (ShouldThrow)
        {
            throw ExceptionToThrow ?? new InvalidOperationException("Handler error");
        }

        return Task.FromResult(new OrderResult
        {
            OrderId = ExecutionCount,
            CustomerName = request.CustomerName,
            Amount = request.Amount,
            Success = true
        });
    }

    public static void Reset()
    {
        ExecutionCount = 0;
        ShouldThrow = false;
        ExceptionToThrow = null;
    }
}

/// <summary>
/// Non-transactional command for comparison testing.
/// </summary>
public class CreateOrderNonTransactionalCommand : IMediatorCommand<OrderResult>
{
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

/// <summary>
/// Handler for non-transactional order creation.
/// </summary>
public class CreateOrderNonTransactionalCommandHandler : IMediatorCommandHandler<CreateOrderNonTransactionalCommand, OrderResult>
{
    public static int ExecutionCount { get; set; }

    public Task<OrderResult> Handle(CreateOrderNonTransactionalCommand request, CancellationToken cancellationToken)
    {
        ExecutionCount++;
        return Task.FromResult(new OrderResult
        {
            OrderId = ExecutionCount,
            CustomerName = request.CustomerName,
            Amount = request.Amount,
            Success = true
        });
    }

    public static void Reset()
    {
        ExecutionCount = 0;
    }
}

/// <summary>
/// Result for order creation commands.
/// </summary>
public class OrderResult
{
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool Success { get; set; }
}

/// <summary>
/// Transactional command that returns an entity with domain events.
/// </summary>
public class CreateAggregateTransactionalCommand : IMediatorCommand<TestAggregate>, ITransactional
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Handler that creates an aggregate with domain events.
/// </summary>
public class CreateAggregateTransactionalCommandHandler : IMediatorCommandHandler<CreateAggregateTransactionalCommand, TestAggregate>
{
    private static int _lastId;

    public Task<TestAggregate> Handle(CreateAggregateTransactionalCommand request, CancellationToken cancellationToken)
    {
        var aggregate = new TestAggregate { Id = ++_lastId };
        aggregate.Register(request.Email);
        return Task.FromResult(aggregate);
    }

    public static void Reset()
    {
        _lastId = 0;
    }
}

