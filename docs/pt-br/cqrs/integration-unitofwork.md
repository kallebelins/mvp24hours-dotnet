# Integração com IUnitOfWork

## Visão Geral

O Mediator integra-se perfeitamente com o `IUnitOfWork` existente no Mvp24Hours para gerenciar transações de banco de dados de forma consistente.

## Interfaces Existentes

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

O `TransactionBehavior` encapsula comandos em transações automaticamente.

### Configuração

```csharp
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.RegisterTransactionBehavior = true;
});
```

### Interface ITransactional

Marque comandos que precisam de transação:

```csharp
public record CreateOrderCommand : IMediatorCommand<OrderDto>, ITransactional
{
    public string CustomerEmail { get; init; } = string.Empty;
    public List<OrderItemDto> Items { get; init; } = new();
}
```

### Como Funciona

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

## Uso em Handlers

### Handler com Repositório

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
        
        // SaveChanges é chamado pelo TransactionBehavior
        // mas também pode ser chamado explicitamente
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OrderDto { Id = order.Id };
    }
}
```

### Handler com Múltiplos Repositórios

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

        // Tudo na mesma transação
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
```

## Domain Events com UnitOfWork

### TransactionWithEventsBehavior

Dispara Domain Events após commit:

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
            
            // Coleta eventos antes de salvar
            var events = _context.ChangeTracker
                .Entries<IHasDomainEvents>()
                .SelectMany(e => e.Entity.DomainEvents)
                .ToList();
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            // Dispara eventos após commit
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

## Boas Práticas

1. **Use ITransactional**: Marque comandos que modificam dados
2. **Uma Transação por Command**: Evite transações aninhadas
3. **Handlers Simples**: Deixe o Behavior gerenciar transações
4. **Domain Events**: Dispare após commit bem-sucedido
5. **Rollback em Exceções**: O Behavior cuida disso automaticamente

