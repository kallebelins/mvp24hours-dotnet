# Comparação de Conceitos

## ICommand (Repository) vs IMediatorCommand (CQRS)

O Mvp24Hours possui duas interfaces `ICommand` com propósitos diferentes. Este guia esclarece as diferenças.

## Repository Pattern - ICommand\<TEntity\>

### Localização

```csharp
namespace Mvp24Hours.Core.Contract.Data
{
    public interface ICommand<TEntity> where TEntity : class
    {
        void Add(TEntity entity);
        void Modify(TEntity entity);
        void Remove(TEntity entity);
    }
}
```

### Propósito

- **Operações CRUD** no banco de dados
- **Acesso a dados** genérico
- Parte do **Repository Pattern**
- Trabalha diretamente com **entidades**

### Uso

```csharp
public class OrderRepository : IRepository<Order>
{
    private readonly ICommand<Order> _command;
    
    public async Task AddAsync(Order order)
    {
        _command.Add(order);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

## Mediator Pattern - IMediatorCommand\<TResponse\>

### Localização

```csharp
namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions
{
    public interface IMediatorCommand<TResponse> : IMediatorRequest<TResponse>
    {
    }
    
    public interface IMediatorCommand : IMediatorCommand<Unit>
    {
    }
}
```

### Propósito

- **Casos de uso** de aplicação
- **Orquestração** de lógica de negócio
- Parte do **CQRS/Mediator Pattern**
- Trabalha com **DTOs e abstrações**

### Uso

```csharp
// Command
public record CreateOrderCommand : IMediatorCommand<OrderDto>
{
    public string CustomerEmail { get; init; }
    public List<OrderItemDto> Items { get; init; }
}

// Handler
public class CreateOrderCommandHandler 
    : IMediatorCommandHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, ...)
    {
        // Lógica de negócio + persistência via repository
    }
}
```

## Tabela Comparativa

| Aspecto | ICommand (Repository) | IMediatorCommand (CQRS) |
|---------|----------------------|-------------------------|
| **Namespace** | `Mvp24Hours.Core.Contract.Data` | `Mvp24Hours.Infrastructure.Cqrs.Abstractions` |
| **Camada** | Infraestrutura/Dados | Aplicação |
| **Responsabilidade** | CRUD no banco | Caso de uso |
| **Operações** | Add, Modify, Remove | Qualquer operação de negócio |
| **Parâmetro** | `TEntity` (entidade) | `TResponse` (DTO/resultado) |
| **Retorno** | void | TResponse ou Unit |
| **Pipeline** | Não | Sim (Behaviors) |
| **Validação** | Manual | Via ValidationBehavior |
| **Transação** | Via UnitOfWork | Via TransactionBehavior |
| **Logging** | Manual | Via LoggingBehavior |

## Fluxo de Integração

```
┌─────────────────────────────────────────────────────────────────┐
│                     Controller/API                               │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      IMediator                                   │
│                   (Mediator Pattern)                             │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                  Pipeline Behaviors                              │
│    [Logging] → [Validation] → [Transaction] → [Caching]        │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│            IMediatorCommandHandler / QueryHandler                │
│                    (Application Layer)                           │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   IRepository<TEntity>                           │
│               (usa ICommand<TEntity> internamente)               │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Database                                    │
└─────────────────────────────────────────────────────────────────┘
```

## Exemplo Prático

### Cenário: Criar Pedido

**Sem Mediator (usando Repository diretamente)**:
```csharp
[HttpPost]
public async Task<ActionResult> CreateOrder(CreateOrderRequest request)
{
    // Validação manual
    if (!ModelState.IsValid)
        return BadRequest(ModelState);
    
    // Lógica direta
    var order = new Order { ... };
    
    // Transação manual
    await _repository.AddAsync(order);
    await _unitOfWork.SaveChangesAsync();
    
    return Ok(new OrderDto { Id = order.Id });
}
```

**Com Mediator**:
```csharp
[HttpPost]
public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderRequest request)
{
    var command = new CreateOrderCommand
    {
        CustomerEmail = request.CustomerEmail,
        Items = request.Items
    };
    
    // Validação, transação, logging = automáticos via behaviors
    var result = await _mediator.SendAsync(command);
    return Ok(result);
}
```

## Quando Usar Cada Um

### Use ICommand (Repository) quando:

- ✅ Operações CRUD simples
- ✅ Acesso direto a dados sem lógica de negócio
- ✅ Dentro de handlers do Mediator
- ✅ Migrations e seeds de banco

### Use IMediatorCommand quando:

- ✅ Casos de uso de aplicação
- ✅ Precisa de validação automática
- ✅ Precisa de transações gerenciadas
- ✅ Precisa de logging/auditoria
- ✅ Precisa de cache automático
- ✅ Lógica de negócio complexa

## Conclusão

Ambos coexistem harmoniosamente:
- **IMediatorCommand** orquestra o caso de uso
- **ICommand/Repository** executa a persistência

O Mediator não substitui o Repository - ele o complementa adicionando uma camada de orquestração com cross-cutting concerns.

