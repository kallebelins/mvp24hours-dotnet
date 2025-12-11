# Commands

## Visão Geral

Commands representam intenções de modificação de estado no sistema. Seguindo o padrão CQRS, commands são usados para operações de escrita.

## Interfaces

### IMediatorCommand<TResponse>

Command que retorna um valor:

```csharp
public interface IMediatorCommand<out TResponse> : IMediatorRequest<TResponse>
{
}
```

### IMediatorCommand

Command sem retorno (void):

```csharp
public interface IMediatorCommand : IMediatorCommand<Unit>
{
}
```

> **Nota**: `Unit` é um struct que representa "void" para tipos genéricos.

## Criando Commands

### Command com Retorno

```csharp
public record CreateOrderCommand : IMediatorCommand<OrderDto>
{
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public List<OrderItemDto> Items { get; init; } = new();
}
```

### Command sem Retorno

```csharp
public record CancelOrderCommand : IMediatorCommand
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
```

### Command com Validação

```csharp
public record UpdateOrderCommand : IMediatorCommand<OrderDto>
{
    public Guid OrderId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
}

public class UpdateOrderCommandValidator : AbstractValidator<UpdateOrderCommand>
{
    public UpdateOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID is required");
            
        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .MaximumLength(100);
    }
}
```

## Criando Handlers

### Handler com Retorno

```csharp
public class CreateOrderCommandHandler 
    : IMediatorCommandHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWorkAsync _unitOfWork;
    private readonly IMapper _mapper;

    public CreateOrderCommandHandler(
        IOrderRepository repository,
        IUnitOfWorkAsync unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OrderDto> Handle(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        // Criar entidade
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending
        };

        // Adicionar itens
        foreach (var item in request.Items)
        {
            order.AddItem(item.ProductId, item.Quantity, item.UnitPrice);
        }

        // Persistir
        await _repository.AddAsync(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Retornar DTO
        return _mapper.Map<OrderDto>(order);
    }
}
```

### Handler sem Retorno

```csharp
public class CancelOrderCommandHandler 
    : IMediatorCommandHandler<CancelOrderCommand>
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWorkAsync _unitOfWork;

    public CancelOrderCommandHandler(
        IOrderRepository repository,
        IUnitOfWorkAsync unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(
        CancelOrderCommand request, 
        CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.OrderId);
        
        if (order is null)
            throw new NotFoundException("Order", request.OrderId);

        order.Cancel(request.Reason);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
```

## Commands Especiais

### Command Transacional

```csharp
public record TransferFundsCommand : IMediatorCommand, ITransactionalCommand
{
    public Guid FromAccountId { get; init; }
    public Guid ToAccountId { get; init; }
    public decimal Amount { get; init; }
}
```

### Command Idempotente

```csharp
public record ProcessPaymentCommand : IMediatorCommand<PaymentResult>, IIdempotentCommand
{
    public Guid PaymentId { get; init; }
    public decimal Amount { get; init; }
    
    // Chave baseada no ID do pagamento
    public string? IdempotencyKey => $"payment:{PaymentId}";
    public TimeSpan? IdempotencyDuration => TimeSpan.FromHours(24);
}
```

### Command com Autorização

```csharp
public record DeleteOrderCommand : IMediatorCommand, IAuthorizedRequest
{
    public Guid OrderId { get; init; }
    
    public IEnumerable<string> RequiredRoles => new[] { "Admin" };
    public IEnumerable<string> RequiredPermissions => new[] { "orders:delete" };
}
```

### Command com Retry

```csharp
public record SendEmailCommand : IMediatorCommand, IRetryableRequest
{
    public string To { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    
    public int MaxRetries => 3;
    public TimeSpan BaseDelay => TimeSpan.FromSeconds(1);
    
    public bool ShouldRetry(Exception ex) => 
        ex is HttpRequestException or TimeoutException;
}
```

## Enviando Commands

```csharp
public class OrderController : ControllerBase
{
    private readonly ISender _sender;

    public OrderController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(
        [FromBody] CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.SendAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Cancel(
        Guid id, 
        [FromBody] string reason,
        CancellationToken cancellationToken)
    {
        await _sender.SendAsync(new CancelOrderCommand 
        { 
            OrderId = id, 
            Reason = reason 
        }, cancellationToken);
        
        return NoContent();
    }
}
```

## Boas Práticas

1. **Imutabilidade**: Use `record` ou propriedades `init`
2. **Nomenclatura**: Use verbos no imperativo (Create, Update, Delete)
3. **Validação**: Use FluentValidation para validar inputs
4. **Single Responsibility**: Um command = uma operação
5. **Transações**: Use `ITransactionalCommand` para operações críticas
6. **Idempotência**: Use `IIdempotentCommand` para operações duplicáveis

