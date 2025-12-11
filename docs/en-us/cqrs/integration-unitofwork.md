# Integration with IUnitOfWork

## Overview

The Mediator seamlessly integrates with the existing `IUnitOfWork` in Mvp24Hours to manage database transactions consistently.

## Existing Interfaces

### IUnitOfWorkAsync

```csharp
public interface IUnitOfWorkAsync : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    IRepository<TEntity> GetRepository<TEntity>() where TEntity : class;
}
```

## TransactionBehavior

The `TransactionBehavior` automatically wraps commands in transactions.

### Configuration

```csharp
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.RegisterTransactionBehavior = true;
});
```

### ITransactional Interface

Mark commands that need a transaction:

```csharp
public record CreateOrderCommand : IMediatorCommand<OrderDto>, ITransactional
{
    public string CustomerEmail { get; init; } = string.Empty;
    public List<OrderItemDto> Items { get; init; } = new();
}
```

### How It Works

```csharp
public sealed class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITransactional
{
    private readonly IUnitOfWorkAsync _unitOfWork;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork
            .BeginTransactionAsync(cancellationToken);
        
        try
        {
            var result = await next();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

## Usage in Handlers

### Handler with Repository

```csharp
public class CreateOrderCommandHandler 
    : IMediatorCommandHandler<CreateOrderCommand, OrderDto>
{
    private readonly IUnitOfWorkAsync _unitOfWork;

    public CreateOrderCommandHandler(IUnitOfWorkAsync unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        var orderRepository = _unitOfWork.GetRepository<Order>();
        
        var order = new Order
        {
            CustomerEmail = request.CustomerEmail,
            Items = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        };

        await orderRepository.AddAsync(order);
        
        // SaveChanges is called by TransactionBehavior
        // but can also be called explicitly
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OrderDto { Id = order.Id };
    }
}
```

### Handler with Multiple Repositories

```csharp
public class TransferFundsCommandHandler 
    : IMediatorCommandHandler<TransferFundsCommand>
{
    private readonly IUnitOfWorkAsync _unitOfWork;

    public async Task<Unit> Handle(
        TransferFundsCommand request, 
        CancellationToken cancellationToken)
    {
        var accountRepository = _unitOfWork.GetRepository<Account>();
        var transactionRepository = _unitOfWork.GetRepository<Transaction>();

        var sourceAccount = await accountRepository.FirstOrDefaultAsync(
            a => a.Id == request.SourceAccountId);
        
        var targetAccount = await accountRepository.FirstOrDefaultAsync(
            a => a.Id == request.TargetAccountId);

        if (sourceAccount.Balance < request.Amount)
            throw new DomainException("Insufficient funds");

        sourceAccount.Balance -= request.Amount;
        targetAccount.Balance += request.Amount;

        await transactionRepository.AddAsync(new Transaction
        {
            SourceAccountId = request.SourceAccountId,
            TargetAccountId = request.TargetAccountId,
            Amount = request.Amount,
            Date = DateTime.UtcNow
        });

        // Everything in the same transaction
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
```

## Domain Events with UnitOfWork

### TransactionWithEventsBehavior

Dispatches Domain Events after commit:

```csharp
public sealed class TransactionWithEventsBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITransactional
{
    private readonly IUnitOfWorkAsync _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly DbContext _context;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork
            .BeginTransactionAsync(cancellationToken);
        
        try
        {
            var result = await next();
            
            // Collect events before saving
            var events = _context.ChangeTracker
                .Entries<IHasDomainEvents>()
                .SelectMany(e => e.Entity.DomainEvents)
                .ToList();
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            // Dispatch events after commit
            foreach (var @event in events)
            {
                await _dispatcher.DispatchAsync(@event, cancellationToken);
            }
            
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

## Best Practices

1. **Use ITransactional**: Mark commands that modify data
2. **One Transaction per Command**: Avoid nested transactions
3. **Simple Handlers**: Let the Behavior manage transactions
4. **Domain Events**: Dispatch after successful commit
5. **Rollback on Exceptions**: The Behavior handles this automatically

