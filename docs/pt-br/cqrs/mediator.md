# IMediator, ISender e IPublisher

## Visão Geral

O Mediator é composto por três interfaces principais que definem as capacidades de comunicação:

| Interface | Responsabilidade |
|-----------|------------------|
| `ISender` | Enviar requests (Commands/Queries) para handlers |
| `IPublisher` | Publicar notificações para múltiplos handlers |
| `IStreamSender` | Enviar requests que retornam streams |
| `IMediator` | Combina todas as interfaces acima |

## ISender

Interface para envio de requests que esperam uma resposta.

```csharp
public interface ISender
{
    Task<TResponse> SendAsync<TResponse>(
        IMediatorRequest<TResponse> request, 
        CancellationToken cancellationToken = default);
}
```

### Uso

```csharp
public class OrderController : ControllerBase
{
    private readonly ISender _sender;

    public OrderController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderCommand command)
    {
        // Envia command e aguarda resposta
        var result = await _sender.SendAsync(command);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id)
    {
        // Envia query e aguarda resposta
        var result = await _sender.SendAsync(new GetOrderByIdQuery { Id = id });
        return result is not null ? Ok(result) : NotFound();
    }
}
```

## IPublisher

Interface para publicação de notificações (eventos in-process).

```csharp
public interface IPublisher
{
    Task PublishAsync<TNotification>(
        TNotification notification, 
        CancellationToken cancellationToken = default)
        where TNotification : IMediatorNotification;
}
```

### Uso

```csharp
public class OrderService
{
    private readonly IPublisher _publisher;

    public OrderService(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task ProcessOrder(Order order)
    {
        // Processa o pedido...

        // Publica notificação para todos os handlers interessados
        await _publisher.PublishAsync(new OrderProcessedNotification
        {
            OrderId = order.Id,
            ProcessedAt = DateTime.UtcNow
        });
    }
}
```

### Estratégias de Publicação

O Publisher suporta diferentes estratégias:

| Estratégia | Descrição |
|------------|-----------|
| `Sequential` | Executa handlers um após o outro (padrão) |
| `ParallelNoWait` | Dispara todos em paralelo sem aguardar |
| `ParallelWhenAll` | Executa em paralelo e aguarda todos |

## IStreamSender

Interface para requests que retornam streams (`IAsyncEnumerable`).

```csharp
public interface IStreamSender
{
    IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request, 
        CancellationToken cancellationToken = default);
}
```

### Uso

```csharp
// Definir request de stream
public record GetOrdersStreamQuery : IStreamRequest<OrderDto>
{
    public DateTime FromDate { get; init; }
}

// Implementar handler
public class GetOrdersStreamQueryHandler 
    : IStreamRequestHandler<GetOrdersStreamQuery, OrderDto>
{
    public async IAsyncEnumerable<OrderDto> Handle(
        GetOrdersStreamQuery request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var order in _repository.GetOrdersAsync(request.FromDate))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
                
            yield return MapToDto(order);
        }
    }
}

// Usar no controller
[HttpGet("stream")]
public async IAsyncEnumerable<OrderDto> GetOrdersStream(
    DateTime fromDate,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    await foreach (var order in _mediator.CreateStream(
        new GetOrdersStreamQuery { FromDate = fromDate }, 
        cancellationToken))
    {
        yield return order;
    }
}
```

## IMediator

Interface principal que combina todas as capacidades:

```csharp
public interface IMediator : ISender, IPublisher, IStreamSender
{
}
```

### Quando usar qual interface?

| Cenário | Interface Recomendada |
|---------|----------------------|
| Apenas enviar Commands/Queries | `ISender` |
| Apenas publicar notificações | `IPublisher` |
| Apenas streams | `IStreamSender` |
| Múltiplas operações | `IMediator` |

### Exemplo com IMediator

```csharp
public class OrderApplicationService
{
    private readonly IMediator _mediator;

    public OrderApplicationService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<OrderDto> CreateAndNotify(CreateOrderCommand command)
    {
        // Envia command
        var order = await _mediator.SendAsync(command);

        // Publica notificação
        await _mediator.PublishAsync(new OrderCreatedNotification
        {
            OrderId = order.Id
        });

        return order;
    }
}
```

## Injeção de Dependência

Todas as interfaces são registradas automaticamente:

```csharp
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
});

// Registra:
// - IMediator -> Mediator (Scoped)
// - ISender -> IMediator (Scoped)
// - IPublisher -> IMediator (Scoped)
// - IStreamSender -> IMediator (Scoped)
```

## Boas Práticas

1. **Prefira interfaces específicas**: Use `ISender` ou `IPublisher` quando possível
2. **Evite dependência circular**: Não injete o Mediator em handlers
3. **Use CancellationToken**: Sempre propague o token de cancelamento
4. **Não misture responsabilidades**: Commands devem modificar, Queries devem ler

